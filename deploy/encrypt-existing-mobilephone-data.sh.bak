#!/bin/bash

# Script to encrypt existing plain text MobilePhone data on DigitalOcean
# This script runs the C# encryption script on the server
# Run this AFTER apply-mobilephone-encryption-migration.sh

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
echo -e "${GREEN}Encrypt Existing MobilePhone Data${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check SSH key
if [ ! -f "$SSH_KEY" ]; then
    echo -e "${RED}❌ SSH key not found: $SSH_KEY${NC}"
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

echo -e "${YELLOW}⚠️  This will encrypt all plain text phone numbers in the database${NC}"
echo -e "${YELLOW}   Make sure the migration has been applied first!${NC}"
echo ""
read -p "Do you want to continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo -e "${YELLOW}Encryption cancelled.${NC}"
    exit 0
fi

echo ""
echo -e "${BLUE}Step 1: Checking if application is running...${NC}"

# Check if service is running
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    if systemctl is-active --quiet mental-health-app; then
        echo "✅ Application service is running"
    else
        echo "⚠️  Application service is not running. Starting it..."
        systemctl start mental-health-app || echo "❌ Failed to start service"
    fi
ENDSSH

echo ""
echo -e "${BLUE}Step 2: Running encryption script on server...${NC}"
echo -e "${YELLOW}This may take a few minutes depending on the amount of data...${NC}"

# Run the encryption script on the server
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << ENDSSH
    cd $APP_DIR
    
    # Source dotnet environment (if available)
    if [ -f /etc/profile.d/dotnet.sh ]; then
        source /etc/profile.d/dotnet.sh
    fi
    
    # Find dotnet path if not in PATH
    if ! command -v dotnet &> /dev/null; then
        DOTNET_PATH=\$(which dotnet 2>/dev/null || find /usr -name dotnet 2>/dev/null | head -1 || find /opt -name dotnet 2>/dev/null | head -1 || echo "")
        if [ -n "\$DOTNET_PATH" ]; then
            export PATH="\$(dirname \$DOTNET_PATH):\$PATH"
        else
            echo "❌ dotnet command not found. Please install .NET SDK or ensure it's in PATH"
            exit 1
        fi
    fi
    
    # Stop the service temporarily to avoid conflicts
    echo "Stopping application service..."
    systemctl stop mental-health-app || true
    
    # Run the encryption script
    echo "Running encryption script..."
    dotnet run --project SM_MentalHealthApp.Server --encrypt-mobilephones 2>&1 | tee /tmp/mobilephone-encryption.log
    
    # Check exit code
    if [ \${PIPESTATUS[0]} -eq 0 ]; then
        echo "✅ Encryption completed successfully"
    else
        echo "❌ Encryption failed. Check /tmp/mobilephone-encryption.log for details"
        exit 1
    fi
    
    # Restart the service
    echo "Restarting application service..."
    systemctl start mental-health-app
    
    # Wait a moment for service to start
    sleep 3
    
    # Check if service started successfully
    if systemctl is-active --quiet mental-health-app; then
        echo "✅ Application service restarted successfully"
    else
        echo "⚠️  Service may not have started. Check status with: systemctl status mental-health-app"
    fi
ENDSSH

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✅ MobilePhone Encryption Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo -e "${YELLOW}Verification:${NC}"
    echo -e "Check the logs on the server: ${BLUE}/tmp/mobilephone-encryption.log${NC}"
    echo -e "Or view service logs: ${BLUE}journalctl -u mental-health-app -f${NC}"
else
    echo ""
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}❌ Encryption Failed!${NC}"
    echo -e "${RED}========================================${NC}"
    echo ""
    echo -e "Check the logs on the server: ${BLUE}/tmp/mobilephone-encryption.log${NC}"
    exit 1
fi

