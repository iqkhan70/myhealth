#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to set up ConnectionStrings for Staging
# This will either:
# 1. Extract password from Production file (if exists)
# 2. Extract password from base appsettings.json
# 3. Or reset MySQL user password to a new known value

SSH_KEY="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app/server"
DB_USER="mentalhealth_user"
DB_NAME="mentalhealthdb"

echo "🔧 Setting up ConnectionStrings for Staging..."
echo ""

# First, try to get the password from existing files
echo "Step 1: Trying to retrieve existing database password..."
DB_PASSWORD=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    APP_DIR="/opt/mental-health-app/server"
    
    # Try Production file first
    if [ -f "$APP_DIR/appsettings.Production.json" ]; then
        python3 << 'PYTHON'
import json
import sys
try:
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
        config = json.load(f)
        if "ConnectionStrings" in config and "MySQL" in config["ConnectionStrings"]:
            conn_str = config["ConnectionStrings"]["MySQL"]
            # Parse password from connection string
            for part in conn_str.split(';'):
                if 'password=' in part.lower():
                    password = part.split('=', 1)[1].strip()
                    print(password)
                    sys.exit(0)
except:
    pass
sys.exit(1)
PYTHON
        if [ $? -eq 0 ]; then
            exit 0
        fi
    fi
    
    # Try base appsettings.json
    if [ -f "$APP_DIR/appsettings.json" ]; then
        python3 << 'PYTHON'
import json
import sys
try:
    with open('/opt/mental-health-app/server/appsettings.json', 'r') as f:
        config = json.load(f)
        if "ConnectionStrings" in config and "MySQL" in config["ConnectionStrings"]:
            conn_str = config["ConnectionStrings"]["MySQL"]
            # Parse password from connection string
            for part in conn_str.split(';'):
                if 'password=' in part.lower():
                    password = part.split('=', 1)[1].strip()
                    print(password)
                    sys.exit(0)
except:
    pass
sys.exit(1)
PYTHON
        if [ $? -eq 0 ]; then
            exit 0
        fi
    fi
    
    # If not found, return empty
    exit 1
ENDSSH
)

if [ -n "$DB_PASSWORD" ]; then
    echo "✅ Found existing database password!"
    echo ""
    read -p "Use this password for Staging? (yes/no): " use_existing
    
    if [ "$use_existing" != "yes" ]; then
        DB_PASSWORD=""
    fi
fi

# If password not found or user wants to reset
if [ -z "$DB_PASSWORD" ]; then
    echo ""
    echo "Step 2: Setting up new MySQL password..."
    echo ""
    echo "Options:"
    echo "  1. Reset MySQL user password to a new random password"
    echo "  2. Enter a password manually"
    echo ""
    read -p "Choose option (1 or 2): " option
    
    if [ "$option" == "1" ]; then
        # Generate new random password
        DB_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
        echo "Generated new password: $DB_PASSWORD"
        echo ""
        
        # Reset MySQL user password
        echo "Resetting MySQL user password..."
        ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << ENDSSH
            # Get MySQL root password
            if [ -f /root/mysql_root_password.txt ]; then
                MYSQL_ROOT_PASS=\$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
            else
                echo "⚠️  MySQL root password file not found!"
                echo "Please enter MySQL root password manually:"
                read -s MYSQL_ROOT_PASS
            fi
            
            # Reset user password
            mysql -u root -p"\$MYSQL_ROOT_PASS" << MYSQL_SCRIPT
            ALTER USER '$DB_USER'@'localhost' IDENTIFIED BY '$DB_PASSWORD';
            FLUSH PRIVILEGES;
MYSQL_SCRIPT
            
            if [ \$? -eq 0 ]; then
                echo "✅ MySQL user password reset successfully"
            else
                echo "❌ Failed to reset MySQL user password"
                exit 1
            fi
ENDSSH
        
        if [ $? -ne 0 ]; then
            echo "❌ Failed to reset password. Please try option 2 (manual entry)."
            exit 1
        fi
    else
        # Manual password entry
        read -sp "Enter MySQL password for $DB_USER: " DB_PASSWORD
        echo ""
        read -sp "Confirm password: " DB_PASSWORD_CONFIRM
        echo ""
        
        if [ "$DB_PASSWORD" != "$DB_PASSWORD_CONFIRM" ]; then
            echo "❌ Passwords don't match!"
            exit 1
        fi
        
        # Test the password
        echo "Testing password..."
        ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP \
            "mysql -u $DB_USER -p'$DB_PASSWORD' $DB_NAME -e 'SELECT 1;'" > /dev/null 2>&1
        
        if [ $? -ne 0 ]; then
            echo "❌ Password test failed! Please check the password."
            exit 1
        fi
        
        echo "✅ Password verified!"
    fi
fi

# Now update the Staging file with ConnectionStrings
echo ""
echo "Step 3: Updating appsettings.Staging.json with ConnectionStrings..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << ENDSSH
    STAGING_FILE="$APP_DIR/appsettings.Staging.json"
    
    python3 << 'PYTHON'
import json
import sys

staging_file = "$APP_DIR/appsettings.Staging.json"
db_user = "$DB_USER"
db_name = "$DB_NAME"
db_password = "$DB_PASSWORD"

try:
    # Read existing Staging file
    with open(staging_file, 'r') as f:
        config = json.load(f)
    
    # Add or update ConnectionStrings
    config["ConnectionStrings"] = {
        "MySQL": f"server=localhost;port=3306;database={db_name};user={db_user};password={db_password}"
    }
    
    # Write back
    with open(staging_file, 'w') as f:
        json.dump(config, f, indent=2)
    
    import os
    os.chmod(staging_file, 0o644)
    
    print("✅ Successfully updated appsettings.Staging.json with ConnectionStrings")
    print(f"   User: {db_user}")
    print(f"   Database: {db_name}")
    print("   Password: [hidden]")
    
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

    # Verify
    echo ""
    echo "Verifying ConnectionStrings in Staging file:"
    python3 -c "import json; f=open('$STAGING_FILE'); c=json.load(f); print('User:', c.get('ConnectionStrings', {}).get('MySQL', '').split('user=')[1].split(';')[0] if 'user=' in c.get('ConnectionStrings', {}).get('MySQL', '') else 'Not found')" 2>/dev/null || echo "Could not verify"
ENDSSH

echo ""
echo "✅ Setup complete!"
echo ""
echo "ConnectionStrings has been added to appsettings.Staging.json"
echo "The application should now be able to connect to the database."

