import path from 'path';
import * as vscode from 'vscode';
import { RobotDiscovery } from './lib/RobotDiscovery';
import { spawn } from 'child_process';
import { SshHelper } from './lib/SshHelper';
import * as fs from 'fs/promises';
import * as fsSync from 'fs';
import * as tar from 'tar-stream';
import { SshPool } from './lib/SshPool';

export class StatisticalRobotTaskProvider implements vscode.TaskProvider<vscode.Task> {

    private discoveryService: RobotDiscovery;
    private sshPool: SshPool;

    constructor(discoveryService: RobotDiscovery, sshPool: SshPool) {
        this.discoveryService = discoveryService;
        this.sshPool = sshPool;
    }

    async provideTasks(token: vscode.CancellationToken): Promise<vscode.Task[]> {
        return await this.getRobotProjectTasks();
    }

    resolveTask(task: vscode.Task, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Task> {
        if(task.definition.projectFile === "") {
            return undefined;
        }

        let result = new vscode.Task(
            task.definition, // Return the original definition, or else vscode won't recognize the task
            vscode.TaskScope.Workspace, 
            task.name ?? 'Build and Deploy to Robot', 
            'statisticalrobot',
            new vscode.CustomExecution(async (definition) => {
                return new StatisticalRobotBuildTaskTerminal(
                    definition as StatisticalRobotTaskDefinition, 
                    this.discoveryService, 
                    this.sshPool
                );
            })
        );

        return result;
    }

    getRobotProjectTasks() {
        return vscode.workspace.findFiles('**/*.csproj')
            .then((csProjFileList) => {
                return csProjFileList.map(pf => {
                    let relativePath = vscode.workspace.asRelativePath(pf, false);

                    return this.createRobotTask(path.basename(relativePath) || path.basename(vscode.workspace.workspaceFolders![0].uri.fsPath), {
                        type: 'statisticalrobot',
                        projectFile: relativePath,
                        robotIpAddress: '${command:avans-statisticalrobot.connectedRobotIpAddress}',
                        robotOutputDir: '/media/csprojects',
                        projectName: '${workspaceFolderBasename}'
                    });
                });
            });
    }

    createRobotTask(projectName: string, taskDefinition: StatisticalRobotTaskDefinition): vscode.Task {
        return new vscode.Task(
            taskDefinition,
            vscode.TaskScope.Workspace,
            `Build and Deploy ${projectName}`,
            'statisticalrobot',
            new vscode.CustomExecution(async (task) => {
                return new StatisticalRobotBuildTaskTerminal(
                    task as StatisticalRobotTaskDefinition,  
                    this.discoveryService, 
                    this.sshPool
                );
            })
        );
    }
}

interface StatisticalRobotTaskDefinition extends vscode.TaskDefinition {
    projectName?: string;
    projectFile: string;
    robotIpAddress: string;
    robotOutputDir: string;
}

class StatisticalRobotBuildTaskTerminal implements vscode.Pseudoterminal {
    private writeEmitter = new vscode.EventEmitter<string>();
    onDidWrite: vscode.Event<string> = this.writeEmitter.event;

    private closeEmitter = new vscode.EventEmitter<number>();
    onDidClose?: vscode.Event<number> = this.closeEmitter.event;

    private _task: StatisticalRobotTaskDefinition;
    private _discoveryService: RobotDiscovery;
    private _sshPool: SshPool;

    constructor(task: StatisticalRobotTaskDefinition, discoveryService: RobotDiscovery, sshPool: SshPool) {
        this._task = task;
        this._discoveryService = discoveryService;
        this._sshPool = sshPool;
    }

    open(initialDimensions: vscode.TerminalDimensions | undefined): void {
        this.run();
    }

    close(): void {
    }

    private async run() {
        let resultCode = -1;

        let rompi: SshHelper | undefined = undefined;

        try {
            let startTime = Date.now();

            let robotHost = await this.getPiHost();
            if(!robotHost) {
                this.echo("Could not resolve robot ip-address. Please connect to a robot first or specify a robotIpAddress in tasks.json!");
                return;
            }

            let buildTime = Date.now();

            this.echo(`Building project ${this._task.projectName}`);
            if(!await this.buildProject()) {
                this.echo('Build of project failed!');
                return;
            }

            buildTime = Date.now() - buildTime;

            this.echo("");

            this.writeEmitter.fire(`Connecting to Robot @ ${robotHost}...`);

            try {
                rompi = await this.getSshConnection(robotHost);
            }
            catch(err) {
                this.echo("Failed");
                this.echo("Connection with Robot failed, Try again! Reboot your robot if this problem persists.");
                throw err;
            }

            this.echo("Connected");

            this.echo("");
            this.echo("Preparing for upload...");

            let projectName = this._task.projectName || path.basename(this._task.projectFile, path.extname(this._task.projectFile));
            let outputDir = `${this._task.robotOutputDir}/${projectName}`;

            let projectFileName = path.basename(this._task.projectFile, path.extname(this._task.projectFile));

            if(!await this.preparePi(rompi, projectFileName, outputDir)) {
                this.echo("Failed preparing for upload! Please restart your robot if this problem persists.");
                return;
            }

            this.echo(`Uploading project ${projectName} to ${outputDir} @ ${robotHost}`);

            let uploadTime = Date.now();

            let projectPath = path.join(vscode.workspace.workspaceFolders![0].uri.fsPath, this._task.projectFile);

            let inputDir = path.join(path.dirname(projectPath), 'bin', 'Debug', 'robot');
            if(!await this.uploadFilesToPi(rompi, inputDir, outputDir)) {
                this.echo("Uploading program to robot failed! Please restart your robot if this problem persists.");
                return;
            }

            uploadTime = Date.now() - uploadTime;

            this.echo("");
            this.echo("Done!");
            this.echo(`(Build time: ${buildTime}ms, Upload time: ${uploadTime}ms, Total time: ${Date.now() - startTime}ms)`);
            this.echo("");

            resultCode = 0; // Success!
        }
        catch(err) {
            console.error(err);
            resultCode = -1;

            this.echo("");
            this.echo("ERROR");
            this.echo("Build and Upload failed!");
        }
        finally {
            // rompi?.close();
            this.closeEmitter.fire(resultCode);
        }
    }

    private async getPiHost(): Promise<string | undefined> {
        if(this._task.robotIpAddress !== "<connected_robot>" && this._task.robotIpAddress) {
            return this._task.robotIpAddress;
        }

        let activeConnectedRobotId = vscode.workspace.getConfiguration()
			.get<string>('avans-statisticalrobot.connected-robot');

		if(!activeConnectedRobotId) {
			this.echo("No robot connected, please connect to a robot first!");
            return undefined;
		}

        let robotInfo = this._discoveryService.getRobotById(activeConnectedRobotId);
        if(!robotInfo) {
            this.echo(`Discovering Robot ${activeConnectedRobotId}...`);

            this._discoveryService.discover();
            await this.delay(500);

            robotInfo = this._discoveryService.getRobotById(activeConnectedRobotId);
            if(!robotInfo) {
                return undefined;
            }
        }

        return robotInfo.address;
    }

    private buildProject(): Promise<boolean> {
        return new Promise<boolean>((resolve, reject) => {
            let projectPath = path.join(vscode.workspace.workspaceFolders![0].uri.fsPath, this._task.projectFile);

            // Run command: dotnet build RobotProject.csproj --runtime linux-arm64 --nologo --no-self-contained --output bin/Debug/robot
            let dotnet = spawn('dotnet', [
                    'build', projectPath, 
                    '--runtime', 'linux-arm64',
                    '--nologo',
                    '--no-self-contained',
                    '--output', 'bin/Debug/robot'
                ], {
                    cwd: path.dirname(projectPath)
                }
            );
        
            dotnet.stdout.on('data', (data) => {
                this.writeEmitter.fire(data.toString());
            });

            dotnet.stderr.on('data', (data) => {
                this.writeEmitter.fire(data.toString());
            });

            dotnet.on('close', (code) => {
                resolve(code === 0);
            });
        });
    }

    private async preparePi(rompi: SshHelper, projectFileName: string, outputDir: string) {

        // Kill all running instances of the project before uploading
        let killResult = await rompi.exec(
            `for pid in $(ps -ef | grep "${projectFileName}.dll" | grep -v "grep" | awk '{print $2}'); do `
                // Kill the running instance
                + 'sudo kill -9 $pid > /dev/null 2> /dev/null || :; '
            + 'done;');

        if(killResult.exitcode !== 0) {
            return false;
        }

        if(!await rompi.rmdir(outputDir)) {
            return false;
        }

        if(!await rompi.mkdir(outputDir)) {
            return false;
        }

        return true;
    }

    private async uploadFilesToPi(rompi: SshHelper, inputDir: string, outputDir: string): Promise<boolean> {
        let filesToUpload = (await fs.readdir(inputDir, { recursive: true, withFileTypes: true }))
            .filter(d => d.isFile());

        try {
            // Set highWaterMark to 256Mb (tar can be max 256Mb in RAM)
            let tarStream = tar.pack({ highWaterMark: 1_024_000 * 256 });

            // Adding all files to a tar file in RAM, which makes uploading faster
            // (Uploading a single file is quicker than uploading multiple files)
            for (const file of filesToUpload) {
                // file.path is deprecated, but the replacing file.parentPath is empty in vscode
                // That's why we're still using file.path as backup
                // When vscode updates to newer node version, file.parentPath will automatically take precedence
                let filePath = path.join(file.parentPath ?? file.path, file.name);
                let relativePath = filePath.substring(inputDir.length + 1)
                    .replaceAll('\\', '/');

                if(!await this.addFileToTar(tarStream, filePath, relativePath)) {
                    tarStream.destroy();
                    break;
                }
            }

            if(tarStream.destroyed) {
                // An error occured whilst adding files to tar
                return false;
            }

            // Finish the tar file to upload it
            tarStream.finalize();

            // Using a trick to speed up the upload process:
            // We're executing the tar-command on the pi to extract the tar to the output directory
            // But instead of uploading the file, we instruct tar to read the file from stdin (the console input)
            // Than, using the created ssh-stream, we can upload the tar file into stdin of tar
            // This way, we upload the files and immediately extract it simultaneously
            await new Promise((res, rej) => {
                rompi.ssh.exec(`tar -xf - -C "${outputDir}"`, (err, stream) => {
                    if(err) {
                        rej(err);
                    }

                    stream.on("error", (err: any) => {
                        tarStream.destroy(err);
                        rej(err);
                    }).on("finish", () => {
                        res(true);
                    });

                    // Upload the tarfile to stdin of tar (extraction mode)
                    tarStream.pipe(stream.stdin, { end: true });
                });
            });

            tarStream.destroy();

            return true;
        }
        catch (err) {
            console.error(err);
            return false;
        }
    }

    private async getSshConnection(robotHost: string): Promise<SshHelper> {
        let result = this._sshPool.get(robotHost);

        if(!result || !result.isConnected()) {
            result = new SshHelper(robotHost);
            await result.connect();

            this._sshPool.set(robotHost, result);
        }
        else {
            // Will result in console as "Already Connected"
            this.writeEmitter.fire("Already ");
        }

        return result;
    }

    private echo(msg: string) {
        this.writeEmitter.fire(msg + '\r\n');
    }

    private delay(ms: number): Promise<void> {
        return new Promise<void>((resolve, _) => setTimeout(resolve, ms));
    }

    private async addFileToTar(tarStream: tar.Pack, filePath: string, relativePath: string): Promise<boolean> {
        let fileSize = (await fs.stat(filePath)).size;

        // Warning: We have to use a file buffer and the fs.readFile() funtion.
        // using fsSync.openReadStream() and than using the pipe-function into the tarStream DOESN'T work
        // Using pipe messes up the binary formatting, causing tarStream to interpret the file as UTF-8 text.
        // This breaks any binary file (like the compiled C# code)
        let fileBuffer = await fs.readFile(filePath);
        tarStream.entry({ name: relativePath, mode: 0o775 }, fileBuffer);

        return true;
    }

}