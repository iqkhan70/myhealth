#!/bin/bash

# Database Setup Script
# Use this to create/update the database schema manually

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration - UPDATE THESE (should match digitalocean-deploy.sh)
DROPLET_IP="159.65.242.79"  # Your DigitalOcean droplet IP address
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

if [ -z "$DROPLET_IP" ]; then
    echo -e "${RED}ERROR: DROPLET_IP is not set!${NC}"
    echo "Please edit this script and set DROPLET_IP"
    exit 1
fi

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Database Setup Script${NC}"
echo -e "${GREEN}========================================${NC}"

echo -e "\n${YELLOW}Running Entity Framework migrations...${NC}"
echo -e "${YELLOW}Note: This requires .NET SDK on the server.${NC}"
echo -e "${YELLOW}If you get an error, run: ./install-ef-tool.sh first${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    cd $APP_DIR/server
    
    # Check if .NET SDK is available
    if ! dotnet --list-sdks &>/dev/null; then
        echo "❌ ERROR: .NET SDK not found on server"
        echo ""
        echo "Please run one of these options:"
        echo "  1. Install SDK on server: ./install-ef-tool.sh"
        echo "  2. Generate SQL locally: ./generate-migration-sql.sh"
        echo "  3. Apply directly from local: ./apply-migration-direct.sh"
        exit 1
    fi
    
    # Check if dotnet ef tool is installed
    export PATH=$PATH:/usr/share/dotnet
    export PATH="$PATH:$HOME/.dotnet/tools"
    
    if ! dotnet ef --version &>/dev/null; then
        echo "Installing dotnet-ef tool..."
        dotnet tool install --global dotnet-ef
    fi
    
    # Run migrations
    echo "Running database migrations..."
    dotnet ef database update --no-build || {
        echo "⚠️ Migration with --no-build failed. Trying with build..."
        dotnet ef database update
    }
    
    echo "✅ Database migrations completed"
ENDSSH

echo -e "\n${GREEN}Database setup complete!${NC}"
echo ""
echo "You can verify the database by connecting:"
echo "  ssh $DROPLET_USER@$DROPLET_IP 'mysql -u mentalhealth_user -p mentalhealthdb'"
echo ""
echo "Check the connection string in appsettings.Production.json for the password"

