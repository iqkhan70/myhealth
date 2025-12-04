#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Comprehensive script to diagnose and fix Ollama issues

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
CONFIG_FILE="$APP_DIR/server/appsettings.Production.json"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "=========================================="
echo "Ollama Diagnostic and Fix Script"
echo "=========================================="
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    
    echo "1. Checking Ollama service status..."
    if systemctl is-active --quiet ollama; then
        echo "   ✅ Ollama service is RUNNING"
    else
        echo "   ❌ Ollama service is NOT running"
        echo "   Starting Ollama..."
        systemctl start ollama
        sleep 2
        if systemctl is-active --quiet ollama; then
            echo "   ✅ Ollama started successfully"
        else
            echo "   ❌ Failed to start Ollama"
            exit 1
        fi
    fi
    echo ""
    
    echo "2. Testing Ollama CLI (ollama run)..."
    if ollama run tinyllama:latest "test" > /dev/null 2>&1; then
        echo "   ✅ Ollama CLI works"
    else
        echo "   ⚠️  Ollama CLI test failed (may be normal if model is loading)"
    fi
    echo ""
    
    echo "3. Testing Ollama HTTP API (127.0.0.1:11434)..."
    API_RESPONSE=$(curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model":"tinyllama:latest","prompt":"test","stream":false}' \
        --max-time 30 2>&1)
    
    if echo "$API_RESPONSE" | grep -q "response"; then
        echo "   ✅ Ollama HTTP API works on 127.0.0.1:11434"
    else
        echo "   ❌ Ollama HTTP API failed on 127.0.0.1:11434"
        echo "   Response: $API_RESPONSE" | head -5
    fi
    echo ""
    
    echo "4. Testing Ollama HTTP API (localhost:11434)..."
    API_RESPONSE2=$(curl -s -X POST http://localhost:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model":"tinyllama:latest","prompt":"test","stream":false}' \
        --max-time 30 2>&1)
    
    if echo "$API_RESPONSE2" | grep -q "response"; then
        echo "   ✅ Ollama HTTP API works on localhost:11434"
    else
        echo "   ❌ Ollama HTTP API failed on localhost:11434"
        echo "   Response: $API_RESPONSE2" | head -5
    fi
    echo ""
    
    echo "5. Checking current appsettings.Production.json..."
    if [ -f "$CONFIG_FILE" ]; then
        if grep -q '"Ollama"' "$CONFIG_FILE"; then
            echo "   ✅ Ollama section exists"
            grep -A 3 '"Ollama"' "$CONFIG_FILE"
        else
            echo "   ❌ Ollama section NOT found"
        fi
    else
        echo "   ❌ Config file not found: $CONFIG_FILE"
        exit 1
    fi
    echo ""
    
    echo "6. Updating appsettings.Production.json with Ollama config..."
    python3 << 'PYTHON'
import json
import sys

CONFIG_FILE = "/opt/mental-health-app/server/appsettings.Production.json"

try:
    # Read existing config
    with open(CONFIG_FILE, 'r') as f:
        config = json.load(f)
    
    # Add or update Ollama section
    if "Ollama" not in config:
        config["Ollama"] = {}
    
    # Use 127.0.0.1 for better reliability in .NET
    config["Ollama"]["BaseUrl"] = "http://127.0.0.1:11434"
    
    # Write back with proper formatting
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=2)
    
    print("   ✅ Ollama configuration updated")
    print("   BaseUrl: http://127.0.0.1:11434")
    
    # Verify
    with open(CONFIG_FILE, 'r') as f:
        config_check = json.load(f)
        if "Ollama" in config_check and "BaseUrl" in config_check["Ollama"]:
            print(f"   Verified: {config_check['Ollama']['BaseUrl']}")
        else:
            print("   ⚠️  Verification failed")
            sys.exit(1)
            
except Exception as e:
    print(f"   ❌ Error updating config: {e}")
    sys.exit(1)
PYTHON
    
    if [ $? -ne 0 ]; then
        echo "   ❌ Failed to update configuration"
        exit 1
    fi
    echo ""
    
    echo "7. Checking recent application logs for Ollama errors..."
    echo "   (Last 5 Ollama-related log entries)"
    journalctl -u mental-health-app --since '30 minutes ago' --no-pager | grep -i ollama | tail -5 || echo "   No recent Ollama logs found"
    echo ""
    
    echo "8. Restarting application service..."
    systemctl restart mental-health-app
    sleep 2
    
    if systemctl is-active --quiet mental-health-app; then
        echo "   ✅ Application service restarted successfully"
    else
        echo "   ❌ Application service failed to restart"
        systemctl status mental-health-app --no-pager | head -10
        exit 1
    fi
    echo ""
    
    echo "=========================================="
    echo "✅ Diagnostic and Fix Complete!"
    echo "=========================================="
    echo ""
    echo "Summary:"
    echo "  - Ollama service: $(systemctl is-active ollama || echo 'NOT RUNNING')"
    echo "  - Ollama BaseUrl: http://127.0.0.1:11434"
    echo "  - Application: $(systemctl is-active mental-health-app || echo 'NOT RUNNING')"
    echo ""
    echo "Next steps:"
    echo "  1. Try using the chat feature in the app"
    echo "  2. Check logs: journalctl -u mental-health-app -f"
    echo "  3. If still not working, check: journalctl -u mental-health-app --since '5 minutes ago' | grep -i ollama"
ENDSSH

echo ""
echo "✅ Script completed!"

