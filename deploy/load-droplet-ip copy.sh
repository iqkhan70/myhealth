#!/bin/bash

# Helper script to load DROPLET_IP from centralized file
# Usage: source ./deploy/load-droplet-ip.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DROPLET_IP_FILE="${SCRIPT_DIR}/DROPLET_IP"

if [ -f "$DROPLET_IP_FILE" ]; then
    DROPLET_IP=$(cat "$DROPLET_IP_FILE" | tr -d '[:space:]')
    export DROPLET_IP
    export SERVER_IP="$DROPLET_IP"  # Alias for compatibility
else
    echo "Error: DROPLET_IP file not found at $DROPLET_IP_FILE" >&2
    exit 1
fi

