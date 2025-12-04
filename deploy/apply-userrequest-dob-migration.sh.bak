#!/bin/bash

# Script to manually apply UserRequests DateOfBirth encryption migration on DigitalOcean
# This migration was missed during the initial deployment

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SERVER_IP="159.65.242.79"
SSH_KEY="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app/server"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}UserRequests DateOfBirth Migration${NC}"
echo -e "${GREEN}========================================${NC}"

# Get database password
echo -e "\n${BLUE}Step 1: Getting database credentials...${NC}"
DB_CREDS=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP \
    "python3 << 'PYTHON'
import json
import sys

try:
    with open('$APP_DIR/appsettings.Production.json', 'r') as f:
        config = json.load(f)
        connection_strings = config.get('ConnectionStrings', {})
        mysql_conn = connection_strings.get('MySQL', '')
        
        # Parse connection string
        parts = {}
        for part in mysql_conn.split(';'):
            if '=' in part:
                key, value = part.split('=', 1)
                parts[key.strip().lower()] = value.strip()
        
        print(f\"DB_PASS={parts.get('password', '')}\")
        print(f\"DB_NAME={parts.get('database', 'mentalhealthdb')}\")
        print(f\"DB_USER={parts.get('user', 'mentalhealth_user')}\")
except Exception as e:
    print('', file=sys.stderr)
    sys.exit(1)
PYTHON
")

# Set variables
while IFS='=' read -r key value; do
    if [[ -n "$key" && -n "$value" ]]; then
        export "$key=$value"
    fi
done <<< "$DB_CREDS"

DB_PASSWORD=${DB_PASS:-""}
DB_NAME=${DB_NAME:-mentalhealthdb}
DB_USER=${DB_USER:-mentalhealth_user}

if [ -z "$DB_PASSWORD" ]; then
    echo -e "${RED}ERROR: Could not retrieve database password${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Database credentials retrieved${NC}"

# Check current state
echo -e "\n${BLUE}Step 2: Checking current UserRequests table structure...${NC}"
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << ENDSSH
    export MYSQL_PWD="$DB_PASSWORD"
    
    echo "Checking for DateOfBirth column..."
    mysql -u "$DB_USER" "$DB_NAME" -e "SHOW COLUMNS FROM UserRequests LIKE 'DateOfBirth%';" 2>/dev/null || echo "No DateOfBirth columns found"
    
    echo ""
    echo "Checking for DateOfBirthEncrypted column..."
    HAS_ENCRYPTED=\$(mysql -u "$DB_USER" "$DB_NAME" -e "SHOW COLUMNS FROM UserRequests LIKE 'DateOfBirthEncrypted';" 2>/dev/null | wc -l)
    if [ "\$HAS_ENCRYPTED" -gt 1 ]; then
        echo "✅ DateOfBirthEncrypted column already exists"
        exit 0
    else
        echo "❌ DateOfBirthEncrypted column missing - migration needed"
    fi
    
    unset MYSQL_PWD
ENDSSH

# Apply migration manually via SQL
echo -e "\n${BLUE}Step 3: Applying migration via SQL...${NC}"
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << ENDSSH
    export MYSQL_PWD="$DB_PASSWORD"
    
    echo "Applying UserRequests DateOfBirth encryption migration..."
    
    mysql -u "$DB_USER" "$DB_NAME" << 'SQL'
-- Step 1: Add the new encrypted column as nullable first
ALTER TABLE UserRequests 
ADD COLUMN DateOfBirthEncrypted VARCHAR(500) CHARACTER SET utf8mb4 NULL;

-- Step 2: Copy existing DateOfBirth values to DateOfBirthEncrypted
UPDATE UserRequests 
SET DateOfBirthEncrypted = DATE_FORMAT(DateOfBirth, '%Y-%m-%dT%H:%i:%s.000000')
WHERE DateOfBirth IS NOT NULL AND DateOfBirth != '0001-01-01 00:00:00';

-- Step 3: Make DateOfBirthEncrypted required (set default for any null values)
UPDATE UserRequests 
SET DateOfBirthEncrypted = '0001-01-01T00:00:00.000000'
WHERE DateOfBirthEncrypted IS NULL;

-- Step 4: Make column NOT NULL
ALTER TABLE UserRequests 
MODIFY COLUMN DateOfBirthEncrypted VARCHAR(500) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

-- Step 5: Drop the old DateOfBirth column
ALTER TABLE UserRequests 
DROP COLUMN DateOfBirth;

-- Step 6: Record migration in history
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251129162652_EncryptUserRequestDateOfBirth', '9.0.0')
ON DUPLICATE KEY UPDATE ProductVersion = '9.0.0';

SELECT 'Migration applied successfully!' AS Status;
SQL
    
    if [ \$? -eq 0 ]; then
        echo "✅ Migration applied successfully"
    else
        echo "❌ Migration failed"
        exit 1
    fi
    
    unset MYSQL_PWD
ENDSSH

# Verify migration
echo -e "\n${BLUE}Step 4: Verifying migration...${NC}"
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << ENDSSH
    export MYSQL_PWD="$DB_PASSWORD"
    
    echo "Checking UserRequests table structure..."
    mysql -u "$DB_USER" "$DB_NAME" -e "SHOW COLUMNS FROM UserRequests LIKE 'DateOfBirth%';" 2>/dev/null
    
    echo ""
    echo "Checking migration history..."
    mysql -u "$DB_USER" "$DB_NAME" -e "SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId = '20251129162652_EncryptUserRequestDateOfBirth';" 2>/dev/null
    
    unset MYSQL_PWD
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}✅ UserRequests DateOfBirth Migration Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "\n${YELLOW}Note:${NC} Existing DateOfBirth data was copied as plain text."
echo -e "The application will encrypt it automatically when accessed."

