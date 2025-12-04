#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Add timeout settings to Nginx /api location block

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
NGINX_CONFIG="/etc/nginx/sites-available/mental-health-app"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 Adding timeout settings to Nginx /api location..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    NGINX_CONFIG="/etc/nginx/sites-available/mental-health-app"
    
    echo "1. Current /api location block:"
    grep -A 15 "location /api" "$NGINX_CONFIG" || echo "   (Not found)"
    echo ""
    
    # Check if timeout already exists
    if grep -A 15 "location /api" "$NGINX_CONFIG" | grep -q "proxy_read_timeout"; then
        echo "2. ✅ Timeout settings already exist"
        echo ""
        echo "Current timeout settings:"
        grep -A 15 "location /api" "$NGINX_CONFIG" | grep -i timeout
        exit 0
    fi
    
    echo "2. Adding timeout settings (15 minutes = 900 seconds)..."
    
    # Backup
    cp "$NGINX_CONFIG" "${NGINX_CONFIG}.backup.$(date +%Y%m%d_%H%M%S)"
    
    # Use sed to add timeout after proxy_cache_bypass in /api location
    # This is safer than Python for simple text replacement
    sed -i '/location \/api {/,/}/ {
        /proxy_cache_bypass/a\
        proxy_read_timeout 900s;\
        proxy_connect_timeout 900s;\
        proxy_send_timeout 900s;
    }' "$NGINX_CONFIG"
    
    echo "   ✅ Timeout settings added"
    echo ""
    
    echo "3. Updated /api location block:"
    grep -A 18 "location /api" "$NGINX_CONFIG"
    echo ""
    
    echo "4. Testing Nginx configuration..."
    if nginx -t 2>&1; then
        echo "   ✅ Nginx config is valid"
    else
        echo "   ❌ Nginx config has errors! Restoring backup..."
        cp "${NGINX_CONFIG}.backup."* "$NGINX_CONFIG" 2>/dev/null || true
        exit 1
    fi
    
    echo ""
    echo "5. Reloading Nginx..."
    systemctl reload nginx
    
    if systemctl is-active --quiet nginx; then
        echo "   ✅ Nginx reloaded successfully"
    else
        echo "   ❌ Nginx failed to reload"
        systemctl status nginx --no-pager | head -10
        exit 1
    fi
    
    echo ""
    echo "=========================================="
    echo "✅ Nginx timeout settings added!"
    echo "=========================================="
    echo ""
    echo "Timeout settings for /api:"
    echo "  - proxy_read_timeout: 900s (15 minutes)"
    echo "  - proxy_connect_timeout: 900s"
    echo "  - proxy_send_timeout: 900s"
    echo ""
    echo "This should prevent Nginx from timing out long-running AI requests."
ENDSSH

echo ""
echo "✅ Fix complete!"

