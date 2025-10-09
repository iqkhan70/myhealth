#!/bin/bash

echo "🚀 Starting Mental Health Mobile App with optimized settings..."

# Increase file descriptor limits
ulimit -n 1048576

# Navigate to the mobile app directory
cd /Users/mohammedkhan/iq/health/MentalHealthMobileV54

# Clean any existing Metro cache
echo "🧹 Cleaning Metro cache..."

# Set environment variables to reduce file watching
export WATCHMAN_NO_LOCAL=1
export EXPO_NO_DOTENV=1
export CI=true

# Try starting with reduced file watching
echo "🚀 Starting with optimized file watching..."
npx expo start --clear --offline

echo "✅ App started successfully!"
echo "💡 If you still get EMFILE errors, run: sudo ./fix-macos-limits.sh"
