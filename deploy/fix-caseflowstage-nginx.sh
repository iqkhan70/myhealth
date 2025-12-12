#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Fix the caseflowstage nginx config to properly serve static files and proxy API

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_NAME="mental-health-app"
DOMAIN="www.caseflowstage.store"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Fix caseflowstage Nginx Config${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if backend is HTTP or HTTPS
BACKEND_PROTOCOL=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "curl -k -s -o /dev/null -w '%{http_code}' https://127.0.0.1:5262/api/health 2>/dev/null || echo 'http'")

if [ "$BACKEND_PROTOCOL" = "200" ] || [ "$BACKEND_PROTOCOL" = "401" ]; then
    BACKEND_URL="https://127.0.0.1:5262"
    echo "✅ Backend is HTTPS"
else
    BACKEND_URL="http://127.0.0.1:5262"
    echo "✅ Backend is HTTP"
fi

echo -e "\n${BLUE}Creating backup and fixing caseflowstage config...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP bash << ENDSSH
    # Backup
    cp /etc/nginx/sites-available/caseflowstage /etc/nginx/sites-available/caseflowstage.backup.\$(date +%Y%m%d_%H%M%S)
    
    # Create proper config
    cat > /etc/nginx/sites-available/caseflowstage << 'NGINXCONF'
# Redirect all HTTP traffic to HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name caseflowstage.store www.caseflowstage.store ${DROPLET_IP};
    
    # Allow Certbot to renew via HTTP challenge if needed
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
    
    location / {
        return 301 https://\$host\$request_uri;
    }
}

# Main HTTPS server - supports both domain and IP
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name caseflowstage.store www.caseflowstage.store ${DROPLET_IP};
    
    ssl_certificate /etc/letsencrypt/live/caseflowstage.store/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/caseflowstage.store/privkey.pem;
    
    include /etc/letsencrypt/options-ssl-nginx.conf;
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;
    
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
    
    # Static files (Blazor client) - SPA routing (MUST be last)
    location / {
        try_files \$uri \$uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
}
NGINXCONF

    # Replace variables
    sed -i "s/\${DROPLET_IP}/$DROPLET_IP/g" /etc/nginx/sites-available/caseflowstage
    sed -i "s|\${BACKEND_URL}|$BACKEND_URL|g" /etc/nginx/sites-available/caseflowstage
    
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

echo -e "\n${BLUE}Verifying...${NC}"

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

