#!/bin/bash

# Fix Nginx timeout settings for long-running AI requests

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
NGINX_CONFIG="/etc/nginx/sites-available/mental-health-app"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 Fixing Nginx Timeout Settings..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    NGINX_CONFIG="/etc/nginx/sites-available/mental-health-app"
    
    echo "1. Checking current Nginx config..."
    if [ ! -f "$NGINX_CONFIG" ]; then
        echo "   ❌ Nginx config not found: $NGINX_CONFIG"
        exit 1
    fi
    
    echo "   ✅ Config file exists"
    echo ""
    
    echo "2. Current timeout settings in /api location:"
    grep -A 15 "location /api" "$NGINX_CONFIG" | grep -i timeout || echo "   (No timeout settings found - using defaults)"
    echo ""
    
    echo "3. Updating Nginx config with longer timeouts..."
    
    # Create backup
    cp "$NGINX_CONFIG" "${NGINX_CONFIG}.backup.$(date +%Y%m%d_%H%M%S)"
    
    # Use sed to add timeout settings to /api location block
    # Add timeouts after proxy_cache_bypass line in /api location
    sed -i '/location \/api {/,/}/ {
        /proxy_cache_bypass/a\
        proxy_read_timeout 600s;\
        proxy_connect_timeout 600s;\
        proxy_send_timeout 600s;
    }' "$NGINX_CONFIG"
    
    # Also add timeouts at the server level (affects all locations)
    if ! grep -q "proxy_read_timeout" "$NGINX_CONFIG" || ! grep -q "location /api" -A 20 "$NGINX_CONFIG" | grep -q "proxy_read_timeout"; then
        # Alternative: Add to server block
        sed -i '/server {/a\
    # Timeout settings for long-running requests (AI calls)\
    proxy_read_timeout 600s;\
    proxy_connect_timeout 600s;\
    proxy_send_timeout 600s;
' "$NGINX_CONFIG"
    fi
    
    echo "   ✅ Timeout settings added"
    echo ""
    
    echo "4. Updated /api location block:"
    grep -A 20 "location /api" "$NGINX_CONFIG" | head -25
    echo ""
    
    echo "5. Testing Nginx configuration..."
    if nginx -t; then
        echo "   ✅ Nginx config is valid"
    else
        echo "   ❌ Nginx config has errors!"
        echo "   Restoring backup..."
        cp "${NGINX_CONFIG}.backup."* "$NGINX_CONFIG" 2>/dev/null || true
        exit 1
    fi
    
    echo ""
    echo "6. Reloading Nginx..."
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
    echo "✅ Nginx timeout settings updated!"
    echo "=========================================="
    echo ""
    echo "Timeout settings:"
    echo "  - proxy_read_timeout: 600s (10 minutes)"
    echo "  - proxy_connect_timeout: 600s"
    echo "  - proxy_send_timeout: 600s"
    echo ""
    echo "This should allow AI requests to run longer without timing out."
ENDSSH

echo ""
echo "✅ Fix complete!"

