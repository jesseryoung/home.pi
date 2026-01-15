FROM mcr.microsoft.com/devcontainers/base:debian


RUN cd /tmp/ \
    && curl -sSL https://www.emqx.com/en/downloads/MQTTX/v1.12.1/mqttx-cli-linux-x64 -o mqttx-cli-linux-x64 \
    && install ./mqttx-cli-linux-x64 /usr/local/bin/mqttx \
    && rm ./mqttx-cli-linux-x64