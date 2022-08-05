ARG VARIANT="buster"
FROM mcr.microsoft.com/vscode/devcontainers/base:0-${VARIANT}

USER vscode
RUN mkdir /tmp/dockerbuild \
    && cd /tmp/dockerbuild \
    && curl -sL https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -O \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 6.0 \
    && ./dotnet-install.sh --channel 7.0 \
    && rm -rf /tmp/dockerbuild
ENV PATH ${PATH}:/home/vscode/.dotnet
USER root


RUN curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /etc/apt/trusted.gpg.d/microsoft.gpg \
    && echo "deb [arch=amd64] https://packages.microsoft.com/debian/$(lsb_release -rs | cut -d'.' -f 1)/prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list \
    && apt-get update && apt-get install -y --no-install-recommends \
        azure-functions-core-tools-4 \
        powershell \
    && apt-get clean
