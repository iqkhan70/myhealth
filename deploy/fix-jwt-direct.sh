#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Direct Python-based fix for JWT configuration

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo "🔧 Fixing JWT Configuration (Python method)..."
echo ""

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDPYTHON'
    APP_DIR="/opt/mental-health-app"
    CONFIG_FILE="$APP_DIR/server/appsettings.Production.json"
    
    # Backup
    cp "$CONFIG_FILE" "${CONFIG_FILE}.backup.$(date +%Y%m%d_%H%M%S)"
    
    # Use Python to fix JSON
    python3 << 'PYTHON_SCRIPT'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
        config = json.load(f)
    
    # Ensure Jwt section exists
    if 'Jwt' not in config:
        config['Jwt'] = {}
    
    # Remove SecretKey if it exists
    if 'SecretKey' in config['Jwt']:
        del config['Jwt']['SecretKey']
    
    # Set correct values
    config['Jwt']['Key'] = 'YourSuperSecretKeyThatIsAtLeast32CharactersLong!'
    config['Jwt']['Issuer'] = 'SM_MentalHealthApp'
    config['Jwt']['Audience'] = 'SM_MentalHealthApp_Users'
    
    # Write back
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'w') as f:
        json.dump(config, f, indent=2)
    
    print("✅ JWT configuration fixed successfully")
    print("\n📋 Updated JWT section:")
    print(json.dumps(config['Jwt'], indent=2))
    
except Exception as e:
    print(f"❌ Error: {e}")
    sys.exit(1)
PYTHON_SCRIPT

    if [ $? -eq 0 ]; then
        echo ""
        echo "🔄 Restarting service..."
        systemctl restart mental-health-app
        sleep 3
        echo "✅ Service restarted!"
    else
        echo "❌ Failed to update configuration"
        exit 1
    fi
ENDPYTHON

echo ""
echo "✅ Fix complete! Test the API now."

