#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Fix Nginx configuration properly - this version correctly handles variables

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_NAME="mental-health-app"
APP_DIR="/opt/mental-health-app"
DOMAIN="www.caseflowstage.store"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Fix Nginx Configuration (Proper)${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

echo -e "\n${BLUE}Step 1: Creating backup...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    if [ -f /etc/nginx/sites-available/$APP_NAME ]; then
        cp /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-available/$APP_NAME.backup.$(date +%Y%m%d_%H%M%S)
        echo "✅ Backup created"
    fi
ENDSSH

echo -e "\n${BLUE}Step 2: Checking if backend server is HTTP or HTTPS...${NC}"

# Check what the backend server is actually running
BACKEND_PROTOCOL=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "curl -k -s -o /dev/null -w '%{http_code}' https://127.0.0.1:5262/api/health 2>/dev/null || echo 'http'")

if [ "$BACKEND_PROTOCOL" = "200" ] || [ "$BACKEND_PROTOCOL" = "401" ]; then
    BACKEND_URL="https://api_backend"
    echo "✅ Backend is HTTPS"
else
    BACKEND_URL="http://api_backend"
    echo "✅ Backend is HTTP"
fi

echo -e "\n${BLUE}Step 3: Creating proper nginx configuration...${NC}"

# Create the config file with proper escaping
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP bash << ENDSSH
    cat > /etc/nginx/sites-available/$APP_NAME << 'NGINXCONF'
# Upstream for API server
upstream api_backend {
    server 127.0.0.1:5262;
}

# HTTP server - redirect to HTTPS
server {
    listen 80;
    server_name ${DROPLET_IP} www.caseflowstage.store caseflowstage.store;
    
    # Allow Let's Encrypt validation
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
    
    # Redirect all other traffic to HTTPS
    location / {
        return 301 https://\$host\$request_uri;
    }
}

# HTTPS server - supports both IP and domain
server {
    listen 443 ssl http2;
    server_name ${DROPLET_IP} www.caseflowstage.store caseflowstage.store;
    
    # SSL certificates
    ssl_certificate /etc/nginx/ssl/nginx-selfsigned.crt;
    ssl_certificate_key /etc/nginx/ssl/nginx-selfsigned.key;
    
    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    
    # API proxy
    location /api {
        proxy_pass ${BACKEND_URL};
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
    
    # OData proxy
    location /odata {
        proxy_pass ${BACKEND_URL};
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_set_header Accept "application/json, application/odata+json";
        proxy_cache_bypass \$http_upgrade;
    }
    
    # SignalR Hub
    location /mobilehub {
        proxy_pass ${BACKEND_URL};
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 86400;
    }
    
    # WebSocket support
    location /realtime {
        proxy_pass ${BACKEND_URL};
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
    
    # Root directory for static files
    root /opt/mental-health-app/client/wwwroot;
    
    # Blazor framework files
    location /_framework {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
    
    # Static files (Blazor client) - SPA routing
    location / {
        try_files \$uri \$uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
}
NGINXCONF

    # Replace variables
    sed -i "s/\${DROPLET_IP}/$DROPLET_IP/g" /etc/nginx/sites-available/$APP_NAME
    sed -i "s|\${BACKEND_URL}|$BACKEND_URL|g" /etc/nginx/sites-available/$APP_NAME
    
    echo "✅ Configuration created"
ENDSSH

echo -e "\n${BLUE}Step 4: Testing nginx configuration...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    if nginx -t; then
        echo "✅ Nginx configuration is valid"
    else
        echo "❌ Nginx configuration has errors!"
        exit 1
    fi
ENDSSH

echo -e "\n${BLUE}Step 5: Reloading nginx...${NC}"

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

echo -e "\n${BLUE}Step 6: Verifying...${NC}"

sleep 2

echo -e "\n${YELLOW}Testing IP address...${NC}"
IP_TEST=$(curl -k -s -o /dev/null -w '%{http_code}' "https://$DROPLET_IP/login" 2>/dev/null || echo "000")
if [ "$IP_TEST" = "200" ]; then
    echo -e "${GREEN}✅ IP address is working (HTTP $IP_TEST)${NC}"
else
    echo -e "${YELLOW}⚠️  IP address test returned: $IP_TEST${NC}"
fi

echo -e "\n${YELLOW}Testing domain...${NC}"
DOMAIN_TEST=$(curl -k -s -o /dev/null -w '%{http_code}' "https://$DOMAIN/login" 2>/dev/null || echo "000")
if [ "$DOMAIN_TEST" = "200" ]; then
    echo -e "${GREEN}✅ Domain is working (HTTP $DOMAIN_TEST)${NC}"
else
    echo -e "${YELLOW}⚠️  Domain test returned: $DOMAIN_TEST${NC}"
fi

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Nginx Configuration Fixed!${NC}"
echo -e "${GREEN}========================================${NC}"

