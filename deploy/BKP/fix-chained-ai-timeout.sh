#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Fix script for Chained AI timeout issues

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
CONFIG_FILE="$APP_DIR/server/appsettings.Production.json"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 Fixing Chained AI Timeout Issues..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    
    echo "1. Ensuring Ollama service is running..."
    if ! systemctl is-active --quiet ollama; then
        echo "   Starting Ollama service..."
        systemctl start ollama
        sleep 3
    fi
    
    if systemctl is-active --quiet ollama; then
        echo "   ✅ Ollama is running"
    else
        echo "   ❌ Failed to start Ollama"
        exit 1
    fi
    echo ""
    
    echo "2. Testing Ollama API connectivity..."
    if curl -s http://127.0.0.1:11434/api/tags > /dev/null 2>&1; then
        echo "   ✅ Ollama API is accessible"
    else
        echo "   ❌ Ollama API is not accessible"
        echo "   Check: systemctl status ollama"
        exit 1
    fi
    echo ""
    
    echo "3. Ensuring Ollama configuration exists..."
    python3 << 'PYTHON'
import json
import sys

CONFIG_FILE = "/opt/mental-health-app/server/appsettings.Production.json"

try:
    with open(CONFIG_FILE, 'r') as f:
        config = json.load(f)
    
    # Add or update Ollama section
    if "Ollama" not in config:
        config["Ollama"] = {}
    
    # Use 127.0.0.1 for better reliability
    config["Ollama"]["BaseUrl"] = "http://127.0.0.1:11434"
    
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=2)
    
    print("   ✅ Ollama configuration updated")
    print(f"   BaseUrl: {config['Ollama']['BaseUrl']}")
    
except Exception as e:
    print(f"   ❌ Error: {e}")
    sys.exit(1)
PYTHON
    
    if [ $? -ne 0 ]; then
        echo "   ❌ Failed to update configuration"
        exit 1
    fi
    echo ""
    
    echo "4. Testing Ollama with actual model call..."
    TEST_OUTPUT=$(curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model":"tinyllama:latest","prompt":"test","stream":false}' \
        --max-time 60 2>&1)
    
    if echo "$TEST_OUTPUT" | grep -q '"response"'; then
        echo "   ✅ Ollama model is responding"
    else
        echo "   ⚠️  Ollama model test failed or timed out"
        echo "   Response preview:"
        echo "$TEST_OUTPUT" | head -5
        echo ""
        echo "   Checking if model is available..."
        ollama list 2>&1 | grep -i tinyllama || echo "   ⚠️  tinyllama model may not be loaded"
    fi
    echo ""
    
    echo "5. Restarting application service..."
    systemctl restart mental-health-app
    sleep 2
    
    if systemctl is-active --quiet mental-health-app; then
        echo "   ✅ Application restarted"
    else
        echo "   ❌ Application failed to restart"
        systemctl status mental-health-app --no-pager | head -10
        exit 1
    fi
    echo ""
    
    echo "6. Checking recent logs for configuration..."
    echo "   (Looking for Ollama BaseUrl in logs)"
    journalctl -u mental-health-app --since '2 minutes ago' --no-pager | \
        grep -i "ollama.*baseurl\|Calling.*Ollama" | head -5 || echo "   No recent Ollama logs yet"
    echo ""
    
    echo "=========================================="
    echo "✅ Fix Complete!"
    echo "=========================================="
    echo ""
    echo "Next steps:"
    echo "  1. Try the chained AI feature again"
    echo "  2. Monitor logs: journalctl -u mental-health-app -f"
    echo "  3. If still timing out, check:"
    echo "     - Ollama service: systemctl status ollama"
    echo "     - Ollama logs: journalctl -u ollama -f"
    echo "     - Model availability: ollama list"
ENDSSH

echo ""
echo "✅ Fix script completed!"

