#!/bin/bash

# Fix bundle URL issue - ensures Metro is properly configured

cd "$(dirname "$0")"

echo "🔧 Fixing Metro Bundle URL Issue..."
echo ""

# Step 1: Kill everything
echo "🛑 Stopping all processes..."
pkill -f "expo" 2>/dev/null || true
pkill -f "metro" 2>/dev/null || true
lsof -ti:8081 | xargs kill -9 2>/dev/null || true
sleep 3

# Step 2: Clear all caches
echo "🧹 Clearing all caches..."
rm -rf .expo 2>/dev/null || true
rm -rf node_modules/.cache 2>/dev/null || true
rm -rf ios/build 2>/dev/null || true
rm -rf ~/Library/Developer/Xcode/DerivedData/* 2>/dev/null || true
echo "✅ Caches cleared"

# Step 3: Reset simulator
echo ""
echo "🔄 Resetting simulator..."
xcrun simctl shutdown all 2>/dev/null || true
sleep 2

# Step 4: Start Metro with explicit localhost
echo ""
echo "🚀 Starting Metro with explicit localhost configuration..."
echo ""

# Start Metro and capture output
npx expo start --clear --localhost 2>&1 | tee /tmp/metro-start.log &
METRO_PID=$!

# Wait for Metro to be ready
echo "⏳ Waiting for Metro to initialize..."
MAX_WAIT=45
WAIT_COUNT=0
METRO_READY=false

while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
    # Check if Metro is responding
    if curl -s http://localhost:8081/status > /dev/null 2>&1; then
        # Also check if bundle endpoint works
        if curl -s "http://localhost:8081/index.bundle?platform=ios&dev=true" > /dev/null 2>&1; then
            METRO_READY=true
            break
        fi
    fi
    
    # Check if Metro process died
    if ! kill -0 $METRO_PID 2>/dev/null; then
        echo ""
        echo "❌ Metro process died. Check /tmp/metro-start.log:"
        tail -30 /tmp/metro-start.log
        exit 1
    fi
    
    sleep 1
    WAIT_COUNT=$((WAIT_COUNT + 1))
    echo -n "."
done

echo ""

if [ "$METRO_READY" = true ]; then
    echo "✅ Metro is ready and bundle URL is accessible!"
    echo ""
    echo "📱 Now you can run the app:"
    echo "   npx expo run:ios"
    echo ""
    echo "💡 Keep Metro running in this terminal"
    echo "💡 Open a NEW terminal to run: npx expo run:ios"
else
    echo "❌ Metro didn't become ready in time"
    echo "Check /tmp/metro-start.log for errors:"
    tail -30 /tmp/metro-start.log
    kill $METRO_PID 2>/dev/null || true
    exit 1
fi

