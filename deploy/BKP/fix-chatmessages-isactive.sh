#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

# Get DB password
DB_PASS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

echo "🔍 Checking ChatMessages table structure..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'DESCRIBE ChatMessages;' 2>&1" | grep -i isactive || echo "❌ IsActive column NOT found"

echo ""
echo "🔄 Adding IsActive column to ChatMessages..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'ALTER TABLE ChatMessages ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1;' 2>&1"

echo ""
echo "🔍 Verifying ChatMessages table structure after update..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'DESCRIBE ChatMessages;' 2>&1" | grep -i isactive && echo "✅ IsActive column found!" || echo "❌ IsActive column still missing"

echo ""
echo "🔄 Restarting service..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl restart mental-health-app"
echo "✅ Service restarted"

