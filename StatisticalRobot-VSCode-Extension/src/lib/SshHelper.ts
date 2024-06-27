import path, { resolve } from 'path';
import { Interface } from 'readline';
import * as ssh from 'ssh2';

export class SshHelper {

    private host: string;
    private _ssh: ssh.Client | undefined;
    private _sftp: ssh.SFTPWrapper | undefined;

    constructor(host: string) {
        this.host = host;
    }

    public get ssh(): ssh.Client {
        this.checkConnected();
        return this._ssh!;
    }

    public get sftp(): ssh.SFTPWrapper {
        this.checkConnected();
        return this._sftp!;
    }

    connect(): Promise<void> {
        return this.promisify((res, rej) => {
            this._ssh = new ssh.Client();

            this._ssh.once('ready', () => {
                this._ssh!.sftp((err, sftp) => {
                    if(err) {
                        rej(err);
                        return;
                    }

                    this._sftp = sftp;

                    res(true);
                });
            });

            this._ssh.once('error', (err) => {
                this._ssh?.end();
                this._ssh = undefined;

                rej(err);
            });

            this._ssh.connect({
                host: this.host,
                port: 22,
                username: 'rompi',
                password: undefined,

            });
        });
    }

    close(): void {
        this._sftp?.end();
        this._ssh?.end();
    }

    // exists(pathStr: string): Promise<boolean> {
    //     this.checkConnected();

    //     return this.promisify((res, rej) => {
    //         let containingDir = path.dirname(pathStr);
    //         let filename = path.basename(pathStr);

    //         this._sftp!.readdir(containingDir, (err, list) => {
    //             if(err) {
    //                 rej(err);
    //                 return;
    //             }

    //             res(list.some(fileStats => fileStats.filename === filename));
    //         });
    //     });
    // }

    async mkdir(pathStr: string): Promise<boolean> {
        if(pathStr.includes('"')) {
            throw new Error("Illegal path character!");
        }

        let result = await this.exec(`mkdir -p "${pathStr}"`);
        return result.exitcode === 0;
    }

    // async mkdirs(pathList: string[]): Promise<boolean> {
    //     let dirsToCreate = pathList.map(p => {
    //         if(p.includes('"')) {
    //             throw new Error("Illegal path character!");
    //         }
            
    //         return '"' + p + '"';
    //     }).join(' ');

    //     let result = await this.exec(`mkdir -p ${dirsToCreate}`);
    //     return result.exitcode === 0;
    // }

    async rmdir(pathStr: string): Promise<boolean> {
        if(pathStr.includes('"')) {
            throw new Error("Illegal path character!");
        }

        let result = await this.exec(`rm -rf "${pathStr}"`);
        return result.exitcode === 0;
    }

    upload(localPath: string, remotePath: string): Promise<boolean> {
        this.checkConnected();

        return this.promisify((res, rej) => {
            this._sftp!.fastPut(localPath, remotePath, { mode: 0o775 }, (err) => {
                if(err) {
                    rej(err);
                    return;
                }

                res(true);
            });
        });
    }

    exec(cmd: string): Promise<SshExecResult> {
        this.checkConnected();

        return this.promisify((res, rej) => {
            this._ssh!.exec(
                cmd,
                (err, stream) => {
                    if(err) {
                        rej(err);
                        return;
                    }
        
                    let stdout = '';
                    // stream.on('data', (data: any) => {
                    //     stdout += data.toString();
                    // });

                    let stderr = '';
                    // stream.stderr.on('data', (data: any) => {
                    //     stderr += data.toString();
                    // });

                    let isResolved = false;
        
                    stream.on('exit', (code: number, signal: undefined) => {
                        if(isResolved) {
                            return;
                        }

                        isResolved = true;
                        res({
                            exitcode: code ?? 0,
                            signal,
                            stdout,
                            stderr
                        });
                    }).on('finish', () => {
                        // Fallback, in case of exit is never fired
                        if(isResolved) {
                            return;
                        }
                        
                        isResolved = true;
                        res({ exitcode: -1 });
                    });
                }
            );
        });
    }

    private checkConnected() {
        if(!this._ssh || !this._sftp) {
            throw new Error("ssh/sftp not connected. Please call connect function first");
        }
    }

    private promisify(cb: (res: (value: any) => void, rej: (reason?: any) => void) => void): Promise<any> {
        return new Promise((res, rej) => {
            try {
                cb(res, rej);
            }
            catch(err) {
                rej(err);
            }
        });
    }

}

interface SshExecResult {
    exitcode: number;
    signal: string | undefined,
    stdout: string,
    stderr: string
}