#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to add missing columns to ClinicalNotes table

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

# Get DB password
DB_PASS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

echo "🔍 Checking ClinicalNotes table structure..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'DESCRIBE ClinicalNotes;' 2>&1" | grep -E "IsArchived|IsActive|IsConfidential|IsIgnoredByDoctor" || echo "Some columns may be missing"
echo ""

echo "🔄 Adding missing columns to ClinicalNotes..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSQL
    mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb << 'SQL'
-- Add IsArchived if missing
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'ClinicalNotes' 
    AND COLUMN_NAME = 'IsArchived'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE ClinicalNotes ADD COLUMN IsArchived TINYINT(1) NOT NULL DEFAULT 0;',
    'SELECT "IsArchived already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add IsActive if missing
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'ClinicalNotes' 
    AND COLUMN_NAME = 'IsActive'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE ClinicalNotes ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1;',
    'SELECT "IsActive already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add IsConfidential if missing
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'ClinicalNotes' 
    AND COLUMN_NAME = 'IsConfidential'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE ClinicalNotes ADD COLUMN IsConfidential TINYINT(1) NOT NULL DEFAULT 0;',
    'SELECT "IsConfidential already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add IsIgnoredByDoctor if missing
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'ClinicalNotes' 
    AND COLUMN_NAME = 'IsIgnoredByDoctor'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE ClinicalNotes ADD COLUMN IsIgnoredByDoctor TINYINT(1) NOT NULL DEFAULT 0;',
    'SELECT "IsIgnoredByDoctor already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT 'ClinicalNotes columns check/update completed' AS result;
SQL
ENDSQL

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Verifying columns were added..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
        "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'SHOW COLUMNS FROM ClinicalNotes LIKE \"Is%\";' 2>&1"
    
    echo ""
    echo "🔄 Restarting service..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl restart mental-health-app"
    echo ""
    echo "✅ Complete! Missing columns should now exist in ClinicalNotes table."
else
    echo "❌ Failed to add columns"
    exit 1
fi

