#!/bin/bash -e

# Install StatisticalRobot-Server
if [ -e "${STAGE_WORK_DIR}/ICT1.2-StatisticalRobot" ]; then
    # Only pull if directory already exists
    (cd "${STAGE_WORK_DIR}/ICT1.2-StatisticalRobot" && git pull origin main)
else
    git clone https://github.com/AvansICT/ICT1.2-StatisticalRobot.git "${STAGE_WORK_DIR}/ICT1.2-StatisticalRobot"
fi

dotnet publish "${STAGE_WORK_DIR}/ICT1.2-StatisticalRobot/StatisticalRobot-Server" --runtime linux-arm64 --no-self-contained --nologo --output "${ROOTFS_DIR}/opt/StatisticalRobot-Server"
install -m 644 files/statisticalrobot-server.service "${ROOTFS_DIR}/etc/systemd/system/statisticalrobot-server.service"

on_chroot << EOF
systemctl enable statisticalrobot-server.service
EOF