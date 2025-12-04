#!/bin/bash

# Script to properly start Metro and then run iOS app
# This ensures Metro is ready before the app launches

cd "$(dirname "$0")"

echo "🔧 Starting Metro and iOS App..."
echo ""

# Step 1: Kill any existing Metro/Expo processes
echo "🛑 Stopping existing Metro/Expo processes..."
pkill -f "expo start" 2>/dev/null || true
pkill -f "expo run:ios" 2>/dev/null || true
pkill -f "metro" 2>/dev/null || true
lsof -ti:8081 | xargs kill -9 2>/dev/null || true
sleep 2

# Step 2: Clear caches
echo "🧹 Clearing caches..."
rm -rf .expo 2>/dev/null || true
rm -rf node_modules/.cache 2>/dev/null || true
echo "✅ Caches cleared"
echo ""

# Step 3: Start Metro in background
echo "🚀 Starting Metro bundler..."
echo "   (This will run in the background)"
echo ""

# Start Metro with localhost for simulator
npx expo start --clear --localhost > /tmp/metro.log 2>&1 &
METRO_PID=$!

# Wait for Metro to be ready
echo "⏳ Waiting for Metro to start..."
MAX_WAIT=30
WAIT_COUNT=0

while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
    if curl -s http://localhost:8081/status > /dev/null 2>&1; then
        echo "✅ Metro is running and ready!"
        echo ""
        break
    fi
    
    # Check if Metro process is still alive
    if ! kill -0 $METRO_PID 2>/dev/null; then
        echo "❌ Metro failed to start. Check /tmp/metro.log for errors:"
        tail -20 /tmp/metro.log
        exit 1
    fi
    
    sleep 1
    WAIT_COUNT=$((WAIT_COUNT + 1))
    echo -n "."
done

if [ $WAIT_COUNT -eq $MAX_WAIT ]; then
    echo ""
    echo "❌ Metro didn't start in time. Check /tmp/metro.log:"
    tail -20 /tmp/metro.log
    kill $METRO_PID 2>/dev/null || true
    exit 1
fi

echo ""
echo "📱 Now launching iOS app..."
echo "   (Metro will continue running in the background)"
echo ""

# Step 4: Run iOS app (this will use the running Metro)
npx expo run:ios

# Note: Metro will keep running in background
# To stop it: pkill -f "expo start"

