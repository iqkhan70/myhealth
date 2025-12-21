#!/bin/bash

# Script to fix JWT configuration mismatch on DigitalOcean server

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo "🔧 Fixing JWT Configuration on Server..."
echo "=========================================="
echo ""

# Fix SSH key permissions
chmod 600 "$SSH_KEY_PATH" 2>/dev/null

# Read the JWT key from local appsettings.json
LOCAL_JWT_KEY=$(grep -A 1 '"Jwt"' /Users/mohammedkhan/iq/health/SM_MentalHealthApp.Server/appsettings.json | grep '"Key"' | cut -d'"' -f4)
LOCAL_JWT_ISSUER=$(grep -A 3 '"Jwt"' /Users/mohammedkhan/iq/health/SM_MentalHealthApp.Server/appsettings.json | grep '"Issuer"' | cut -d'"' -f4)
LOCAL_JWT_AUDIENCE=$(grep -A 4 '"Jwt"' /Users/mohammedkhan/iq/health/SM_MentalHealthApp.Server/appsettings.json | grep '"Audience"' | cut -d'"' -f4)

if [ -z "$LOCAL_JWT_KEY" ]; then
    echo "❌ Could not read JWT Key from local appsettings.json"
    exit 1
fi

echo "📋 Local JWT Configuration:"
echo "   Key: ${LOCAL_JWT_KEY:0:20}..."
echo "   Issuer: $LOCAL_JWT_ISSUER"
echo "   Audience: $LOCAL_JWT_AUDIENCE"
echo ""

echo "🔍 Checking current server configuration..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    if [ -f $APP_DIR/server/appsettings.Production.json ]; then
        echo "Current JWT section:"
        grep -A 5 '"Jwt"' $APP_DIR/server/appsettings.Production.json || echo "⚠️ JWT section not found"
    else
        echo "⚠️ appsettings.Production.json not found"
    fi
ENDSSH

echo ""
read -p "Do you want to update the server's JWT configuration? (y/n) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Cancelled"
    exit 1
fi

echo "🔄 Updating server configuration..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    # Backup existing file
    if [ -f $APP_DIR/server/appsettings.Production.json ]; then
        cp $APP_DIR/server/appsettings.Production.json $APP_DIR/server/appsettings.Production.json.backup.\$(date +%Y%m%d_%H%M%S)
        echo "✅ Backup created"
    fi
    
    # Read existing file and update JWT section
    if [ -f $APP_DIR/server/appsettings.Production.json ]; then
        # Use Python to update JSON (more reliable than sed)
        python3 << PYTHON_SCRIPT
import json
import sys

try:
    with open('$APP_DIR/server/appsettings.Production.json', 'r') as f:
        config = json.load(f)
    
    # Update JWT section
    if 'Jwt' not in config:
        config['Jwt'] = {}
    
    # Remove old SecretKey if it exists
    if 'SecretKey' in config['Jwt']:
        del config['Jwt']['SecretKey']
    
    # Set correct properties
    config['Jwt']['Key'] = '$LOCAL_JWT_KEY'
    config['Jwt']['Issuer'] = '$LOCAL_JWT_ISSUER'
    config['Jwt']['Audience'] = '$LOCAL_JWT_AUDIENCE'
    
    # Write back
    with open('$APP_DIR/server/appsettings.Production.json', 'w') as f:
        json.dump(config, f, indent=2)
    
    print("✅ JWT configuration updated successfully")
except Exception as e:
    print(f"❌ Error: {e}")
    sys.exit(1)
PYTHON_SCRIPT
    else
        echo "❌ appsettings.Production.json not found"
        exit 1
    fi
    
    echo ""
    echo "📋 Updated JWT section:"
    grep -A 5 '"Jwt"' $APP_DIR/server/appsettings.Production.json
ENDSSH

if [ $? -eq 0 ]; then
    echo ""
    echo "🔄 Restarting service..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl restart mental-health-app"
    
    echo ""
    echo "✅ JWT configuration fixed and service restarted!"
    echo ""
    echo "🧪 Test the fix:"
    echo "   1. Try logging in again on the server"
    echo "   2. Check if /api/journal/user/3 works"
    echo "   3. Check server logs: journalctl -u mental-health-app -f"
else
    echo ""
    echo "❌ Failed to update configuration"
    exit 1
fi

