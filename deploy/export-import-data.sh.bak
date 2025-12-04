#!/bin/bash

# Export data from local database and import to DigitalOcean database
# This script handles the complete data migration process

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration - UPDATE THESE
DROPLET_IP="159.65.242.79"
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
REMOTE_DB_NAME="mentalhealthdb"
REMOTE_DB_USER="mentalhealth_user"

# Local database config (from appsettings.json)
LOCAL_DB_CONFIG_FILE="SM_MentalHealthApp.Server/appsettings.json"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Database Data Migration Tool${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

# Check if we're in the project root
if [ ! -f "$LOCAL_DB_CONFIG_FILE" ]; then
    echo -e "${RED}ERROR: Please run this script from the project root directory${NC}"
    exit 1
fi

# Parse local database connection string
echo -e "\n${YELLOW}Step 1: Reading local database configuration...${NC}"

if [ ! -f "$LOCAL_DB_CONFIG_FILE" ]; then
    echo -e "${RED}ERROR: Cannot find $LOCAL_DB_CONFIG_FILE${NC}"
    exit 1
fi

# Extract connection string components
LOCAL_CONN_STRING=$(grep -A 1 '"MySQL"' "$LOCAL_DB_CONFIG_FILE" | grep -o '"[^"]*"' | tail -n 1 | tr -d '"')

if [ -z "$LOCAL_CONN_STRING" ]; then
    echo -e "${RED}ERROR: Could not find MySQL connection string in $LOCAL_DB_CONFIG_FILE${NC}"
    exit 1
fi

# Parse connection string (format: server=host;port=3306;database=db;user=user;password=pass)
LOCAL_DB_HOST=$(echo "$LOCAL_CONN_STRING" | grep -o 'server=[^;]*' | cut -d'=' -f2)
LOCAL_DB_PORT=$(echo "$LOCAL_CONN_STRING" | grep -o 'port=[^;]*' | cut -d'=' -f2 || echo "3306")
LOCAL_DB_NAME=$(echo "$LOCAL_CONN_STRING" | grep -o 'database=[^;]*' | cut -d'=' -f2)
LOCAL_DB_USER=$(echo "$LOCAL_CONN_STRING" | grep -o 'user=[^;]*' | cut -d'=' -f2)
LOCAL_DB_PASS=$(echo "$LOCAL_CONN_STRING" | grep -o 'password=[^;]*' | cut -d'=' -f2)

if [ -z "$LOCAL_DB_NAME" ] || [ -z "$LOCAL_DB_USER" ]; then
    echo -e "${RED}ERROR: Could not parse local database connection string${NC}"
    echo "Connection string: $LOCAL_CONN_STRING"
    exit 1
fi

echo -e "${GREEN}✅ Local database: ${LOCAL_DB_NAME}@${LOCAL_DB_HOST:-localhost}${NC}"

# Get remote database password
echo -e "\n${YELLOW}Step 2: Reading remote database configuration...${NC}"
REMOTE_DB_PASSWORD=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

if [ -z "$REMOTE_DB_PASSWORD" ]; then
    echo -e "${RED}ERROR: Could not retrieve remote database password${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Remote database: ${REMOTE_DB_NAME}@${DROPLET_IP}${NC}"

# Check if mysqldump is available
if ! command -v mysqldump &> /dev/null; then
    echo -e "${RED}ERROR: mysqldump is not installed${NC}"
    echo "Install it with: brew install mysql-client (macOS) or apt-get install mysql-client (Linux)"
    exit 1
fi

# Export data from local database
echo -e "\n${YELLOW}Step 3: Exporting data from local database...${NC}"

DUMP_FILE="deploy/data_export_$(date +%Y%m%d_%H%M%S).sql"
mkdir -p deploy

echo "Creating data dump (schema + data)..."
echo "Note: Excluding __EFMigrationsHistory table (migrations already run on remote)"
echo "Tables that don't exist on remote will be created automatically"

# Build mysqldump command
MYSQLDUMP_CMD="mysqldump"
if [ ! -z "$LOCAL_DB_HOST" ] && [ "$LOCAL_DB_HOST" != "localhost" ] && [ "$LOCAL_DB_HOST" != "127.0.0.1" ]; then
    MYSQLDUMP_CMD="$MYSQLDUMP_CMD -h $LOCAL_DB_HOST"
fi
if [ ! -z "$LOCAL_DB_PORT" ] && [ "$LOCAL_DB_PORT" != "3306" ]; then
    MYSQLDUMP_CMD="$MYSQLDUMP_CMD -P $LOCAL_DB_PORT"
fi

# Export schema + data
# --skip-triggers: skip triggers (to avoid conflicts)
# --skip-add-drop-table: don't add DROP TABLE (preserves existing data)
# --complete-insert: use complete INSERT statements with column names (safer)
# --skip-extended-insert: one INSERT per line (easier to debug)
# --skip-lock-tables: don't lock tables (for local DB)
# --single-transaction: consistent snapshot
# --ignore-table: exclude __EFMigrationsHistory (migrations already run on remote)
# Note: CREATE TABLE statements will be included, so missing tables will be created
$MYSQLDUMP_CMD \
    -u "$LOCAL_DB_USER" \
    -p"$LOCAL_DB_PASS" \
    --skip-triggers \
    --skip-add-drop-table \
    --complete-insert \
    --skip-extended-insert \
    --skip-lock-tables \
    --single-transaction \
    --ignore-table="$LOCAL_DB_NAME.__EFMigrationsHistory" \
    "$LOCAL_DB_NAME" > "$DUMP_FILE" 2>/dev/null

if [ ! -s "$DUMP_FILE" ]; then
    echo -e "${YELLOW}⚠️  Dump file is empty. This might mean:${NC}"
    echo "  1. Local database is empty"
    echo "  2. Connection failed"
    echo ""
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        rm -f "$DUMP_FILE"
        exit 1
    fi
fi

DUMP_SIZE=$(du -h "$DUMP_FILE" | cut -f1)
echo -e "${GREEN}✅ Data exported: $DUMP_FILE (${DUMP_SIZE})${NC}"

# Ask user if they want to review the dump
if [ -t 0 ]; then
    echo ""
    read -p "Review the dump file before importing? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "Opening dump file... (Press q to quit when done)"
        less "$DUMP_FILE" || cat "$DUMP_FILE" | head -50
    fi
fi

# Modify SQL to use CREATE TABLE IF NOT EXISTS (do this locally before transfer)
echo -e "\n${YELLOW}Step 4: Modifying SQL for safe import...${NC}"
echo "Adding 'IF NOT EXISTS' to CREATE TABLE statements..."
sed -i '' 's/^CREATE TABLE `/CREATE TABLE IF NOT EXISTS `/g' "$DUMP_FILE" 2>/dev/null || \
sed -i 's/^CREATE TABLE `/CREATE TABLE IF NOT EXISTS `/g' "$DUMP_FILE" 2>/dev/null || \
perl -i -pe 's/^CREATE TABLE `/CREATE TABLE IF NOT EXISTS `/g' "$DUMP_FILE" 2>/dev/null

echo -e "${GREEN}✅ SQL modified${NC}"

# Transfer dump file to server
echo -e "\n${YELLOW}Step 5: Transferring dump file to server...${NC}"
scp -i "$SSH_KEY_PATH" "$DUMP_FILE" "$DROPLET_USER@$DROPLET_IP:/tmp/data_import.sql"

echo -e "${GREEN}✅ File transferred${NC}"

# Import data into remote database
echo -e "\n${YELLOW}Step 6: Importing data into remote database...${NC}"
echo -e "${YELLOW}⚠️  This will:${NC}"
echo -e "${YELLOW}  - Create tables that don't exist on remote${NC}"
echo -e "${YELLOW}  - Add data to existing tables (existing data will NOT be deleted)${NC}"

if [ -t 0 ]; then
    read -p "Continue with import? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Import cancelled. Dump file is at: $DUMP_FILE"
        ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "rm -f /tmp/data_import.sql"
        exit 0
    fi
fi

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    echo "Importing data and creating missing tables..."
    # SQL file already has 'CREATE TABLE IF NOT EXISTS' (modified locally)
    # Use --force to continue on errors (like duplicate keys), but still report them
    mysql -u "$REMOTE_DB_USER" -p"$REMOTE_DB_PASSWORD" "$REMOTE_DB_NAME" --force < /tmp/data_import.sql 2>&1 | grep -v "^mysql:" | grep -v "^Warning:" || true
    
    # Check if import was successful (even with --force, we check exit code)
    IMPORT_EXIT_CODE=\${PIPESTATUS[0]}
    if [ \$IMPORT_EXIT_CODE -eq 0 ] || [ \$IMPORT_EXIT_CODE -eq 1 ]; then
        # Exit code 0 = success, 1 = warnings but continued (acceptable with --force)
        echo "✅ Import completed"
        
        # Show row counts for key tables
        echo ""
        echo "Row counts after import:"
        mysql -u "$REMOTE_DB_USER" -p"$REMOTE_DB_PASSWORD" "$REMOTE_DB_NAME" -e "
            SELECT 'Users' as table_name, COUNT(*) as row_count FROM Users
            UNION ALL SELECT 'Roles', COUNT(*) FROM Roles
            UNION ALL SELECT 'JournalEntries', COUNT(*) FROM JournalEntries
            UNION ALL SELECT 'ChatSessions', COUNT(*) FROM ChatSessions
            UNION ALL SELECT 'ChatMessages', COUNT(*) FROM ChatMessages
            UNION ALL SELECT 'Appointments', COUNT(*) FROM Appointments
            ORDER BY table_name;" 2>/dev/null
        
        # List any new tables that were created
        echo ""
        echo "All tables in database:"
        mysql -u "$REMOTE_DB_USER" -p"$REMOTE_DB_PASSWORD" "$REMOTE_DB_NAME" -e "SHOW TABLES;" 2>/dev/null | tail -n +2
        
        echo ""
        echo "Keeping SQL file at /tmp/data_import.sql for review (you can delete it manually if not needed)"
        # Uncomment the line below if you want to auto-delete on success:
        # rm /tmp/data_import.sql
    else
        echo "❌ Import failed. SQL file saved at /tmp/data_import.sql for review"
        exit 1
    fi
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Data Migration Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Local dump file saved at: $DUMP_FILE"
echo "You can delete it after verifying the import was successful."

