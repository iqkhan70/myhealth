#!/bin/bash

# Query database on DigitalOcean server to verify tables and data
# This script helps you inspect the database structure and contents

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
DB_NAME="mentalhealthdb"
DB_USER="mentalhealth_user"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Database Query Tool${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

# Get database password from server
echo -e "\n${YELLOW}Connecting to database...${NC}"
DB_PASSWORD=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

if [ -z "$DB_PASSWORD" ]; then
    echo -e "${RED}ERROR: Could not retrieve database password from server${NC}"
    exit 1
fi

# Function to run SQL query
run_query() {
    local query="$1"
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
        "mysql -u \"$DB_USER\" -p\"$DB_PASSWORD\" \"$DB_NAME\" -e \"$query\"" 2>/dev/null
}

echo -e "${GREEN}✅ Connected to database: $DB_NAME${NC}\n"

# 1. List all tables
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}1. All Tables in Database${NC}"
echo -e "${BLUE}========================================${NC}"
run_query "SHOW TABLES;"

# 2. Count tables
echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}2. Table Count${NC}"
echo -e "${BLUE}========================================${NC}"
TABLE_COUNT=$(run_query "SELECT COUNT(*) as table_count FROM information_schema.tables WHERE table_schema = '$DB_NAME';" | tail -n 1 | awk '{print $1}')
echo "Total tables: $TABLE_COUNT"

# 3. Migration history
echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}3. Applied Migrations${NC}"
echo -e "${BLUE}========================================${NC}"
run_query "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 10;"

# 4. Key tables with row counts
echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}4. Key Tables Row Counts${NC}"
echo -e "${BLUE}========================================${NC}"

# Use a single query to get all counts - much simpler
run_query "SELECT 
    'Users' as table_name, COUNT(*) as row_count FROM Users
    UNION ALL SELECT 'Roles', COUNT(*) FROM Roles
    UNION ALL SELECT 'JournalEntries', COUNT(*) FROM JournalEntries
    UNION ALL SELECT 'ChatSessions', COUNT(*) FROM ChatSessions
    UNION ALL SELECT 'ChatMessages', COUNT(*) FROM ChatMessages
    UNION ALL SELECT 'Appointments', COUNT(*) FROM Appointments
    UNION ALL SELECT 'ClinicalNotes', COUNT(*) FROM ClinicalNotes
    UNION ALL SELECT 'ContentAnalyses', COUNT(*) FROM ContentAnalyses
    UNION ALL SELECT 'AIInstructions', COUNT(*) FROM AIInstructions
    UNION ALL SELECT 'KnowledgeBaseEntries', COUNT(*) FROM KnowledgeBaseEntries
    UNION ALL SELECT 'AIResponseTemplates', COUNT(*) FROM AIResponseTemplates
    UNION ALL SELECT 'AIInstructionCategories', COUNT(*) FROM AIInstructionCategories
    UNION ALL SELECT 'KnowledgeBaseCategories', COUNT(*) FROM KnowledgeBaseCategories
    ORDER BY table_name;" 2>/dev/null

# 5. Database size
echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}5. Database Size${NC}"
echo -e "${BLUE}========================================${NC}"
run_query "SELECT 
    table_schema AS 'Database',
    ROUND(SUM(data_length + index_length) / 1024 / 1024, 2) AS 'Size (MB)'
FROM information_schema.tables 
WHERE table_schema = '$DB_NAME'
GROUP BY table_schema;"

# 6. Table structures (sample)
echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}6. Sample Table Structures${NC}"
echo -e "${BLUE}========================================${NC}"

echo -e "${YELLOW}Users table structure:${NC}"
run_query "DESCRIBE Users;" 2>/dev/null | head -10

echo -e "\n${YELLOW}Roles table structure:${NC}"
run_query "DESCRIBE Roles;" 2>/dev/null | head -10

# 7. Sample data from Users table (if any)
echo -e "\n${BLUE}========================================${NC}"
echo -e "${BLUE}7. Sample Data${NC}"
echo -e "${BLUE}========================================${NC}"

USER_COUNT=$(run_query "SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -n 1)
if [ ! -z "$USER_COUNT" ] && [ "$USER_COUNT" != "0" ] && [ "$USER_COUNT" != "COUNT(*)" ]; then
    echo -e "${YELLOW}Users (first 5):${NC}"
    run_query "SELECT Id, Username, Email, FirstName, LastName, RoleId FROM Users LIMIT 5;" 2>/dev/null
else
    echo "Users table is empty (no users yet)"
fi

ROLE_COUNT=$(run_query "SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -n 1)
if [ ! -z "$ROLE_COUNT" ] && [ "$ROLE_COUNT" != "0" ] && [ "$ROLE_COUNT" != "COUNT(*)" ]; then
    echo -e "\n${YELLOW}Roles:${NC}"
    run_query "SELECT * FROM Roles;" 2>/dev/null
else
    echo "Roles table is empty (no roles yet)"
fi

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Query Complete!${NC}"
echo -e "${GREEN}========================================${NC}"

echo -e "\n${YELLOW}To run custom queries, use:${NC}"
echo "ssh $DROPLET_USER@$DROPLET_IP 'mysql -u $DB_USER -p[PASSWORD] $DB_NAME -e \"YOUR_QUERY\"'"
echo ""
echo "Or connect interactively:"
echo "ssh $DROPLET_USER@$DROPLET_IP"
echo "mysql -u $DB_USER -p $DB_NAME"

