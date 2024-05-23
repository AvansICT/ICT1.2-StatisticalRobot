
import * as vscode from 'vscode';
import * as os from 'os';
import dgram from 'dgram';
import { Socket, RemoteInfo } from 'dgram';

let udpClient: Socket|null = null;

export function activate(context: vscode.ExtensionContext) {
	console.log('Congratulations, your extension "avans-statisticalrobot" is now active!');

	udpClient = dgram.createSocket("udp4");
	udpClient.on('message', processMessage);
	udpClient.on('error', (err) => {
		console.error(err);
	});

	udpClient.bind(0, () => {
		udpClient!.setBroadcast(true);
	});
	
	let disposable = vscode.commands.registerCommand('avans-statisticalrobot.helloWorld', async () => {
		for(let broadcastAddress of getAllBroadcastAddresses()) {
			udpClient!.send(Buffer.from("STATROBOT_V0.0_DISCOV", "ascii"), 9999, broadcastAddress);
		}
	});

	context.subscriptions.push(disposable);
}

export function deactivate() {
	udpClient?.close();
}

function processMessage(msg: Buffer, rinfo: RemoteInfo) {
	console.log(`server got: '${msg}' from ${rinfo.address}:${rinfo.port}`);
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