#!/bin/bash

# Complete fix for Metro connection issue
# This ensures only ONE Metro instance is running and fully ready

cd "$(dirname "$0")"

echo "🔧 Complete Metro Fix..."
echo ""

# Step 1: Kill EVERYTHING
echo "🛑 Killing all Metro/Expo processes..."
pkill -9 -f "expo" 2>/dev/null || true
pkill -9 -f "metro" 2>/dev/null || true
pkill -9 -f "node.*8081" 2>/dev/null || true
lsof -ti:8081 | xargs kill -9 2>/dev/null || true
sleep 5

# Verify nothing is running
if pgrep -f "expo\|metro" > /dev/null; then
    echo "⚠️  Some processes still running, forcing kill..."
    pkill -9 -f "expo\|metro" 2>/dev/null || true
    sleep 2
fi

# Step 2: Clear ALL caches
echo "🧹 Clearing all caches..."
rm -rf .expo 2>/dev/null || true
rm -rf node_modules/.cache 2>/dev/null || true
rm -rf ios/build 2>/dev/null || true
rm -rf ~/Library/Caches/com.apple.dt.Xcode 2>/dev/null || true
echo "✅ Caches cleared"

# Step 3: Reset simulator (optional but recommended)
echo ""
read -p "🔄 Reset simulator? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Resetting simulator..."
    xcrun simctl shutdown all 2>/dev/null || true
    sleep 2
    echo "✅ Simulator reset"
fi

# Step 4: Start Metro with explicit configuration
echo ""
echo "🚀 Starting Metro (single instance)..."
echo ""

# Start Metro and wait for it
npx expo start --clear --localhost > /tmp/metro-complete.log 2>&1 &
METRO_PID=$!

echo "⏳ Waiting for Metro to be fully ready..."
echo "   (This may take 15-30 seconds)"
echo ""

MAX_WAIT=60
WAIT_COUNT=0
METRO_READY=false

while [ $WAIT_COUNT -lt $MAX_WAIT ]; do
    # Check multiple things to ensure Metro is fully ready
    STATUS_OK=false
    BUNDLE_OK=false
    
    # Check status endpoint
    if curl -s http://localhost:8081/status > /dev/null 2>&1; then
        STATUS_OK=true
    fi
    
    # Check bundle endpoint (this is what the app actually uses)
    if curl -s "http://localhost:8081/index.bundle?platform=ios&dev=true" > /dev/null 2>&1; then
        BUNDLE_OK=true
    fi
    
    # Check if only one Metro process
    METRO_COUNT=$(pgrep -f "expo start" | wc -l | tr -d ' ')
    
    if [ "$STATUS_OK" = true ] && [ "$BUNDLE_OK" = true ] && [ "$METRO_COUNT" -eq 1 ]; then
        # Wait a bit more to ensure Metro is fully initialized
        sleep 3
        METRO_READY=true
        break
    fi
    
    # Check if Metro process died
    if ! kill -0 $METRO_PID 2>/dev/null; then
        echo ""
        echo "❌ Metro process died. Check /tmp/metro-complete.log:"
        tail -30 /tmp/metro-complete.log
        exit 1
    fi
    
    sleep 1
    WAIT_COUNT=$((WAIT_COUNT + 1))
    
    # Show progress every 5 seconds
    if [ $((WAIT_COUNT % 5)) -eq 0 ]; then
        echo "   Still waiting... ($WAIT_COUNT/$MAX_WAIT seconds)"
    fi
done

echo ""

if [ "$METRO_READY" = true ]; then
    echo "✅ Metro is fully ready!"
    echo ""
    echo "📊 Verification:"
    echo "   - Status endpoint: ✅"
    echo "   - Bundle endpoint: ✅"
    echo "   - Metro instances: $METRO_COUNT (should be 1)"
    echo ""
    echo "📱 Now you can run the app in a NEW terminal:"
    echo "   cd MentalHealthMobileClean"
    echo "   npx expo run:ios"
    echo ""
    echo "💡 Keep this terminal open - Metro is running here"
    echo "💡 The app will connect to this Metro instance"
    echo ""
    echo "🔍 To verify Metro is accessible from simulator:"
    echo "   1. Open Safari in Simulator"
    echo "   2. Go to: http://localhost:8081"
    echo "   3. Should show Metro bundler interface"
else
    echo "❌ Metro didn't become ready in time"
    echo ""
    echo "Check /tmp/metro-complete.log for errors:"
    tail -40 /tmp/metro-complete.log
    echo ""
    echo "Current Metro processes:"
    ps aux | grep -E "expo|metro" | grep -v grep
    kill $METRO_PID 2>/dev/null || true
    exit 1
fi

