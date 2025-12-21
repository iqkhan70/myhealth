#!/bin/bash

# Comprehensive fix for Ollama config and restart

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 Fixing Ollama Configuration and Restarting..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    
    echo "1. Checking current config file..."
    if [ ! -f "$CONFIG_FILE" ]; then
        echo "   ❌ Config file not found: $CONFIG_FILE"
        exit 1
    fi
    echo "   ✅ Config file exists"
    echo ""
    
    echo "2. Current Ollama section (if any):"
    grep -A 5 '"Ollama"' "$CONFIG_FILE" || echo "   (No Ollama section found)"
    echo ""
    
    echo "3. Updating configuration..."
    python3 << 'PYTHON'
import json
import sys

CONFIG_FILE = "/opt/mental-health-app/server/appsettings.Production.json"

try:
    with open(CONFIG_FILE, 'r') as f:
        config = json.load(f)
    
    # Ensure HuggingFace exists (keep it separate)
    if "HuggingFace" not in config:
        config["HuggingFace"] = {
            "ApiKey": "",
            "BioMistralModelUrl": "https://api-inference.huggingface.co/models/medalpaca/medalpaca-7b",
            "MeditronModelUrl": "https://api-inference.huggingface.co/models/epfl-llm/meditron-7b"
        }
    
    # Add/update Ollama section (SEPARATE from HuggingFace)
    config["Ollama"] = {
        "BaseUrl": "http://127.0.0.1:11434"
    }
    
    # Remove any incorrect settings from Ollama
    for key in list(config.get("Ollama", {}).keys()):
        if key != "BaseUrl":
            del config["Ollama"][key]
    
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=2)
    
    print("   ✅ Configuration updated")
    print("")
    print("   HuggingFace section:")
    print(json.dumps(config["HuggingFace"], indent=4))
    print("")
    print("   Ollama section:")
    print(json.dumps(config["Ollama"], indent=4))
    
except Exception as e:
    print(f"   ❌ Error: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
PYTHON
    
    if [ $? -ne 0 ]; then
        echo "   ❌ Failed to update config"
        exit 1
    fi
    
    echo ""
    echo "4. Verifying config file is valid JSON..."
    python3 -m json.tool "$CONFIG_FILE" > /dev/null && echo "   ✅ Valid JSON" || {
        echo "   ❌ Invalid JSON!"
        exit 1
    }
    
    echo ""
    echo "5. Stopping application..."
    systemctl stop mental-health-app
    sleep 2
    
    echo "6. Starting application..."
    systemctl start mental-health-app
    sleep 3
    
    if systemctl is-active --quiet mental-health-app; then
        echo "   ✅ Application started"
    else
        echo "   ❌ Application failed to start"
        systemctl status mental-health-app --no-pager | head -15
        exit 1
    fi
    
    echo ""
    echo "7. Waiting 5 seconds for app to initialize..."
    sleep 5
    
    echo ""
    echo "8. Checking recent logs for Ollama BaseUrl..."
    journalctl -u mental-health-app --since '10 seconds ago' --no-pager | \
        grep -i "ollama.*baseurl\|Calling.*Ollama" | head -5 || \
        echo "   (No Ollama logs yet - try using the feature to generate logs)"
    
    echo ""
    echo "=========================================="
    echo "✅ Configuration and restart complete!"
    echo "=========================================="
    echo ""
    echo "The app should now use: http://127.0.0.1:11434"
    echo "Try the chained AI feature again and check logs:"
    echo "  journalctl -u mental-health-app -f"
ENDSSH

echo ""
echo "✅ Fix complete!"

