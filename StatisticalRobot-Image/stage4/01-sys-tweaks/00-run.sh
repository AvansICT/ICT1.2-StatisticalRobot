#!/bin/bash -e

# IMPORTANT!!!!
# If you modify this file using windows, MAKE SURE that you save it in linux format
# This means, use '\n' (line-feat, LF) as new-line and not windows: '\r\n' (Criage-Return+Line-feat, CRLF)!!!
# THIS IS VERY IMPORTANT, or else this file won't execute in the image

# Add ramdisk
mkdir -p "${ROOTFS_DIR}/media/csprojects"

if [ "${CSPROJECTS_RAM_DISK_SIZE}" != "0" ]; then
    echo "tmpfs   /media/csprojects  tmpfs  size=${CSPROJECTS_RAM_DISK_SIZE},mode=755,gid=1000,uid=1000,exec 0 0" >> "${ROOTFS_DIR}"/etc/fstab
fi

# Disable writing systemlogs to reduce sd card wear
sed -i '/#Storage=auto/c\Storage=none' "${ROOTFS_DIR}"/etc/systemd/journald.conf

# Disable swap by removing the swapfile application
if [ "${DISABLE_SWAP}" = "1" ]; then
on_chroot << EOF
apt-get remove -y --purge dphys-swapfile
EOF
fi