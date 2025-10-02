#!/bin/bash

echo "🔧 Fixing macOS file descriptor limits for React Native..."

# Clean up any existing configuration first
echo "🧹 Cleaning up existing configuration..."
if [ -f /Library/LaunchDaemons/limit.maxfiles.plist ]; then
    if launchctl bootstrap --help >/dev/null 2>&1; then
        sudo launchctl bootout system /Library/LaunchDaemons/limit.maxfiles.plist 2>/dev/null || true
    else
        sudo launchctl unload /Library/LaunchDaemons/limit.maxfiles.plist 2>/dev/null || true
    fi
fi

# Create launchd plist to increase limits permanently
sudo tee /Library/LaunchDaemons/limit.maxfiles.plist > /dev/null <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>Label</key>
    <string>limit.maxfiles</string>
    <key>ProgramArguments</key>
    <array>
      <string>launchctl</string>
      <string>limit</string>
      <string>maxfiles</string>
      <string>1048576</string>
      <string>1048576</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>ServiceIPC</key>
    <false/>
  </dict>
</plist>
EOF

# Load the new limits (use bootstrap for newer macOS versions)
echo "📦 Loading launchd configuration..."
if launchctl bootstrap --help >/dev/null 2>&1; then
    if sudo launchctl bootstrap system /Library/LaunchDaemons/limit.maxfiles.plist; then
        echo "✅ LaunchDaemon loaded successfully with bootstrap"
    else
        echo "⚠️  Bootstrap failed, trying alternative method..."
        sudo launchctl load -w /Library/LaunchDaemons/limit.maxfiles.plist
    fi
else
    if sudo launchctl load -w /Library/LaunchDaemons/limit.maxfiles.plist; then
        echo "✅ LaunchDaemon loaded successfully with load"
    else
        echo "❌ Failed to load LaunchDaemon"
        exit 1
    fi
fi

# Apply limits to current session
sudo launchctl limit maxfiles 1048576 1048576

echo "✅ macOS file limits increased permanently"
echo "🔄 Please restart your terminal or reboot for full effect"

# Show current limits
echo "📊 Current limits:"
launchctl limit maxfiles
