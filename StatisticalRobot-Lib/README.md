# StatisticalRobot-Lib

A C# library for interfacing with the Pololu Romi. The library also exists of various helper functions, hiding complex code functions and structures required to make projects for the Robot.

## Developing

The library is developed using VSCode, but Visual Studio should also work. The C# solution exists of two folders:

- **StatisticalRobot-Lib** -
  This folder contains the actual library code.
- **StatisticalRobot-LibTest** -
  This folder contains a test project to test the library. The project uses the libraries project as reference.

The library code can be developed within the StatisticalRobot-Lib project. The StatisticalRobot-LibTest project can be used to test and debug the library.
When using VSCode, please open this folder as workspace (encapsulating both the library and test projects) for the provided tasks to work properly.

## Building

Building the project can be done using the provided VSCode tasks or by running the `dotnet` build commands yourself. For the best compatibility, the library should be build with `linux-arm64` as runtime. The library doesn't have to be self contained, which saves space.

The following VSCode tasks are available:

- **statisticalrobot: Build and Deploy** (Default Task) - This task builds the project and deploys it to the connecteded StatisticalRobot for debugging purposes.
- **StatisticalRobot-Lib: Publish as DLL** - Compiles the library to a DLL for use in real projects. This DLL is already optimized for linux-arm64 and is configured for release.
- **Update StatisticalRobot-ProjectTemplate Library** - Copies the published DLL to the StatisticalRobot-ProjectTemplate folder of this git-repository. This task uses a relative copy from the publish folder to the StatisticalRobot-ProjectTemplate folder. If the dotnet version or the folder names change, this task also needs to be changed.

## Deploying

To deploy a new version of the library, run the VSCode task `Update StatisticalRobot-ProjectTemplate Library` or follow these steps:

- Run `dotnet publish StatisticalRobot-Lib.csproj --runtime linux-arm64 --no-self-contained`
- Copy the file `bin/Release/{DOTNET_VERSION}/linux-arm64/publish/StatisticalRobot-Lib.dll` to the `StatisticalRobot-ProjectTemplate` folder, replacing the old DLL.
- Distribute the new version of the `StatisticalRobot-ProjectTemplate`
