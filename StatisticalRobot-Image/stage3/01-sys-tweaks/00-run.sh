#!/bin/bash -e

# IMPORTANT!!!!
# If you modify this file using windows, MAKE SURE that you save it in linux format
# This means, use '\n' (line-feat, LF) as new-line and not windows: '\r\n' (Criage-Return+Line-feat, CRLF)!!!
# THIS IS VERY IMPORTANT, or else this file won't execute in the image

# Add directory for csprojects (none ram disk in dev version, stage 4 makes a ramdisk at this mount point)
mkdir -p "${ROOTFS_DIR}/media/csprojects"

if [ "${DISABLE_USER_PASSWORD}" = "1" ]; then

    # For easy SSH usage, password is disabled
    sed -i '/#PermitEmptyPasswords no/c\PermitEmptyPasswords yes' "${ROOTFS_DIR}"/etc/ssh/sshd_config

    # Disable password for user
    on_chroot << EOF
passwd -d ${FIRST_USER_NAME}
EOF

fi

# Disable automatic updates
on_chroot << EOF
systemctl mask man-db.timer
systemctl mask apt-daily.timer
systemctl mask apt-daily-upgrade.timer
EOF

# set DOTNET environment variables
echo "export PATH=\${PATH}:/opt/dotnet" >> "${ROOTFS_DIR}"/home/"${FIRST_USER_NAME}"/.bashrc
echo "export DOTNET_ROOT=/opt/dotnet" >> "${ROOTFS_DIR}"/home/"${FIRST_USER_NAME}"/.bashrc

# Enable PWM
echo "dtoverlay=pwm-2chan,pin=12,func=4,pin2=13,func2=4" >> "${ROOTFS_DIR}/boot/firmware/config.txt"

# Disable bluetooth
if [ "${DISABLE_BLUETOOTH}" = "1" ]; then
    echo "dtoverlay=disable-bt" >> "${ROOTFS_DIR}/boot/firmware/config.txt"
fi

# Enable GPIO peripherals on first boot by registering a script that runs on boot and deletes its self
install -m 755 files/statisticalrobot-init "${ROOTFS_DIR}/etc/init.d/"
on_chroot << EOF
systemctl enable statisticalrobot-init
EOF