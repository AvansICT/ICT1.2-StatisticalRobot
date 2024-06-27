import path from 'path';
import * as vscode from 'vscode';
import { RobotDiscovery } from './lib/RobotDiscovery';
import { spawn } from 'child_process';
import { SshHelper } from './lib/SshHelper';
import * as fs from 'fs/promises';
import * as fsSync from 'fs';
import * as tar from 'tar-stream';

export class StatisticalRobotTaskProvider implements vscode.TaskProvider<vscode.Task> {

    private discoveryService: RobotDiscovery;

    constructor(discoveryService: RobotDiscovery) {
        this.discoveryService = discoveryService;
    }

    async provideTasks(token: vscode.CancellationToken): Promise<vscode.Task[]> {
        return await this.getRobotProjectTasks();
    }

    resolveTask(task: vscode.Task, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Task> {
        if(task.definition.projectName === "") {
            return undefined;
        }

        let result = new vscode.Task(
            task.definition, // Return the original definition, or else vscode won't recognize the task
            vscode.TaskScope.Workspace, 
            task.name ?? 'Build and Deploy to Robot', 
            'statisticalrobot',
            new vscode.CustomExecution(async (definition) => {
                return new StatisticalRobotBuildTaskTerminal(definition as StatisticalRobotTaskDefinition, this.discoveryService);
            })
        );

        return result;
    }

    getRobotProjectTasks() {
        return vscode.workspace.findFiles('**/*.csproj')
            .then((csProjFileList) => {
                return csProjFileList.map(pf => {
                    let relativePath = vscode.workspace.asRelativePath(pf, false);

                    return this.createRobotTask(path.basename(relativePath), {
                        type: 'statisticalrobot',
                        projectName: relativePath,
                        robotIpAddress: '${command:avans-statisticalrobot.connectedRobotIpAddress}',
                        robotOutputDir: '/mnt/csprojects'
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
                return new StatisticalRobotBuildTaskTerminal(task as StatisticalRobotTaskDefinition,  this.discoveryService);
            })
        );
    }
}

interface StatisticalRobotTaskDefinition extends vscode.TaskDefinition {
    projectName: string;
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

    constructor(task: StatisticalRobotTaskDefinition, discoveryService: RobotDiscovery) {
        this._task = task;
        this._discoveryService = discoveryService;
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
            if(!await this.testRobotConnection(robotHost)) {
                this.echo("Failed");
                this.echo("Connection with Robot failed! Try rebooting your robot if this problem persists!");
                return;
            }

            try {
                rompi = new SshHelper(robotHost);
                await rompi.connect();
            }
            catch(err) {
                this.echo("Failed");
                this.echo("Connection with Robot failed, Try again! Reboot your robot if this problem persists.");
                throw err;
            }

            this.echo("Connected");

            this.echo("");
            this.echo("Preparing for upload...");

            let projectName = path.basename(this._task.projectName, path.extname(this._task.projectName));
            let outputDir = `${this._task.robotOutputDir}/${projectName}`;

            if(!await this.preparePi(rompi, projectName, outputDir)) {
                this.echo("Failed preparing for upload! Please restart your robot if this problem persists.");
                return;
            }

            this.echo(`Uploading project ${projectName} to ${outputDir} @ ${robotHost}`);

            let uploadTime = Date.now();

            let inputDir = path.join(vscode.workspace.workspaceFolders![0].uri.fsPath, 'bin', 'Debug', 'robot');
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
            rompi?.close();
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
            // Run command: dotnet build RobotProject.csproj --runtime linux-arm64 --nologo --no-self-contained --output bin/Debug/robot
            let dotnet = spawn('dotnet', [
                    'build', this._task.projectName, 
                    '--runtime', 'linux-arm64',
                    '--nologo',
                    '--no-self-contained',
                    '--output', 'bin/Debug/robot'
                ], {
                    cwd: vscode.workspace.workspaceFolders![0].uri.fsPath
                }
            );

            const trimPath = vscode.workspace.workspaceFolders![0].uri.fsPath + path.delimiter;
        
            dotnet.stdout.on('data', (data) => {
                this.writeEmitter.fire(data.toString().replaceAll(trimPath, ''));
            });

            dotnet.stderr.on('data', (data) => {
                this.writeEmitter.fire(data.toString().replaceAll(trimPath, ''));
            });

            dotnet.on('close', (code) => {
                resolve(code === 0);
            });
        });
    }

    private async preparePi(rompi: SshHelper, projectName: string, outputDir: string) {

        let killResult = await rompi.exec(
            `for pid in $(ps -ef | grep "${projectName}.dll" | grep -v "grep" | awk '{print $2}'); do `
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
                return false;
            }

            tarStream.finalize();

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
            return false;
        }
    }

    private async testRobotConnection(robotHost: string): Promise<boolean> {
        try {
            let response = await fetch(`http://${robotHost}:5000/test/ping`, {
                method: 'GET',
                signal: AbortSignal.timeout(500)
            });

            return response.ok && await response.text() === "pong!";
        }
        catch {
            return false;
        }
    }

    private echo(msg: string) {
        this.writeEmitter.fire(msg + '\r\n');
    }

    private delay(ms: number): Promise<void> {
        return new Promise<void>((resolve, _) => setTimeout(resolve, ms));
    }

    private async addFileToTar(tarStream: tar.Pack, filePath: string, relativePath: string): Promise<boolean> {
        let fileSize = (await fs.stat(filePath)).size;

        return await new Promise((resolve, reject) => {
            let writer = tarStream.entry({ name: relativePath, size: fileSize, mode: 0o775 }, (err) => {
                if(err) { 
                    reject(err);
                    return;
                }

                resolve(true);
            });

            // Force ASCII encoding, or else the writer won't understand the encoding
            let reader = fsSync.createReadStream(filePath, { encoding: "ascii" });
            reader.pipe(writer);
        });
    }

}