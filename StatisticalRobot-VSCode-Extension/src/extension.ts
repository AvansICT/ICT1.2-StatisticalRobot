
import * as vscode from 'vscode';
import { RobotDiscovery, RobotInfo } from './lib/RobotDiscovery';
import { RobotListProvider } from './RobotListProvider';
import path from 'path';
import * as fs from 'fs';
import { StatisticalRobotTaskProvider } from './StatisticalRobotTaskProvider';
import { SshPool } from './lib/SshPool';

// Entry point of the extension
export function activate(context: vscode.ExtensionContext) {
	// Create a list of disposables to register them all at once at the end
	let disposables: vscode.Disposable[] = [];

	let sshPool = new SshPool();
	disposables.push(sshPool);

	let robotListProvider: RobotListProvider;

	const discoveryService = new RobotDiscovery(() => {
		// refreshes the robot list every time a new robot has been discovered
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

		// TODO only allow one settings view to open per robot
		const settingsView = vscode.window.createWebviewPanel(
			'statisticalrobot-settingsview',
			`Settings ${robotInfo}`, 
			vscode.ViewColumn.Active,
			{
				enableScripts: true,
				retainContextWhenHidden: true
			}
		);

		// TODO: fix this
		// settingsView.iconPath = vscode.Uri.joinPath(vscode.Uri.parse(__dirname), '../resources/robot-icon.svg');

		settingsView.webview.html = fs.readFileSync(path.join(__dirname, '..', 'webviews', 'robotsettings.html')).toString();

		settingsView.webview.postMessage({
			id: robotInfo.id,
			ipAddress: robotInfo.address,
			simpleId: robotInfo.simpleId
		});
	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.openDesignWebview', (treeItem) => {

		// TODO only allow one settings view to open per robot
		const designView = vscode.window.createWebviewPanel(
			'statisticalrobot-designview',
			`Settings Design`, 
			vscode.ViewColumn.Active,
			{
				enableScripts: true,
				retainContextWhenHidden: true
			}
		);

		designView.webview.html = fs.readFileSync(path.join(__dirname, '..', 'webviews', 'design.html')).toString();
	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.connectedRobotIpAddress', async () => {
		let activeConnectedRobotId = vscode.workspace.getConfiguration()
			.get<string>('avans-statisticalrobot.connected-robot');

		if(typeof activeConnectedRobotId !== 'string') {
			vscode.window.showErrorMessage('No connected robot or saved robot id is invalid!');
			return undefined;
		}

		let connectedRobot = discoveryService.getRobotById(activeConnectedRobotId);
		if(connectedRobot === null) {
			let messageResult = await vscode.window.showErrorMessage(
				`Could not detect robot ${activeConnectedRobotId} on your network!`, 
				"View available robots"
			);

			if(messageResult === "View available robots") {
				vscode.commands.executeCommand("statisticalrobot-list.focus");
			}

			return undefined;
		}

		return connectedRobot.address;
	}));

	disposables.push(vscode.window.registerTreeDataProvider('statisticalrobot-list', robotListProvider));

	disposables.push(vscode.workspace.onDidChangeConfiguration((e) => {
		if(e.affectsConfiguration('avans-statisticalrobot.connected-robot')) {
			robotListProvider.refresh();
		}
	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.fallbackChangeLocalRobotSettings', async () => {
		let ip = await vscode.window.showInputBox({
			title: 'IP Address or Host',
			value: 'raspberrypi.local'
		});

		if(!ip) {
			return;
		}

		// TODO only allow one settings view to open per robot
		const settingsView = vscode.window.createWebviewPanel(
			'statisticalrobot-settingsview',
			`Settings Local Robot`, 
			vscode.ViewColumn.Active,
			{
				enableScripts: true,
				retainContextWhenHidden: true
			}
		);

		settingsView.webview.html = fs.readFileSync(path.join(__dirname, '..', 'webviews', 'robotsettings.html')).toString();

		settingsView.webview.postMessage({
			id: 'local',
			ipAddress: ip,
			simpleId: 'local'
		});
	}));

	disposables.push(vscode.tasks.registerTaskProvider('statisticalrobot', new StatisticalRobotTaskProvider(discoveryService, sshPool)));

	context.subscriptions.push(...disposables);
}

export function deactivate() {
}
