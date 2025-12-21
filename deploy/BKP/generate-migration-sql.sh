#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Generate SQL migration script from local database and apply to remote server
# This works around the issue of not having .NET SDK on the server

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration - UPDATE THESE
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
DB_NAME="mentalhealthdb"
DB_USER="mentalhealth_user"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Generate and Apply Database Migrations${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
        echo -e "${GREEN}✅ SSH key permissions fixed${NC}"
    fi
fi

# Check if we're in the project root
if [ ! -f "SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj" ]; then
    echo -e "${RED}ERROR: Please run this script from the project root directory${NC}"
    exit 1
fi

echo -e "\n${YELLOW}Step 1: Generating SQL migration script locally...${NC}"

# Get database password from server
echo "Getting database password from server..."
DB_PASSWORD=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

if [ -z "$DB_PASSWORD" ]; then
    echo -e "${RED}ERROR: Could not retrieve database password from server${NC}"
    echo "Please check appsettings.Production.json on the server"
    exit 1
fi

# Generate SQL script from migrations
echo "Generating SQL script from Entity Framework migrations..."
cd SM_MentalHealthApp.Server

# Check if dotnet ef is installed locally
if ! dotnet ef --version &>/dev/null; then
    echo "Installing dotnet-ef tool..."
    dotnet tool install --global dotnet-ef || true
fi

# Generate SQL script (this creates a SQL file with all pending migrations)
echo "Creating SQL migration script..."
dotnet ef migrations script --idempotent --output ../deploy/migration.sql || {
    echo -e "${YELLOW}⚠️ No migrations found or error generating script${NC}"
    echo "Trying alternative: Generate script from current database state..."
    
    # Alternative: Generate script that creates the entire database
    dotnet ef dbcontext info &>/dev/null || {
        echo -e "${RED}ERROR: Cannot access database context${NC}"
        echo "Please ensure your local appsettings.json has a valid connection string"
        exit 1
    }
}

cd ..

if [ ! -f "deploy/migration.sql" ]; then
    echo -e "${RED}ERROR: Migration SQL file was not generated${NC}"
    exit 1
fi

echo -e "${GREEN}✅ SQL migration script generated: deploy/migration.sql${NC}"
echo -e "${YELLOW}⚠️  You can review the SQL file before it's applied: deploy/migration.sql${NC}"
echo ""
# Only prompt if running interactively (stdin is a TTY)
if [ -t 0 ]; then
    read -p "Press Enter to continue applying to remote database, or Ctrl+C to cancel..."
fi

echo -e "\n${YELLOW}Step 2: Applying SQL script to remote database...${NC}"

# Copy SQL script to server
scp -i "$SSH_KEY_PATH" deploy/migration.sql "$DROPLET_USER@$DROPLET_IP:/tmp/migration.sql"

# Apply SQL script on server
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    echo "Applying migration SQL to database..."
    echo "Database: $DB_NAME"
    echo "User: $DB_USER"
    
    # Use mysql with password (no space after -p is intentional)
    mysql -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" < /tmp/migration.sql
    
    if [ \$? -eq 0 ]; then
        echo "✅ Migration applied successfully"
        rm /tmp/migration.sql
    else
        echo "❌ Migration failed. SQL script saved at /tmp/migration.sql for review"
        echo "You can review it with: cat /tmp/migration.sql"
        exit 1
    fi
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Database Migration Complete!${NC}"
echo -e "${GREEN}========================================${NC}"

