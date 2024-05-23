
import * as os from 'os';
import dgram from 'dgram';
import { Socket, RemoteInfo } from 'dgram';

export interface RobotInfo {
    get address(): string;
    get id(): string;
    get lastSeen(): Date;

    get simpleId(): string;
}

class RobotInfoImpl implements RobotInfo {
    
    public _address: string;
    public _id: string;
    public _lastSeen: Date;

    constructor(address: string, id: string) {
        this._address = address;
        this._id = id;
        this._lastSeen = new Date();
    }
    get address(): string {
        return this._address;
    }
    get id(): string {
        return this._id;
    }
    get lastSeen(): Date {
        return this._lastSeen;
    }

    get simpleId(): string {
        return this._id.substring(1).replace(/^0+/, '');
    }
}

export class RobotDiscovery {

    private udpClient: Socket|null = null;
    private discoveredRobots: NodeJS.Dict<RobotInfoImpl> = {};
    private scanInterval: NodeJS.Timeout|undefined;

    constructor(private readonly changeCallback: () => void ) {
        
    }

    start() {
        if(this.udpClient !== null) {
            return;
        }

        this.udpClient = dgram.createSocket("udp4");
        this.discoveredRobots = {};

        this.udpClient.on('error', (error) => this.handleError(error));
        this.udpClient.on('message', (buffer, rInfo) => this.handleMessage(buffer, rInfo));
        this.udpClient.bind(0, () => this.handleBind());

        this.broadcastDiscoverMessage();
        this.scanInterval = setInterval(() => this.handleScan(), 10 * 1000); // Scan every 10-seconds
    }

    discover() {
        this.scanInterval?.refresh(); // Reset the interval
        this.broadcastDiscoverMessage();
    }

    getDiscoveredRobots() : (RobotInfo | undefined)[] {
        return Object.values(this.discoveredRobots);
    }

    refresh() {
        console.log("Refresh all robots");

        this.discoveredRobots = {};
        this.changeCallback();
        this.discover();
    }

    close() {
        this.udpClient?.close();

        clearInterval(this.scanInterval);
        this.scanInterval = undefined;
    }

    dispose() {
        this.close();
    }

    private handleScan() {
        this.broadcastDiscoverMessage();
        this.purgeOldDiscoveries();
    }

    private broadcastDiscoverMessage() {
        if(this.udpClient === null) {
            throw new Error("The udp client was not setup. Please call start() first!");
        }
            
        let buffer = Buffer.from("STATROBOT_V0.0_DISCOV", "ascii");
        for(let broadcastAddress of getAllBroadcastAddresses()) {
            this.udpClient.send(buffer, 9999, broadcastAddress);
        }
    }

    private purgeOldDiscoveries() {
        const TIMEOUT = 30 * 1000; // 30 seconds

        let idListToPurge: string[] = [];
        let now = Date.now();
         
        for(let id in this.discoveredRobots) {
            let robotInfo = this.discoveredRobots[id]!;
            
            if(now - robotInfo.lastSeen.getTime() > TIMEOUT) {
                idListToPurge.push(id);
            }
        }

        for(let idToPurge of idListToPurge) {
            console.log(`${idToPurge}: Purge, device cannot be found on the network anymore`);
            delete this.discoveredRobots[idToPurge];
        }

        if(idListToPurge.length > 0) {
            this.changeCallback();
        }
    }

    private handleBind() {
        this.udpClient?.setBroadcast(true);
    }

    private handleMessage(msgBuffer: Buffer, rInfo: RemoteInfo): void {
        let msg = msgBuffer.toString('ascii');

        if(msg.startsWith('STATROBOT_V0.0_ACK')) {
            let id = msg.substring(msg.lastIndexOf('_') + 1);
            
            if(this.discoveredRobots[id] !== undefined) {
                let robotInfo = this.discoveredRobots[id]!;

                if(robotInfo._address !== rInfo.address) {
                    console.log(`${id}: Address changed from ${robotInfo._address} to ${rInfo.address}`);
                    robotInfo._address = rInfo.address;

                    this.changeCallback();
                }

                robotInfo._lastSeen = new Date();
            }
            else {
                this.discoveredRobots[id] = new RobotInfoImpl(rInfo.address, id);

                console.log(`Discovered ${id} (${this.discoveredRobots[id]!.simpleId})`);
                this.changeCallback();
            }
        }
    }

    private handleError(error: Error) {
        console.error(error);
    }
}

function * getAllBroadcastAddresses() : Generator<string> {
	let ifaceDict = os.networkInterfaces();
	for(let ifaceName in ifaceDict) {
		let iface = ifaceDict[ifaceName];
		if(iface === undefined) {
			continue;
		}

		for(let conf of iface) {
			if(conf.family !== "IPv4") {
				continue;
			}

			let ipBytes = conf.address.split('.')
				.map((val) => Number(val));
				
			let maskBytes = conf.netmask.split('.')
				.map((val) => Number(val));

			let ipLength = Math.min(ipBytes.length, maskBytes.length);
			let broadcastAddress = '';
			for(let i = 0; i < ipLength; i++)
			{
				broadcastAddress += ((ipBytes[i] | ~maskBytes[i]) & 0xFF) + '.';
			}

			yield broadcastAddress.substring(0, broadcastAddress.length - 1);
		}
	}
}