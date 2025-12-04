#!/bin/bash

# Quick script to switch app config to DigitalOcean server

cd "$(dirname "$0")"

CONFIG_FILE="src/config/app.config.js"

echo "🔄 Switching to DigitalOcean server configuration..."
echo ""

# Backup current config
if [ ! -f "${CONFIG_FILE}.backup" ]; then
    cp "$CONFIG_FILE" "${CONFIG_FILE}.backup"
    echo "✅ Backed up current config to ${CONFIG_FILE}.backup"
fi

# Update config
sed -i '' \
    -e "s/SERVER_IP: '[^']*'/SERVER_IP: '159.65.242.79'/" \
    -e "s/SERVER_PORT: [0-9]*/SERVER_PORT: 443/" \
    -e "s/USE_HTTPS: [a-z]*/USE_HTTPS: true/" \
    "$CONFIG_FILE"

echo "✅ Updated configuration:"
echo "   SERVER_IP: 159.65.242.79"
echo "   SERVER_PORT: 443"
echo "   USE_HTTPS: true"
echo ""
echo "📋 Next steps:"
echo "   1. Trust certificate in Safari (Simulator):"
echo "      - Open Safari in Simulator"
echo "      - Go to: https://159.65.242.79/api/health"
echo "      - Accept the certificate warning"
echo ""
echo "   2. Start Metro: npx expo start --clear --localhost"
echo "   3. Run app: npx expo run:ios"
echo ""
echo "💡 To switch back to local: ./switch-to-local.sh"

