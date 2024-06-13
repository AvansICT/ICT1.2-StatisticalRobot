ARG BASE_IMAGE=debian:bullseye

FROM ${BASE_IMAGE}

ENV DEBIAN_FRONTEND noninteractive

RUN apt-get -y update && \
    apt-get -y install --no-install-recommends \
        git vim parted \
        quilt coreutils qemu-user-static debootstrap zerofree zip dosfstools \
        libarchive-tools libcap2-bin rsync grep udev xz-utils curl xxd file kmod bc \
        binfmt-support ca-certificates fdisk gpg pigz arch-test \
    && rm -rf /var/lib/apt/lists/*

ARG DOTNET_VERSION="8.0"
ENV DOTNET_VERSION=$DOTNET_VERSION
ENV DOTNET_ROOT="/opt/dotnet"
ENV PATH="${PATH}:${DOTNET_ROOT}"

RUN curl -sSL https://dot.net/v1/dotnet-install.sh > /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel "${DOTNET_VERSION}" --install-dir "${DOTNET_ROOT}"

COPY . /pi-gen/

VOLUME [ "/pi-gen/work", "/pi-gen/deploy"]
