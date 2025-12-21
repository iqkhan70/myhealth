#!/bin/bash

# Simple script to fix JWT configuration on server

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo "🔧 Fixing JWT Configuration..."
echo ""

# Fix SSH key permissions
chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "📋 Current JWT config on server:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -A 3 '\"Jwt\"' $APP_DIR/server/appsettings.Production.json 2>/dev/null || echo 'Not found'"

echo ""
echo "🔍 Checking if 'Key' property exists..."
KEY_EXISTS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -q '\"Key\"' $APP_DIR/server/appsettings.Production.json 2>/dev/null && echo 'yes' || echo 'no'")

if [ "$KEY_EXISTS" = "no" ]; then
    echo "❌ 'Key' property not found. Need to fix..."
    echo ""
    echo "Please manually edit the server file:"
    echo "  ssh root@$DROPLET_IP"
    echo "  nano $APP_DIR/server/appsettings.Production.json"
    echo ""
    echo "Change the Jwt section from:"
    echo '  "Jwt": {'
    echo '    "SecretKey": "..."'
    echo "  }"
    echo ""
    echo "To:"
    echo '  "Jwt": {'
    echo '    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",'
    echo '    "Issuer": "SM_MentalHealthApp",'
    echo '    "Audience": "SM_MentalHealthApp_Users"'
    echo "  }"
    echo ""
    echo "Then restart: systemctl restart mental-health-app"
else
    echo "✅ 'Key' property exists. JWT config looks correct!"
    echo ""
    echo "🧪 Testing API endpoint..."
    echo ""
    echo "To test the API, you need to:"
    echo "1. Login first to get a JWT token"
    echo "2. Use that token in the Authorization header"
    echo ""
    echo "Example:"
    echo '  curl -k -H "Authorization: Bearer YOUR_TOKEN_HERE" https://159.65.242.79/api/journal/user/3'
fi

