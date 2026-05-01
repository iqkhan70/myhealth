#!/bin/bash

# macOS Firewall Fix Script
# This script allows .NET through the macOS firewall

echo "🔧 macOS Firewall Fix for .NET Server"
echo "======================================"
echo ""

# Find dotnet location
DOTNET_PATH=$(which dotnet)

if [ -z "$DOTNET_PATH" ]; then
    echo "❌ dotnet not found in PATH"
    echo "Trying common locations..."
    
    if [ -f "/usr/local/share/dotnet/dotnet" ]; then
        DOTNET_PATH="/usr/local/share/dotnet/dotnet"
    elif [ -f "/usr/local/bin/dotnet" ]; then
        DOTNET_PATH="/usr/local/bin/dotnet"
    elif [ -f "/opt/homebrew/bin/dotnet" ]; then
        DOTNET_PATH="/opt/homebrew/bin/dotnet"
    else
        echo "❌ Could not find dotnet. Please install .NET SDK first."
        exit 1
    fi
fi

echo "✅ Found dotnet at: $DOTNET_PATH"
echo ""

# Check firewall status
echo "📊 Checking firewall status..."
FIREWALL_STATE=$(sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate 2>&1)
echo "$FIREWALL_STATE"
echo ""

# Add dotnet to firewall
echo "🔓 Adding dotnet to firewall exceptions..."
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add "$DOTNET_PATH" 2>&1

if [ $? -eq 0 ]; then
    echo "✅ dotnet added to firewall"
else
    echo "⚠️  dotnet might already be in firewall list"
fi

# Unblock dotnet
echo "🔓 Unblocking dotnet..."
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp "$DOTNET_PATH" 2>&1

if [ $? -eq 0 ]; then
    echo "✅ dotnet unblocked"
else
    echo "⚠️  Could not unblock (might already be unblocked)"
fi

echo ""
echo "✅ Firewall configuration complete!"
echo ""
echo "📝 Verification:"
echo "   Run: sudo /usr/libexec/ApplicationFirewall/socketfilterfw --listapps | grep dotnet"
echo ""
echo "🧪 Test from other machine:"
echo "   curl http://192.168.86.34:5262/api/health"
echo ""

