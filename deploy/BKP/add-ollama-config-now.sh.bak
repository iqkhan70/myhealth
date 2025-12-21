#!/bin/bash

# Quick script to add Ollama config to appsettings.Production.json

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
CONFIG_FILE="$APP_DIR/server/appsettings.Production.json"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 Adding Ollama configuration to appsettings.Production.json..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    
    if [ ! -f "$CONFIG_FILE" ]; then
        echo "❌ Config file not found: $CONFIG_FILE"
        exit 1
    fi
    
    echo "Current config location: $CONFIG_FILE"
    echo ""
    
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
        print("✅ Added Ollama section")
    else:
        print("✅ Ollama section already exists")
    
    # Use 127.0.0.1 instead of localhost (more reliable in .NET on Linux)
    old_url = config["Ollama"].get("BaseUrl", "not set")
    config["Ollama"]["BaseUrl"] = "http://127.0.0.1:11434"
    
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=2)
    
    print(f"✅ Ollama BaseUrl updated: {old_url} -> {config['Ollama']['BaseUrl']}")
    print("")
    print("Updated Ollama section:")
    print(json.dumps(config["Ollama"], indent=2))
    
except Exception as e:
    print(f"❌ Error: {e}")
    sys.exit(1)
PYTHON
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "🔄 Restarting application service..."
        systemctl restart mental-health-app
        sleep 2
        
        if systemctl is-active --quiet mental-health-app; then
            echo "✅ Application restarted successfully"
        else
            echo "❌ Application failed to restart"
            systemctl status mental-health-app --no-pager | head -10
        fi
    fi
ENDSSH

echo ""
echo "✅ Configuration update complete!"

