#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to apply Accident Fields migration on DigitalOcean
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
echo -e "${GREEN}Accident Fields Migration${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if we're in the project root
if [ ! -f "SM_MentalHealthApp.Server/Scripts/AddAccidentFieldsMigration.sql" ]; then
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
    "SM_MentalHealthApp.Server/Scripts/AddAccidentFieldsMigration.sql" \
    root@$SERVER_IP:$APP_DIR/AddAccidentFieldsMigration.sql

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to copy migration file${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Migration file copied${NC}"

echo -e "\n${BLUE}Step 2: Reading MySQL connection string from appsettings.Production.json...${NC}"

# Read MySQL connection string from appsettings.Production.json
MYSQL_CONNECTION_STRING=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP \
    "python3 << 'PYTHON'
import json
import sys

try:
    with open('$APP_DIR/appsettings.Production.json', 'r') as f:
        config = json.load(f)
        connection_strings = config.get('ConnectionStrings', {})
        mysql_conn = connection_strings.get('MySQL', '')
        print(mysql_conn)
except Exception as e:
    print('', file=sys.stderr)
    sys.exit(1)
PYTHON
")

if [ -z "$MYSQL_CONNECTION_STRING" ]; then
    echo -e "${YELLOW}⚠️  Could not extract MySQL connection string from appsettings.Production.json${NC}"
    echo -e "${YELLOW}Please enter MySQL connection string manually (format: Server=localhost;Database=mental_health_db;User=root;Password=yourpassword;):${NC}"
    read -r MYSQL_CONNECTION_STRING
fi

# Parse connection string using Python (compatible with all systems)
MYSQL_CONNECTION_PARTS=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP \
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
        
        print(f\"DB_HOST={parts.get('server', 'localhost')}\")
        print(f\"DB_PORT={parts.get('port', '3306')}\")
        print(f\"DB_NAME={parts.get('database', 'mental_health_db')}\")
        print(f\"DB_USER={parts.get('user', 'root')}\")
        print(f\"DB_PASS={parts.get('password', '')}\")
except Exception as e:
    print('', file=sys.stderr)
    sys.exit(1)
PYTHON
")

# Safely set variables from output
while IFS='=' read -r key value; do
    if [[ -n "$key" && -n "$value" ]]; then
        export "$key=$value"
    fi
done <<< "$MYSQL_CONNECTION_PARTS"

MYSQL_HOST=${DB_HOST:-localhost}
MYSQL_DB=${DB_NAME:-mental_health_db}
MYSQL_USER=${DB_USER:-root}
MYSQL_PASSWORD=${DB_PASS:-""}

if [ -z "$MYSQL_PASSWORD" ]; then
    echo -e "${YELLOW}⚠️  Could not extract password from connection string${NC}"
    echo -e "${YELLOW}Please enter MySQL root password:${NC}"
    read -s MYSQL_PASSWORD
fi

echo -e "${GREEN}✅ MySQL connection details extracted${NC}"
echo -e "   Host: ${BLUE}$MYSQL_HOST${NC}"
echo -e "   Database: ${BLUE}$MYSQL_DB${NC}"
echo -e "   User: ${BLUE}$MYSQL_USER${NC}"

echo -e "\n${BLUE}Step 3: Checking if columns already exist...${NC}"

# Check if columns already exist (idempotent check)
EXISTING_COLUMNS=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP \
    "export MYSQL_PWD='$MYSQL_PASSWORD'
    mysql -h $MYSQL_HOST -u $MYSQL_USER $MYSQL_DB -N -e \"
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = '$MYSQL_DB' 
      AND TABLE_NAME = 'UserRequests' 
      AND COLUMN_NAME IN ('Age', 'Race', 'AccidentAddress');
    \"" 2>/dev/null || echo "0")

if [ "$EXISTING_COLUMNS" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  Some accident fields already exist. Checking all columns...${NC}"
    
    # Check UserRequests table
    USERREQUESTS_COLS=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP \
        "export MYSQL_PWD='$MYSQL_PASSWORD'
        mysql -h $MYSQL_HOST -u $MYSQL_USER $MYSQL_DB -N -e \"
        SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = '$MYSQL_DB' 
          AND TABLE_NAME = 'UserRequests' 
          AND COLUMN_NAME IN ('Age', 'Race', 'AccidentAddress', 'AccidentDate', 'VehicleDetails', 
                              'DateReported', 'PoliceCaseNumber', 'AccidentDetails', 'RoadConditions',
                              'DoctorsInformation', 'LawyersInformation', 'AdditionalNotes');
        \"" 2>/dev/null || echo "0")
    
    # Check Users table
    USERS_COLS=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP \
        "export MYSQL_PWD='$MYSQL_PASSWORD'
        mysql -h $MYSQL_HOST -u $MYSQL_USER $MYSQL_DB -N -e \"
        SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = '$MYSQL_DB' 
          AND TABLE_NAME = 'Users' 
          AND COLUMN_NAME IN ('Age', 'Race', 'AccidentAddress', 'AccidentDate', 'VehicleDetails', 
                              'DateReported', 'PoliceCaseNumber', 'AccidentDetails', 'RoadConditions',
                              'DoctorsInformation', 'LawyersInformation', 'AdditionalNotes');
        \"" 2>/dev/null || echo "0")
    
    if [ "$USERREQUESTS_COLS" -eq 12 ] && [ "$USERS_COLS" -eq 12 ]; then
        echo -e "${GREEN}✅ All accident fields already exist. Migration already applied!${NC}"
        exit 0
    else
        echo -e "${YELLOW}⚠️  Some columns exist but not all. Will attempt to add missing ones...${NC}"
    fi
fi

echo -e "\n${BLUE}Step 4: Applying migration...${NC}"

# Apply migration (with error handling for existing columns)
# Use mysql with proper password handling (avoid command line password warning)
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << EOF
    set -e
    # Use mysql_config_editor or environment variable to avoid password in command line
    export MYSQL_PWD='$MYSQL_PASSWORD'
    mysql -h $MYSQL_HOST -u $MYSQL_USER $MYSQL_DB < $APP_DIR/AddAccidentFieldsMigration.sql 2>&1 | grep -v "Duplicate column name" | grep -v "Warning" || true
    unset MYSQL_PWD
EOF

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Migration SQL executed${NC}"
else
    echo -e "${YELLOW}⚠️  Migration may have encountered errors (some columns might already exist)${NC}"
fi

echo -e "\n${BLUE}Step 5: Verifying migration...${NC}"

# Verify migration
VERIFY_RESULT=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << EOF
    export MYSQL_PWD='$MYSQL_PASSWORD'
    mysql -h $MYSQL_HOST -u $MYSQL_USER $MYSQL_DB << 'MYSQL'
    SELECT 
        (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
         WHERE TABLE_SCHEMA = '$MYSQL_DB' 
           AND TABLE_NAME = 'UserRequests' 
           AND COLUMN_NAME IN ('Age', 'Race', 'AccidentAddress', 'AccidentDate', 'VehicleDetails', 
                               'DateReported', 'PoliceCaseNumber', 'AccidentDetails', 'RoadConditions',
                               'DoctorsInformation', 'LawyersInformation', 'AdditionalNotes')) as UserRequestsCols,
        (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
         WHERE TABLE_SCHEMA = '$MYSQL_DB' 
           AND TABLE_NAME = 'Users' 
           AND COLUMN_NAME IN ('Age', 'Race', 'AccidentAddress', 'AccidentDate', 'VehicleDetails', 
                               'DateReported', 'PoliceCaseNumber', 'AccidentDetails', 'RoadConditions',
                               'DoctorsInformation', 'LawyersInformation', 'AdditionalNotes')) as UsersCols;
MYSQL
    unset MYSQL_PWD
EOF
)

USERREQUESTS_COUNT=$(echo "$VERIFY_RESULT" | tail -1 | awk '{print $1}')
USERS_COUNT=$(echo "$VERIFY_RESULT" | tail -1 | awk '{print $2}')

if [ "$USERREQUESTS_COUNT" -eq 12 ] && [ "$USERS_COUNT" -eq 12 ]; then
    echo -e "${GREEN}✅ Migration verified successfully!${NC}"
    echo -e "   UserRequests table: ${GREEN}$USERREQUESTS_COUNT/12${NC} columns"
    echo -e "   Users table: ${GREEN}$USERS_COUNT/12${NC} columns"
    echo -e "\n${GREEN}========================================${NC}"
    echo -e "${GREEN}Migration completed successfully!${NC}"
    echo -e "${GREEN}========================================${NC}"
else
    echo -e "${RED}❌ Migration verification failed!${NC}"
    echo -e "   UserRequests table: $USERREQUESTS_COUNT/12 columns"
    echo -e "   Users table: $USERS_COUNT/12 columns"
    echo -e "\n${YELLOW}Please check the migration manually${NC}"
    exit 1
fi
