#!/bin/bash

# Set DigitalOcean Spaces credentials as environment variables
export DIGITALOCEAN_ACCESS_KEY="DO00Z6VU8Q38KXLFZ7V4"
export DIGITALOCEAN_SECRET_KEY="ZDK61LfGdaqu5FpTcKnUfK8GNSW+cTSSbK8vK8GnMno"

echo "✅ Environment variables set for DigitalOcean Spaces"
echo "🔐 Using secure environment variable approach"
echo ""

# Start the server
cd SM_MentalHealthApp.Server
dotnet run
