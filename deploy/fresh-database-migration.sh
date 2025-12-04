#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Fresh database migration script - drops and recreates database with all migrations
# Use this when you want to start from scratch and ensure schema matches local

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
DB_NAME="mentalhealthdb"
DB_USER="mentalhealth_user"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Fresh Database Migration Script${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}⚠️  WARNING: This will DROP and RECREATE the database!${NC}"
echo -e "${YELLOW}⚠️  All existing data will be LOST!${NC}"
echo ""

# Fix SSH key permissions
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

# Check if we're in the project root
if [ ! -f "SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj" ]; then
    echo -e "${RED}ERROR: Please run this script from the project root directory${NC}"
    exit 1
fi

# Get database password from server
echo "Getting database password from server..."
DB_PASSWORD=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

if [ -z "$DB_PASSWORD" ]; then
    echo -e "${RED}ERROR: Could not retrieve database password from server${NC}"
    exit 1
fi

# Confirm before proceeding
if [ -t 0 ]; then
    echo -e "${RED}Are you sure you want to DROP and recreate the database?${NC}"
    echo -e "${YELLOW}Type 'yes' to continue, or anything else to cancel:${NC}"
    read -r CONFIRM
    if [ "$CONFIRM" != "yes" ]; then
        echo "Cancelled."
        exit 0
    fi
fi

echo -e "\n${YELLOW}Step 1: Generating complete migration SQL from scratch...${NC}"

cd SM_MentalHealthApp.Server

# Check if dotnet ef is installed
if ! dotnet ef --version &>/dev/null; then
    echo "Installing dotnet-ef tool..."
    dotnet tool install --global dotnet-ef || true
fi

# Generate SQL script from ALL migrations (from scratch)
echo "Generating SQL script from all migrations..."
dotnet ef migrations script --idempotent --output ../deploy/fresh-migration.sql || {
    echo -e "${RED}ERROR: Failed to generate migration script${NC}"
    exit 1
}

cd ..

if [ ! -f "deploy/fresh-migration.sql" ]; then
    echo -e "${RED}ERROR: Migration SQL file was not generated${NC}"
    exit 1
fi

echo -e "${GREEN}✅ SQL migration script generated${NC}"
echo ""

# Review option
if [ -t 0 ]; then
    echo -e "${YELLOW}Review the SQL file? (y/n)${NC}"
    read -r REVIEW
    if [ "$REVIEW" = "y" ] || [ "$REVIEW" = "Y" ]; then
        less deploy/fresh-migration.sql || cat deploy/fresh-migration.sql | head -100
        echo ""
        read -p "Press Enter to continue..."
    fi
fi

echo -e "\n${YELLOW}Step 2: Dropping and recreating database on server...${NC}"

# Copy SQL script to server
scp -i "$SSH_KEY_PATH" deploy/fresh-migration.sql "$DROPLET_USER@$DROPLET_IP:/tmp/fresh-migration.sql"

# Drop and recreate database, then apply migrations
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    set -e
    
    echo "Dropping database if it exists..."
    # Try with DB user first (if they have DROP privileges)
    mysql -u "$DB_USER" -p"$DB_PASSWORD" -e "DROP DATABASE IF EXISTS $DB_NAME;" 2>/dev/null || {
        echo "DB user doesn't have DROP privileges, trying as root..."
        # If that fails, try as root (may need password)
        mysql -u root -e "DROP DATABASE IF EXISTS $DB_NAME;" 2>/dev/null || {
            echo "⚠️  Could not drop database automatically. You may need to do this manually:"
            echo "   mysql -u root -p"
            echo "   DROP DATABASE IF EXISTS $DB_NAME;"
            echo "   CREATE DATABASE $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
            exit 1
        }
    }
    
    echo "Creating fresh database..."
    mysql -u "$DB_USER" -p"$DB_PASSWORD" -e "CREATE DATABASE $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" 2>/dev/null || {
        mysql -u root -e "CREATE DATABASE $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
    }
    
    echo "✅ Database recreated"
    echo ""
    echo "Applying all migrations..."
    mysql -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" < /tmp/fresh-migration.sql
    
    if [ \$? -eq 0 ]; then
        echo "✅ All migrations applied successfully"
        rm /tmp/fresh-migration.sql
        
        # Verify migration history
        echo ""
        echo "Applied migrations:"
        mysql -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -e "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;" 2>/dev/null || echo "No migration history found"
    else
        echo "❌ Migration failed. SQL script saved at /tmp/fresh-migration.sql for review"
        exit 1
    fi
ENDSSH

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✅ Fresh Database Migration Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo "The database has been recreated with all migrations applied."
    echo "You may need to:"
    echo "  1. Restart the application: systemctl restart mental-health-app"
    echo "  2. Re-seed any initial data if needed"
else
    echo -e "${RED}Migration failed. Check the error messages above.${NC}"
    exit 1
fi

