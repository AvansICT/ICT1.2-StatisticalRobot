
import * as vscode from 'vscode';
import { RobotDiscovery, RobotInfo } from './lib/RobotDiscovery';
import path, { resolve } from 'path';
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
        if(element === undefined) {
            let discoveredRobotList = this.discoveryService.getDiscoveredRobots();

            if(discoveredRobotList.length > 0) {
                return Promise.resolve(discoveredRobotList.map((robotInfo) => 
                    new RobotListTreeItem(
                        robotInfo!, 
                        `Robot ${robotInfo?.simpleId} (${robotInfo?.address})`, 
                        vscode.TreeItemCollapsibleState.None
                    )
                ));
            }
            else {
                // Add some fake delay, to give the user loading feedback
                return new Promise((resolve, reject) => setTimeout(() => resolve(undefined), 1000));
            }
            
        }
        else {
            return [];
        }
    }

    refresh() {
        this._onDidChangeTreeData.fire();
    }

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