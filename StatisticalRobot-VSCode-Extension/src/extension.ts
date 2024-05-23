
import * as vscode from 'vscode';
import { RobotDiscovery } from './RobotDiscovery';

const discoveryService = new RobotDiscovery();

export function activate(context: vscode.ExtensionContext) {
	console.log('Avans Statistical Robot Extension Active');

	discoveryService.start();
	
	let disposable = vscode.commands.registerCommand('avans-statisticalrobot.helloWorld', async () => {
		discoveryService.discover();
	});

	context.subscriptions.push(disposable);
}

export function deactivate() {
	discoveryService.close();
}

