#!/bin/bash

# Script to update mobile app config with current DROPLET_IP

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DROPLET_IP_FILE="${SCRIPT_DIR}/DROPLET_IP"
MOBILE_CONFIG="${PROJECT_ROOT}/MentalHealthMobileClean/src/config/app.config.js"

if [ ! -f "$DROPLET_IP_FILE" ]; then
    echo "Error: DROPLET_IP file not found at $DROPLET_IP_FILE"
    exit 1
fi

if [ ! -f "$MOBILE_CONFIG" ]; then
    echo "Error: Mobile config file not found at $MOBILE_CONFIG"
    exit 1
fi

DROPLET_IP=$(cat "$DROPLET_IP_FILE" | tr -d '[:space:]')

echo "Updating mobile app config with DROPLET_IP: $DROPLET_IP"

# Update SERVER_IP in app.config.js
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s/SERVER_IP: '[^']*'/SERVER_IP: '$DROPLET_IP'/" "$MOBILE_CONFIG"
else
    # Linux
    sed -i "s/SERVER_IP: '[^']*'/SERVER_IP: '$DROPLET_IP'/" "$MOBILE_CONFIG"
fi

echo "✅ Mobile app config updated"
echo "   SERVER_IP: $DROPLET_IP"

