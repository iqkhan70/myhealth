#!/bin/bash

# Run database migrations on the server via SSH
# This script runs from your LOCAL machine and connects to the server

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
echo -e "${GREEN}Run Database Migrations (via SSH)${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

echo -e "\n${YELLOW}Running database migrations on server...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    export PATH=$PATH:/usr/share/dotnet:/root/.dotnet/tools
    cd /opt/mental-health-app/server
    
    echo "Checking dotnet-ef installation..."
    if ! dotnet ef --version &>/dev/null; then
        echo "❌ dotnet-ef not found. Installing..."
        dotnet tool install --global dotnet-ef --version 9.0.0
        export PATH="$PATH:/root/.dotnet/tools"
    fi
    
    echo "Running migrations..."
    dotnet ef database update --no-build || dotnet ef database update
    
    echo "✅ Migrations completed!"
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Migrations Complete!${NC}"
echo -e "${GREEN}========================================${NC}"

