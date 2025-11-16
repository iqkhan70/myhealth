#!/bin/bash

# Script to start both ngrok tunnels (client and server) in separate terminal windows
# This requires macOS (uses 'osascript' to open new terminal windows)

echo "🚀 Starting ngrok tunnels for both client and server..."
echo ""

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "❌ This script requires macOS (uses osascript)"
    echo "Please run start-ngrok-client.sh and start-ngrok-server.sh in separate terminals"
    exit 1
fi

# Get the current directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Start client ngrok in new terminal
echo "📱 Opening ngrok tunnel for client (port 5282)..."
osascript -e "tell application \"Terminal\" to do script \"cd '$SCRIPT_DIR' && ./start-ngrok-client.sh\""

# Wait a bit
sleep 2

# Start server ngrok in new terminal
echo "🖥️  Opening ngrok tunnel for server (port 5262)..."
osascript -e "tell application \"Terminal\" to do script \"cd '$SCRIPT_DIR' && ./start-ngrok-server.sh\""

echo ""
echo "✅ Both ngrok tunnels started in separate terminal windows"
echo ""
echo "📋 Next steps:"
echo "   1. Note the ngrok URLs from both terminal windows"
echo "   2. Update the client's HttpClient configuration (see NGROK_SETUP.md)"
echo "   3. Access the client via the client ngrok URL"
echo ""

