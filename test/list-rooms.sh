#!/bin/bash

# Script to list all rooms and their grouped light IDs from Hue API v2
# Reads configuration from appsettings.Production.json

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="${SCRIPT_DIR}/../src/Home.Pi.Daemon/appsettings.Production.json"

if [ ! -f "$CONFIG_FILE" ]; then
    echo "Error: Configuration file not found at $CONFIG_FILE" >&2
    exit 1
fi

# Extract Hue configuration from JSON
HUE_BASE=$(jq -r '.HueLightControllerOptions.HueBaseAddress' "$CONFIG_FILE")
HUE_KEY=$(jq -r '.HueLightControllerOptions.HueApplicationKey' "$CONFIG_FILE")

if [ "$HUE_BASE" == "null" ] || [ "$HUE_KEY" == "null" ]; then
    echo "Error: Could not read Hue configuration from $CONFIG_FILE" >&2
    exit 1
fi

echo "Querying Hue API for all rooms..."
echo ""

# Query all rooms and display their information
curl -k -s \
  -H "hue-application-key: ${HUE_KEY}" \
  "${HUE_BASE}resource/room" | \
  jq -r '.data[] | "Room: \(.metadata.name)\n  Room ID: \(.id)\n  Grouped Light ID: \(.services[]? | select(.rtype == "grouped_light") | .rid // "N/A")\n"'
