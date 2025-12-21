#!/bin/bash

# Master script to update all IP references from DROPLET_IP file
# This updates mobile config and provides instructions for other files

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DROPLET_IP_FILE="${SCRIPT_DIR}/DROPLET_IP"

if [ ! -f "$DROPLET_IP_FILE" ]; then
    echo "❌ Error: DROPLET_IP file not found at $DROPLET_IP_FILE"
    echo "   Please create it first: echo 'YOUR_IP' > deploy/DROPLET_IP"
    exit 1
fi

DROPLET_IP=$(cat "$DROPLET_IP_FILE" | tr -d '[:space:]')

echo "🔄 Updating all IP references to: $DROPLET_IP"
echo ""

# Update mobile app config
echo "📱 Updating mobile app config..."
"$SCRIPT_DIR/update-mobile-config.sh"

echo ""
echo "✅ IP update complete!"
echo ""
echo "📋 Summary:"
echo "   - Mobile app config: ✅ Updated"
echo "   - GitHub Actions workflows: ✅ Auto-read from DROPLET_IP"
echo "   - Deployment scripts: Use load-droplet-ip.sh"
echo ""
echo "💡 Note: Some files (like documentation) may still reference the old IP."
echo "   These don't affect functionality and can be updated as needed."

