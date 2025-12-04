#!/bin/bash

# Fix Metro Bundler for iOS Simulator
# This ensures Metro is running and accessible before building

cd "$(dirname "$0")"

echo "🔧 Fixing Metro Bundler for iOS Simulator..."
echo ""

# Step 1: Kill ALL existing processes
echo "🛑 Stopping all Metro/Expo processes..."
pkill -f "expo start" 2>/dev/null || true
pkill -f "expo run:ios" 2>/dev/null || true
pkill -f "metro" 2>/dev/null || true
lsof -ti:8081 | xargs kill -9 2>/dev/null || true
sleep 3

# Step 2: Clear caches
echo "🧹 Clearing caches..."
rm -rf .expo 2>/dev/null || true
rm -rf node_modules/.cache 2>/dev/null || true
rm -rf ios/build 2>/dev/null || true
echo "✅ Caches cleared"

# Step 3: Verify port 8081 is free
echo ""
echo "🔍 Checking port 8081..."
if lsof -i :8081 > /dev/null 2>&1; then
    echo "⚠️  Port 8081 is still in use, forcing cleanup..."
    lsof -ti:8081 | xargs kill -9 2>/dev/null || true
    sleep 2
fi

# Step 4: Start Metro in background
echo ""
echo "🚀 Starting Metro bundler..."
echo "   - Metro will run on localhost:8081"
echo "   - Simulator should be able to connect"
echo ""

# Start Metro with localhost (simulator uses localhost)
npx expo start --clear --localhost &

# Wait for Metro to start
echo "⏳ Waiting for Metro to start..."
sleep 5

# Verify Metro is running
if curl -s http://localhost:8081/status > /dev/null 2>&1; then
    echo "✅ Metro is running and accessible!"
    echo ""
    echo "📱 Now you can:"
    echo "   1. Open iOS Simulator"
    echo "   2. Run: npx expo run:ios"
    echo "   OR"
    echo "   3. If app is already built, shake device → Reload"
    echo ""
    echo "💡 Keep this terminal open - Metro is running in the background"
    echo "💡 To stop Metro: Press Ctrl+C or run: pkill -f 'expo start'"
else
    echo "❌ Metro failed to start. Check the error messages above."
    exit 1
fi

