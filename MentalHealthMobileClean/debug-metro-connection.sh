#!/bin/bash

# Debug script to check Metro connection from simulator perspective

cd "$(dirname "$0")"

echo "🔍 Debugging Metro Connection..."
echo ""

# Check if Metro is running
echo "1. Checking if Metro is running..."
if curl -s http://localhost:8081/status > /dev/null 2>&1; then
    echo "   ✅ Metro is running"
    curl -s http://localhost:8081/status
else
    echo "   ❌ Metro is NOT running"
    echo "   Start Metro first: npx expo start --clear --localhost"
    exit 1
fi

echo ""
echo "2. Checking Metro bundle URL..."
BUNDLE_URL=$(curl -s http://localhost:8081/index.bundle?platform=ios\&dev=true 2>&1 | head -1)
if [ -n "$BUNDLE_URL" ]; then
    echo "   ✅ Bundle URL is accessible"
else
    echo "   ❌ Bundle URL is NOT accessible"
fi

echo ""
echo "3. Checking port 8081..."
if lsof -i :8081 > /dev/null 2>&1; then
    echo "   ✅ Port 8081 is open"
    lsof -i :8081 | head -2
else
    echo "   ❌ Port 8081 is NOT open"
fi

echo ""
echo "4. Testing from localhost..."
if curl -s http://127.0.0.1:8081/status > /dev/null 2>&1; then
    echo "   ✅ localhost (127.0.0.1) works"
else
    echo "   ❌ localhost (127.0.0.1) doesn't work"
fi

echo ""
echo "5. Checking Metro process..."
if pgrep -f "expo start" > /dev/null; then
    echo "   ✅ Expo Metro process is running"
    ps aux | grep "expo start" | grep -v grep | head -1
else
    echo "   ❌ Expo Metro process is NOT running"
fi

echo ""
echo "6. Checking for multiple Metro instances..."
METRO_COUNT=$(pgrep -f "expo start" | wc -l | tr -d ' ')
if [ "$METRO_COUNT" -gt 1 ]; then
    echo "   ⚠️  Multiple Metro instances detected: $METRO_COUNT"
    echo "   This might cause connection issues"
else
    echo "   ✅ Single Metro instance"
fi

echo ""
echo "7. Checking simulator network..."
if xcrun simctl list devices | grep Booted > /dev/null; then
    echo "   ✅ Simulator is running"
    echo "   💡 Try opening Safari in Simulator and go to: http://localhost:8081"
    echo "   💡 If Safari can't load it, there's a network issue"
else
    echo "   ⚠️  No simulator is running"
fi

echo ""
echo "📋 Next Steps:"
echo "   1. If Metro is running, try:"
echo "      - Open Safari in Simulator"
echo "      - Go to: http://localhost:8081"
echo "      - If it loads, Metro is accessible"
echo ""
echo "   2. If Safari can't load Metro:"
echo "      - Kill all Metro: pkill -f 'expo start'"
echo "      - Restart Metro: npx expo start --clear --localhost"
echo ""
echo "   3. Try resetting simulator:"
echo "      - xcrun simctl erase all"
echo "      - Then rebuild app"

