#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Diagnose nginx 404 errors

SSH_KEY_PATH="$HOME/.ssh/id_rsa"

echo "🔍 Diagnosing nginx 404 errors..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    echo "1. Checking nginx status..."
    systemctl status nginx --no-pager | head -5
    echo ""
    
    echo "2. Checking nginx error log..."
    tail -20 /var/log/nginx/error.log
    echo ""
    
    echo "3. Checking nginx access log..."
    tail -10 /var/log/nginx/access.log
    echo ""
    
    echo "4. Checking nginx configuration..."
    cat /etc/nginx/sites-available/mental-health-app
    echo ""
    
    echo "5. Checking if static files exist..."
    echo "   Looking for index.html in common locations:"
    find /opt/mental-health-app/client -name "index.html" -type f 2>/dev/null | head -5
    echo ""
    
    echo "6. Checking client directory structure..."
    ls -la /opt/mental-health-app/client/ 2>/dev/null | head -10
    echo ""
    
    echo "7. Checking wwwroot directory..."
    ls -la /opt/mental-health-app/client/wwwroot/ 2>/dev/null | head -10
    echo ""
    
    echo "8. Testing if nginx can access the root directory..."
    if [ -d "/opt/mental-health-app/client/wwwroot" ]; then
        echo "   ✅ wwwroot directory exists"
        if [ -f "/opt/mental-health-app/client/wwwroot/index.html" ]; then
            echo "   ✅ index.html exists in wwwroot"
        else
            echo "   ❌ index.html NOT found in wwwroot"
        fi
    else
        echo "   ❌ wwwroot directory does NOT exist"
    fi
    echo ""
    
    echo "9. Checking nginx configuration syntax..."
    nginx -t
ENDSSH

