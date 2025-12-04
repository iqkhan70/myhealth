#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to encrypt existing plain text DateOfBirth data using Python
# This works on servers without .NET SDK

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
echo -e "${GREEN}Encrypt DateOfBirth Data (Python)${NC}"
echo -e "${GREEN}========================================${NC}"

# Check current data
echo -e "\n${BLUE}Step 1: Checking current data...${NC}"
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    DB_PASSWORD=$(python3 << 'PYTHON'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
        config = json.load(f)
        connection_strings = config.get('ConnectionStrings', {})
        mysql_conn = connection_strings.get('MySQL', '')
        
        parts = {}
        for part in mysql_conn.split(';'):
            if '=' in part:
                key, value = part.split('=', 1)
                parts[key.strip().lower()] = value.strip()
        
        print(parts.get('password', ''))
except Exception as e:
    print('', file=sys.stderr)
    sys.exit(1)
PYTHON
)
    
    export MYSQL_PWD="$DB_PASSWORD"
    
    echo "UserRequests with plain text dates:"
    mysql -u mentalhealth_user mentalhealthdb -e "SELECT COUNT(*) as cnt FROM UserRequests WHERE DateOfBirthEncrypted LIKE '%-%-%T%:%:%' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000';" 2>/dev/null || echo "0"
    
    echo "Users with plain text dates:"
    mysql -u mentalhealth_user mentalhealthdb -e "SELECT COUNT(*) as cnt FROM Users WHERE DateOfBirthEncrypted LIKE '%-%-%T%:%:%' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000';" 2>/dev/null || echo "0"
    
    unset MYSQL_PWD
ENDSSH

# Copy Python script to server
echo -e "\n${BLUE}Step 2: Copying encryption script to server...${NC}"
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    "$PROJECT_DIR/deploy/encrypt-dob-python.py" \
    root@$SERVER_IP:/tmp/encrypt-dob.py

# Install pycryptodome if needed and run encryption
echo -e "\n${BLUE}Step 3: Installing dependencies and running encryption...${NC}"
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    # Install pycryptodome and pymysql if needed
    if ! python3 -c "from Crypto.Cipher import AES" 2>/dev/null && ! python3 -c "from Cryptodome.Cipher import AES" 2>/dev/null; then
        echo "Installing pycryptodome..."
        DEBIAN_FRONTEND=noninteractive apt-get update -qq && DEBIAN_FRONTEND=noninteractive apt-get install -y python3-pycryptodome python3-pymysql -qq || {
            echo "❌ Failed to install dependencies"
            exit 1
        }
    fi
    
    if ! python3 -c "import pymysql" 2>/dev/null; then
        echo "Installing pymysql..."
        DEBIAN_FRONTEND=noninteractive apt-get install -y python3-pymysql -qq || {
            echo "❌ Failed to install pymysql"
            exit 1
        }
    fi
    
    echo "✅ Dependencies installed"
    echo ""
    echo "Running encryption script..."
    chmod +x /tmp/encrypt-dob.py
    python3 /tmp/encrypt-dob.py
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "✅ Encryption completed successfully!"
        rm -f /tmp/encrypt-dob.py
    else
        echo ""
        echo "❌ Encryption failed. Check the error messages above."
        exit 1
    fi
ENDSSH

# Verify encryption
echo -e "\n${BLUE}Step 4: Verifying encryption...${NC}"
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    DB_PASSWORD=$(python3 << 'PYTHON'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
        config = json.load(f)
        connection_strings = config.get('ConnectionStrings', {})
        mysql_conn = connection_strings.get('MySQL', '')
        
        parts = {}
        for part in mysql_conn.split(';'):
            if '=' in part:
                key, value = part.split('=', 1)
                parts[key.strip().lower()] = value.strip()
        
        print(parts.get('password', ''))
except Exception as e:
    print('', file=sys.stderr)
    sys.exit(1)
PYTHON
)
    
    export MYSQL_PWD="$DB_PASSWORD"
    
    echo "Checking for remaining plain text dates in UserRequests:"
    REMAINING=$(mysql -u mentalhealth_user mentalhealthdb -e "SELECT COUNT(*) as cnt FROM UserRequests WHERE DateOfBirthEncrypted LIKE '%-%-%T%:%:%' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000';" 2>/dev/null | tail -1)
    if [ "$REMAINING" -eq 0 ]; then
        echo "✅ All UserRequests dates are encrypted!"
    else
        echo "⚠️  $REMAINING UserRequests still have plain text dates"
    fi
    
    echo ""
    echo "Checking for remaining plain text dates in Users:"
    REMAINING=$(mysql -u mentalhealth_user mentalhealthdb -e "SELECT COUNT(*) as cnt FROM Users WHERE DateOfBirthEncrypted LIKE '%-%-%T%:%:%' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000';" 2>/dev/null | tail -1)
    if [ "$REMAINING" -eq 0 ]; then
        echo "✅ All Users dates are encrypted!"
    else
        echo "⚠️  $REMAINING Users still have plain text dates"
    fi
    
    echo ""
    echo "Sample encrypted data (should be base64, not ISO date):"
    mysql -u mentalhealth_user mentalhealthdb -e "SELECT Id, FirstName, LEFT(DateOfBirthEncrypted, 50) as DOB_Preview FROM UserRequests WHERE DateOfBirthEncrypted IS NOT NULL AND DateOfBirthEncrypted != '' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000' LIMIT 3;" 2>/dev/null || echo "Error querying"
    
    unset MYSQL_PWD
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Encryption Complete!${NC}"
echo -e "${GREEN}========================================${NC}"

