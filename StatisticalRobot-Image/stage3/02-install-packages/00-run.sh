#!/bin/bash -e

# IMPORTANT!!!!
# If you modify this file using windows, MAKE SURE that you save it in linux format
# This means, use '\n' (line-feat, LF) as new-line and not windows: '\r\n' (Criage-Return+Line-feat, CRLF)!!!
# THIS IS VERY IMPORTANT, or else this file won't execute in the image

# Install dotnet to /opt/dotnet
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel ${DOTNET_VERSION} --install-dir "${ROOTFS_DIR}/opt/dotnet" --architecture "arm64" --os "linux"

# Install vsdbg to /opt/vsdbg
curl -sSL https://aka.ms/getvsdbgsh | bash -s -- -v latest -r linux-arm64 -l "${ROOTFS_DIR}/opt/vsdbg"