#!/bin/bash
# Quick script to start Metro for iOS Simulator

cd "$(dirname "$0")"

echo "🛑 Stopping existing Metro..."
pkill -f "expo start" 2>/dev/null || true
lsof -ti:8081 | xargs kill -9 2>/dev/null || true
sleep 2

echo "🚀 Starting Metro for iOS Simulator..."
echo "   (Use --localhost for simulator, --host lan for physical device)"
echo ""

npx expo start --clear --localhost
