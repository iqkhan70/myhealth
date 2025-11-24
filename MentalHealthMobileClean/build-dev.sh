#!/bin/bash

# Quick script to build development version of the app
# Usage: ./build-dev.sh [ios|android]

set -e

PLATFORM=${1:-ios}

echo "🚀 Building development build for $PLATFORM..."
echo ""

if [ "$PLATFORM" = "ios" ]; then
    echo "📱 Building for iOS..."
    echo ""
    echo "⚠️  Make sure:"
    echo "   • iOS Simulator is running (or device connected)"
    echo "   • CocoaPods are installed: sudo gem install cocoapods"
    echo ""
    read -p "Press Enter to continue or Ctrl+C to cancel..."
    
    # Install pods if needed
    if [ ! -d "ios/Pods" ]; then
        echo "📦 Installing CocoaPods dependencies..."
        cd ios
        pod install
        cd ..
    fi
    
    echo "🔨 Building iOS app..."
    npx expo run:ios
    
elif [ "$PLATFORM" = "android" ]; then
    echo "🤖 Building for Android..."
    echo ""
    echo "⚠️  Make sure:"
    echo "   • Android emulator is running (or device connected via USB)"
    echo "   • ANDROID_HOME is set in your environment"
    echo ""
    read -p "Press Enter to continue or Ctrl+C to cancel..."
    
    echo "🔨 Building Android app..."
    npx expo run:android
    
else
    echo "❌ Invalid platform: $PLATFORM"
    echo "Usage: ./build-dev.sh [ios|android]"
    exit 1
fi

echo ""
echo "✅ Build complete!"
echo "📱 The app should now be installed and running on your device/simulator"
echo ""
echo "💡 Tips:"
echo "   • Press 'r' in the Metro terminal to reload"
echo "   • Press 'R' to reload and clear cache"
echo "   • Shake device (or Cmd+D on iOS) for developer menu"

