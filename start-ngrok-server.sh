#!/bin/bash

# Script to start ngrok tunnel for the .NET server (HTTPS on port 5262)

echo "🚀 Starting ngrok tunnel for .NET server (HTTPS port 5262)..."
echo ""
echo "⚠️  Make sure the server is running first:"
echo "   cd SM_MentalHealthApp.Server && dotnet run --launch-profile https"
echo ""
echo "Press Ctrl+C to stop ngrok"
echo ""

# Start ngrok tunnel for server HTTPS
ngrok http 5262 --scheme=https

