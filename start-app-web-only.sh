#!/bin/bash

echo "🌐 Starting Mental Health App in WEB-ONLY mode (no file watching issues)..."

# Kill any existing processes
pkill -f "expo\|metro" 2>/dev/null || true
sleep 2

# Navigate to the simple app
cd /Users/mohammedkhan/iq/health/MentalHealthMobileSimple2

# Set environment variables to minimize file operations
export WATCHMAN_NO_LOCAL=1
export EXPO_NO_DOTENV=1
export CI=true
export EXPO_OFFLINE=1

echo "🚀 Starting in web-only mode..."
npx expo start --web --offline

echo "✅ App should be running at http://localhost:8081"
echo "📱 Open this URL in your phone's browser for mobile experience"
