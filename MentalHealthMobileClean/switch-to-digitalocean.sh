#!/bin/bash

# Quick script to switch app config to DigitalOcean server

cd "$(dirname "$0")"

CONFIG_FILE="src/config/app.config.js"

# Load centralized DROPLET_IP from deploy directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DROPLET_IP_FILE="${PROJECT_ROOT}/deploy/DROPLET_IP"

if [ ! -f "$DROPLET_IP_FILE" ]; then
    echo "❌ Error: DROPLET_IP file not found at $DROPLET_IP_FILE"
    exit 1
fi

DROPLET_IP=$(cat "$DROPLET_IP_FILE" | tr -d '[:space:]')

echo "🔄 Switching to DigitalOcean server configuration..."
echo ""

# Backup current config
if [ ! -f "${CONFIG_FILE}.backup" ]; then
    cp "$CONFIG_FILE" "${CONFIG_FILE}.backup"
    echo "✅ Backed up current config to ${CONFIG_FILE}.backup"
fi

# Update config
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' \
        -e "s/SERVER_IP: '[^']*'/SERVER_IP: '$DROPLET_IP'/" \
        -e "s/SERVER_PORT: [0-9]*/SERVER_PORT: 443/" \
        -e "s/USE_HTTPS: [a-z]*/USE_HTTPS: true/" \
        "$CONFIG_FILE"
else
    # Linux
    sed -i \
        -e "s/SERVER_IP: '[^']*'/SERVER_IP: '$DROPLET_IP'/" \
        -e "s/SERVER_PORT: [0-9]*/SERVER_PORT: 443/" \
        -e "s/USE_HTTPS: [a-z]*/USE_HTTPS: true/" \
        "$CONFIG_FILE"
fi

echo "✅ Updated configuration:"
echo "   SERVER_IP: $DROPLET_IP"
echo "   SERVER_PORT: 443"
echo "   USE_HTTPS: true"
echo ""
echo "📋 Next steps:"
echo "   1. Trust certificate in Safari (Simulator):"
echo "      - Open Safari in Simulator"
echo "      - Go to: https://$DROPLET_IP/api/health"
echo "      - Accept the certificate warning"
echo ""
echo "   2. Start Metro: npx expo start --clear --localhost"
echo "   3. Run app: npx expo run:ios"
echo ""
echo "💡 To switch back to local: ./switch-to-local.sh"

