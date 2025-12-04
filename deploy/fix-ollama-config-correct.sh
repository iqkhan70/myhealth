#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Fix Ollama configuration - add it as a SEPARATE section from HuggingFace

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔧 Fixing Ollama Configuration..."
echo ""
echo "Note: Ollama and HuggingFace are SEPARATE providers:"
echo "  - HuggingFace: Cloud API (needs ApiKey)"
echo "  - Ollama: Local LLM server (needs BaseUrl only, no API key)"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    
    python3 << 'PYTHON'
import json
import sys

CONFIG_FILE = "/opt/mental-health-app/server/appsettings.Production.json"

try:
    with open(CONFIG_FILE, 'r') as f:
        config = json.load(f)
    
    print("Current configuration sections:")
    for key in config.keys():
        if key not in ["ConnectionStrings", "Logging", "AllowedHosts"]:
            print(f"  - {key}")
    print("")
    
    # Check if Ollama section exists and has wrong config
    if "Ollama" in config:
        ollama_config = config["Ollama"]
        if "ApiKey" in ollama_config or "BioMistralModelUrl" in ollama_config:
            print("❌ Ollama section has HuggingFace settings (wrong!)")
            print("   Removing incorrect settings...")
            # Remove HuggingFace-specific settings from Ollama
            ollama_config.pop("ApiKey", None)
            ollama_config.pop("BioMistralModelUrl", None)
            ollama_config.pop("MeditronModelUrl", None)
    
    # Ensure HuggingFace section exists (keep it separate)
    if "HuggingFace" not in config:
        print("⚠️  HuggingFace section not found - adding it...")
        config["HuggingFace"] = {
            "ApiKey": "",
            "BioMistralModelUrl": "https://api-inference.huggingface.co/models/medalpaca/medalpaca-7b",
            "MeditronModelUrl": "https://api-inference.huggingface.co/models/epfl-llm/meditron-7b"
        }
    else:
        print("✅ HuggingFace section exists (keeping as-is)")
    
    # Add/update Ollama section with correct config
    if "Ollama" not in config:
        config["Ollama"] = {}
        print("✅ Added Ollama section")
    else:
        print("✅ Ollama section exists (updating BaseUrl)")
    
    # Ollama only needs BaseUrl (no API keys!)
    config["Ollama"]["BaseUrl"] = "http://127.0.0.1:11434"
    
    # Remove any incorrect settings from Ollama
    for key in list(config["Ollama"].keys()):
        if key != "BaseUrl":
            print(f"   Removing incorrect setting: {key}")
            del config["Ollama"][key]
    
    # Write back
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=2)
    
    print("")
    print("✅ Configuration updated:")
    print("")
    print("HuggingFace section:")
    print(json.dumps(config["HuggingFace"], indent=2))
    print("")
    print("Ollama section:")
    print(json.dumps(config["Ollama"], indent=2))
    
except Exception as e:
    print(f"❌ Error: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
PYTHON
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "🔄 Restarting application..."
        systemctl restart mental-health-app
        sleep 2
        
        if systemctl is-active --quiet mental-health-app; then
            echo "✅ Application restarted"
        else
            echo "❌ Application failed to restart"
            systemctl status mental-health-app --no-pager | head -10
        fi
    fi
ENDSSH

echo ""
echo "✅ Configuration fix complete!"

