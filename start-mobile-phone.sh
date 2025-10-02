#!/bin/bash

echo "📱 Starting Mental Health Mobile App for Phone..."

# Kill any existing processes
pkill -f "expo\|metro" 2>/dev/null || true
sleep 2

# Navigate to the mobile app directory
cd /Users/mohammedkhan/iq/health/MentalHealthMobile

# Set environment variables to reduce file watching
export WATCHMAN_NO_LOCAL=1
export EXPO_NO_DOTENV=1

echo "🚀 Starting Expo development server..."
echo "📱 Scan the QR code with Expo Go app on your phone"
echo "🌐 Or press 'w' to open in web browser"

# Start with tunnel mode for easier phone access
npx expo start --tunnel

echo "✅ App should be running!"
echo "💡 If you get EMFILE errors, try: npx expo start --offline"
