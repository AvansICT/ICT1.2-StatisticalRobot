
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


	}));

	disposables.push(vscode.commands.registerCommand('avans-statisticalrobot.changeRobotSettings', (treeItem) => {
		if(treeItem === undefined) {
			return;
		}

		let robotInfo: RobotInfo = treeItem.robotInfo;
		if(robotInfo === undefined) {
			return;
		}

		
	}));

	disposables.push(vscode.window.registerTreeDataProvider('robot-list', robotListProvider));

	context.subscriptions.push(...disposables);
}

export function deactivate() {
}
