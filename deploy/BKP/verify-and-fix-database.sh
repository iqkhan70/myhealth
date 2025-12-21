#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to verify database setup and run migrations/seeding if needed

set -e

DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
DB_NAME="customerhealthdb"
DB_USER="mentalhealth_user"

echo "Verifying database setup..."

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    APP_DIR="$APP_DIR"
    DB_NAME="$DB_NAME"
    DB_USER="$DB_USER"
    
    # Get MySQL root password
    MYSQL_ROOT_PASS=$(grep 'MySQL root password:' /root/mysql_root_password.txt 2>/dev/null | cut -d' ' -f4)
    
    # Get database password from appsettings
    DB_PASSWORD=$(python3 << PYEOF
import json
try:
    with open("$APP_DIR/server/appsettings.Production.json") as f:
        data = json.load(f)
        conn_str = data["ConnectionStrings"]["MySQL"]
        if "password=" in conn_str:
            pwd = conn_str.split("password=")[1].split(";")[0]
            print(pwd)
except:
    pass
PYEOF
)
    
    echo "Checking database status..."
    
    # Check if database exists
    DB_EXISTS=$(mysql -u root -p$MYSQL_ROOT_PASS -e "SHOW DATABASES LIKE '$DB_NAME';" 2>/dev/null | grep -c "$DB_NAME" || echo "0")
    
    if [ "$DB_EXISTS" = "0" ]; then
        echo "Database $DB_NAME does not exist"
        echo "Creating database..."
        mysql -u root -p$MYSQL_ROOT_PASS << MYSQL_SCRIPT
CREATE DATABASE IF NOT EXISTS $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '$DB_USER'@'localhost' IDENTIFIED BY '$DB_PASSWORD';
GRANT ALL PRIVILEGES ON $DB_NAME.* TO '$DB_USER'@'localhost';
FLUSH PRIVILEGES;
MYSQL_SCRIPT
        echo "Database created"
    else
        echo "Database $DB_NAME exists"
    fi
    
    # Check if tables exist
    TABLE_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = '$DB_NAME';" 2>/dev/null | tail -1)
    
    echo "Current table count: $TABLE_COUNT"
    
    if [ "$TABLE_COUNT" = "0" ] || [ -z "$TABLE_COUNT" ]; then
        echo "No tables found - migrations need to be run"
        
        # Check if .NET SDK is available
        export PATH=$PATH:/usr/share/dotnet
        
        if ! /usr/share/dotnet/dotnet --list-sdks &>/dev/null 2>&1; then
            echo "Installing .NET SDK..."
            if [ ! -f /tmp/dotnet-install.sh ]; then
                wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
                chmod +x /tmp/dotnet-install.sh
            fi
            /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
            export PATH=$PATH:/usr/share/dotnet
            ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet 2>/dev/null || true
        fi
        
        # Use full path to dotnet
        DOTNET_CMD=/usr/share/dotnet/dotnet
        if [ ! -f "$DOTNET_CMD" ]; then
            DOTNET_CMD=dotnet
        fi
        
        export PATH="$PATH:$HOME/.dotnet/tools"
        
        if ! $DOTNET_CMD ef --version &>/dev/null 2>&1; then
            echo "Installing dotnet-ef tool..."
            $DOTNET_CMD tool install --global dotnet-ef --version 9.0.0 2>&1 || $DOTNET_CMD tool install --global dotnet-ef 2>&1
            export PATH="$PATH:$HOME/.dotnet/tools"
        fi
        
        echo "Running migrations..."
        cd $APP_DIR/server
        $DOTNET_CMD ef database update --no-build 2>&1 || $DOTNET_CMD ef database update 2>&1
        
        # Check table count again
        NEW_TABLE_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = '$DB_NAME';" 2>/dev/null | tail -1)
        echo "Migrations completed. Table count: $NEW_TABLE_COUNT"
        
        # Check if seeding is needed
        ROLES_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME -e "SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 || echo "0")
        
        if [ "$ROLES_COUNT" = "0" ] || [ -z "$ROLES_COUNT" ]; then
            echo "No data found - seeding needed"
            
            if [ -f "$APP_DIR/scripts/SeedingInitialConsolidatedScript.sh" ]; then
                echo "Running seeding script..."
                # Remove blank lines at the start and execute SQL
                sed '/^[[:space:]]*$/d' "$APP_DIR/scripts/SeedingInitialConsolidatedScript.sh" | mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME 2>&1 | grep -v "Enter password" || {
                    if [ -n "$DB_PASSWORD" ]; then
                        sed '/^[[:space:]]*$/d' "$APP_DIR/scripts/SeedingInitialConsolidatedScript.sh" | mysql -u "$DB_USER" -p"$DB_PASSWORD" $DB_NAME 2>&1 | grep -v "Enter password" || true
                    fi
                }
                echo "Seeding completed"
            else
                echo "Seeding script not found at $APP_DIR/scripts/SeedingInitialConsolidatedScript.sh"
            fi
        else
            echo "Data already exists - Roles count: $ROLES_COUNT"
        fi
    else
        echo "Tables exist - count: $TABLE_COUNT"
        
        # Check if seeding is needed
        ROLES_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME -e "SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 || echo "0")
        
        if [ "$ROLES_COUNT" = "0" ] || [ -z "$ROLES_COUNT" ]; then
            echo "Tables exist but no data - seeding needed"
            
            if [ -f "$APP_DIR/scripts/SeedingInitialConsolidatedScript.sh" ]; then
                echo "Running seeding script..."
                # Remove blank lines at the start and execute SQL
                sed '/^[[:space:]]*$/d' "$APP_DIR/scripts/SeedingInitialConsolidatedScript.sh" | mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME 2>&1 | grep -v "Enter password" || {
                    if [ -n "$DB_PASSWORD" ]; then
                        sed '/^[[:space:]]*$/d' "$APP_DIR/scripts/SeedingInitialConsolidatedScript.sh" | mysql -u "$DB_USER" -p"$DB_PASSWORD" $DB_NAME 2>&1 | grep -v "Enter password" || true
                    fi
                }
                echo "Seeding completed"
            else
                echo "Seeding script not found"
            fi
        else
            echo "Data exists - Roles count: $ROLES_COUNT"
        fi
    fi
    
    # Final status
    echo ""
    echo "=== Final Database Status ==="
    TABLE_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = '$DB_NAME';" 2>/dev/null | tail -1)
    ROLES_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME -e "SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 || echo "0")
    USERS_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS $DB_NAME -e "SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -1 || echo "0")
    
    echo "Tables: $TABLE_COUNT"
    echo "Roles: $ROLES_COUNT"
    echo "Users: $USERS_COUNT"
ENDSSH

echo ""
echo "Database verification complete!"
