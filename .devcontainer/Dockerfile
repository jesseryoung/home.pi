FROM mcr.microsoft.com/devcontainers/base:bookworm


RUN cd /tmp/ \
    && curl -sSL https://www.emqx.com/en/downloads/MQTTX/v1.10.1/mqttx-cli-linux-x64 -o mqttx-cli-linux-x64 \
    && install ./mqttx-cli-linux-x64 /usr/local/bin/mqttx \
    && rm ./mqttx-cli-linux-x64

RUN cd /tmp/ \
    && wget -q https://github.com/PowerShell/PowerShell/releases/download/v7.4.4/powershell_7.4.4-1.deb_amd64.deb \
    && dpkg -i powershell_7.4.4-1.deb_amd64.deb \
    && rm powershell_7.4.4-1.deb_amd64.deb