#!/bin/bash

# Script to run iOS app with Metro already running
# This prevents the timing issue where app launches before Metro is ready

cd "$(dirname "$0")"

echo "🔧 Running iOS App with Metro..."
echo ""

# Step 1: Check if Metro is already running
if curl -s http://localhost:8081/status > /dev/null 2>&1; then
    echo "✅ Metro is already running"
    echo ""
else
    echo "⚠️  Metro is not running. Starting Metro first..."
    echo ""
    
    # Kill any existing processes
    pkill -f "expo start" 2>/dev/null || true
    lsof -ti:8081 | xargs kill -9 2>/dev/null || true
    sleep 2
    
    # Start Metro in background
    echo "🚀 Starting Metro bundler..."
    npx expo start --clear --localhost > /tmp/metro-ios.log 2>&1 &
    METRO_PID=$!
    
    # Wait for Metro to be ready
    echo "⏳ Waiting for Metro to be ready..."
    MAX_WAIT=30
    WAIT_COUNT=0
    
    while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
        if curl -s http://localhost:8081/status > /dev/null 2>&1; then
            echo "✅ Metro is ready!"
            echo ""
            break
        fi
        
        if ! kill -0 $METRO_PID 2>/dev/null; then
            echo "❌ Metro failed to start. Check /tmp/metro-ios.log:"
            tail -20 /tmp/metro-ios.log
            exit 1
        fi
        
        sleep 1
        WAIT_COUNT=$((WAIT_COUNT + 1))
        echo -n "."
    done
    
    if [ $WAIT_COUNT -eq $MAX_WAIT ]; then
        echo ""
        echo "❌ Metro didn't start in time"
        tail -20 /tmp/metro-ios.log
        kill $METRO_PID 2>/dev/null || true
        exit 1
    fi
    
    echo ""
fi

# Step 2: Verify Metro is accessible
echo "🔍 Verifying Metro connection..."
if ! curl -s http://localhost:8081/status > /dev/null 2>&1; then
    echo "❌ Metro is not accessible. Please start Metro manually first:"
    echo "   npx expo start --clear --localhost"
    exit 1
fi

echo "✅ Metro is accessible"
echo ""

# Step 3: Run iOS app with EXPO_NO_METRO flag to prevent auto-start
# Actually, we want Metro to be running, so we'll just run the app
# The key is that Metro is already running and ready

echo "📱 Launching iOS app..."
echo "   (Metro should already be running)"
echo ""

# Set environment variable to tell Expo that Metro is already running
export EXPO_NO_METRO_START=1

# Run iOS app
npx expo run:ios --no-build-cache

echo ""
echo "💡 If you see the error again, try:"
echo "   1. Wait a few more seconds for Metro to fully initialize"
echo "   2. Shake device → Reload in the simulator"
echo "   3. Or manually start Metro first: npx expo start --clear --localhost"

