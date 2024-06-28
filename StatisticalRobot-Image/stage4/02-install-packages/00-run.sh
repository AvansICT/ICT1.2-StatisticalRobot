#!/bin/bash -e

# IMPORTANT!!!!
# If you modify this file using windows, MAKE SURE that you save it in linux format
# This means, use '\n' (line-feat, LF) as new-line and not windows: '\r\n' (Criage-Return+Line-feat, CRLF)!!!
# THIS IS VERY IMPORTANT, or else this file won't execute in the image

# Install StatisticalRobot-Server from github
if [ -e "${STAGE_WORK_DIR}/ICT1.2-StatisticalRobot" ]; then
    # Only pull if directory already exists
    (cd "${STAGE_WORK_DIR}/ICT1.2-StatisticalRobot" && git pull origin main)
else
    git clone https://github.com/AvansICT/ICT1.2-StatisticalRobot.git "${STAGE_WORK_DIR}/ICT1.2-StatisticalRobot"
fi

# Build the latest version of the statisticalrobot-server
dotnet publish "${STAGE_WORK_DIR}/ICT1.2-StatisticalRobot/StatisticalRobot-Server.csproj" --runtime linux-arm64 --no-self-contained --nologo --output "${ROOTFS_DIR}/opt/StatisticalRobot-Server"

# Install StatisticalRobot-Server as a systemd service. See the service file for more information
install -m 644 files/statisticalrobot-server.service "${ROOTFS_DIR}/etc/systemd/system/statisticalrobot-server.service"

on_chroot << EOF
systemctl enable statisticalrobot-server.service
EOF