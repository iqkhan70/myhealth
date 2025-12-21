#!/bin/bash

# Comprehensive script to check and fix all database schema mismatches

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
CONFIG_FILE="$APP_DIR/server/appsettings.Production.json"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔍 Comprehensive Database Schema Check and Fix"
echo "================================================"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    
    # Get DB password
    DB_PASS=$(grep -A 1 '"MySQL"' $CONFIG_FILE | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '"' | tr -d '}')
    DB_USER="mentalhealth_user"
    DB_NAME="mentalhealthdb"
    
    echo "1. Checking for missing IsActive columns..."
    echo ""
    
    # Tables that should have IsActive column
    TABLES_WITH_ISACTIVE=(
        "JournalEntries"
        "ChatMessages"
        "ClinicalNotes"
        "ChatSessions"
        "Users"
        "ContentTypes"
        "Contents"
    )
    
    MISSING_COLUMNS=()
    
    for TABLE in "${TABLES_WITH_ISACTIVE[@]}"; do
        COL_EXISTS=$(mysql -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -sN -e "
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = '$DB_NAME' 
            AND TABLE_NAME = '$TABLE' 
            AND COLUMN_NAME = 'IsActive'
        " 2>/dev/null || echo "0")
        
        if [ "$COL_EXISTS" = "0" ]; then
            echo "   ❌ $TABLE.IsActive - MISSING"
            MISSING_COLUMNS+=("$TABLE.IsActive")
        else
            echo "   ✅ $TABLE.IsActive - exists"
        fi
    done
    
    echo ""
    echo "2. Checking ClinicalNotes for missing columns..."
    echo ""
    
    CLINICAL_NOTES_COLS=("IsArchived" "IsActive" "IsConfidential" "IsIgnoredByDoctor")
    
    for COL in "${CLINICAL_NOTES_COLS[@]}"; do
        COL_EXISTS=$(mysql -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -sN -e "
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = '$DB_NAME' 
            AND TABLE_NAME = 'ClinicalNotes' 
            AND COLUMN_NAME = '$COL'
        " 2>/dev/null || echo "0")
        
        if [ "$COL_EXISTS" = "0" ]; then
            echo "   ❌ ClinicalNotes.$COL - MISSING"
            MISSING_COLUMNS+=("ClinicalNotes.$COL")
        else
            echo "   ✅ ClinicalNotes.$COL - exists"
        fi
    done
    
    echo ""
    echo "3. Checking JournalEntries for missing columns..."
    echo ""
    
    JOURNAL_COLS=("IsActive" "IgnoredByDoctorId" "IsIgnoredByDoctor" "IgnoredAt")
    
    for COL in "${JOURNAL_COLS[@]}"; do
        COL_EXISTS=$(mysql -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" -sN -e "
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = '$DB_NAME' 
            AND TABLE_NAME = 'JournalEntries' 
            AND COLUMN_NAME = '$COL'
        " 2>/dev/null || echo "0")
        
        if [ "$COL_EXISTS" = "0" ]; then
            echo "   ❌ JournalEntries.$COL - MISSING"
            MISSING_COLUMNS+=("JournalEntries.$COL")
        else
            echo "   ✅ JournalEntries.$COL - exists"
        fi
    done
    
    echo ""
    if [ ${#MISSING_COLUMNS[@]} -eq 0 ]; then
        echo "✅ All expected columns exist!"
    else
        echo "⚠️  Found ${#MISSING_COLUMNS[@]} missing column(s)"
        echo ""
        echo "4. Adding missing columns..."
        echo ""
        
        mysql -u "$DB_USER" -p"$DB_PASS" "$DB_NAME" << 'SQL'
-- Add IsActive to JournalEntries if missing
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'JournalEntries' AND COLUMN_NAME = 'IsActive');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE JournalEntries ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1;', 'SELECT "JournalEntries.IsActive exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Add IsActive to ChatMessages if missing
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'ChatMessages' AND COLUMN_NAME = 'IsActive');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE ChatMessages ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1;', 'SELECT "ChatMessages.IsActive exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Add missing columns to ClinicalNotes
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'ClinicalNotes' AND COLUMN_NAME = 'IsArchived');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE ClinicalNotes ADD COLUMN IsArchived TINYINT(1) NOT NULL DEFAULT 0;', 'SELECT "ClinicalNotes.IsArchived exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'ClinicalNotes' AND COLUMN_NAME = 'IsActive');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE ClinicalNotes ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1;', 'SELECT "ClinicalNotes.IsActive exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'ClinicalNotes' AND COLUMN_NAME = 'IsConfidential');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE ClinicalNotes ADD COLUMN IsConfidential TINYINT(1) NOT NULL DEFAULT 0;', 'SELECT "ClinicalNotes.IsConfidential exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'ClinicalNotes' AND COLUMN_NAME = 'IsIgnoredByDoctor');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE ClinicalNotes ADD COLUMN IsIgnoredByDoctor TINYINT(1) NOT NULL DEFAULT 0;', 'SELECT "ClinicalNotes.IsIgnoredByDoctor exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Add missing columns to JournalEntries
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'JournalEntries' AND COLUMN_NAME = 'IgnoredByDoctorId');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE JournalEntries ADD COLUMN IgnoredByDoctorId INT NULL;', 'SELECT "JournalEntries.IgnoredByDoctorId exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'JournalEntries' AND COLUMN_NAME = 'IsIgnoredByDoctor');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE JournalEntries ADD COLUMN IsIgnoredByDoctor TINYINT(1) NOT NULL DEFAULT 0;', 'SELECT "JournalEntries.IsIgnoredByDoctor exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'mentalhealthdb' AND TABLE_NAME = 'JournalEntries' AND COLUMN_NAME = 'IgnoredAt');
SET @sql = IF(@col_exists = 0, 'ALTER TABLE JournalEntries ADD COLUMN IgnoredAt DATETIME(6) NULL;', 'SELECT "JournalEntries.IgnoredAt exists" AS msg;');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SELECT '✅ Missing columns added' AS result;
SQL
        
        echo "   ✅ Missing columns added"
    fi
    
    echo ""
    echo "5. Checking Ollama configuration..."
    echo ""
    
    if [ -f "$CONFIG_FILE" ] && grep -q '"Ollama"' "$CONFIG_FILE"; then
        echo "   ✅ Ollama section exists"
        grep -A 3 '"Ollama"' "$CONFIG_FILE"
    else
        echo "   ❌ Ollama section NOT found - adding it..."
        python3 << 'PYTHON'
import json

with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
    config = json.load(f)

# Ensure HuggingFace exists (keep it)
if "HuggingFace" not in config:
    config["HuggingFace"] = {
        "ApiKey": "",
        "BioMistralModelUrl": "https://api-inference.huggingface.co/models/medalpaca/medalpaca-7b",
        "MeditronModelUrl": "https://api-inference.huggingface.co/models/epfl-llm/meditron-7b"
    }

# Add Ollama section (separate from HuggingFace)
config["Ollama"] = {
    "BaseUrl": "http://127.0.0.1:11434"
}

# Remove any incorrect settings from Ollama
for key in list(config.get("Ollama", {}).keys()):
    if key != "BaseUrl":
        del config["Ollama"][key]

with open('/opt/mental-health-app/server/appsettings.Production.json', 'w') as f:
    json.dump(config, f, indent=2)

print("   ✅ Ollama config added: BaseUrl = http://127.0.0.1:11434")
PYTHON
    fi
    
    echo ""
    echo "6. Restarting application..."
    systemctl restart mental-health-app
    sleep 2
    
    if systemctl is-active --quiet mental-health-app; then
        echo "   ✅ Application restarted"
    else
        echo "   ❌ Application failed to restart"
        systemctl status mental-health-app --no-pager | head -10
    fi
    
    echo ""
    echo "================================================"
    echo "✅ Schema check and fix complete!"
    echo "================================================"
ENDSSH

echo ""
echo "✅ All done!"

