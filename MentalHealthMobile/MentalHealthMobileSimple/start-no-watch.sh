#!/bin/bash

echo "🚀 Starting React Native app without file watchers..."

# Set environment variables to disable file watching
export NODE_ENV=production
export EXPO_NO_DOTENV=1
export WATCHMAN_NO_LOCAL=1
export CI=true
export DISABLE_ESLINT_PLUGIN=true

# Increase file descriptor limit
ulimit -n 65536

echo "📱 Environment configured for minimal file watching"

# Start with production-like settings
echo "🔧 Starting Expo with minimal file operations..."

# Try different methods in order of preference
if command -v expo &> /dev/null; then
    echo "✅ Expo CLI found, starting app..."
    
    # Method 1: Production mode with no dev features
    echo "📦 Attempting production mode..."
    npx expo start --no-dev --minify --clear --offline 2>/dev/null &
    EXPO_PID=$!
    
    # Wait a moment to see if it starts successfully
    sleep 5
    
    if kill -0 $EXPO_PID 2>/dev/null; then
        echo "✅ App started successfully in production mode!"
        echo "📱 Scan the QR code with Expo Go app"
        wait $EXPO_PID
    else
        echo "⚠️  Production mode failed, trying web mode..."
        npx expo start --web --clear
    fi
else
    echo "❌ Expo CLI not found. Please install with: npm install -g @expo/cli"
    exit 1
fi
