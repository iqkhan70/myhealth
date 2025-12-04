#!/bin/bash

# Quick script to switch app config back to local server

cd "$(dirname "$0")"

CONFIG_FILE="src/config/app.config.js"

echo "🔄 Switching to local server configuration..."
echo ""

# Restore from backup if available
if [ -f "${CONFIG_FILE}.backup" ]; then
    cp "${CONFIG_FILE}.backup" "$CONFIG_FILE"
    echo "✅ Restored from backup"
else
    # Update config manually
    sed -i '' \
        -e "s/SERVER_IP: '[^']*'/SERVER_IP: '192.168.86.25'/" \
        -e "s/SERVER_PORT: [0-9]*/SERVER_PORT: 5262/" \
        -e "s/USE_HTTPS: [a-z]*/USE_HTTPS: false/" \
        "$CONFIG_FILE"
    echo "✅ Updated configuration"
fi

echo ""
echo "✅ Updated configuration:"
echo "   SERVER_IP: 192.168.86.25"
echo "   SERVER_PORT: 5262"
echo "   USE_HTTPS: false"
echo ""
echo "📋 Next steps:"
echo "   1. Start Metro: npx expo start --clear --localhost"
echo "   2. Run app: npx expo run:ios"
echo ""
echo "💡 To switch to DigitalOcean: ./switch-to-digitalocean.sh"

