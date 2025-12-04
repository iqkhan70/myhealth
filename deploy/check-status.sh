#!/bin/bash

# Check application status on DigitalOcean server
# This script verifies if the application is running and healthy

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration - UPDATE THESE
DROPLET_IP="159.65.242.79"
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
SERVICE_NAME="mental-health-app"
APP_DIR="/opt/mental-health-app"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Application Status Check${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}1. Systemd Service Status${NC}"
echo -e "${BLUE}========================================${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    systemctl status $SERVICE_NAME --no-pager -l | head -20 || echo "Service not found or not running"
ENDSSH

echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}2. Service Active Status${NC}"
echo -e "${BLUE}========================================${NC}"

SERVICE_STATUS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "systemctl is-active $SERVICE_NAME 2>/dev/null || echo 'inactive'")

if [ "$SERVICE_STATUS" = "active" ]; then
    echo -e "${GREEN}✅ Service is ACTIVE${NC}"
else
    echo -e "${RED}❌ Service is $SERVICE_STATUS${NC}"
fi

echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}3. Nginx Status${NC}"
echo -e "${BLUE}========================================${NC}"

NGINX_STATUS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "systemctl is-active nginx 2>/dev/null || echo 'inactive'")

if [ "$NGINX_STATUS" = "active" ]; then
    echo -e "${GREEN}✅ Nginx is ACTIVE${NC}"
else
    echo -e "${RED}❌ Nginx is $NGINX_STATUS${NC}"
fi

echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}4. Port Status${NC}"
echo -e "${BLUE}========================================${NC}"

# Check if app is listening on port 5262
PORT_5262=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "netstat -tlnp 2>/dev/null | grep ':5262' || ss -tlnp 2>/dev/null | grep ':5262' || echo 'not listening'")

if echo "$PORT_5262" | grep -q "5262"; then
    echo -e "${GREEN}✅ Application is listening on port 5262${NC}"
    echo "$PORT_5262" | head -1
else
    echo -e "${RED}❌ Application is NOT listening on port 5262${NC}"
fi

# Check if Nginx is listening on port 80
PORT_80=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "netstat -tlnp 2>/dev/null | grep ':80' || ss -tlnp 2>/dev/null | grep ':80' || echo 'not listening'")

if echo "$PORT_80" | grep -q "80"; then
    echo -e "${GREEN}✅ Nginx is listening on port 80${NC}"
else
    echo -e "${RED}❌ Nginx is NOT listening on port 80${NC}"
fi

echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}5. HTTP Response Test${NC}"
echo -e "${BLUE}========================================${NC}"

# Test if we can reach the application via HTTP
HTTP_RESPONSE=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "curl -s -o /dev/null -w '%{http_code}' http://localhost:5262 2>/dev/null || echo '000'")

if [ "$HTTP_RESPONSE" = "200" ] || [ "$HTTP_RESPONSE" = "302" ] || [ "$HTTP_RESPONSE" = "301" ]; then
    echo -e "${GREEN}✅ Application responds with HTTP $HTTP_RESPONSE${NC}"
elif [ "$HTTP_RESPONSE" = "000" ]; then
    echo -e "${RED}❌ Cannot connect to application (curl failed)${NC}"
else
    echo -e "${YELLOW}⚠️  Application responds with HTTP $HTTP_RESPONSE${NC}"
fi

# Test via Nginx (external access)
NGINX_RESPONSE=$(curl -s -o /dev/null -w '%{http_code}' "http://$DROPLET_IP" 2>/dev/null || echo "000")

if [ "$NGINX_RESPONSE" = "200" ] || [ "$NGINX_RESPONSE" = "302" ] || [ "$NGINX_RESPONSE" = "301" ]; then
    echo -e "${GREEN}✅ Nginx proxy responds with HTTP $NGINX_RESPONSE${NC}"
    echo -e "${GREEN}   Application is accessible at: http://$DROPLET_IP${NC}"
elif [ "$NGINX_RESPONSE" = "000" ]; then
    echo -e "${RED}❌ Cannot connect via Nginx (external access failed)${NC}"
else
    echo -e "${YELLOW}⚠️  Nginx responds with HTTP $NGINX_RESPONSE${NC}"
fi

echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}6. Recent Application Logs (last 10 lines)${NC}"
echo -e "${BLUE}========================================${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    journalctl -u $SERVICE_NAME --no-pager -n 10 2>/dev/null || echo "No logs found"
ENDSSH

echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}7. Process Status${NC}"
echo -e "${BLUE}========================================${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    echo "Dotnet processes:"
    ps aux | grep -E "dotnet|SM_MentalHealthApp" | grep -v grep || echo "No dotnet processes found"
    echo ""
    echo "Nginx processes:"
    ps aux | grep nginx | grep -v grep | head -3 || echo "No nginx processes found"
ENDSSH

echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}8. Resource Usage${NC}"
echo -e "${BLUE}========================================${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    echo "Memory usage:"
    free -h | head -2
    echo ""
    echo "Disk usage:"
    df -h / | tail -1
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Status Check Complete!${NC}"
echo -e "${GREEN}========================================${NC}"

echo -e "\n${YELLOW}Useful Commands:${NC}"
echo "  View logs:      ssh $DROPLET_USER@$DROPLET_IP 'journalctl -u $SERVICE_NAME -f'"
echo "  Restart app:    ssh $DROPLET_USER@$DROPLET_IP 'systemctl restart $SERVICE_NAME'"
echo "  Stop app:       ssh $DROPLET_USER@$DROPLET_IP 'systemctl stop $SERVICE_NAME'"
echo "  Start app:      ssh $DROPLET_USER@$DROPLET_IP 'systemctl start $SERVICE_NAME'"
echo "  Check Nginx:    ssh $DROPLET_USER@$DROPLET_IP 'systemctl status nginx'"
echo ""
echo "  Application URL: http://$DROPLET_IP"

