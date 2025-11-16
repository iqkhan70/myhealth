#!/bin/bash

# Script to start ngrok tunnel for the Blazor client (HTTPS on port 5282)

echo "🚀 Starting ngrok tunnel for Blazor client (HTTPS port 5282)..."
echo ""
echo "⚠️  Make sure the client is running first:"
echo "   cd SM_MentalHealthApp.Client && dotnet run --launch-profile https"
echo ""
echo "Press Ctrl+C to stop ngrok"
echo ""

# Start ngrok tunnel for client HTTPS
ngrok http 5282 --scheme=https

