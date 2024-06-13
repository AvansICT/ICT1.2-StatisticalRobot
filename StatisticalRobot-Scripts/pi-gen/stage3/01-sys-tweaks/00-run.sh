#!/bin/bash -e

# Add directory for csprojects (none ram disk in dev version, stage 4 makes a ramdisk at this mount point)
mkdir -p "${ROOTFS_DIR}/mnt/csprojects"

# For easy SSH usage, password is disabled
sed -i '/#PermitEmptyPasswords no/c\PermitEmptyPasswords yes' "${ROOTFS_DIR}"/etc/ssh/sshd_config

# Disable password for user
on_chroot << EOF
passwd -d ${FIRST_USER_NAME}
EOF

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

# Enable GPIO peripherals on first boot
install -m 755 files/statisticalrobot-init "${ROOTFS_DIR}/etc/init.d/"
on_chroot << EOF
systemctl enable statisticalrobot-init
EOF