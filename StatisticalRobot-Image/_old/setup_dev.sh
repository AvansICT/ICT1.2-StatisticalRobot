#!/bin/bash

# WARNING: This script is different from the normal script!!!
# This script is meant for the development pi and should only be used for:
# - figuring out the pi configuration or setting up a new configuration
# - Development of the StatisticalRobot Library for students
# - Development of the StatisticalRobot-Server <= VERY IMPORTANT THAT YOU USE THIS SCRIPT INSTEAD OF THE NORMAL ONE, BECAUSE OF UDP-PORT RESERVATIONS
#
# This script lacks the setup for the StatisticalRobot-Server (Remote build and deploying or on-device deploying should be setup manually by the developer)
# This script also lacks the ram disk, for easy saving older/build versions between reboots

# IMPORTANT!!!!
# If you modify this file using windows, MAKE SURE that you save it in linux format
# This means, use '\n' (line-feat, LF) as new-line and not windows: '\r\n' (Criage-Return+Line-feat, CRLF)!!!
# THIS IS VERY IMPORTANT, or else this file won't execute in the image

# Enable script exit on error, when an error occures, this script will end immediately
set -e

# Select dotnet version. Please only use format "{MAJOR}.0"
# The installer will resolve the best version for that .NET-version automatically
DOTNET_VERSION=8.0
export DOTNET_ROOT=/opt/dotnet

# Select vsdbg version
VSDBG_VERSION="latest"

# Install updates
echo "[Setup] Installing updates"
sudo apt update
sudo apt full-upgrade -y

echo "[Setup] Installing dependencies"
sudo apt install -y git

# Download dotnet install script
echo "[Setup] Downloading dotnet installer"
curl -sSL https://dot.net/v1/dotnet-install.sh > /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh

# Install dotnet, this will take a while...
echo "[Setup] Installing dotnet $DOTNET_VERSION to /opt"
sudo /tmp/dotnet-install.sh --channel $DOTNET_VERSION --install-dir "$DOTNET_ROOT"
sudo chmod -R 755 $DOTNET_ROOT

# Download vsdbg install script
echo "[Setup] Downloading vsdbg installer"
curl -sSL https://aka.ms/getvsdbgsh > /tmp/getvsdbg.sh
chmod +x /tmp/getvsdbg.sh

# Install vsdbg, this will take a while...
echo "[Setup] Installing vsdbg $VSDBG_VERSION to /opt"
sudo /tmp/getvsdbg.sh -v latest -l /opt/vsdbg
sudo chmod -R 755 /opt/vsdbg

# Setup Environment Vars
echo "[Setup] Setting up environment"
export DOTNET_ROOT=/opt/dotnet
export PATH=$PATH:$DOTNET_ROOT

echo "export PATH=\$PATH:$DOTNET_ROOT" >> ~/.bashrc
echo "export DOTNET_ROOT=$DOTNET_ROOT" >> ~/.bashrc

# Test if installation was succesfull
echo -n "[Setup] Installed dotnet version: "
dotnet --version

# For easy SSH usage, password is disabled
echo "[Setup] Allowing empty password SSH authentication"
sudo sed -i '/#PermitEmptyPasswords no/c\PermitEmptyPasswords yes' /etc/ssh/sshd_config

echo "[Setup] Disabling rompi password"
sudo passwd -d rompi

# Setup GPIO and hardware interfaces
echo "[Setup] Enabling I2C interface"
sudo raspi-config nonint do_i2c 0 # 0 means on

echo "[Setup] Enabling SPI interface"
sudo raspi-config nonint do_spi 0 # 0 means on

echo "[Setup] Enabling UART (Serial) interface"
sudo raspi-config nonint do_serial_hw 0 # 0 means on

echo "[Setup] Enabling PWM on pin 12 & pin 13"
echo "dtoverlay=pwm-2chan,pin=12,func=4,pin2=13,func2=4" | sudo tee -a /boot/firmware/config.txt > /dev/null

# Setup memory disk for C# projects
echo "[Setup] Folder for C# projects"
sudo mkdir /media/csprojects
sudo chmod -R 755 /media/csprojects
sudo chown -R rompi:rompi /media/csprojects

# Setup finished
echo ""
echo "[Setup] Installation succesfull"
echo "[Setup] Please reboot the device now!