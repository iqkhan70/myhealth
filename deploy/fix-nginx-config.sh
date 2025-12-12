#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Fix Nginx configuration to support both IP address and DNS domain
# This script restores a working nginx configuration

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
# DROPLET_IP is loaded from load-droplet-ip.sh

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Fix Nginx Configuration${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

echo -e "\n${BLUE}Step 1: Creating backup of current nginx config...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    # Backup current config
    if [ -f /etc/nginx/sites-available/$APP_NAME ]; then
        cp /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-available/$APP_NAME.backup.$(date +%Y%m%d_%H%M%S)
        echo "✅ Backup created"
    else
        echo "⚠️  No existing config to backup"
    fi
ENDSSH

echo -e "\n${BLUE}Step 2: Ensuring SSL certificates exist...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    # Create certificates directory
    mkdir -p /etc/nginx/ssl
    
    # Generate self-signed certificate for both IP and domain if it doesn't exist
    if [ ! -f /etc/nginx/ssl/nginx-selfsigned.crt ]; then
        openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
            -keyout /etc/nginx/ssl/nginx-selfsigned.key \
            -out /etc/nginx/ssl/nginx-selfsigned.crt \
            -subj "/C=US/ST=State/L=City/O=Organization/CN=$DOMAIN" \
            -addext "subjectAltName=IP:$DROPLET_IP,DNS:$DOMAIN,DNS:caseflowstage.store"
        
        chmod 600 /etc/nginx/ssl/nginx-selfsigned.key
        chmod 644 /etc/nginx/ssl/nginx-selfsigned.crt
        echo "✅ SSL certificate generated"
    else
        echo "✅ SSL certificate already exists"
    fi
ENDSSH

echo -e "\n${BLUE}Step 3: Creating nginx configuration...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    cat > /etc/nginx/sites-available/$APP_NAME << 'NGINXCONF'
# Upstream for API server (HTTPS backend)
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
        return 301 https://$host$request_uri;
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
    
    # API proxy - to HTTPS backend
    location /api {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
    
    # OData proxy - to HTTPS backend (required for OData endpoints)
    location /odata {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Accept "application/json, application/odata+json";
        proxy_cache_bypass $http_upgrade;
    }
    
    # SignalR Hub
    location /mobilehub {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400;
    }
    
    # WebSocket support
    location /realtime {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Root directory for static files
    root /opt/mental-health-app/client/wwwroot;
    
    # Blazor framework files - must come before the catch-all
    location /_framework {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
    
    # Static files (Blazor client) - catch-all for SPA routing
    location / {
        try_files $uri $uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
}
NGINXCONF

    # Replace variables in the config file
    sed -i "s/\${DROPLET_IP}/$DROPLET_IP/g" /etc/nginx/sites-available/$APP_NAME
    
    echo "✅ Nginx configuration created"
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

echo -e "\n${BLUE}Step 6: Verifying nginx is serving content...${NC}"

sleep 2

# Test both IP and domain
echo -e "\n${YELLOW}Testing IP address (https://$DROPLET_IP)...${NC}"
IP_TEST=$(curl -k -s -o /dev/null -w '%{http_code}' "https://$DROPLET_IP/login" 2>/dev/null || echo "000")
if [ "$IP_TEST" = "200" ] || [ "$IP_TEST" = "301" ] || [ "$IP_TEST" = "302" ]; then
    echo -e "${GREEN}✅ IP address is working (HTTP $IP_TEST)${NC}"
else
    echo -e "${YELLOW}⚠️  IP address test returned: $IP_TEST${NC}"
fi

echo -e "\n${YELLOW}Testing domain (https://$DOMAIN)...${NC}"
DOMAIN_TEST=$(curl -k -s -o /dev/null -w '%{http_code}' "https://$DOMAIN/login" 2>/dev/null || echo "000")
if [ "$DOMAIN_TEST" = "200" ] || [ "$DOMAIN_TEST" = "301" ] || [ "$DOMAIN_TEST" = "302" ]; then
    echo -e "${GREEN}✅ Domain is working (HTTP $DOMAIN_TEST)${NC}"
else
    echo -e "${YELLOW}⚠️  Domain test returned: $DOMAIN_TEST${NC}"
    echo -e "${YELLOW}   Make sure DNS is pointing to $DROPLET_IP${NC}"
fi

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Nginx Configuration Fixed!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Your application should now be available at:"
echo "  - https://$DROPLET_IP/login"
echo "  - https://$DOMAIN/login"
echo ""
echo -e "${YELLOW}Note: You'll need to accept the self-signed certificate in your browser${NC}"
echo "The browser will show a security warning - click 'Advanced' and 'Proceed'"

