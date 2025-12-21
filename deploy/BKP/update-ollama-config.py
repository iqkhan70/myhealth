#!/usr/bin/env python3
"""
Update appsettings.Production.json to add/update Ollama configuration
"""
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
    
    # Set BaseUrl to 127.0.0.1 for better reliability
    config["Ollama"]["BaseUrl"] = "http://127.0.0.1:11434"
    
    # Write back
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f, indent=2)
    
    print("✅ Ollama configuration updated successfully")
    print(f"   BaseUrl: {config['Ollama']['BaseUrl']}")
    sys.exit(0)
    
except FileNotFoundError:
    print(f"❌ Config file not found: {CONFIG_FILE}")
    sys.exit(1)
except json.JSONDecodeError as e:
    print(f"❌ Invalid JSON in config file: {e}")
    sys.exit(1)
except Exception as e:
    print(f"❌ Error: {e}")
    sys.exit(1)

