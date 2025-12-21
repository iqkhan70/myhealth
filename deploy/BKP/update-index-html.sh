#!/bin/bash

# Quick script to update only index.html on the server

# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo "🔄 Updating index.html on server..."
echo ""

# Fix SSH key permissions
chmod 600 "$SSH_KEY_PATH" 2>/dev/null

# Copy the updated index.html to server
scp -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no \
    SM_MentalHealthApp.Client/wwwroot/index.html \
    root@$DROPLET_IP:$APP_DIR/client/wwwroot/index.html

if [ $? -eq 0 ]; then
    echo "✅ index.html updated successfully!"
    echo ""
    echo "ℹ️  Note: Since index.html is a static file, you may need to:"
    echo "   1. Hard refresh the browser (Ctrl+Shift+R or Cmd+Shift+R)"
    echo "   2. Or clear browser cache"
    echo ""
    echo "🧪 Test: Open https://$DROPLET_IP and check browser console"
    echo "   ServiceWorker errors should be gone!"
else
    echo "❌ Failed to update index.html"
    exit 1
fi

