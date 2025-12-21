#!/bin/bash

echo "🔧 Fixing production certificate configuration..."

SERVER_IP="159.65.242.79"
SSH_KEY="$HOME/.ssh/id_rsa"

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    # Backup current appsettings files
    if [ -f "/opt/mental-health-app/server/appsettings.json" ]; then
        cp /opt/mental-health-app/server/appsettings.json /opt/mental-health-app/server/appsettings.json.backup.$(date +%Y%m%d_%H%M%S)
    fi
    if [ -f "/opt/mental-health-app/server/appsettings.Production.json" ]; then
        cp /opt/mental-health-app/server/appsettings.Production.json /opt/mental-health-app/server/appsettings.Production.json.backup.$(date +%Y%m%d_%H%M%S)
    fi
    
    # Fix both appsettings.json and appsettings.Production.json
    for config_file in "/opt/mental-health-app/server/appsettings.json" "/opt/mental-health-app/server/appsettings.Production.json"; do
        if [ ! -f "$config_file" ]; then
            echo "Skipping $config_file (does not exist)"
            continue
        fi
        
        echo "Processing $config_file..."
        
        # Fix the malformed JSON by removing the orphaned Kestrel/Endpoints section
        python3 << PYTHON
import json
import sys

config_file = "$config_file"

try:
    # Read the existing file
    with open(config_file, 'r') as f:
        content = f.read()
    
    # Try to parse JSON
    try:
        config = json.loads(content)
    except json.JSONDecodeError:
        print(f"⚠️  {config_file} has invalid JSON, will create clean version")
        config = {}
    
    # Remove Kestrel section if it exists
    if "Kestrel" in config:
        del config["Kestrel"]
        print(f"✅ Removed Kestrel section from {config_file}")
    
    # Ensure ConnectionStrings exists (preserve if it does)
    if "ConnectionStrings" not in config:
        config["ConnectionStrings"] = {
            "MySQL": "server=localhost;port=3306;database=mentalhealthdb;user=root;password=UthmanBasima70"
        }
    
    # Write back
    with open(config_file, 'w') as f:
        json.dump(config, f, indent=2)
    
    print(f"✅ Fixed {config_file}")
    
except Exception as e:
    print(f"❌ Error processing {config_file}: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
PYTHON
        echo ""
    done
    
    # Verify the file is valid JSON
    echo ""
    echo "Validating JSON..."
    python3 -m json.tool /opt/mental-health-app/server/appsettings.json > /dev/null && echo "✅ JSON is valid" || echo "❌ JSON is invalid"
    
    # Show last 10 lines to confirm
    echo ""
    echo "Last 10 lines of appsettings.json:"
    tail -10 /opt/mental-health-app/server/appsettings.json
    
    # Restart the service
    echo ""
    echo "Restarting mental-health-app service..."
    systemctl restart mental-health-app
    sleep 5
    
    # Check status
    echo ""
    echo "Service status:"
    systemctl status mental-health-app --no-pager | head -20
    
    # Check if it's listening
    echo ""
    echo "Checking if service is listening on port 5262..."
    sleep 2
    ss -tlnp | grep 5262 && echo "✅ Service is listening on port 5262" || echo "⚠️  Service not listening on port 5262 yet"
    
    # Test backend directly
    echo ""
    echo "Testing backend health endpoint (from server)..."
    curl -k https://localhost:5262/api/health 2>&1 | head -5
ENDSSH

echo ""
echo "✅ Certificate configuration fix complete!"
echo ""
echo "Testing backend health endpoint (from local machine)..."
sleep 2
curl -k https://159.65.242.79/api/health 2>&1 | head -10
