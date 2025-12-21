#!/bin/bash
# Fix nginx timeout for /api location to prevent 504 Gateway Timeout errors

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
NGINX_CONFIG="/etc/nginx/sites-available/caseflowstage"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Fix Nginx API Timeout Settings${NC}"
echo -e "${GREEN}========================================${NC}"

echo -e "\n${BLUE}Updating nginx configuration...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP bash << ENDSSH
    # Backup current config
    cp $NGINX_CONFIG $NGINX_CONFIG.backup.\$(date +%Y%m%d_%H%M%S)
    
    # Check if timeout settings already exist
    if grep -A 10 "location /api" $NGINX_CONFIG | grep -q "proxy_read_timeout"; then
        echo "⚠️  Timeout settings already exist, updating them..."
        # Remove existing timeout lines
        sed -i '/location \/api/,/proxy_cache_bypass/ {
            /proxy_read_timeout/d
            /proxy_connect_timeout/d
            /proxy_send_timeout/d
        }' $NGINX_CONFIG
        
        # Add new timeout settings after proxy_cache_bypass
        sed -i '/location \/api/,/proxy_cache_bypass/ {
            /proxy_cache_bypass/a\
        proxy_read_timeout 300s;\
        proxy_connect_timeout 60s;\
        proxy_send_timeout 300s;
        }' $NGINX_CONFIG
    else
        echo "✅ Adding timeout settings..."
        # Add timeout settings after proxy_cache_bypass in /api location
        sed -i '/location \/api/,/proxy_cache_bypass/ {
            /proxy_cache_bypass/a\
        proxy_read_timeout 300s;\
        proxy_connect_timeout 60s;\
        proxy_send_timeout 300s;
        }' $NGINX_CONFIG
    fi
    
    # Also update /odata location if it doesn't have timeouts
    if ! grep -A 10 "location /odata" $NGINX_CONFIG | grep -q "proxy_read_timeout"; then
        echo "✅ Adding timeout settings to /odata location..."
        sed -i '/location \/odata/,/proxy_cache_bypass/ {
            /proxy_cache_bypass/a\
        proxy_read_timeout 300s;\
        proxy_connect_timeout 60s;\
        proxy_send_timeout 300s;
        }' $NGINX_CONFIG
    fi
    
    echo "✅ Configuration updated"
ENDSSH

echo -e "\n${BLUE}Testing nginx configuration...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    if nginx -t; then
        echo "✅ Nginx configuration is valid"
    else
        echo "❌ Nginx configuration has errors!"
        exit 1
    fi
ENDSSH

echo -e "\n${BLUE}Reloading nginx...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    systemctl reload nginx
    
    if systemctl is-active --quiet nginx; then
        echo "✅ Nginx reloaded successfully"
    else
        echo "❌ Nginx failed to reload"
        systemctl status nginx --no-pager | head -15
        exit 1
    fi
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Nginx Timeout Settings Updated!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "\n${YELLOW}Timeout settings:${NC}"
echo -e "  - proxy_read_timeout: 300s (5 minutes)"
echo -e "  - proxy_connect_timeout: 60s"
echo -e "  - proxy_send_timeout: 300s"

