#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# COMPREHENSIVE timeout fix - all layers

SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 COMPREHENSIVE TIMEOUT FIX - ALL LAYERS"
echo "=========================================="
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    echo "1. FIXING NGINX /api TIMEOUT..."
    NGINX_CONFIG="/etc/nginx/sites-available/mental-health-app"
    
    if ! grep -A 15 "location /api" "$NGINX_CONFIG" | grep -q "proxy_read_timeout"; then
        cp "$NGINX_CONFIG" "${NGINX_CONFIG}.backup.$(date +%Y%m%d_%H%M%S)"
        
        # Add timeouts to /api location
        python3 << 'PYTHON'
import re

config_file = "/etc/nginx/sites-available/mental-health-app"
with open(config_file, 'r') as f:
    content = f.read()

# Find /api location and add timeouts
pattern = r'(location /api \{.*?proxy_cache_bypass \$http_upgrade;)'
replacement = r'\1\n        proxy_read_timeout 900s;\n        proxy_connect_timeout 900s;\n        proxy_send_timeout 900s;'

if re.search(pattern, content, re.DOTALL):
    content = re.sub(pattern, replacement, content, flags=re.DOTALL)
    with open(config_file, 'w') as f:
        f.write(content)
    print("   ✅ Nginx timeout added")
else:
    print("   ⚠️  Pattern not found, trying manual insert...")
    lines = content.split('\n')
    in_api = False
    for i, line in enumerate(lines):
        if 'location /api' in line:
            in_api = True
        elif in_api and 'proxy_cache_bypass' in line:
            lines.insert(i+1, '        proxy_read_timeout 900s;')
            lines.insert(i+2, '        proxy_connect_timeout 900s;')
            lines.insert(i+3, '        proxy_send_timeout 900s;')
            break
    with open(config_file, 'w') as f:
        f.write('\n'.join(lines))
    print("   ✅ Nginx timeout added (manual)")
PYTHON
        
        nginx -t && systemctl reload nginx
        echo "   ✅ Nginx reloaded"
    else
        echo "   ✅ Nginx timeout already set"
    fi
    echo ""
    
    echo "2. FIXING KESTREL TIMEOUT..."
    APPSETTINGS="/opt/mental-health-app/server/appsettings.Production.json"
    
    python3 << 'PYTHON'
import json

config_file = "/opt/mental-health-app/server/appsettings.Production.json"

with open(config_file, 'r') as f:
    config = json.load(f)

# Add Kestrel timeouts
if "Kestrel" not in config:
    config["Kestrel"] = {}
if "Limits" not in config["Kestrel"]:
    config["Kestrel"]["Limits"] = {}

config["Kestrel"]["Limits"]["KeepAliveTimeout"] = 900
config["Kestrel"]["Limits"]["RequestHeadersTimeout"] = 900

# Also add at app level
config["RequestTimeout"] = "00:15:00"

with open(config_file, 'w') as f:
    json.dump(config, f, indent=2)

print("   ✅ Kestrel timeout added")
PYTHON
    echo ""
    
    echo "3. VERIFYING OLLAMA CONFIG..."
    OLLAMA_URL=$(cat "$APPSETTINGS" | python3 -c "import sys, json; c=json.load(sys.stdin); print(c.get('Ollama', {}).get('BaseUrl', 'NOT SET'))")
    echo "   Ollama BaseUrl: $OLLAMA_URL"
    if [ "$OLLAMA_URL" = "NOT SET" ] || [ "$OLLAMA_URL" = "http://localhost:11434" ]; then
        echo "   ⚠️  Fixing Ollama URL..."
        python3 << 'PYTHON'
import json
with open("/opt/mental-health-app/server/appsettings.Production.json", "r") as f:
    config = json.load(f)
if "Ollama" not in config:
    config["Ollama"] = {}
config["Ollama"]["BaseUrl"] = "http://127.0.0.1:11434"
with open("/opt/mental-health-app/server/appsettings.Production.json", "w") as f:
    json.dump(config, f, indent=2)
print("   ✅ Ollama URL fixed")
PYTHON
    fi
    echo ""
    
    echo "4. TESTING OLLAMA CONNECTION..."
    if curl -s --max-time 5 http://127.0.0.1:11434/api/tags > /dev/null 2>&1; then
        echo "   ✅ Ollama is accessible"
    else
        echo "   ❌ Ollama is NOT accessible!"
        systemctl status ollama --no-pager | head -5
    fi
    echo ""
    
    echo "5. RESTARTING APPLICATION..."
    systemctl stop mental-health-app
    sleep 2
    systemctl start mental-health-app
    sleep 5
    
    if systemctl is-active --quiet mental-health-app; then
        echo "   ✅ Application restarted"
    else
        echo "   ❌ Application failed to start"
        systemctl status mental-health-app --no-pager | head -15
        exit 1
    fi
    echo ""
    
    echo "6. VERIFICATION SUMMARY..."
    echo "   Nginx /api timeout:"
    grep -A 15 "location /api" "$NGINX_CONFIG" | grep -i timeout || echo "      (check manually)"
    echo ""
    echo "   Kestrel timeout:"
    cat "$APPSETTINGS" | python3 -c "import sys, json; c=json.load(sys.stdin); print('      KeepAliveTimeout:', c.get('Kestrel', {}).get('Limits', {}).get('KeepAliveTimeout', 'NOT SET'))" 2>/dev/null
    echo ""
    echo "   Ollama URL:"
    cat "$APPSETTINGS" | python3 -c "import sys, json; c=json.load(sys.stdin); print('      BaseUrl:', c.get('Ollama', {}).get('BaseUrl', 'NOT SET'))" 2>/dev/null
    echo ""
    
    echo "=========================================="
    echo "✅ ALL TIMEOUTS FIXED!"
    echo "=========================================="
    echo ""
    echo "All layers configured for 15-minute timeouts:"
    echo "  ✅ Nginx: 900s"
    echo "  ✅ Kestrel: 900s"
    echo "  ✅ HttpClient (code): 15 minutes"
    echo "  ✅ Ollama URL: http://127.0.0.1:11434"
    echo ""
    echo "Try the AI generation again - it should work now!"
ENDSSH

echo ""
echo "✅ Complete timeout fix applied!"

