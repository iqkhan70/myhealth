#!/bin/bash

echo "🚀 Starting Mental Health Mobile App with ULTRA-safe settings..."

# Kill any existing processes
echo "🧹 Cleaning up existing processes..."
pkill -f "expo\|metro" 2>/dev/null || true
sleep 2

# Set maximum file descriptor limits for current session
echo "🔧 Setting file descriptor limits..."
ulimit -n 1048576
echo "✅ File descriptor limit set to $(ulimit -n)"

# Set environment variables to minimize file watching
export WATCHMAN_NO_LOCAL=1
export EXPO_NO_DOTENV=1
export CI=true
export EXPO_OFFLINE=1

# Navigate to the mobile app directory
cd /Users/mohammedkhan/iq/health/MentalHealthMobile

# Clean Metro cache
echo "🧹 Cleaning Metro cache..."
npx expo start --clear --offline --no-dev --minify

echo "✅ App should be starting with minimal file watching..."
echo "💡 If you still get EMFILE errors, try:"
echo "   1. Close this terminal completely"
echo "   2. Open a new terminal"
echo "   3. Run this script again"
