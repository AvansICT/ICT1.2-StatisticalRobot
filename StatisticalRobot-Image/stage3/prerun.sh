#!/bin/bash -e

if [ ! -d "${ROOTFS_DIR}" ]; then
	copy_previous
fi

# vsdbg needs to be cleared if exists
if [ -e "${ROOTFS_DIR}/opt/vsdbg" ]; then
	rm -r "${ROOTFS_DIR}/opt/vsdbg"
fi