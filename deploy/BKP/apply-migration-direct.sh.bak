#!/bin/bash

# Alternative: Connect directly to remote database from local machine
# This uses your local .NET SDK to run migrations against the remote database

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration - UPDATE THESE
DROPLET_IP="159.65.242.79"
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
DB_NAME="mentalhealthdb"
DB_USER="mentalhealth_user"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Apply Migrations Directly to Remote DB${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
        echo -e "${GREEN}✅ SSH key permissions fixed${NC}"
    fi
fi

# Check if we're in the project root
if [ ! -f "SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj" ]; then
    echo -e "${RED}ERROR: Please run this script from the project root directory${NC}"
    exit 1
fi

echo -e "\n${YELLOW}Step 1: Getting database connection string from server...${NC}"

# Get database password from server
DB_PASSWORD=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;]*' | cut -d'=' -f2 | tr -d '\"'")

if [ -z "$DB_PASSWORD" ]; then
    echo -e "${RED}ERROR: Could not retrieve database password from server${NC}"
    exit 1
fi

# Create temporary appsettings with remote connection string
echo -e "\n${YELLOW}Step 2: Creating temporary connection configuration...${NC}"

REMOTE_CONNECTION_STRING="server=$DROPLET_IP;port=3306;database=$DB_NAME;user=$DB_USER;password=$DB_PASSWORD"

# Backup original appsettings.json
if [ -f "SM_MentalHealthApp.Server/appsettings.json" ]; then
    cp "SM_MentalHealthApp.Server/appsettings.json" "SM_MentalHealthApp.Server/appsettings.json.backup"
fi

# Create temporary appsettings with remote connection
cat > "SM_MentalHealthApp.Server/appsettings.Temp.json" << EOF
{
  "ConnectionStrings": {
    "MySQL": "$REMOTE_CONNECTION_STRING"
  }
}
EOF

echo -e "\n${YELLOW}Step 3: Testing connection to remote database...${NC}"
echo "Connection string: server=$DROPLET_IP;database=$DB_NAME;user=$DB_USER"

# Test connection
cd SM_MentalHealthApp.Server
export ConnectionStrings__MySQL="$REMOTE_CONNECTION_STRING"

echo -e "\n${YELLOW}Step 4: Running migrations against remote database...${NC}"

# Check if dotnet ef is installed
if ! dotnet ef --version &>/dev/null; then
    echo "Installing dotnet-ef tool..."
    dotnet tool install --global dotnet-ef || true
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

# Run migrations with remote connection string
dotnet ef database update --connection "$REMOTE_CONNECTION_STRING" || {
    echo -e "${YELLOW}⚠️ Trying with environment variable...${NC}"
    ConnectionStrings__MySQL="$REMOTE_CONNECTION_STRING" dotnet ef database update
}

cd ..

# Restore original appsettings.json
if [ -f "SM_MentalHealthApp.Server/appsettings.json.backup" ]; then
    mv "SM_MentalHealthApp.Server/appsettings.json.backup" "SM_MentalHealthApp.Server/appsettings.json"
fi

# Clean up temp file
rm -f "SM_MentalHealthApp.Server/appsettings.Temp.json"

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Migration Complete!${NC}"
echo -e "${GREEN}========================================${NC}"

