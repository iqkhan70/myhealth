#!/bin/bash

echo "🌐 Starting Mental Health App in WEB-ONLY mode (no file watching)..."

# Kill any existing processes
pkill -f "expo\|metro" 2>/dev/null || true
sleep 2

# Set environment variables
export WATCHMAN_NO_LOCAL=1
export EXPO_NO_DOTENV=1
export CI=true
export EXPO_OFFLINE=1

# Navigate to the mobile app directory
cd /Users/mohammedkhan/iq/health/MentalHealthMobile

echo "🚀 Starting in web mode only..."
npx expo start --web --offline --no-dev

echo "✅ App should be running at http://localhost:8081"
