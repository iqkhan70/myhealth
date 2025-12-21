#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to retrieve ConnectionStrings from the DigitalOcean server
# This helps you discover what database credentials are actually being used

SSH_KEY="$HOME/.ssh/id_rsa"

echo "🔍 Retrieving ConnectionStrings from DigitalOcean server..."
echo ""

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    APP_DIR="/opt/mental-health-app/server"
    
    echo "Checking for ConnectionStrings in appsettings files..."
    echo ""
    
    # Check Production file
    PROD_FILE="$APP_DIR/appsettings.Production.json"
    if [ -f "$PROD_FILE" ]; then
        echo "📄 appsettings.Production.json:"
        python3 << 'PYTHON'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
        config = json.load(f)
        if "ConnectionStrings" in config:
            conn_strs = config["ConnectionStrings"]
            print(json.dumps(conn_strs, indent=2))
        else:
            print("  ⚠️  No ConnectionStrings section found")
except FileNotFoundError:
    print("  ❌ File not found")
except json.JSONDecodeError as e:
    print(f"  ❌ Invalid JSON: {e}")
except Exception as e:
    print(f"  ❌ Error: {e}")
PYTHON
        echo ""
    else
        echo "📄 appsettings.Production.json: ❌ File not found"
        echo ""
    fi
    
    # Check Staging file
    STAGING_FILE="$APP_DIR/appsettings.Staging.json"
    if [ -f "$STAGING_FILE" ]; then
        echo "📄 appsettings.Staging.json:"
        python3 << 'PYTHON'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.Staging.json', 'r') as f:
        config = json.load(f)
        if "ConnectionStrings" in config:
            conn_strs = config["ConnectionStrings"]
            print(json.dumps(conn_strs, indent=2))
        else:
            print("  ⚠️  No ConnectionStrings section found")
except FileNotFoundError:
    print("  ❌ File not found")
except json.JSONDecodeError as e:
    print(f"  ❌ Invalid JSON: {e}")
except Exception as e:
    print(f"  ❌ Error: {e}")
PYTHON
        echo ""
    else
        echo "📄 appsettings.Staging.json: ❌ File not found"
        echo ""
    fi
    
    # Check base appsettings.json
    BASE_FILE="$APP_DIR/appsettings.json"
    if [ -f "$BASE_FILE" ]; then
        echo "📄 appsettings.json (base):"
        python3 << 'PYTHON'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.json', 'r') as f:
        config = json.load(f)
        if "ConnectionStrings" in config:
            conn_strs = config["ConnectionStrings"]
            print(json.dumps(conn_strs, indent=2))
        else:
            print("  ⚠️  No ConnectionStrings section found")
except FileNotFoundError:
    print("  ❌ File not found")
except json.JSONDecodeError as e:
    print(f"  ❌ Invalid JSON: {e}")
except Exception as e:
    print(f"  ❌ Error: {e}")
PYTHON
        echo ""
    else
        echo "📄 appsettings.json: ❌ File not found"
        echo ""
    fi
    
    # Check systemd service for environment variables
    echo "🔍 Checking systemd service for database connection info..."
    if [ -f "/etc/systemd/system/mental-health-app.service" ]; then
        echo "Environment variables in systemd service:"
        grep -i "Environment=" /etc/systemd/system/mental-health-app.service | grep -i -E "(connection|mysql|database|db)" || echo "  No database-related environment variables found"
        echo ""
    fi
    
    # Check MySQL users (if we can connect)
    echo "🔍 Checking MySQL users (if accessible)..."
    mysql -u root -e "SELECT User, Host FROM mysql.user WHERE User NOT IN ('mysql.sys', 'mysql.session', 'mysql.infoschema');" 2>/dev/null || echo "  ⚠️  Cannot access MySQL directly"
    echo ""
    
    echo "💡 Tip: The ConnectionStrings from Production file should be used for Staging if they're on the same server."
ENDSSH

echo ""
echo "✅ ConnectionStrings retrieval complete!"
echo ""
echo "Next steps:"
echo "1. Use the ConnectionStrings from Production file (if found)"
echo "2. Or manually add ConnectionStrings to Staging file"
echo "3. Run: ./deploy/fix-staging-connectionstring.sh to apply"

