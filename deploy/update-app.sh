#!/bin/bash

# Quick update script - Use this to update the application after code changes
# This is faster than running the full deployment script

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Configuration - UPDATE THESE (should match digitalocean-deploy.sh)
DROPLET_IP="159.65.242.79"  # Your DigitalOcean droplet IP address
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_NAME="mental-health-app"
APP_DIR="/opt/$APP_NAME"
DEPLOY_USER="appuser"

if [ -z "$DROPLET_IP" ]; then
    echo "ERROR: DROPLET_IP is not set!"
    exit 1
fi

echo -e "${GREEN}Building application...${NC}"
cd "$(dirname "$0")/.."
dotnet publish SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj -c Release -o ./publish/server
dotnet publish SM_MentalHealthApp.Client/SM_MentalHealthApp.Client.csproj -c Release -o ./publish/client

echo -e "${GREEN}Copying files to server...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "systemctl stop $APP_NAME"
scp -i "$SSH_KEY_PATH" -r ./publish/server/* "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/"
scp -i "$SSH_KEY_PATH" -r ./publish/client/* "$DROPLET_USER@$DROPLET_IP:$APP_DIR/client/"

echo -e "${GREEN}Restarting application...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "chown -R $DEPLOY_USER:$DEPLOY_USER $APP_DIR && systemctl start $APP_NAME"

echo -e "${GREEN}Update complete!${NC}"
echo "Check status: ssh $DROPLET_USER@$DROPLET_IP 'systemctl status $APP_NAME'"

