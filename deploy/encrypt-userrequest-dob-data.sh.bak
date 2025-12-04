#!/bin/bash

# Script to encrypt existing plain text DateOfBirth data in UserRequests table on DigitalOcean
# This uses the EncryptExistingDateOfBirthData script via --encrypt-dob argument

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
echo -e "${GREEN}Encrypt UserRequests DateOfBirth Data${NC}"
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
    
    echo "Sample UserRequests DateOfBirthEncrypted data (first 30 chars):"
    mysql -u mentalhealth_user mentalhealthdb -e "SELECT Id, FirstName, LEFT(DateOfBirthEncrypted, 30) as DOB_Preview FROM UserRequests WHERE DateOfBirthEncrypted IS NOT NULL AND DateOfBirthEncrypted != '' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000' LIMIT 5;" 2>/dev/null || echo "Error querying data"
    
    echo ""
    echo "Counting plain text dates (ISO format like '2005-02-03T...'):"
    PLAIN_TEXT_COUNT=$(mysql -u mentalhealth_user mentalhealthdb -e "SELECT COUNT(*) as cnt FROM UserRequests WHERE DateOfBirthEncrypted LIKE '%-%-%T%:%:%' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000';" 2>/dev/null | tail -1)
    echo "Found $PLAIN_TEXT_COUNT records with plain text dates (need encryption)"
    
    unset MYSQL_PWD
ENDSSH

# Copy updated Program.cs to server
echo -e "\n${BLUE}Step 2: Updating Program.cs on server...${NC}"
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    "$(pwd)/SM_MentalHealthApp.Server/Program.cs" \
    root@$SERVER_IP:$APP_DIR/Program.cs

# Run encryption
echo -e "\n${BLUE}Step 3: Running encryption script...${NC}"
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    cd /opt/mental-health-app/server
    
    # Find dotnet
    DOTNET_PATH=$(which dotnet 2>/dev/null || find /usr/local -name dotnet 2>/dev/null | head -1 || find /usr -name dotnet 2>/dev/null | head -1 || echo "")
    
    if [ -z "$DOTNET_PATH" ]; then
        echo "❌ dotnet not found. Please install .NET SDK first."
        exit 1
    fi
    
    export PATH=$PATH:$(dirname "$DOTNET_PATH"):/usr/share/dotnet:/root/.dotnet/tools
    
    echo "✅ dotnet found: $DOTNET_PATH"
    echo "✅ dotnet version: $($DOTNET_PATH --version 2>&1 || echo 'version check failed')"
    echo ""
    echo "Running: dotnet run --encrypt-dob"
    echo "This will encrypt all plain text DateOfBirth data in Users and UserRequests tables..."
    echo ""
    
    # Run the encryption script
    # Use the full path to dotnet and specify the project file
    $DOTNET_PATH run --project SM_MentalHealthApp.Server.csproj --encrypt-dob 2>&1 || {
        echo "⚠️  Trying alternative method..."
        cd SM_MentalHealthApp.Server 2>/dev/null || true
        $DOTNET_PATH run --encrypt-dob 2>&1
    }
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "✅ Encryption completed successfully!"
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
    
    echo "Sample encrypted data (first 50 chars - should be base64, not ISO date):"
    mysql -u mentalhealth_user mentalhealthdb -e "SELECT Id, FirstName, LEFT(DateOfBirthEncrypted, 50) as DOB_Preview FROM UserRequests WHERE DateOfBirthEncrypted IS NOT NULL AND DateOfBirthEncrypted != '' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000' LIMIT 5;" 2>/dev/null || echo "Error querying data"
    
    echo ""
    echo "Checking for remaining plain text dates:"
    REMAINING_PLAIN=$(mysql -u mentalhealth_user mentalhealthdb -e "SELECT COUNT(*) as cnt FROM UserRequests WHERE DateOfBirthEncrypted LIKE '%-%-%T%:%:%' AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000';" 2>/dev/null | tail -1)
    if [ "$REMAINING_PLAIN" -eq 0 ]; then
        echo "✅ All dates are encrypted!"
    else
        echo "⚠️  $REMAINING_PLAIN records still have plain text dates"
    fi
    
    unset MYSQL_PWD
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Encryption Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
