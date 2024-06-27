import path, { resolve } from 'path';
import { Interface } from 'readline';
import * as ssh from 'ssh2';

export class SshHelper {

    private host: string;
    private _ssh: ssh.Client | undefined;
    private _sftp: ssh.SFTPWrapper | undefined;
    private _isConnected: boolean = false;

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
        if(this.isConnected()) {
            return Promise.resolve();
        }

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

                this._isConnected = true;
            });

            this._ssh.once('error', (err) => {
                this._ssh?.end();
                this._ssh = undefined;

                this._isConnected = false;

                rej(err);
            });

            this._ssh.on("end", () => {
                this._isConnected = false;

                this._ssh = undefined;
                this._sftp = undefined;
            });

            this._ssh.connect({
                host: this.host,
                port: 22,
                username: 'rompi',
                password: undefined,
                timeout: 5000,
                readyTimeout: 5000
            });
        });
    }

    isConnected(): boolean {
        return this._ssh !== undefined && this._sftp !== undefined && this._isConnected;
    }

    close(): void {
        this._sftp?.end();
        this._ssh?.end();
    }

    async mkdir(pathStr: string): Promise<boolean> {
        if(pathStr.includes('"')) {
            throw new Error("Illegal path character!");
        }

        let result = await this.exec(`mkdir -p "${pathStr}"`);
        return result.exitcode === 0;
    }

    async rmdir(pathStr: string): Promise<boolean> {
        if(pathStr.includes('"')) {
            throw new Error("Illegal path character!");
        }

        let result = await this.exec(`rm -rf "${pathStr}"`);
        return result.exitcode === 0;
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
        if(!this.isConnected()) {
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