#!/bin/bash

# Script to add IsActive column to ChatMessages table

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

# Get DB password
DB_PASS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

echo "🔍 Checking if IsActive column exists in ChatMessages..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'SHOW COLUMNS FROM ChatMessages LIKE \"IsActive\";' 2>&1"

echo ""
echo "🔄 Adding IsActive column to ChatMessages..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSQL
    mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb << 'SQL'
-- Check if column exists, if not add it
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'ChatMessages' 
    AND COLUMN_NAME = 'IsActive'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE ChatMessages ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1;',
    'SELECT "Column IsActive already exists in ChatMessages" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT 'ChatMessages.IsActive column check/update completed' AS result;
SQL
ENDSQL

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Verifying column was added..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
        "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'SHOW COLUMNS FROM ChatMessages LIKE \"IsActive\";' 2>&1"
    
    echo ""
    echo "🔄 Restarting service..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl restart mental-health-app"
    echo ""
    echo "✅ Complete! The IsActive column should now exist in ChatMessages table."
else
    echo "❌ Failed to add column"
    exit 1
fi

