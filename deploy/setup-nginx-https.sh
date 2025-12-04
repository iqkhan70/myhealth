#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Setup HTTPS for Nginx (self-signed certificate)
# This allows the application to be accessed via HTTPS

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_NAME="mental-health-app"
APP_DIR="/opt/mental-health-app"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Nginx HTTPS Setup${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

echo -e "\n${YELLOW}Step 1: Generating self-signed SSL certificate for Nginx...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Create certificates directory
    mkdir -p /etc/nginx/ssl
    
    # Generate self-signed certificate
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout /etc/nginx/ssl/nginx-selfsigned.key \
        -out /etc/nginx/ssl/nginx-selfsigned.crt \
        -subj "/C=US/ST=State/L=City/O=Organization/CN=$DROPLET_IP" \
        -addext "subjectAltName=IP:$DROPLET_IP,DNS:$DROPLET_IP"
    
    # Set proper permissions
    chmod 600 /etc/nginx/ssl/nginx-selfsigned.key
    chmod 644 /etc/nginx/ssl/nginx-selfsigned.crt
    
    echo "✅ SSL certificate generated"
ENDSSH

echo -e "\n${YELLOW}Step 2: Updating Nginx configuration for HTTPS...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Backup current config
    cp /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-available/$APP_NAME.backup.$(date +%Y%m%d_%H%M%S)
    
    # Update config to support both HTTP and HTTPS
    cat > /etc/nginx/sites-available/$APP_NAME << 'NGINXCONF'
# Upstream for API server (HTTPS backend)
upstream api_backend {
    server localhost:5262;
}

# HTTP server - redirect to HTTPS
server {
    listen 80;
    server_name ${DROPLET_IP};
    
    # Allow Let's Encrypt validation
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
    
    # Redirect all other traffic to HTTPS
    location / {
        return 301 https://$server_name$request_uri;
    }
}

# HTTPS server
server {
    listen 443 ssl http2;
    server_name ${DROPLET_IP};
    
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
    
    # Static files (Blazor client)
    location / {
        root /opt/mental-health-app/client/wwwroot;
        try_files $uri $uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
    
    # Blazor framework files
    location /_framework {
        root /opt/mental-health-app/client/wwwroot;
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
}
NGINXCONF

    # Test Nginx configuration
    nginx -t
    
    # Reload Nginx
    systemctl reload nginx
    
    echo "✅ Nginx configuration updated and reloaded"
ENDSSH

echo -e "\n${YELLOW}Step 3: Opening port 443 in firewall...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Allow HTTPS
    ufw allow 443/tcp
    ufw status | grep 443 || echo "Port 443 already allowed"
ENDSSH

echo -e "\n${YELLOW}Step 4: Testing HTTPS connection...${NC}"

# Test HTTPS connection
sleep 2
HTTPS_TEST=$(curl -k -s -o /dev/null -w '%{http_code}' "https://$DROPLET_IP/" 2>/dev/null || echo "000")

if [ "$HTTPS_TEST" = "200" ]; then
    echo -e "${GREEN}✅ HTTPS is working!${NC}"
elif [ "$HTTPS_TEST" = "301" ] || [ "$HTTPS_TEST" = "302" ]; then
    echo -e "${GREEN}✅ HTTPS is working (redirect detected)${NC}"
else
    echo -e "${YELLOW}⚠️  HTTPS test returned: $HTTPS_TEST${NC}"
    echo "Checking Nginx status..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
        "systemctl status nginx --no-pager -l | head -15"
fi

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Nginx HTTPS Setup Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Your application is now available at:"
echo "  HTTP:  http://$DROPLET_IP (redirects to HTTPS)"
echo "  HTTPS: https://$DROPLET_IP"
echo ""
echo -e "${YELLOW}Note: You'll need to accept the self-signed certificate in your browser${NC}"
echo "The browser will show a security warning - click 'Advanced' and 'Proceed'"

