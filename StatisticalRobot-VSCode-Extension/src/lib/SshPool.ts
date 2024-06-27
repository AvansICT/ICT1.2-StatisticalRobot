import { SshHelper } from "./SshHelper";

export class SshPool {

    private connectionById: NodeJS.Dict<SshHelper> = {};

    get(id: string): SshHelper | undefined {
        return this.connectionById[id];
    }

    set(id: string, ssh: SshHelper) {
        let existingItem = this.connectionById[id];
        if(existingItem) {
            existingItem.close();
        }

        if(ssh.isConnected()) {
            console.log(`Adding ${id} to ssh-pool`);
            
            // Remove the connection from the pool when the socket disconnects
            ssh.ssh.on("end", () => this._handleDisconnect(id))
                .on("close", () => this._handleDisconnect(id))
                .on("error", () => this._handleDisconnect(id));

            this.connectionById[id] = ssh;
        }
    }

    dispose() {
        for(const id in this.connectionById) {
            this.connectionById[id]?.close();
            delete this.connectionById[id];
        }
    }

    _handleDisconnect(id:  string) {
        if(id in this.connectionById) {
            delete this.connectionById[id];
            console.log(`Removing ${id} from ssh-pool`);
        }
    }

}