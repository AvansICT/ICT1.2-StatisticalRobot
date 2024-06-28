# pi-gen (Avans Statistical Robot)

Tool used to create Raspberry Pi OS images, and custom images based on Raspberry Pi OS,
which was in turn derived from the Raspbian project.

**Note**: Raspberry Pi OS 32 bit images are based primarily on Raspbian, while
Raspberry Pi OS 64 bit images are based primarily on Debian.

**AVANS**: This copy of pi-gen is based on RPi-Distro/pi-gen from github, using branch arm64

>PLEASE READ THE WHOLE README BEFORE MAKING ANY MODIFICATIONS TO THE AVANS STATISTICALROBOT IMAGE CONFIGURATION

>**IMPORTANT** When modifying files, make sure you use Unix line-feeds (\n). If Windows line-feeds are used (\r\n), the image build WILL fail with the message 'Invalid option' or the generated image contains invalid files. When using visual studio code to modify the build files, the settings in the `.vscode` folder should already enforce this rule.

## Updating Raspbian

For updating the base raspbian image, clone the arm64 branch from RPi-Distro/pi-gen to a seperate directory:
`git clone --branch arm64 https://github.com/RPi-Distro/pi-gen.git`.
Copy all files EXCEPT the folders/files `stage4`, `stage5` (These folders contain the configuration for the desktop environment), `export-noobs` and `README`.
Please read the README of the pi-gen repository to see if anything has changed with the raspbian image.
Also, for faster build times, disable the Raspbian-Lite image by removing the file `EXPORT_IMAGE` from the `stage2` folder.

After updating the `build.sh` file, please add the following exports to the list of exported variables in the `build.sh` file:

```bash
export CSPROJECTS_RAM_DISK_SIZE=${CSPROJECTS_RAM_DISK_SIZE:-512M}
export DOTNET_VERSION
export DISABLE_BLUETOOTH=${DISABLE_BLUETOOTH:0}
export DISABLE_SWAP=${DISABLE_SWAP:0}
export DISABLE_USER_PASSWORD=${DISABLE_USER_PASSWORD:1}
```

## Configuring the image

Upon execution, `build.sh` will source the file `config` in the current
working directory. This bash shell fragment is intended to set needed
environment variables.

Building the image for the raspberry pi zero-series is possible. Because the raspberry pi zero has limited resources, a seperate configuration file is used. You can specify this configuration file by using (for normal and docker build respectively):

```bash
./build.sh -c config-pizero
./build-docker.sh -c config-pizero
```

The following environment variables are supported:

 * `IMG_NAME` (Default: `raspios-$RELEASE-$ARCH`, for example: `raspios-bookworm-armhf`)

   The name of the image to build with the current stage directories. Use this
   variable to set the root name of your OS, eg `IMG_NAME=Frobulator`.
   Export files in stages may add suffixes to `IMG_NAME`.

 * `PI_GEN_RELEASE` (Default: `Raspberry Pi reference`)

   The release name to use in `/etc/issue.txt`. The default should only be used
   for official Raspberry Pi builds.

* `RELEASE` (Default: `bookworm`)

   The release version to build images against. Valid values are any supported
   Debian release. However, since different releases will have different sets of
   packages available, you'll need to either modify your stages accordingly, or
   checkout the appropriate branch. For example, if you'd like to build a
   `bullseye` image, you should do so from the `bullseye` branch.

 * `APT_PROXY` (Default: unset)

   If you require the use of an apt proxy, set it here.  This proxy setting
   will not be included in the image, making it safe to use an `apt-cacher` or
   similar package for development.

 * `BASE_DIR`  (Default: location of `build.sh`)

   **CAUTION**: Currently, changing this value will probably break build.sh

   Top-level directory for `pi-gen`.  Contains stage directories, build
   scripts, and by default both work and deployment directories.

 * `WORK_DIR`  (Default: `$BASE_DIR/work`)

   Directory in which `pi-gen` builds the target system.  This value can be
   changed if you have a suitably large, fast storage location for stages to
   be built and cached.  Note, `WORK_DIR` stores a complete copy of the target
   system for each build stage, amounting to tens of gigabytes in the case of
   Raspbian.

   **CAUTION**: If your working directory is on an NTFS partition you probably won't be able to build: make sure this is a proper Linux filesystem.

 * `DEPLOY_DIR`  (Default: `$BASE_DIR/deploy`)

   Output directory for target system images and NOOBS bundles.

 * `DEPLOY_COMPRESSION` (Default: `zip`)

   Set to:
   * `none` to deploy the actual image (`.img`).
   * `zip` to deploy a zipped image (`.zip`).
   * `gz` to deploy a gzipped image (`.img.gz`).
   * `xz` to deploy a xzipped image (`.img.xz`).


 * `DEPLOY_ZIP` (Deprecated)

   This option has been deprecated in favor of `DEPLOY_COMPRESSION`.

   If `DEPLOY_ZIP=0` is still present in your config file, the behavior is the
   same as with `DEPLOY_COMPRESSION=none`.

 * `COMPRESSION_LEVEL` (Default: `6`)

   Compression level to be used when using `zip`, `gz` or `xz` for
   `DEPLOY_COMPRESSION`. From 0 to 9 (refer to the tool man page for more
   information on this. Usually 0 is no compression but very fast, up to 9 with
   the best compression but very slow ).

 * `USE_QEMU` (Default: `0`)

   Setting to '1' enables the QEMU mode - creating an image that can be mounted via QEMU for an emulated
   environment. These images include "-qemu" in the image file name.

 * `LOCALE_DEFAULT` (Default: 'en_GB.UTF-8' )

   Default system locale.

 * `TARGET_HOSTNAME` (Default: 'raspberrypi' )

   Setting the hostname to the specified value.

 * `KEYBOARD_KEYMAP` (Default: 'gb' )

   Default keyboard keymap.

   To get the current value from a running system, run `debconf-show
   keyboard-configuration` and look at the
   `keyboard-configuration/xkb-keymap` value.

 * `KEYBOARD_LAYOUT` (Default: 'English (UK)' )

   Default keyboard layout.

   To get the current value from a running system, run `debconf-show
   keyboard-configuration` and look at the
   `keyboard-configuration/variant` value.

 * `TIMEZONE_DEFAULT` (Default: 'Europe/London' )

   Default keyboard layout.

   To get the current value from a running system, look in
   `/etc/timezone`.

 * `FIRST_USER_NAME` (Default: `pi`)

   Username for the first user. This user only exists during the image creation process. Unless
   `DISABLE_FIRST_BOOT_USER_RENAME` is set to `1`, this user will be renamed on the first boot with
   a name chosen by the final user. This security feature is designed to prevent shipping images
   with a default username and help prevent malicious actors from taking over your devices.

 * `FIRST_USER_PASS` (Default: unset)

   Password for the first user. If unset, the account is locked.

 * `DISABLE_FIRST_BOOT_USER_RENAME` (Default: `0`)

   Disable the renaming of the first user during the first boot. This make it so `FIRST_USER_NAME`
   stays activated. `FIRST_USER_PASS` must be set for this to work. Please be aware of the implied
   security risk of defining a default username and password for your devices.

 * `WPA_COUNTRY` (Default: unset)

   Sets the default WLAN regulatory domain and unblocks WLAN interfaces. This should be a 2-letter ISO/IEC 3166 country Code, i.e. `GB`

 * `ENABLE_SSH` (Default: `0`)

   Setting to `1` will enable ssh server for remote log in. Note that if you are using a common password such as the defaults there is a high risk of attackers taking over you Raspberry Pi.

  * `PUBKEY_SSH_FIRST_USER` (Default: unset)

   Setting this to a value will make that value the contents of the FIRST_USER_NAME's ~/.ssh/authorized_keys.  Obviously the value should
   therefore be a valid authorized_keys file. Note that this does not
   automatically enable SSH.

  * `PUBKEY_ONLY_SSH` (Default: `0`)

   * Setting to `1` will disable password authentication for SSH and enable
   public key authentication.  Note that if SSH is not enabled this will take
   effect when SSH becomes enabled.

 * `SETFCAP` (Default: unset)

   * Setting to `1` will prevent pi-gen from dropping the "capabilities"
   feature. Generating the root filesystem with capabilities enabled and running
   it from a filesystem that does not support capabilities (like NFS) can cause
   issues. Only enable this if you understand what it is.

 * `STAGE_LIST` (Default: `stage*`)

    If set, then instead of working through the numeric stages in order, this list will be followed. For example setting to `"stage0 stage1 mystage stage2"` will run the contents of `mystage` before stage2. Note that quotes are needed around the list. An absolute or relative path can be given for stages outside the pi-gen directory.

**The following environment variables are added by Avans:**

 * `DOTNET_VERSION` (Default: unset, Required)

    The dotnet version to install in the image. Example values: `7.0`, `8.0`, `9.0`.

 * `CSPROJECTS_RAM_DISK_SIZE` (Default: 512M)

    To relieve the SD-Card, a ram disk is created to store all C# project from the students. Is is recommended to leave 50% of the memory free for other programs. The OS uses approximately 150MB RAM whilst running multiple C# applications. A minimal size of 256M is recommended, but student projects shouldn't be larger than a couple of megabytes. **The `M` suffix is required to specify Megabytes**. By setting this value to `0` (with the M suffix), disables the ram disk and will store al C# projects on the sd-card.

 * `DISABLE_BLUETOOTH` (Default: 0)

    Disables bluetooth if this value is set to `1`. It is recommended to turn off bluetooth for the statistical-robot when not needed, because it will lower the raspberry pi's power consumption (= longer battery life).

 * `DISABLE_SWAP` (Default: 0)

    Disables the swap file (storing RAM-pages on the sd-card) when set to `1` to reduce the wear on the sd-card.

 * `DISABLE_USER_PASSWORD` (Default: 1)

    Disables the users password (for local login and ssh) when set to `1`. At the time of writing, the user password **MUST** be disabled for the StatisticalRobot-VSCode-Extension to function correctly. It is recommended to re-enable the user password when the VSCode extension fully supports password (Uploading already supports passwords, debugging using the launch.json file doesn't).

## Building the Image

If this folder is opened in VSCode, the pre-configured build tasks can be used to build the image. In VSCode press `Ctrl+Shift+P` and type: `Tasks: Run Task` and choose one of the options described below.

**Warning:** The build-scripts automate the image-creation, but this doesn't mean it's fast. Expect the script to run for about 20 minutes on a high-end machine with a 1Gbps internet connection before the image creation is done.

**Ignore** warnings about `Possible missing firmware /lib/firmware/rtl_nic/rtl8156b-2.fw for built-in driver r8152`, these won't break anything and the build continu's as normally.

### Build using Docker (Recommended)

**Important**: Make sure qemu-user-static and binfmt-support packages are installed
on the system where the ./build-docker.sh is run.
Use: `sudo apt update && sudo apt install qemu-user-static binfmt-support`
These dependecies NEED to be available or the docker image won't run!
In Windows/WSL: Install these packages inside the default wsl container,
the WSL distribution must be debian based.
After installing these dependencies and when running the build-docker.sh
script for the first time, the script might ask for sudo/root privileges.

On Windows: Make sure a WSL-distribution is installed (Ubuntu is recommended, but any debian system should work) and Docker Desktop is running.

When using VSCode, run the `Build image using docker (Recommended)`. This will execute the `./build-docker.sh` file in the integrated-console. Wait for the image to finish building. The build script might ask for sudo permission at the start of the build. When using windows, the script will be run in the default Windows-Subsystem-for-Linux terminal (Ubuntu is recommended, but any debian system should work).

The generated images can be found in the `./deploy` directory in this folder.

The pi-gen tools creates a very large docker image. After all images are created and you're not planning to generate more images, it is recommended to remove the left over images by running the commands `docker container rm pigen_work` (if it exists), `docker image rm pi-gen`, `docker image prune` and `docker builder prune`. This will free a couple of gigabytes on your disk.

### Build using Linux

pi-gen runs on Debian-based operating systems released after 2017, and we
always advise you use the latest OS for security reasons.

To install the required dependencies for `pi-gen` you should run:

```bash
apt install coreutils quilt parted qemu-user-static debootstrap zerofree zip \
dosfstools libarchive-tools libcap2-bin grep rsync xz-utils file git curl bc \
gpg pigz xxd arch-test
```

Dotnet is also required. You have to install this manually using 

When using Windows, install these packages in your default Windows-Subsystem-for-Linux terminal (Ubuntu is recommended, but any debian system should work).

After installing these dependencies, run the `./build.sh` script (or run the `Build Image` task in vscode). This will execute the `./build.sh` file in the integrated-console. Wait for the image to finish building.

The generated images can be found in the `./deploy` directory in this folder.

When modifying the images between builds, it is recommended to run the `./build.sh` script with the `CLEAN` environment set to `1` or to remove the `./work` directory. This makes sure you're always working with a clean image. Also, any stages before the stage you've modified can be skipped by adding an empty `SKIP` file in the root of the stage folder, which speeds up the re-build process.

## How the build process works

The following process is followed to build images:

 * Interate through all of the stage directories in alphanumeric order

 * Bypass a stage directory if it contains a file called
   "SKIP"

 * Run the script ```prerun.sh``` which is generally just used to copy the build
   directory between stages.

 * In each stage directory iterate through each subdirectory and then run each of the
   install scripts it contains, again in alphanumeric order. **These need to be named
   with a two digit padded number at the beginning.**
   There are a number of different files and directories which can be used to
   control different parts of the build process:

     - **00-run.sh** - A unix shell script. Needs to be made executable for it to run.

     - **00-run-chroot.sh** - A unix shell script which will be run in the chroot
       of the image build directory. Needs to be made executable for it to run.

     - **00-debconf** - Contents of this file are passed to debconf-set-selections
       to configure things like locale, etc.

     - **00-packages** - A list of packages to install. Can have more than one, space
       separated, per line.

     - **00-packages-nr** - As 00-packages, except these will be installed using
       the ```--no-install-recommends -y``` parameters to apt-get.

     - **00-patches** - A directory containing patch files to be applied, using quilt.
       If a file named 'EDIT' is present in the directory, the build process will
       be interrupted with a bash session, allowing an opportunity to create/revise
       the patches.

  * If the stage directory contains files called "EXPORT_NOOBS" or "EXPORT_IMAGE" then
    add this stage to a list of images to generate

  * Generate the images for any stages that have specified them

It is recommended to examine build.sh for finer details.

## Stage Anatomy

### Avans StatisticalRobot OS Stage Overview

(Based on the raspberry pi os (lite) arm64 image)
The build of the image is divided up into several stages for logical clarity
and modularity.  This causes some initial complexity, but it simplifies
maintenance and allows for more easy customization.
Stage 0 to 2 are copied from raspberry pi os lite and
will produce the normal raspberrypi os lite image.

 - **Stage 0** - bootstrap.  The primary purpose of this stage is to create a
   usable filesystem.  This is accomplished largely through the use of
   `debootstrap`, which creates a minimal filesystem suitable for use as a
   base.tgz on Debian systems.  This stage also configures apt settings and
   installs `raspberrypi-bootloader` which is missed by debootstrap.  The
   minimal core is installed but not configured. As a result, this stage will not boot.

 - **Stage 1** - truly minimal system.  This stage makes the system bootable by
   installing system files like `/etc/fstab`, configures the bootloader, makes
   the network operable, and installs packages like raspi-config.  At this
   stage the system should boot to a local console from which you have the
   means to perform basic tasks needed to configure and install the system.

 - **Stage 2** - lite system.  This stage produces the Raspberry Pi OS Lite image.
   Stage 2 installs some optimized memory functions, sets timezone and charmap
   defaults, installs fake-hwclock and ntp, wireless LAN and bluetooth support,
   dphys-swapfile, and other basics for managing the hardware.  It also
   creates necessary groups and gives the pi user access to sudo and the
   standard console hardware permission groups.

   Note: Raspberry Pi OS Lite contains a number of tools for development,
   including `Python`, `Lua` and the `build-essential` package. If you are
   creating an image to deploy in products, be sure to remove extraneous development
   tools before deployment.

 - **Stage 3 (Avans)** - Creates an dev image for the statistical robot
   The following settings will be applied: disable ssh password, disable user password,
   disable automatic updates, adding enable GPIO peripherals (like i2c and spi) script
   on first boot (this must be run on the pi), adds dotnet to path for user rompi.
   Also enables pwm on pin 12/13.
   This stage installs dotnet and the vsdbg remote debugger.
   Also the StatisticalRobot-Server will be installed and configured to start on boot.

 - **Stage 4 (Avans)** - Creates the student image for the statistical robot.
   The following settings will be applied: disable writing systemlogs (journal),
   csprojects ram disk, disable swap.
   The StatisticalRobot-Server will be installed and configured to start on boot.

## Docker Build

Docker can be used to perform the build inside a container. This partially isolates
the build from the host system, and allows using the script on non-debian based
systems (e.g. Fedora Linux). The isolation is not complete due to the need to use
some kernel level services for arm emulation (binfmt) and loop devices (losetup).

To build:

```bash
nano config         # Edit your config file. See above.
./build-docker.sh
```

If everything goes well, your finished image will be in the `deploy/` folder.
You can then remove the build container with `docker rm -v pigen_work`

If you encounter errors during the build, you can edit the corresponding scripts, and
continue:

```bash
CONTINUE=1 ./build-docker.sh
```

To examine the container after a failure you can enter a shell within it using:

```bash
sudo docker run -it --privileged --volumes-from=pigen_work pi-gen /bin/bash
```

After successful build, the build container is by default removed. This may be undesired when making incremental changes to a customized build. To prevent the build script from remove the container add

```bash
PRESERVE_CONTAINER=1 ./build-docker.sh
```

There is a possibility that even when running from a docker container, the
installation of `qemu-user-static` will silently fail when building the image
because `binfmt-support` _must be enabled on the underlying kernel_. An easy
fix is to ensure `binfmt-support` is installed on the host machine before
starting the `./build-docker.sh` script (or using your own docker build
solution).

### Passing arguments to Docker

When the docker image is run various required command line arguments are provided.  For example the system mounts the `/dev` directory to the `/dev` directory within the docker container.  If other arguments are required they may be specified in the PIGEN_DOCKER_OPTS environment variable.  For example setting `PIGEN_DOCKER_OPTS="--add-host foo:192.168.0.23"` will add '192.168.0.23   foo' to the `/etc/hosts` file in the container.  The `--name`
and `--privileged` options are already set by the script and should not be redefined.

## Skipping stages to speed up development

If you're working on a specific stage the recommended development process is as
follows:

 * Add a file called SKIP_IMAGES into the directories containing EXPORT_* files
   (currently stage2, stage4 and stage5)
 * Add SKIP files to the stages you don't want to build. For example, if you're
   basing your image on the lite image you would add these to stages 3, 4 and 5.
 * Run build.sh to build all stages
 * Add SKIP files to the earlier successfully built stages
 * Modify the last stage
 * Rebuild just the last stage using ```sudo CLEAN=1 ./build.sh```
 * Once you're happy with the image you can remove the SKIP_IMAGES files and
   export your image to test

# Troubleshooting


## `binfmt_misc`

Linux is able execute binaries from other architectures, meaning that it should be
possible to make use of `pi-gen` on an x86_64 system, even though it will be running
ARM binaries. This requires support from the [`binfmt_misc`](https://en.wikipedia.org/wiki/Binfmt_misc)
kernel module.

You may see one of the following errors:

```
update-binfmts: warning: Couldn't load the binfmt_misc module.
```
```
W: Failure trying to run: chroot "/pi-gen/work/test/stage0/rootfs" /bin/true
and/or
chroot: failed to run command '/bin/true': Exec format error
```

To resolve this, ensure that the following files are available (install them if necessary):

```
/lib/modules/$(uname -r)/kernel/fs/binfmt_misc.ko
/usr/bin/qemu-aarch64-static
```

You may also need to load the module by hand - run `modprobe binfmt_misc`.

If you are using WSL to build you may have to enable the service `sudo update-binfmts --enable`
