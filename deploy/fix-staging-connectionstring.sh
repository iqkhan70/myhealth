#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Quick fix script to add ConnectionStrings to appsettings.Staging.json
# Copies ConnectionStrings from Production file if Staging doesn't have it

SSH_KEY="$HOME/.ssh/id_rsa"

echo "🔧 Fixing ConnectionStrings in appsettings.Staging.json..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    STAGING_FILE="/opt/mental-health-app/server/appsettings.Staging.json"
    PROD_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    
    python3 << 'PYTHON'
import json
import sys
import os

staging_file = "/opt/mental-health-app/server/appsettings.Staging.json"
prod_file = "/opt/mental-health-app/server/appsettings.Production.json"

try:
    # Read staging file
    with open(staging_file, 'r') as f:
        staging_config = json.load(f)
    
    # Check if ConnectionStrings exists
    if "ConnectionStrings" in staging_config and staging_config["ConnectionStrings"]:
        print("✅ ConnectionStrings already exists in Staging file")
        print(f"   MySQL: {staging_config['ConnectionStrings'].get('MySQL', 'Not set')[:50]}...")
        sys.exit(0)
    
    # Try to get from Production file
    connection_strings = None
    try:
        with open(prod_file, 'r') as f:
            prod_config = json.load(f)
            if "ConnectionStrings" in prod_config:
                connection_strings = prod_config["ConnectionStrings"]
                print("✅ Found ConnectionStrings in Production file")
    except FileNotFoundError:
        print("⚠️  Production file not found")
    except json.JSONDecodeError:
        print("⚠️  Production file has invalid JSON")
    
    if connection_strings:
        # Add ConnectionStrings to staging config
        staging_config["ConnectionStrings"] = connection_strings
        
        # Write back
        with open(staging_file, 'w') as f:
            json.dump(staging_config, f, indent=2)
        
        os.chmod(staging_file, 0o644)
        print("✅ Successfully added ConnectionStrings to Staging file")
        print(f"   MySQL: {connection_strings.get('MySQL', 'Not set')[:50]}...")
    else:
        print("❌ ERROR: No ConnectionStrings found in Production file!")
        print("   You need to manually add ConnectionStrings to Staging file")
        sys.exit(1)
        
except FileNotFoundError:
    print(f"❌ ERROR: Staging file not found: {staging_file}")
    print("   Run create-appsettingsstaging.sh first")
    sys.exit(1)
except Exception as e:
    print(f"❌ Error: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
PYTHON

    echo ""
    echo "Verifying file contents:"
    if [ -f "$STAGING_FILE" ]; then
        echo "ConnectionStrings section:"
        python3 -c "import json; f=open('$STAGING_FILE'); c=json.load(f); print(json.dumps(c.get('ConnectionStrings', {}), indent=2))" 2>/dev/null || echo "Could not read ConnectionStrings"
    fi
ENDSSH

echo ""
echo "✅ Fix complete!"

