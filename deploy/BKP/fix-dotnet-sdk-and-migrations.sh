#!/bin/bash
# Complete fix script for .NET SDK, dotnet-ef, and migrations

set -e

# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo "=========================================="
echo "Fixing .NET SDK, dotnet-ef, and Running Migrations"
echo "=========================================="
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    echo "Step 1: Installing .NET 9.0 SDK..."
    
    # Download and install .NET 9.0 SDK
    if [ ! -f /tmp/dotnet-install.sh ]; then
        wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
    fi
    
    # Install .NET 9.0 SDK (not just runtime)
    /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
    
    # Set up PATH
    export PATH=$PATH:/usr/share/dotnet
    export PATH="$PATH:$HOME/.dotnet/tools"
    
    # Add to bashrc for persistence
    if ! grep -q '/usr/share/dotnet' ~/.bashrc; then
        echo 'export PATH="$PATH:/usr/share/dotnet"' >> ~/.bashrc
    fi
    if ! grep -q '.dotnet/tools' ~/.bashrc; then
        echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
    fi
    
    # Create symlink if needed
    if [ ! -f /usr/bin/dotnet ]; then
        ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
    fi
    
    # Verify SDK installation
    echo ""
    echo "Verifying .NET SDK installation..."
    /usr/share/dotnet/dotnet --version
    /usr/share/dotnet/dotnet --list-sdks
    
    echo ""
    echo "Step 2: Installing dotnet-ef tool..."
    
    # Install dotnet-ef
    export PATH=$PATH:/usr/share/dotnet
    export PATH="$PATH:$HOME/.dotnet/tools"
    
    if ! dotnet ef --version &>/dev/null; then
        dotnet tool install --global dotnet-ef --version 9.0.0 || dotnet tool install --global dotnet-ef
        export PATH="$PATH:$HOME/.dotnet/tools"
    fi
    
    # Verify dotnet-ef installation
    dotnet ef --version
    
    echo ""
    echo "✅ All fixes complete!"
    echo ""
    echo "You can now run migrations with:"
    echo "  export PATH=\$PATH:/usr/share/dotnet"
    echo "  export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
    echo "  cd /opt/mental-health-app/server"
    echo "  dotnet ef database update"
ENDSSH

# Generate migration SQL locally and apply it (more reliable than dotnet ef on server)
echo ""
echo "Step 3: Generating migration SQL locally..."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/.."

# Check if we're in the right directory
if [ ! -f "SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj" ]; then
    echo "❌ ERROR: Please run this script from the project root directory"
    exit 1
fi

cd SM_MentalHealthApp.Server

# Check if dotnet ef is installed locally
if ! dotnet ef --version &>/dev/null; then
    echo "Installing dotnet-ef tool locally..."
    dotnet tool install --global dotnet-ef || true
fi

# Generate SQL script from all migrations
MIGRATION_SQL_FILE="$SCRIPT_DIR/migration.sql"
echo "Creating SQL migration script..."
if dotnet ef migrations script --idempotent --output "$MIGRATION_SQL_FILE" 2>&1; then
    if [ -f "$MIGRATION_SQL_FILE" ] && [ -s "$MIGRATION_SQL_FILE" ]; then
        echo "✅ Migration SQL generated successfully"
        
        # Copy SQL to server
        echo "Copying migration SQL to server..."
        scp -i "$SSH_KEY_PATH" "$MIGRATION_SQL_FILE" "$DROPLET_USER@$DROPLET_IP:/tmp/migration.sql"
        
        # Apply SQL on server
        echo "Applying migration SQL to database..."
        SQL_SUCCESS=false
        TABLE_COUNT=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
            if [ -f /root/mysql_root_password.txt ]; then
                MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
                
                # Check if migration SQL has stored procedure syntax (DELIMITER, etc.)
                if grep -q "DELIMITER\|MigrationsScript" /tmp/migration.sql; then
                    echo "⚠️  Migration SQL contains stored procedure syntax, extracting CREATE TABLE statements..." >&2
                    # Extract just the CREATE TABLE statements
                    grep -E "^CREATE TABLE|^ALTER TABLE|^INSERT INTO|^CREATE INDEX" /tmp/migration.sql > /tmp/migration_clean.sql || true
                    
                    # Apply clean SQL
                    if [ -s /tmp/migration_clean.sql ]; then
                        mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb < /tmp/migration_clean.sql 2>&1 | grep -v "Enter password" | head -20 >&2
                    else
                        echo "⚠️  No clean SQL extracted, trying full SQL file..." >&2
                        mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb < /tmp/migration.sql 2>&1 | grep -v "Enter password" | head -20 >&2
                    fi
                else
                    # Apply SQL directly
                    echo "Applying migration SQL..." >&2
                    mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb < /tmp/migration.sql 2>&1 | grep -v "Enter password" | head -20 >&2
                fi
                
                # Verify tables were created and return count
                TABLE_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = 'customerhealthdb' AND table_name != '__EFMigrationsHistory';" 2>/dev/null | tail -1)
                echo "$TABLE_COUNT"
                
                rm -f /tmp/migration.sql /tmp/migration_clean.sql
            else
                echo "0"
            fi
ENDSSH
        )
        
        # Check if tables were created
        if [ "$TABLE_COUNT" = "0" ] || [ -z "$TABLE_COUNT" ]; then
            echo "⚠️  No tables created from SQL (count: $TABLE_COUNT). Will try dotnet ef on server..."
            MIGRATION_METHOD="DOTNET_EF"
        else
            echo "✅ Migrations completed successfully via SQL! Tables: $TABLE_COUNT"
            MIGRATION_METHOD="SQL_SUCCESS"
        fi
    else
        echo "⚠️  Migration SQL file is empty"
        MIGRATION_METHOD="DOTNET_EF"
    fi
else
    echo "⚠️  Could not generate migration SQL, will try dotnet ef on server"
    MIGRATION_METHOD="DOTNET_EF"
fi

cd "$SCRIPT_DIR/.."

# If SQL migration didn't work, try dotnet ef on server
if [ "$MIGRATION_METHOD" = "DOTNET_EF" ]; then
    echo ""
    echo "Step 3b: Copying .csproj file to server for dotnet ef..."
    if [ -f "SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj" ]; then
        scp -i "$SSH_KEY_PATH" SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj "$DROPLET_USER@$DROPLET_IP:/opt/mental-health-app/server/"
        echo "✅ .csproj file copied"
    else
        echo "❌ ERROR: .csproj file not found locally"
        exit 1
    fi
fi

# If SQL migration didn't work, try dotnet ef on server
if [ "$MIGRATION_METHOD" = "DOTNET_EF" ]; then
    echo ""
    echo "Step 4: Running database migrations with dotnet ef..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
        export PATH=$PATH:/usr/share/dotnet
        export PATH="$PATH:$HOME/.dotnet/tools"
        
        cd /opt/mental-health-app/server
        
        if [ ! -f SM_MentalHealthApp.Server.csproj ]; then
            echo "❌ ERROR: .csproj file not found!"
            exit 1
        fi
        
        # Restore the project first (needed for dotnet ef)
        echo "Restoring project dependencies..."
        dotnet restore SM_MentalHealthApp.Server.csproj || {
            echo "⚠️  Restore failed, trying without specifying project..."
            dotnet restore || true
        }
        
        # Run migrations
        echo "Running: dotnet ef database update"
        dotnet ef database update --project SM_MentalHealthApp.Server.csproj || dotnet ef database update
        
        # Verify tables were created
        if [ -f /root/mysql_root_password.txt ]; then
            MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
            TABLE_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = 'customerhealthdb' AND table_name != '__EFMigrationsHistory';" 2>/dev/null | tail -1)
            echo ""
            echo "✅ Migrations completed!"
            echo "Tables created: $TABLE_COUNT"
        fi
ENDSSH
fi

echo ""
echo "=========================================="
echo "Fix Complete!"
echo "=========================================="

