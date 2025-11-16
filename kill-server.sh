#!/bin/bash

# Kill any server process on port 5262

echo "🔍 Finding processes on port 5262..."

# Method 1: Find by port
PID=$(lsof -ti :5262 2>/dev/null)

if [ ! -z "$PID" ]; then
    echo "📋 Found process on port 5262: PID $PID"
    echo "🛑 Killing process $PID..."
    kill -9 $PID 2>/dev/null
    sleep 1
    echo "✅ Process killed"
else
    echo "⚠️  No process found on port 5262 via lsof"
fi

# Method 2: Find by process name
SERVER_PID=$(pgrep -f "SM_MentalHealthApp.Server" 2>/dev/null)

if [ ! -z "$SERVER_PID" ]; then
    echo "📋 Found server process: PID $SERVER_PID"
    echo "🛑 Killing server process $SERVER_PID..."
    kill -9 $SERVER_PID 2>/dev/null
    sleep 1
    echo "✅ Server process killed"
fi

# Verify port is free
if lsof -ti :5262 > /dev/null 2>&1; then
    echo "❌ Port 5262 is still in use"
    echo "   Try manually: kill -9 \$(lsof -ti :5262)"
else
    echo "✅ Port 5262 is now free"
    echo "   You can now start the server: dotnet run --launch-profile https"
fi

