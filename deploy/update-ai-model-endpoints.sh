#!/bin/bash

# Script to update AIModelConfigs table ApiEndPoint column

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo "🔄 Updating AIModelConfigs.ApiEndPoint to 'tinyllama:latest'..."
echo ""

# Fix SSH key permissions
chmod 600 "$SSH_KEY_PATH" 2>/dev/null

# Get DB password from server
DB_PASS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

echo "📋 Current values:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'SELECT Id, ModelName, ApiEndPoint FROM AIModelConfigs;' 2>&1"

echo ""
read -p "Update all rows to 'tinyllama:latest'? (y/n) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Cancelled"
    exit 1
fi

echo ""
echo "🔄 Updating..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e \"UPDATE AIModelConfigs SET ApiEndPoint = 'tinyllama:latest';\" 2>&1"

if [ $? -eq 0 ]; then
    echo "✅ Update successful!"
    echo ""
    echo "📋 Updated values:"
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
        "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'SELECT Id, ModelName, ApiEndPoint FROM AIModelConfigs;' 2>&1"
else
    echo "❌ Update failed"
    exit 1
fi

