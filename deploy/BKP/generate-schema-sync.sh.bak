#!/bin/bash

# Generate schema sync script from local database to fix server database

set -e

# Colors
GREEN='\033[0;32m'
YELLON='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
LOCAL_DB_NAME="mentalhealthdb"
LOCAL_DB_USER="root"
LOCAL_DB_PASS="UthmanBasima70"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Generate Schema Sync Script${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Fix SSH key permissions
chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "📋 Step 1: Getting local database schema..."
echo ""

# Get local JournalEntries table structure
LOCAL_SCHEMA=$(mysql -u "$LOCAL_DB_USER" -p"$LOCAL_DB_PASS" "$LOCAL_DB_NAME" -e "DESCRIBE JournalEntries;" 2>/dev/null)

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to connect to local database${NC}"
    exit 1
fi

echo "✅ Local schema retrieved"
echo ""

echo "📋 Step 2: Getting server database schema..."
echo ""

# Get server DB password
SERVER_DB_PASS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

# Get server JournalEntries table structure
SERVER_SCHEMA=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$SERVER_DB_PASS mentalhealthdb -e 'DESCRIBE JournalEntries;' 2>&1")

if [ $? -ne 0 ]; then
    echo -e "${YELLOW}⚠️  Could not get server schema (might be connection issue)${NC}"
    echo "Will generate full schema script instead..."
    SERVER_SCHEMA=""
fi

echo "📋 Step 3: Generating sync SQL script..."
echo ""

# Create SQL script
SQL_SCRIPT="/tmp/sync_journal_entries_schema.sql"

cat > "$SQL_SCRIPT" << 'EOFSQL'
-- Schema Sync Script for JournalEntries table
-- Generated: $(date)
-- This script adds missing columns to match local database schema

USE mentalhealthdb;

-- Check and add IsActive column if missing
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'JournalEntries' 
    AND COLUMN_NAME = 'IsActive'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE JournalEntries ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1 AFTER Mood;',
    'SELECT "Column IsActive already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check and add IgnoredByDoctorId column if missing
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'JournalEntries' 
    AND COLUMN_NAME = 'IgnoredByDoctorId'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE JournalEntries ADD COLUMN IgnoredByDoctorId INT NULL AFTER EnteredByUserId;',
    'SELECT "Column IgnoredByDoctorId already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check and add IsIgnoredByDoctor column if missing
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'JournalEntries' 
    AND COLUMN_NAME = 'IsIgnoredByDoctor'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE JournalEntries ADD COLUMN IsIgnoredByDoctor TINYINT(1) NOT NULL DEFAULT 0 AFTER IgnoredByDoctorId;',
    'SELECT "Column IsIgnoredByDoctor already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check and add IgnoredAt column if missing
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'JournalEntries' 
    AND COLUMN_NAME = 'IgnoredAt'
);

SET @sql = IF(@col_exists = 0,
    'ALTER TABLE JournalEntries ADD COLUMN IgnoredAt DATETIME(6) NULL AFTER IsIgnoredByDoctor;',
    'SELECT "Column IgnoredAt already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add indexes if they don't exist
CREATE INDEX IF NOT EXISTS IX_JournalEntries_IgnoredByDoctorId ON JournalEntries(IgnoredByDoctorId);

-- Add foreign key for IgnoredByDoctorId if it doesn't exist
SET @fk_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'mentalhealthdb' 
    AND TABLE_NAME = 'JournalEntries' 
    AND CONSTRAINT_NAME = 'FK_JournalEntries_Users_IgnoredByDoctorId'
);

SET @sql = IF(@fk_exists = 0,
    'ALTER TABLE JournalEntries ADD CONSTRAINT FK_JournalEntries_Users_IgnoredByDoctorId FOREIGN KEY (IgnoredByDoctorId) REFERENCES Users(Id) ON DELETE SET NULL;',
    'SELECT "Foreign key FK_JournalEntries_Users_IgnoredByDoctorId already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT 'Schema sync completed!' AS result;
EOFSQL

echo "✅ SQL script generated: $SQL_SCRIPT"
echo ""
echo "📋 Step 4: Copying script to server..."
echo ""

scp -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$SQL_SCRIPT" root@$DROPLET_IP:/tmp/sync_journal_entries_schema.sql

if [ $? -eq 0 ]; then
    echo "✅ Script copied to server"
    echo ""
    echo "📋 Step 5: Applying schema changes..."
    echo ""
    
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
        DB_PASS=\$(grep -A 1 '"MySQL"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '"' | tr -d '}')
        
        echo "Applying schema changes..."
        mysql -u mentalhealth_user -p"\$DB_PASS" mentalhealthdb < /tmp/sync_journal_entries_schema.sql
        
        if [ \$? -eq 0 ]; then
            echo "✅ Schema sync completed successfully!"
            echo ""
            echo "📋 Verifying IsActive column exists:"
            mysql -u mentalhealth_user -p"\$DB_PASS" mentalhealthdb -e "SHOW COLUMNS FROM JournalEntries LIKE 'IsActive';"
        else
            echo "❌ Schema sync failed"
            exit 1
        fi
ENDSSH
    
    if [ $? -eq 0 ]; then
        echo ""
        echo -e "${GREEN}✅ Schema sync completed!${NC}"
        echo ""
        echo "🔄 Restarting service..."
        ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl restart mental-health-app"
        echo ""
        echo -e "${GREEN}✅ Done! The API should work now.${NC}"
    else
        echo -e "${RED}❌ Failed to apply schema changes${NC}"
        exit 1
    fi
else
    echo -e "${RED}❌ Failed to copy script to server${NC}"
    exit 1
fi

