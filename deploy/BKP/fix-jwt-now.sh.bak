#!/bin/bash

# Direct fix for JWT configuration - no questions asked

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo "🔧 Fixing JWT Configuration NOW..."
echo ""

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

# Read correct values from local file
LOCAL_JWT_KEY="YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
LOCAL_JWT_ISSUER="SM_MentalHealthApp"
LOCAL_JWT_AUDIENCE="SM_MentalHealthApp_Users"

echo "📋 Updating server configuration..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    APP_DIR="/opt/mental-health-app"
    LOCAL_JWT_KEY="YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
    LOCAL_JWT_ISSUER="SM_MentalHealthApp"
    LOCAL_JWT_AUDIENCE="SM_MentalHealthApp_Users"
    
    # Backup
    cp $APP_DIR/server/appsettings.Production.json $APP_DIR/server/appsettings.Production.json.backup.$(date +%Y%m%d_%H%M%S)
    
    # Use sed to fix the JWT section
    # Remove SecretKey line if it exists
    sed -i '/"SecretKey"/d' $APP_DIR/server/appsettings.Production.json
    
    # Replace or add Key property
    if grep -q '"Key"' $APP_DIR/server/appsettings.Production.json; then
        # Key exists, update it
        sed -i 's/"Key": *"[^"]*"/"Key": "'"$LOCAL_JWT_KEY"'"/' $APP_DIR/server/appsettings.Production.json
    else
        # Key doesn't exist, add it after "Jwt": {
        sed -i '/"Jwt": *{/a\    "Key": "'"$LOCAL_JWT_KEY"'",' $APP_DIR/server/appsettings.Production.json
    fi
    
    # Update Issuer
    if grep -q '"Issuer"' $APP_DIR/server/appsettings.Production.json; then
        sed -i 's/"Issuer": *"[^"]*"/"Issuer": "'"$LOCAL_JWT_ISSUER"'"/' $APP_DIR/server/appsettings.Production.json
    else
        sed -i '/"Key":/a\    "Issuer": "'"$LOCAL_JWT_ISSUER"'",' $APP_DIR/server/appsettings.Production.json
    fi
    
    # Update Audience
    if grep -q '"Audience"' $APP_DIR/server/appsettings.Production.json; then
        sed -i 's/"Audience": *"[^"]*"/"Audience": "'"$LOCAL_JWT_AUDIENCE"'"/' $APP_DIR/server/appsettings.Production.json
    else
        sed -i '/"Issuer":/a\    "Audience": "'"$LOCAL_JWT_AUDIENCE"'"' $APP_DIR/server/appsettings.Production.json
    fi
    
    echo "✅ Configuration updated"
    echo ""
    echo "📋 Updated JWT section:"
    grep -A 5 '"Jwt"' $APP_DIR/server/appsettings.Production.json
ENDSSH

if [ $? -eq 0 ]; then
    echo ""
    echo "🔄 Restarting service..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl restart mental-health-app"
    sleep 3
    
    echo ""
    echo "✅ DONE! Service restarted."
    echo ""
    echo "🧪 Test it now:"
    echo "   1. Login on https://159.65.242.79/login"
    echo "   2. Try accessing the journal page"
    echo "   3. Check browser console - 401 should be gone!"
else
    echo "❌ Failed to update configuration"
    exit 1
fi

