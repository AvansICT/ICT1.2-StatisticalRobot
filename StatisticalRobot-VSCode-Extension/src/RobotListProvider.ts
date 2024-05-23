
import * as vscode from 'vscode';
import { RobotDiscovery, RobotInfo } from './lib/RobotDiscovery';
import path from 'path';
import { rejects } from 'assert';

export class RobotListProvider implements vscode.TreeDataProvider<RobotListTreeItem> {

    public readonly onDidChangeTreeData?: vscode.Event<void | RobotListTreeItem | RobotListTreeItem[] | null | undefined> | undefined;

    private _onDidChangeTreeData: vscode.EventEmitter<RobotListTreeItem | undefined | null | void> = new vscode.EventEmitter<RobotListTreeItem | undefined | null | void>();

    constructor(private discoveryService: RobotDiscovery) {
        this.onDidChangeTreeData = this._onDidChangeTreeData.event;
    }

    getTreeItem(element: RobotListTreeItem): vscode.TreeItem | Thenable<vscode.TreeItem> {
        return element;
    }

    getChildren(element?: RobotListTreeItem | undefined): vscode.ProviderResult<RobotListTreeItem[]> {
        return (async () => {
            if(element === undefined) {
                return this.discoveryService.getDiscoveredRobots()
                    .map((robotInfo) => new RobotListTreeItem(
                        robotInfo!, 
                        `Robot ${robotInfo?.simpleId} (${robotInfo?.address})`, 
                        vscode.TreeItemCollapsibleState.None
                    ));
            }
            else {
                return [];
            }
        })();
    }

    refresh() {
        this.discoveryService.discover();
        this._onDidChangeTreeData.fire();
    }

    // getParent?(element: RobotListTreeItem): vscode.ProviderResult<RobotListTreeItem> {
    //     throw new Error('Method not implemented.');
    // }

    // resolveTreeItem?(item: vscode.TreeItem, element: RobotListTreeItem, token: vscode.CancellationToken): vscode.ProviderResult<vscode.TreeItem> {
    //     throw new Error('Method not implemented.');
    // }

}

class RobotListTreeItem extends vscode.TreeItem {

    constructor(
        public robotInfo: RobotInfo, 
        public readonly label: string, 
        public readonly collapsibleState: vscode.TreeItemCollapsibleState
    ) {
        super(label, collapsibleState);

        this.iconPath = path.join(__dirname, '..', 'resources', 'robot-active.svg');
        this.contextValue = 'robotinfo';
    }

}