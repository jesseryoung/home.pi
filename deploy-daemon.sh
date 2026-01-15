#!/bin/bash

set -euo pipefail

# Parse arguments
if [ $# -lt 1 ]; then
    echo "Usage: $0 <ServerName> [User]"
    echo "  ServerName: The hostname or IP address of the server (required)"
    echo "  User: The SSH user (default: pi)"
    exit 1
fi

SERVER_NAME="$1"
USER="${2:-pi}"

# Configuration
REMOTE_DIRECTORY="/home/pi/home.pi.daemon"
EXECUTABLE_NAME="Home.Pi.Daemon"

echo "Removing old build output..."
rm -rf ./out

echo "Building...."
dotnet publish ./src/Home.Pi.Daemon -o ./out --self-contained --runtime linux-arm64
if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

# Stop running services
for service in ./out/systemd/*.service; do
    if [ -f "$service" ]; then
        SERVICE_NAME=$(basename "$service")
        # Check if service exists and is running
        if ssh "$USER@$SERVER_NAME" sudo systemctl is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
            echo "Stopping $SERVICE_NAME on $SERVER_NAME...."
            if ! ssh "$USER@$SERVER_NAME" sudo systemctl stop "$SERVICE_NAME"; then
                echo "Failed to stop $SERVICE_NAME on $SERVER_NAME."
                exit 1
            fi
        fi
    fi
done

echo "Deploying...."
ssh "$USER@$SERVER_NAME" "rm -rf $REMOTE_DIRECTORY" && \
    ssh "$USER@$SERVER_NAME" "mkdir -p $REMOTE_DIRECTORY" && \
    scp -r -o "User=$USER" ./out/* "$SERVER_NAME:$REMOTE_DIRECTORY/" && \
    ssh "$USER@$SERVER_NAME" "chmod +x $REMOTE_DIRECTORY/$EXECUTABLE_NAME" && \
    ssh "$USER@$SERVER_NAME" "chmod +x $REMOTE_DIRECTORY/systemd/*.service" && \
    ssh "$USER@$SERVER_NAME" "sudo ln -sf $REMOTE_DIRECTORY/systemd/*.service /etc/systemd/system/"

if [ $? -ne 0 ]; then
    echo "Deployment failed!"
    exit 1
fi

if ! ssh "$USER@$SERVER_NAME" sudo systemctl daemon-reload; then
    echo "Failed to reload services on $SERVER_NAME."
    exit 1
fi

# Restart services
for service in ./out/systemd/*.service; do
    if [ -f "$service" ]; then
        SERVICE_NAME=$(basename "$service")
        echo "Restarting $SERVICE_NAME...."
        if ! ssh "$USER@$SERVER_NAME" sudo systemctl start "$SERVICE_NAME"; then
            echo "Failed to restart $SERVICE_NAME on $SERVER_NAME."
            exit 1
        fi
    fi
done

echo "Deployment completed successfully!"
