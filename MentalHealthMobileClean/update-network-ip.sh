#!/bin/bash

# Script to automatically update the network IP in the mobile app's config file
# This helps when your network IP changes (common with DHCP)
# Usage: ./update-network-ip.sh [IP_ADDRESS]
# If no IP is provided, it will detect the current IP automatically

# Get the current network IP if not provided
if [ -z "$1" ]; then
  CURRENT_IP=$(ipconfig getifaddr en0 2>/dev/null || ipconfig getifaddr en1 2>/dev/null || ifconfig | grep "inet " | grep -v 127.0.0.1 | head -1 | awk '{print $2}')
  if [ -z "$CURRENT_IP" ]; then
    echo "❌ Could not detect network IP address"
    echo "💡 Please provide IP manually: ./update-network-ip.sh [IP_ADDRESS]"
    exit 1
  fi
  echo "🔍 Detected network IP: $CURRENT_IP"
else
  CURRENT_IP="$1"
  echo "📝 Using provided IP: $CURRENT_IP"
fi

# Config file location
CONFIG_FILE="src/config/app.config.js"

if [ ! -f "$CONFIG_FILE" ]; then
  echo "❌ Config file not found: $CONFIG_FILE"
  exit 1
fi

# Update the SERVER_IP in the config file
# Handle both single and double quotes
if [[ "$OSTYPE" == "darwin"* ]]; then
  # macOS
  sed -i '' "s/SERVER_IP: '[0-9.]*'/SERVER_IP: '$CURRENT_IP'/g" "$CONFIG_FILE"
  sed -i '' "s/SERVER_IP: \"[0-9.]*\"/SERVER_IP: \"$CURRENT_IP\"/g" "$CONFIG_FILE"
else
  # Linux
  sed -i "s/SERVER_IP: '[0-9.]*'/SERVER_IP: '$CURRENT_IP'/g" "$CONFIG_FILE"
  sed -i "s/SERVER_IP: \"[0-9.]*\"/SERVER_IP: \"$CURRENT_IP\"/g" "$CONFIG_FILE"
fi

echo "✅ Updated IP address to $CURRENT_IP in:"
echo "   - $CONFIG_FILE"
echo ""
echo "💡 All API calls will now use: https://$CURRENT_IP:5262/api"
echo ""
echo "📱 You can now test the mobile app on your device"
echo ""
echo "⚠️  NOTE: Server uses HTTPS on port 5262. Make sure to:"
echo "   1. Accept the self-signed certificate in browser first"
echo "   2. For mobile: May need to configure SSL certificate trust"
echo ""
echo "🔍 To verify the server is accessible:"
echo "   curl -k https://$CURRENT_IP:5262/api/auth/login -X POST -H 'Content-Type: application/json' -d '{\"email\":\"test@example.com\",\"password\":\"password\"}'"
echo ""
echo "📋 Server should be running with HTTPS:"
echo "   cd ../SM_MentalHealthApp.Server && dotnet run --launch-profile https"
