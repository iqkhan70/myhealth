#!/bin/bash

# Fix Metro Bundler Connection Issue
# This script restarts Metro with proper configuration so the app can connect

cd "$(dirname "$0")"

echo "🔧 Fixing Metro Bundler Connection..."
echo ""

# Step 1: Kill existing Metro processes
echo "🛑 Stopping existing Metro processes..."
pkill -f "expo start" 2>/dev/null || true
pkill -f "metro" 2>/dev/null || true
sleep 2

# Step 2: Clear caches
echo "🧹 Clearing caches..."
rm -rf .expo 2>/dev/null
rm -rf node_modules/.cache 2>/dev/null
rm -rf ios/build 2>/dev/null
echo "✅ Caches cleared"

# Step 3: Get network IP
NETWORK_IP=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | awk '{print $2}' | head -1)
if [ -z "$NETWORK_IP" ]; then
    NETWORK_IP="192.168.86.34"
fi

echo ""
echo "🌐 Network IP: $NETWORK_IP"
echo ""
echo "🚀 Starting Metro bundler..."
echo "   - This will start Metro on port 8081"
echo "   - The app should be able to connect automatically"
echo "   - If not, reload the app (shake device → Reload)"
echo ""

# Start Metro with LAN host and clear cache
npx expo start --clear --host lan

