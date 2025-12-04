#!/bin/bash

# Copy migration files to server so dotnet ef can run
# This copies the .csproj file and Migrations folder to the server

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DROPLET_IP="159.65.242.79"
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Copy Migration Files to Server${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if we're in the project root
if [ ! -f "SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj" ]; then
    echo -e "${RED}ERROR: Please run this script from the project root directory${NC}"
    exit 1
fi

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

echo -e "\n${YELLOW}Copying .csproj file to server...${NC}"
scp -i "$SSH_KEY_PATH" SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/"

echo -e "\n${YELLOW}Copying Migrations folder...${NC}"
if [ -d "SM_MentalHealthApp.Server/Migrations" ]; then
    echo "Found Migrations folder, copying all migration files..."
    ssh -i "$SSH_KEY_PATH" "$DROPLET_USER@$DROPLET_IP" "mkdir -p $APP_DIR/server/Migrations"
    scp -i "$SSH_KEY_PATH" -r SM_MentalHealthApp.Server/Migrations/* "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/Migrations/"
    echo "✅ Migrations copied"
else
    echo -e "${YELLOW}⚠️ No Migrations folder found locally${NC}"
    echo "If you have migrations, they should be in SM_MentalHealthApp.Server/Migrations/"
fi

echo -e "\n${YELLOW}Copying Data folder (contains DbContext)...${NC}"
if [ -d "SM_MentalHealthApp.Server/Data" ]; then
    ssh -i "$SSH_KEY_PATH" "$DROPLET_USER@$DROPLET_IP" "mkdir -p $APP_DIR/server/Data"
    scp -i "$SSH_KEY_PATH" -r SM_MentalHealthApp.Server/Data/* "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/Data/"
    echo "✅ Data folder copied"
fi

echo -e "\n${YELLOW}Copying appsettings files...${NC}"
# Copy appsettings.json if it exists (for reference)
if [ -f "SM_MentalHealthApp.Server/appsettings.json" ]; then
    scp -i "$SSH_KEY_PATH" SM_MentalHealthApp.Server/appsettings.json "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/appsettings.json.local" 2>/dev/null || true
fi

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Files Copied!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Now on the server, run:"
echo "  cd /opt/mental-health-app/server"
echo "  export PATH=\$PATH:/usr/share/dotnet:/root/.dotnet/tools"
echo "  dotnet ef database update"

