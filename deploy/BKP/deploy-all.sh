#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# ============================================================================
# Complete DigitalOcean Deployment Script
# ============================================================================
# This script performs a complete deployment including:
# 1. Full application deployment (server + client)
# 2. Database setup and migrations
# 3. HTTPS configuration (server + Nginx)
# 4. Service startup
# ============================================================================

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration - UPDATE THESE VALUES
DROPLET_IP="${DROPLET_IP:-${DROPLET_IP}}"  # Your DigitalOcean droplet IP address (can override with env var)
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_NAME="mental-health-app"
APP_DIR="/opt/$APP_NAME"
SERVICE_NAME="$APP_NAME"

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/.."

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Complete DigitalOcean Deployment${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Configuration:${NC}"
echo -e "  Droplet IP: ${YELLOW}$DROPLET_IP${NC}"
echo -e "  SSH Key: ${YELLOW}$SSH_KEY_PATH${NC}"
echo ""

# Check prerequisites
if [ ! -f "$SSH_KEY_PATH" ]; then
    echo -e "${RED}ERROR: SSH key not found at $SSH_KEY_PATH${NC}"
    exit 1
fi

# Fix SSH key permissions
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Step 0: Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

# Test SSH connection
echo -e "${YELLOW}Testing SSH connection...${NC}"
if ! ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no -o ConnectTimeout=5 "$DROPLET_USER@$DROPLET_IP" "echo 'SSH connection successful'" >/dev/null 2>&1; then
    echo -e "${RED}ERROR: Cannot connect to $DROPLET_IP${NC}"
    echo "Please verify:"
    echo "  1. Droplet IP is correct"
    echo "  2. SSH key is added to the droplet"
    echo "  3. Firewall allows SSH (port 22)"
    exit 1
fi
echo -e "${GREEN}✅ SSH connection successful${NC}"
echo ""

# Step 1: Full Application Deployment
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Step 1: Deploying Application${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
if [ -f "$SCRIPT_DIR/digitalocean-deploy.sh" ]; then
    # Update DROPLET_IP in the script temporarily
    sed -i.bak "s|DROPLET_IP=\".*\"|DROPLET_IP=\"$DROPLET_IP\"|g" "$SCRIPT_DIR/digitalocean-deploy.sh"
    chmod +x "$SCRIPT_DIR/digitalocean-deploy.sh"
    "$SCRIPT_DIR/digitalocean-deploy.sh"
    # Restore backup
    if [ -f "$SCRIPT_DIR/digitalocean-deploy.sh.bak" ]; then
        mv "$SCRIPT_DIR/digitalocean-deploy.sh.bak" "$SCRIPT_DIR/digitalocean-deploy.sh"
    fi
else
    echo -e "${RED}ERROR: digitalocean-deploy.sh not found${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Step 2: Setting Up HTTPS for Server${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
if [ -f "$SCRIPT_DIR/setup-https-server.sh" ]; then
    chmod +x "$SCRIPT_DIR/setup-https-server.sh"
    "$SCRIPT_DIR/setup-https-server.sh"
else
    echo -e "${YELLOW}⚠️  setup-https-server.sh not found, skipping...${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Step 3: Setting Up HTTPS for Nginx${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
if [ -f "$SCRIPT_DIR/setup-nginx-https.sh" ]; then
    chmod +x "$SCRIPT_DIR/setup-nginx-https.sh"
    "$SCRIPT_DIR/setup-nginx-https.sh"
else
    echo -e "${YELLOW}⚠️  setup-nginx-https.sh not found, skipping...${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Step 4: Running Database Migrations${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Choose migration method:${NC}"
echo "  1) Generate SQL locally and apply remotely (recommended)"
echo "  2) Copy migration files and run on server"
echo "  3) Skip migrations (run manually later)"
read -p "Enter choice [1-3]: " MIGRATION_CHOICE

case $MIGRATION_CHOICE in
    1)
        if [ -f "$SCRIPT_DIR/generate-migration-sql.sh" ]; then
            chmod +x "$SCRIPT_DIR/generate-migration-sql.sh"
            "$SCRIPT_DIR/generate-migration-sql.sh"
        else
            echo -e "${RED}ERROR: generate-migration-sql.sh not found${NC}"
        fi
        ;;
    2)
        if [ -f "$SCRIPT_DIR/copy-migration-files.sh" ]; then
            chmod +x "$SCRIPT_DIR/copy-migration-files.sh"
            "$SCRIPT_DIR/copy-migration-files.sh"
            echo ""
            echo -e "${YELLOW}Now run migrations on server:${NC}"
            echo "  ssh root@$DROPLET_IP 'cd $APP_DIR/server && dotnet ef database update'"
        else
            echo -e "${RED}ERROR: copy-migration-files.sh not found${NC}"
        fi
        ;;
    3)
        echo -e "${YELLOW}⚠️  Skipping migrations. Run manually later:${NC}"
        echo "  ssh root@$DROPLET_IP 'cd $APP_DIR/server && dotnet ef database update'"
        ;;
    *)
        echo -e "${YELLOW}⚠️  Invalid choice, skipping migrations${NC}"
        ;;
esac

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Step 5: Verifying Deployment${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check service status
echo -e "${YELLOW}Checking service status...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    echo "--- Service Status ---"
    systemctl status $SERVICE_NAME --no-pager | head -10 || echo "Service not running"
    echo ""
    echo "--- Nginx Status ---"
    systemctl status nginx --no-pager | head -5 || echo "Nginx not running"
    echo ""
    echo "--- Ports Listening ---"
    netstat -tlnp | grep -E ':(80|443|5262|6379|3306)' || echo "No expected ports found"
ENDSSH

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo ""
echo "1. ${YELLOW}Update Configuration:${NC}"
echo "   ssh root@$DROPLET_IP"
echo "   nano $APP_DIR/server/appsettings.Production.json"
echo "   # Update: Agora App ID/Certificate, JWT Secret, S3 keys, etc."
echo "   systemctl restart $SERVICE_NAME"
echo ""
echo "2. ${YELLOW}Test the Application:${NC}"
echo "   https://$DROPLET_IP/login"
echo "   (Accept the self-signed certificate warning)"
echo ""
echo "3. ${YELLOW}Check Logs (if needed):${NC}"
echo "   ssh root@$DROPLET_IP 'journalctl -u $SERVICE_NAME -f'"
echo ""
echo "4. ${YELLOW}Import Data (optional):${NC}"
echo "   ./deploy/export-import-data.sh"
echo ""
echo -e "${GREEN}✅ Deployment completed successfully!${NC}"

