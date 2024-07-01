# StatisticalRobot

This is the repository for the StatisticalRobot used by Avans ICT.
The repository contains everything that's needed for the StatisticalRobot to function and for the students to build and save their C# projects on their computer and upload them to the Robot for running and debugging.

The StatisticalRobot uses a raspberry pi to run the C# programs. Attached to the pi is a Pololu Romi for all robot functionality, like driving and powersupply. The raspberry pi communicates with the Romi using I2C.

The help the students communicate with the Romi, a library has been created. This library provides easy methods for the functionality of the Romi board and abstracts some advanced features in C# or the Raspberry Pi.

## Components

### StatisticalRobot-Image

A copy of pi-gen (the official raspberry pi image generator) modified to include all dependencies and confguration needed for the students to develop C# applications utilizing all GPIO-capabilities of the raspberry pi.

Modifying the image build scripts requires linux (chroot, systemd and basic linux configuration experience is required) and bash-scripting knowledge.

For more information, see the [README](./StatisticalRobot-Image/README.md) in the StatisticalRobot-Image folder.

### StatisticalRobot-Lib

A C# library for interfacing with the Pololu Romi. The library also exists of various helper functions, hiding complex code functions and structures required to make projects for the Robot.
For more information, see the [README](./StatisticalRobot-Lib/README.md) in the StatisticalRobot-Lib folder.

### StatisticalRobot-ProjectTemplate

The C# project template which is ready to be used by the students. The project template has a default, generic configuration that should work with every project out of the box. Modification of the configuration by the student is allowed, but at his own risk. The project template uses the folder name as the project name for deployment to the Robot.

To project template can be distributed as a zip folder. When zipping the template, make sure to exclude the following files as they're auto generated when the project template is opened in vscode:

- `.vscode/settings.json`
- the `bin` folder
- the `obj` folder
- The solution file (ending with `.sln`)

The `Program.cs` file contains a `Blinking LED` demo application.

The project template uses a feature of the C#-Extension called pipe-transport to enable remote debugging. This feature makes it possible to launch and debug the C# project over ssh. This feature uses ssh which is required to be installed by the user. Also, the pipeArgs-field adds some parameters to the ssh-program to ensure the debugger doesn't fail to launch if ssh could not verify the host. Removing these arguments could lead to some students not being able to connect to their or another robot and requiring them to modify their `%userdirectory%/.ssh/known_hosts` file.

### StatisticalRobot-Server

The server application running on the robots raspberry pi for discovering and configuring the robot within your local network. The server application allows the student to discover the robot, connect with the robot, configure WiFi connections and provides access to the power options of the raspberry pi.
For more information, see the [README](./StatisticalRobot-Server/README.md) in the StatisticalRobot-Server folder.

### StatisticalRobot-VSCode-Extension

The VSCode Extension enables the students to discover, configure, build, deploy and debug on their robot. The extension implements the discovery protocol and provides a user-friendly web interface to configure the robot. Using the installed dotnet version, the extension builds their project with the correct build configuration. The build application is than automatically uploaded to their connected pi using SSH.

The VSCode extension is developed using typescript. Modifying the extension requires knowledge about using node.js, javascript and typescript. Also, some experience with building vscode extensions is recommended.

For more information, see the [README_AVANS](./StatisticalRobot-Server/README_AVANS.md) in the StatisticalRobot-VSCode-Extension folder.

## Good to know

- When making a change to the StatisticalRobot-Server, don't forget to regenerate the new image for the students to use.
- If the default location for C# projects on the raspberry pi (Currently `/media/csprojects`) is changed, don't forget to the paths update the StatisticalRobot-ProjectTemplate and it is recommended to change the default values for the statisticalrobot task in the StatisticalRobot-VSCode-Extension (`package.json` and `StatisticalRobotTaskProvider.ts` need to be chnaged).
- If you want to update the used dotnet version (Currently `8.0`), this is possible very easily, but don't forget to change the version everywhere: The StatisticalRobot-Image configuration must be changed, the StatisticalRobot-ProjectTemplate `.csproj` file needs to specify the new version, the StatisticalRobot-Server `.csproj` file needs to specify the new version, the StatisticalRobot-Lib `.csproj` files need to specify the new version.
