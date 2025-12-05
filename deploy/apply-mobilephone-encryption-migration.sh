#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to apply MobilePhone encryption migrations on DigitalOcean
# This script is idempotent - safe to run multiple times

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SSH_KEY="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app/server"
PROJECT_DIR="/Users/mohammedkhan/iq/health"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}MobilePhone Encryption Migration${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if we're in the project root
if [ ! -f "SM_MentalHealthApp.Server/Scripts/EncryptMobilePhoneMigration.sql" ]; then
    echo -e "${RED}ERROR: Please run this script from the project root directory${NC}"
    exit 1
fi

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY" 2>/dev/null || stat -c "%a" "$SSH_KEY" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY"
    fi
fi

echo -e "\n${BLUE}Step 1: Copying migration SQL to server...${NC}"

# Copy migration SQL file to server
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    "SM_MentalHealthApp.Server/Scripts/EncryptMobilePhoneMigration.sql" \
    root@$SERVER_IP:$APP_DIR/EncryptMobilePhoneMigration.sql

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to copy migration file${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Migration file copied${NC}"

echo -e "\n${BLUE}Step 2: Getting database credentials...${NC}"

# Get database credentials using Python (more reliable than grep and safer than eval)
DB_CREDS=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP \
    "python3 << 'PYTHON'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
        config = json.load(f)
        connection_strings = config.get('ConnectionStrings', {})
        mysql_conn = connection_strings.get('MySQL', '')
        
        # Parse connection string
        parts = {}
        for part in mysql_conn.split(';'):
            if '=' in part:
                key, value = part.split('=', 1)
                parts[key.strip().lower()] = value.strip()
        
        print(f\"DB_USER={parts.get('user', 'root')}\")
        print(f\"DB_PASS={parts.get('password', '')}\")
        print(f\"DB_HOST={parts.get('server', 'localhost')}\")
        print(f\"DB_PORT={parts.get('port', '3306')}\")
        print(f\"DB_NAME={parts.get('database', 'mental_health_db')}\")
except Exception as e:
    print('', file=sys.stderr)
    sys.exit(1)
PYTHON
")

# Evaluate the output to set variables (safely)
while IFS='=' read -r key value; do
    if [[ -n "$key" && -n "$value" ]]; then
        export "$key=$value"
    fi
done <<< "$DB_CREDS"

if [ -z "$DB_USER" ] || [ -z "$DB_PASS" ] || [ -z "$DB_NAME" ]; then
    echo -e "${RED}❌ Could not extract database credentials${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Database credentials extracted${NC}"
echo -e "   Host: ${DB_HOST:-localhost}"
echo -e "   Port: ${DB_PORT:-3306}"
echo -e "   Database: $DB_NAME"
echo -e "   User: $DB_USER"

echo -e "\n${BLUE}Step 3: Applying migration SQL...${NC}"
echo -e "${YELLOW}This will:${NC}"
echo -e "   - Add MobilePhoneEncrypted column to Users and UserRequests tables"
echo -e "   - Copy existing MobilePhone data to MobilePhoneEncrypted (as plain text)"
echo -e "   - Drop the old MobilePhone column"
echo ""
read -p "Do you want to continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo -e "${YELLOW}Migration cancelled.${NC}"
    exit 0
fi

# Apply migration via SSH
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << EOF
    export MYSQL_PWD="$DB_PASS"
    mysql -h ${DB_HOST:-localhost} -P ${DB_PORT:-3306} -u "$DB_USER" "$DB_NAME" < $APP_DIR/EncryptMobilePhoneMigration.sql
    unset MYSQL_PWD
EOF

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Migration applied successfully!${NC}"
else
    echo -e "${RED}❌ Migration failed!${NC}"
    exit 1
fi

echo -e "\n${BLUE}Step 4: Verifying migration...${NC}"

# Verify migration
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << EOF
    export MYSQL_PWD="$DB_PASS"
    mysql -h ${DB_HOST:-localhost} -P ${DB_PORT:-3306} -u "$DB_USER" "$DB_NAME" -e "SHOW COLUMNS FROM Users LIKE 'MobilePhoneEncrypted';" 2>/dev/null | grep -q "MobilePhoneEncrypted" && echo "✅ MobilePhoneEncrypted column exists in Users table" || echo "❌ MobilePhoneEncrypted column NOT found in Users table"
    mysql -h ${DB_HOST:-localhost} -P ${DB_PORT:-3306} -u "$DB_USER" "$DB_NAME" -e "SHOW COLUMNS FROM UserRequests LIKE 'MobilePhoneEncrypted';" 2>/dev/null | grep -q "MobilePhoneEncrypted" && echo "✅ MobilePhoneEncrypted column exists in UserRequests table" || echo "❌ MobilePhoneEncrypted column NOT found in UserRequests table"
    
    # Check if old MobilePhone column still exists (should be dropped)
    mysql -h ${DB_HOST:-localhost} -P ${DB_PORT:-3306} -u "$DB_USER" "$DB_NAME" -e "SHOW COLUMNS FROM Users LIKE 'MobilePhone';" 2>/dev/null | grep -q "MobilePhone" && echo "⚠️  Old MobilePhone column still exists in Users table" || echo "✅ Old MobilePhone column removed from Users table"
    mysql -h ${DB_HOST:-localhost} -P ${DB_PORT:-3306} -u "$DB_USER" "$DB_NAME" -e "SHOW COLUMNS FROM UserRequests LIKE 'MobilePhone';" 2>/dev/null | grep -q "MobilePhone" && echo "⚠️  Old MobilePhone column still exists in UserRequests table" || echo "✅ Old MobilePhone column removed from UserRequests table"
    unset MYSQL_PWD
EOF

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Migration Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo -e "1. Restart the application service: ${BLUE}systemctl restart mental-health-app${NC}"
echo -e "2. Run the encryption script to encrypt existing plain text phone numbers:"
echo -e "   ${BLUE}./deploy/encrypt-existing-mobilephone-data.sh${NC}"
echo ""
