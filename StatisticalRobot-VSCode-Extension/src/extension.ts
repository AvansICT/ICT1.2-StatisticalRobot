
import * as vscode from 'vscode';
import { RobotDiscovery, RobotInfo } from './lib/RobotDiscovery';
import { RobotListProvider } from './RobotListProvider';

export function activate(context: vscode.ExtensionContext) {
	console.log('Avans Statistical Robot Extension Active');

	// TODO: Make cmd connectToRobot and changeRobotSettings functional
	// connectToRobot sets the selected robot as connected (build and launch ip are set, selected robot-id is saved in project)
	// changeRobotSettings opens a webview with robot settings (wifi + power)

	// TODO: Research if you can have hidden commands (unreachable from cmd-palette) for UI-only commands, like connect and settings

	let disposables: vscode.Disposable[] = [];

	let robotListProvider: RobotListProvider;

	const discoveryService = new RobotDiscovery(() => {
		robotListProvider.refresh();
	});
	disposables.push(discoveryService);

	robotListProvider = new RobotListProvider(discoveryService);

	discoveryService.start();
	
	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.discoverRobots', () => {
		discoveryService.discover();
		robotListProvider.refresh();
	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.refreshRobotList', () => {
		discoveryService.discover();
		robotListProvider.refresh();
	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.refreshAllRobots', () => {
		discoveryService.refresh();
	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.connectToRobot', (treeItem) => {
		if(treeItem === undefined) {
			return;
		}

		let robotInfo: RobotInfo = treeItem.robotInfo;
		if(robotInfo === undefined) {
			return;
		}

		let activeConnectedRobotId = vscode.workspace.getConfiguration()
			.get<string>('avans-statisticalrobot.connected-robot');

		if(activeConnectedRobotId === robotInfo.id) {
			vscode.window.showInformationMessage(`You're already connected to ${robotInfo}`);
			return;
		}

		vscode.workspace.getConfiguration()
			.update('avans-statisticalrobot.connected-robot', robotInfo.id, null)
			.then(() => vscode.window.showInformationMessage(`${robotInfo} as been set as the connected robot!`));
	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.changeRobotSettings', (treeItem) => {
		if(treeItem === undefined) {
			return;
		}

		let robotInfo: RobotInfo = treeItem.robotInfo;
		if(robotInfo === undefined) {
			return;
		}

		// TODO
	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.connectedRobotIpAddress', () => {
		let activeConnectedRobotId = vscode.workspace.getConfiguration()
			.get<string>('avans-statisticalrobot.connected-robot');

		if(typeof activeConnectedRobotId !== 'string') {
			vscode.window.showErrorMessage('No connected robot or saved robot id is invalid!');
			return undefined;
		}

		let connectedRobot = discoveryService.getRobotById(activeConnectedRobotId);
		if(connectedRobot === null) {
			vscode.window.showErrorMessage(`Could not detect robot ${activeConnectedRobotId} on your network!`, "View available robots");
			return undefined;
		}

		return connectedRobot.address;
	}));

	disposables.push(vscode.window.registerTreeDataProvider('robot-list', robotListProvider));

	vscode.workspace.onDidChangeConfiguration((e) => {
		if(e.affectsConfiguration('avans-statisticalrobot.connected-robot')) {
			robotListProvider.refresh();
		}
	});

	context.subscriptions.push(...disposables);
}

export function deactivate() {
}
