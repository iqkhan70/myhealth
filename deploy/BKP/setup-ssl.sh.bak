#!/bin/bash

# SSL Certificate Setup Script for DigitalOcean
# This script sets up Let's Encrypt SSL certificates using certbot

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Configuration
DROPLET_IP="159.65.242.79"  # Your DigitalOcean droplet IP
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
DOMAIN=""  # Your domain name
APP_NAME="mental-health-app"
EMAIL=""  # Your email for Let's Encrypt

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}SSL Certificate Setup${NC}"
echo -e "${GREEN}========================================${NC}"

if [ -z "$DOMAIN" ]; then
    echo -e "${RED}ERROR: DOMAIN is not set!${NC}"
    echo "Please edit this script and set DOMAIN to your domain name"
    exit 1
fi

if [ -z "$EMAIL" ]; then
    echo -e "${RED}ERROR: EMAIL is not set!${NC}"
    echo "Please edit this script and set EMAIL for Let's Encrypt notifications"
    exit 1
fi

remote_exec() {
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "$@"
}

echo -e "\n${YELLOW}Setting up SSL certificate for $DOMAIN...${NC}"

# Update Nginx config to include SSL
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Backup current config
    cp /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-available/$APP_NAME.backup
    
    # Update config to support SSL
    cat > /etc/nginx/sites-available/$APP_NAME << 'EOF'
# Upstream for API server
upstream api_backend {
    server localhost:5262;
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name $DOMAIN;
    return 301 https://\$server_name\$request_uri;
}

# HTTPS server
server {
    listen 443 ssl http2;
    server_name $DOMAIN;
    
    # SSL certificates (will be added by certbot)
    # ssl_certificate /etc/letsencrypt/live/$DOMAIN/fullchain.pem;
    # ssl_certificate_key /etc/letsencrypt/live/$DOMAIN/privkey.pem;
    
    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # API proxy
    location /api {
        proxy_pass http://api_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
    
    # SignalR Hub
    location /mobilehub {
        proxy_pass http://api_backend;
        proxy_http_version 1.1;
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
        proxy_pass http://api_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
    
    # Static files (Blazor client)
    location / {
        root /opt/$APP_NAME/client/wwwroot;
        try_files \$uri \$uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
    
    # Blazor framework files
    location /_framework {
        root /opt/$APP_NAME/client/wwwroot;
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
}
EOF

    nginx -t && systemctl reload nginx
ENDSSH

echo -e "\n${YELLOW}Obtaining SSL certificate from Let's Encrypt...${NC}"
remote_exec "certbot --nginx -d $DOMAIN --non-interactive --agree-tos --email $EMAIL --redirect"

echo -e "\n${YELLOW}Setting up automatic renewal...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Test renewal
    certbot renew --dry-run
    
    # Certbot automatically sets up renewal via systemd timer
    systemctl status certbot.timer
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}SSL Setup Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Your application is now available at: https://$DOMAIN"
echo ""
echo "Certificate will auto-renew via certbot timer"

