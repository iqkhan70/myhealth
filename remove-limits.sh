#!/bin/bash

echo "🗑️  Removing file descriptor limits..."

# Unload the launchd service
echo "📦 Unloading launchd service..."
if launchctl bootstrap --help >/dev/null 2>&1; then
    sudo launchctl bootout system /Library/LaunchDaemons/limit.maxfiles.plist 2>/dev/null || true
else
    sudo launchctl unload /Library/LaunchDaemons/limit.maxfiles.plist 2>/dev/null || true
fi

# Remove the plist file
echo "🗑️  Removing configuration file..."
sudo rm -f /Library/LaunchDaemons/limit.maxfiles.plist

# Reset limits to default
echo "🔄 Resetting to default limits..."
sudo launchctl limit maxfiles 256 1024

echo "✅ File descriptor limits removed and reset to defaults"
echo "📊 Current limits:"
launchctl limit maxfiles

echo ""
echo "🔄 Please restart your terminal for changes to take effect"
