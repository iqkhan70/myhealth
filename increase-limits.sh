#!/bin/bash

echo "🔧 Increasing file descriptor limits for current session..."

# Increase ulimit for current session
ulimit -n 1048576

# Try to increase launchctl limits (may require password)
echo "Attempting to increase system limits..."
launchctl limit maxfiles 1048576 1048576 2>/dev/null || echo "⚠️  System limits require sudo - run 'sudo launchctl limit maxfiles 1048576 1048576'"

echo "✅ Current session limits increased"
echo "📊 Current ulimit: $(ulimit -n)"
echo "📊 Current launchctl limits:"
launchctl limit maxfiles

echo ""
echo "🚀 You can now run your React Native project!"
echo "💡 For permanent fix, run: sudo ./fix-macos-limits.sh"
