#!/bin/bash
# Quick fix script for current server (137.184.80.13)
# Fixes: .NET 9.0 installation, migrations, seeding

set -e

# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
DB_NAME="customerhealthdb"
DB_USER="mentalhealth_user"

echo "=========================================="
echo "Fixing Current Server Issues"
echo "=========================================="
echo ""

# Step 1: Install .NET 9.0
echo "Step 1: Installing .NET 9.0 runtime..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    if [ ! -f /tmp/dotnet-install.sh ]; then
        wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
    fi
    
    # Install .NET 9.0 runtime
    /tmp/dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir /usr/share/dotnet
    /tmp/dotnet-install.sh --channel 9.0 --runtime dotnet --install-dir /usr/share/dotnet
    
    # Verify installation
    echo "Installed runtimes:"
    /usr/share/dotnet/dotnet --list-runtimes | grep "9.0"
ENDSSH

# Step 2: Run migrations
echo ""
echo "Step 2: Running database migrations..."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/.."

# Generate migration SQL locally
echo "Generating migration SQL..."
cd SM_MentalHealthApp.Server
if ! dotnet ef --version &>/dev/null; then
    dotnet tool install --global dotnet-ef || true
fi

MIGRATION_SQL_FILE="$SCRIPT_DIR/migration.sql"
if dotnet ef migrations script --idempotent --output "$MIGRATION_SQL_FILE" 2>&1; then
    if [ -f "$MIGRATION_SQL_FILE" ] && [ -s "$MIGRATION_SQL_FILE" ]; then
        echo "✅ Migration SQL generated"
        
        # Copy and apply
        scp -i "$SSH_KEY_PATH" "$MIGRATION_SQL_FILE" "$DROPLET_USER@$DROPLET_IP:/tmp/migration.sql"
        
        ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
            if [ -f /root/mysql_root_password.txt ]; then
                MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
                echo "Applying migrations..."
                mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb < /tmp/migration.sql 2>&1 | grep -v "Enter password" || true
                
                # Verify
                TABLE_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = 'customerhealthdb';" 2>/dev/null | tail -1)
                echo "✅ Tables created: $TABLE_COUNT"
            fi
            rm -f /tmp/migration.sql
ENDSSH
    else
        echo "⚠️  Migration SQL file is empty"
    fi
else
    echo "⚠️  Could not generate migration SQL"
fi

cd "$SCRIPT_DIR/.."

# Step 3: Run seeding
echo ""
echo "Step 3: Running database seeding..."
if [ -f "deploy/Scripts121925/SeedingInitialConsolidatedScript.sh" ]; then
    scp -i "$SSH_KEY_PATH" deploy/Scripts121925/SeedingInitialConsolidatedScript.sh "$DROPLET_USER@$DROPLET_IP:/tmp/seed.sql"
    
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
        if [ -f /root/mysql_root_password.txt ]; then
            MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
            echo "Applying seeding script..."
            sed '/^[[:space:]]*$/d' /tmp/seed.sql | mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb 2>&1 | grep -v "Enter password" || true
            
            # Verify
            ROLES_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 || echo "0")
            USERS_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -1 || echo "0")
            echo "✅ Roles seeded: $ROLES_COUNT"
            echo "✅ Users seeded: $USERS_COUNT"
        fi
        rm -f /tmp/seed.sql
ENDSSH
else
    echo "⚠️  Seeding script not found"
fi

# Step 4: Restart service
echo ""
echo "Step 4: Restarting service..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    systemctl daemon-reload
    systemctl restart mental-health-app
    sleep 5
    
    if systemctl is-active --quiet mental-health-app; then
        echo "✅ Service is running"
        systemctl status mental-health-app --no-pager | head -10
    else
        echo "❌ Service failed to start. Checking logs..."
        journalctl -u mental-health-app --no-pager -n 20 | tail -15
    fi
ENDSSH

echo ""
echo "=========================================="
echo "Fix Complete!"
echo "=========================================="

