#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Comprehensive fix for all timeout layers: Nginx, Kestrel, and appsettings

SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 Fixing ALL Timeout Settings (Nginx + Kestrel + AppSettings)..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    NGINX_CONFIG="/etc/nginx/sites-available/mental-health-app"
    APPSETTINGS="/opt/mental-health-app/server/appsettings.Production.json"
    
    echo "=========================================="
    echo "1. FIXING NGINX TIMEOUTS"
    echo "=========================================="
    echo ""
    
    if [ ! -f "$NGINX_CONFIG" ]; then
        echo "   ❌ Nginx config not found"
        exit 1
    fi
    
    # Backup
    cp "$NGINX_CONFIG" "${NGINX_CONFIG}.backup.$(date +%Y%m%d_%H%M%S)"
    
    # Check current /api location
    echo "   Current /api location:"
    grep -A 12 "location /api" "$NGINX_CONFIG" | head -15
    echo ""
    
    # Add timeout settings if not present
    if ! grep -A 15 "location /api" "$NGINX_CONFIG" | grep -q "proxy_read_timeout"; then
        echo "   Adding timeout settings to /api location..."
        
        # Use Python for reliable JSON-like config modification
        python3 << 'PYTHON'
import re

config_file = "/etc/nginx/sites-available/mental-health-app"

with open(config_file, 'r') as f:
    content = f.read()

# Pattern: location /api block ending with proxy_cache_bypass
# Add timeouts after proxy_cache_bypass
pattern = r'(location /api \{.*?proxy_cache_bypass \$http_upgrade;)'
replacement = r'\1\n        proxy_read_timeout 900s;\n        proxy_connect_timeout 900s;\n        proxy_send_timeout 900s;'

if re.search(pattern, content, re.DOTALL):
    new_content = re.sub(pattern, replacement, content, flags=re.DOTALL)
    with open(config_file, 'w') as f:
        f.write(new_content)
    print("   ✅ Timeout settings added")
else:
    print("   ⚠️  Pattern not found, trying alternative method...")
    # Alternative: add after the last line in /api block
    lines = content.split('\n')
    in_api_block = False
    api_block_end = -1
    
    for i, line in enumerate(lines):
        if 'location /api' in line:
            in_api_block = True
        elif in_api_block and line.strip() == '}' and i > 0:
            api_block_end = i
            break
    
    if api_block_end > 0:
        # Insert timeout settings before the closing brace
        timeout_lines = [
            '        proxy_read_timeout 900s;',
            '        proxy_connect_timeout 900s;',
            '        proxy_send_timeout 900s;'
        ]
        lines[api_block_end:api_block_end] = timeout_lines
        with open(config_file, 'w') as f:
            f.write('\n'.join(lines))
        print("   ✅ Timeout settings added (alternative method)")
    else:
        print("   ❌ Could not find /api location block")
        exit(1)
PYTHON
        
        echo ""
        echo "   Updated /api location:"
        grep -A 18 "location /api" "$NGINX_CONFIG" | head -20
    else
        echo "   ✅ Timeout settings already exist"
    fi
    
    echo ""
    echo "   Testing Nginx config..."
    if nginx -t; then
        echo "   ✅ Nginx config valid"
        systemctl reload nginx
        echo "   ✅ Nginx reloaded"
    else
        echo "   ❌ Nginx config invalid!"
        exit 1
    fi
    
    echo ""
    echo "=========================================="
    echo "2. FIXING KESTREL/ASPNET TIMEOUTS"
    echo "=========================================="
    echo ""
    
    if [ ! -f "$APPSETTINGS" ]; then
        echo "   ❌ appsettings.Production.json not found"
        exit 1
    fi
    
    # Backup
    cp "$APPSETTINGS" "${APPSETTINGS}.backup.$(date +%Y%m%d_%H%M%S)"
    
    # Add Kestrel timeout settings
    python3 << 'PYTHON'
import json
import os

config_file = "/opt/mental-health-app/server/appsettings.Production.json"

with open(config_file, 'r') as f:
    config = json.load(f)

# Add Kestrel settings if not present
if "Kestrel" not in config:
    config["Kestrel"] = {}

if "Limits" not in config["Kestrel"]:
    config["Kestrel"]["Limits"] = {}

# Set timeouts (15 minutes = 900 seconds)
config["Kestrel"]["Limits"]["KeepAliveTimeout"] = 900
config["Kestrel"]["Limits"]["RequestHeadersTimeout"] = 900

# Also add RequestTimeout at the app level
if "RequestTimeout" not in config:
    config["RequestTimeout"] = "00:15:00"  # 15 minutes

with open(config_file, 'w') as f:
    json.dump(config, f, indent=2)

print("   ✅ Kestrel timeout settings added:")
print(json.dumps({
    "Kestrel": {
        "Limits": {
            "KeepAliveTimeout": config["Kestrel"]["Limits"]["KeepAliveTimeout"],
            "RequestHeadersTimeout": config["Kestrel"]["Limits"]["RequestHeadersTimeout"]
        }
    },
    "RequestTimeout": config.get("RequestTimeout")
}, indent=4))
PYTHON
    
    echo ""
    echo "   Restarting application..."
    systemctl stop mental-health-app
    sleep 2
    systemctl start mental-health-app
    sleep 3
    
    if systemctl is-active --quiet mental-health-app; then
        echo "   ✅ Application restarted"
    else
        echo "   ❌ Application failed to start"
        systemctl status mental-health-app --no-pager | head -15
        exit 1
    fi
    
    echo ""
    echo "=========================================="
    echo "✅ ALL TIMEOUT FIXES COMPLETE!"
    echo "=========================================="
    echo ""
    echo "Timeout settings applied:"
    echo ""
    echo "  Nginx (/api location):"
    echo "    - proxy_read_timeout: 900s (15 minutes)"
    echo "    - proxy_connect_timeout: 900s"
    echo "    - proxy_send_timeout: 900s"
    echo ""
    echo "  Kestrel Server:"
    echo "    - KeepAliveTimeout: 900s"
    echo "    - RequestHeadersTimeout: 900s"
    echo "    - RequestTimeout: 15 minutes"
    echo ""
    echo "  HttpClient (in code):"
    echo "    - Timeout: Infinite (already set)"
    echo ""
    echo "Long-running AI requests should now work without timing out!"
ENDSSH

echo ""
echo "✅ All timeout fixes complete!"

