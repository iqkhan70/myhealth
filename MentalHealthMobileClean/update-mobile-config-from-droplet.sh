#!/bin/bash

# Script to update mobile app config with current DROPLET_IP
# This reads from deploy/DROPLET_IP and updates app.config.js

cd "$(dirname "$0")"

CONFIG_FILE="src/config/app.config.js"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DROPLET_IP_FILE="${PROJECT_ROOT}/deploy/DROPLET_IP"

if [ ! -f "$DROPLET_IP_FILE" ]; then
    echo "❌ Error: DROPLET_IP file not found at $DROPLET_IP_FILE"
    exit 1
fi

DROPLET_IP=$(cat "$DROPLET_IP_FILE" | tr -d '[:space:]')

echo "🔄 Updating mobile app config with DROPLET_IP: $DROPLET_IP"
echo ""

# Update SERVER_IP in app.config.js
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s/SERVER_IP: '[^']*'/SERVER_IP: '$DROPLET_IP'/" "$CONFIG_FILE"
else
    # Linux
    sed -i "s/SERVER_IP: '[^']*'/SERVER_IP: '$DROPLET_IP'/" "$CONFIG_FILE"
fi

echo "✅ Mobile app config updated"
echo "   SERVER_IP: $DROPLET_IP"
echo ""
echo "💡 To switch to DigitalOcean (HTTPS on port 443):"
echo "   ./switch-to-digitalocean.sh"
echo ""
echo "💡 To switch back to local:"
echo "   ./switch-to-local.sh"

