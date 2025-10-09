#!/bin/bash

# Script to automatically update the network IP in the mobile app
# This helps when your network IP changes (common with DHCP)

# Get the current network IP (excluding localhost)
CURRENT_IP=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | head -1 | awk '{print $2}')

if [ -z "$CURRENT_IP" ]; then
    echo "❌ Could not detect network IP address"
    exit 1
fi

echo "🌐 Detected network IP: $CURRENT_IP"

# Update the App.js file with the new IP
if [ -f "App.js" ]; then
    # Create backup
    cp App.js App.js.backup
    
    # Replace the IP in the file
    sed -i '' "s/http:\/\/192\.168\.[0-9]*\.[0-9]*:5262\/api/http:\/\/$CURRENT_IP:5262\/api/g" App.js
    
    echo "✅ Updated App.js with network IP: $CURRENT_IP"
    echo "📱 You can now test the mobile app on your device"
    echo ""
    echo "🔍 To verify the server is accessible:"
    echo "   curl http://$CURRENT_IP:5262/api/auth/login -X POST -H 'Content-Type: application/json' -d '{\"email\":\"john@doe.com\",\"password\":\"demo123\"}'"
    echo ""
    echo "📋 Server should be running with:"
    echo "   cd ../SM_MentalHealthApp.Server && dotnet run --urls \"http://0.0.0.0:5262;https://0.0.0.0:5443\""
else
    echo "❌ App.js not found in current directory"
    exit 1
fi
