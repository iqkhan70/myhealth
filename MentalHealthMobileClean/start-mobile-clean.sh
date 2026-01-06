#!/bin/bash

# Clean Mobile App Startup Script
# This script ensures proper environment setup for the mobile app

echo "🚀 Starting Clean Customer Helpline App..."

# Set environment variables for better performance
export WATCHMAN_NO_LOCAL=1
export EXPO_IMAGE_UTILS_NO_SHARP=1

# Increase file descriptor limits (macOS specific)
ulimit -n 65536

# Clear any existing Metro cache
echo "🧹 Clearing Metro cache..."
npx expo start --clear --host lan --port 8081

echo "✅ Clean mobile app started successfully!"
echo "📱 You can now:"
echo "   - Scan QR code with Expo Go app on your phone"
echo "   - Press 'i' for iOS Simulator"
echo "   - Press 'a' for Android Emulator"
echo "   - Press 'w' for Web browser"
