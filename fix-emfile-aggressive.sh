#!/bin/bash

echo "😤 AGGRESSIVE EMFILE FIX - We're going to solve this once and for all!"

# Kill everything first
echo "🧹 Killing all processes..."
pkill -f "expo\|metro\|node.*8081" 2>/dev/null || true
sleep 2

# Set maximum limits for current session
echo "🔧 Setting maximum file descriptor limits..."
ulimit -n 65536
echo "✅ Current limit: $(ulimit -n)"

# Set system-wide limits with sudo
echo "🔧 Setting system-wide limits..."
sudo launchctl limit maxfiles 65536 65536

# Create a more aggressive launchd plist
echo "📦 Creating aggressive launchd configuration..."
sudo tee /Library/LaunchDaemons/limit.maxfiles.aggressive.plist > /dev/null <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>Label</key>
    <string>limit.maxfiles.aggressive</string>
    <key>ProgramArguments</key>
    <array>
      <string>launchctl</string>
      <string>limit</string>
      <string>maxfiles</string>
      <string>65536</string>
      <string>65536</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>ServiceIPC</key>
    <false/>
  </dict>
</plist>
EOF

# Load the configuration
echo "📦 Loading configuration..."
if launchctl bootstrap --help >/dev/null 2>&1; then
    sudo launchctl bootstrap system /Library/LaunchDaemons/limit.maxfiles.aggressive.plist
else
    sudo launchctl load /Library/LaunchDaemons/limit.maxfiles.aggressive.plist
fi

# Apply to current session
sudo launchctl limit maxfiles 65536 65536

echo "✅ AGGRESSIVE FIX APPLIED!"
echo "📊 Current limits:"
launchctl limit maxfiles

echo ""
echo "🔄 PLEASE RESTART YOUR TERMINAL NOW!"
echo "Then run: cd /Users/mohammedkhan/iq/health/MentalHealthMobileSimple2 && npx expo start --tunnel"
