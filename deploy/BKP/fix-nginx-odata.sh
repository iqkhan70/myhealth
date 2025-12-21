#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Fix Nginx configuration to add OData proxy location
# This is required for OData endpoints to work on DigitalOcean

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
NGINX_CONFIG="/etc/nginx/sites-available/mental-health-app"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 Adding OData proxy location to Nginx..."
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
    
    echo "2. Checking if OData location already exists..."
    if grep -q "location /odata" "$NGINX_CONFIG"; then
        echo "   ⚠️  OData location already exists, updating it..."
    else
        echo "   ℹ️  OData location not found, adding it..."
    fi
    echo ""
    
    echo "3. Creating backup..."
    cp "$NGINX_CONFIG" "${NGINX_CONFIG}.backup.$(date +%Y%m%d_%H%M%S)"
    echo "   ✅ Backup created"
    echo ""
    
    echo "4. Adding/updating OData location block..."
    
    # Remove existing OData location if it exists
    sed -i '/location \/odata {/,/^    }$/d' "$NGINX_CONFIG"
    
    # Find the line with "location /api {" and add OData location after it
    # We'll insert after the closing brace of the /api location block
    if grep -q "location /api" "$NGINX_CONFIG"; then
        # Find the line number of the closing brace after /api location
        API_END_LINE=$(awk '/location \/api {/,/^    }$/ {if (/^    }$/) print NR}' "$NGINX_CONFIG" | tail -1)
        
        if [ -n "$API_END_LINE" ]; then
            # Insert OData location block after /api location
            sed -i "${API_END_LINE}a\\
    \\
    # OData proxy - to HTTPS backend (required for OData endpoints)\\
    location /odata {\\
        proxy_pass https://api_backend;\\
        proxy_http_version 1.1;\\
        proxy_ssl_verify off;\\
        proxy_set_header Upgrade \$http_upgrade;\\
        proxy_set_header Connection \"keep-alive\";\\
        proxy_set_header Host \$host;\\
        proxy_set_header X-Real-IP \$remote_addr;\\
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;\\
        proxy_set_header X-Forwarded-Proto \$scheme;\\
        proxy_set_header Accept \"application/json, application/odata+json\";\\
        proxy_cache_bypass \$http_upgrade;\\
    }" "$NGINX_CONFIG"
        else
            echo "   ⚠️  Could not find end of /api location, appending to server block..."
            # Fallback: append before the SignalR location
            sed -i '/location \/mobilehub/i\
    # OData proxy - to HTTPS backend (required for OData endpoints)\
    location /odata {\
        proxy_pass https://api_backend;\
        proxy_http_version 1.1;\
        proxy_ssl_verify off;\
        proxy_set_header Upgrade $http_upgrade;\
        proxy_set_header Connection "keep-alive";\
        proxy_set_header Host $host;\
        proxy_set_header X-Real-IP $remote_addr;\
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\
        proxy_set_header X-Forwarded-Proto $scheme;\
        proxy_set_header Accept "application/json, application/odata+json";\
        proxy_cache_bypass $http_upgrade;\
    }\
' "$NGINX_CONFIG"
        fi
    else
        echo "   ⚠️  /api location not found, appending OData location to server block..."
        # Append before SignalR location
        sed -i '/location \/mobilehub/i\
    # OData proxy - to HTTPS backend (required for OData endpoints)\
    location /odata {\
        proxy_pass https://api_backend;\
        proxy_http_version 1.1;\
        proxy_ssl_verify off;\
        proxy_set_header Upgrade $http_upgrade;\
        proxy_set_header Connection "keep-alive";\
        proxy_set_header Host $host;\
        proxy_set_header X-Real-IP $remote_addr;\
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\
        proxy_set_header X-Forwarded-Proto $scheme;\
        proxy_set_header Accept "application/json, application/odata+json";\
        proxy_cache_bypass $http_upgrade;\
    }\
' "$NGINX_CONFIG"
    fi
    
    echo "   ✅ OData location block added/updated"
    echo ""
    
    echo "5. Updated OData location block:"
    grep -A 12 "location /odata" "$NGINX_CONFIG" | head -15
    echo ""
    
    echo "6. Testing Nginx configuration..."
    if nginx -t; then
        echo "   ✅ Nginx config is valid"
    else
        echo "   ❌ Nginx config has errors!"
        echo "   Restoring backup..."
        cp "${NGINX_CONFIG}.backup."* "$NGINX_CONFIG" 2>/dev/null || true
        exit 1
    fi
    
    echo ""
    echo "7. Reloading Nginx..."
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
    echo "✅ OData proxy location added to Nginx!"
    echo "=========================================="
    echo ""
    echo "OData endpoints should now work correctly."
    echo "Test with: curl -k https://$DROPLET_IP/odata/Users"
ENDSSH

echo ""
echo "✅ Fix complete!"
echo ""
echo "The OData endpoints should now work. The enhanced error logging in the client"
echo "will help diagnose any remaining issues."

