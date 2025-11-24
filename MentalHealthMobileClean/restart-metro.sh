#!/bin/bash

# Script to restart Metro bundler with proper configuration

echo "🛑 Stopping any existing Metro bundler..."
pkill -f "expo start" || pkill -f "metro" || true
sleep 2

echo "🧹 Clearing Metro cache..."
cd "$(dirname "$0")"
rm -rf .expo
rm -rf node_modules/.cache

echo "🚀 Starting Metro bundler..."
echo ""
echo "💡 Tips:"
echo "   - Press 'r' to reload app"
echo "   - Press 'R' to reload and clear cache"
echo "   - Press 'm' to toggle menu"
echo ""

# Start Metro with LAN host so simulator can connect
npx expo start --clear --host lan

