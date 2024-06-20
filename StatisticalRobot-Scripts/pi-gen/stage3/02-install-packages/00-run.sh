#!/bin/bash -e

# Install dotnet to /opt/dotnet
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel ${DOTNET_VERSION} --install-dir "${ROOTFS_DIR}/opt/dotnet" --architecture "arm64" --os "linux"

# Install vsdbg to /opt/vsdbg
curl -sSL https://aka.ms/getvsdbgsh | bash -s -- -v latest -r linux-arm64 -l "${ROOTFS_DIR}/opt/vsdbg"