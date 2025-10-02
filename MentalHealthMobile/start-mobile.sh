#!/bin/bash

# Script to start React Native app with file watcher optimizations

echo "🔧 Optimizing system for React Native development..."

# Increase file descriptor limits for current session
ulimit -n 10240
echo "✅ File descriptor limit increased to $(ulimit -n)"

# Set environment variables to reduce file watching
export WATCHMAN_NO_LOCAL=1
export EXPO_NO_DOTENV=1
export CI=true

echo "🚀 Starting React Native app..."

# Try different startup methods in order of preference
echo "📱 Attempting to start mobile app..."

# Method 1: Try with minimal file watching
if npx expo start --offline --no-dev 2>/dev/null; then
    echo "✅ Started with offline mode"
elif npx expo start --web; then
    echo "✅ Started in web mode (mobile-responsive)"
else
    echo "❌ Failed to start app"
    echo "💡 Try running manually: npx expo start --web"
fi
