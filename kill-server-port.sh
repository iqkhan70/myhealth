#!/bin/bash

# Kill process using port 5262

echo "🔍 Finding process on port 5262..."

PID=$(lsof -ti :5262)

if [ -z "$PID" ]; then
    echo "✅ No process found on port 5262"
    exit 0
fi

echo "📋 Found process: PID $PID"
echo "   Details:"
lsof -i :5262 | grep LISTEN

echo ""
read -p "Kill this process? (y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "🛑 Killing process $PID..."
    kill -9 $PID
    sleep 1
    
    # Verify it's gone
    if lsof -ti :5262 > /dev/null 2>&1; then
        echo "❌ Process still running, trying force kill..."
        kill -9 $PID 2>/dev/null
    else
        echo "✅ Port 5262 is now free"
    fi
else
    echo "❌ Cancelled"
    exit 1
fi

