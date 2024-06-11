#!/bin/bash

# Enable script exit on error, when an error occures, this script will end immediately
set -e

# Select dotnet version. Please only use format "{MAJOR}.0"
# The installer will resolve the best version for that .NET-version automatically
DOTNET_VERSION=8.0
export DOTNET_ROOT=/opt/dotnet_$DOTNET_VERSION

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
export DOTNET_ROOT=/opt/dotnet_$DOTNET_VERSION
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
echo "[Setup] Creating ram-disk for C# projects"
sudo mkdir /mnt/csprojects

# Add a tmpfs filesystem (ram-based filesystem) in folder /mnt/csprojects for user rompi with read/write permissions
echo "none /mnt/csprojects tmpfs nodev,nosuid,nodiratime,size=2048M,umask=0022,gid=1000,uid=1000 0 0" | sudo tee -a /etc/fstab > /dev/null

# Load the created filesystem and update permissions
sudo mount -a
sudo systemctl daemon-reload
sudo chmod -R 755 /mnt/csprojects
sudo chown -R 1000:1000 /mnt/csprojects

# Setup StatisticalRobot-Server

# We download and build this locally, so it will always be up to date
# Also, by building the first time in setup, we will cache build-in dotnet dependencies build files
echo "[Setup] Setting up service StatisicalRobot-Server for Robot discovery and configuration"
git clone https://github.com/AvansICT/ICT1.2-StatisticalRobot.git /tmp/ICT1.2-StatisticalRobot
dotnet publish /tmp/ICT1.2-StatisticalRobot/StatisticalRobot-Server

# Install StatisticalRobot-Server to /opt
mkdir /opt/StatisticalRobot-Server
sudo mv /tmp/ICT1.2-StatisticalRobot/StatisticalRobot-Server/bin/Release/net$DOTNET_VERSION/publish/* /opt/StatisticalRobot-Server
sudo chmod -R 755 /opt/StatisticalRobot-Server

# Register service at startup
sudo cp /tmp/ICT1.2-StatisticalRobot/StatisticalRobot-Scripts/statisticalrobot-server.service /etc/systemd/system
sudo chmod 644 /etc/systemd/system/statisticalrobot-server.service
sudo systemctl daemon-reload
sudo systemctl enable statisticalrobot-server.service

# Setup finished
echo ""
echo "[Setup] Installation succesfull"
echo "[Setup] Please reboot the device now!