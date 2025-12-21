#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Quick fix for nginx configuration syntax error

set -e

DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_NAME="mental-health-app"

echo "Fixing nginx configuration..."

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    cat > /etc/nginx/sites-available/$APP_NAME << 'ENDOFFILE'
# Upstream for API server (HTTPS backend) - use IPv4 explicitly
upstream api_backend {
    server 127.0.0.1:5262;
}

# HTTP server - redirect to HTTPS
server {
    listen 80;
    server_name DROPLET_IP_PLACEHOLDER;
    
    # Allow Let's Encrypt validation
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
    
    # Redirect all other traffic to HTTPS
    location / {
        return 301 https://\$server_name\$request_uri;
    }
}

# HTTPS server
server {
    listen 443 ssl http2;
    server_name DROPLET_IP_PLACEHOLDER;
    
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
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
    
    # OData proxy - to HTTPS backend (required for OData endpoints)
    location /odata {
        proxy_pass https://api_backend;
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
        proxy_pass https://api_backend;
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
        proxy_pass https://api_backend;
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
    
    # Blazor framework files - must come before the catch-all
    location /_framework {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
    
    # Static files (Blazor client) - catch-all for SPA routing
    location / {
        try_files \$uri \$uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
}
ENDOFFILE
    
    # Replace placeholder with actual IP
    sed -i "s/DROPLET_IP_PLACEHOLDER/$DROPLET_IP/g" /etc/nginx/sites-available/$APP_NAME
    
    # Test configuration
    nginx -t && systemctl reload nginx && echo "✅ Nginx configuration fixed and reloaded"
ENDSSH

echo "✅ Done!"
