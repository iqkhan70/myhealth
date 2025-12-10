#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to add performance indexes for Users and UserAssignments tables
# This script is idempotent - safe to run multiple times

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SSH_KEY="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app/server"
PROJECT_DIR="/Users/mohammedkhan/iq/health"
DB_NAME="mentalhealthdb"
DB_USER="mentalhealth_user"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Add Performance Indexes${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if we're in the project root
if [ ! -f "SM_MentalHealthApp.Server/Scripts/AddPerformanceIndexes.sql" ]; then
    echo -e "${RED}ERROR: Please run this script from the project root directory${NC}"
    exit 1
fi

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY" 2>/dev/null || stat -c "%a" "$SSH_KEY" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY"
    fi
fi

echo -e "\n${BLUE}Step 1: Copying SQL script to server...${NC}"

# Copy SQL script to server
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    "SM_MentalHealthApp.Server/Scripts/AddPerformanceIndexes.sql" \
    root@$DROPLET_IP:$APP_DIR/AddPerformanceIndexes.sql

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to copy SQL script${NC}"
    exit 1
fi

echo -e "${GREEN}✅ SQL script copied${NC}"

echo -e "\n${BLUE}Step 2: Reading MySQL connection string from appsettings.Production.json...${NC}"

# Get database password from server
DB_PASSWORD=$(ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -A 1 '\"MySQL\"' $APP_DIR/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

if [ -z "$DB_PASSWORD" ]; then
    echo -e "${RED}ERROR: Could not retrieve database password from server${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Database credentials retrieved${NC}"

echo -e "\n${BLUE}Step 3: Running SQL script on server...${NC}"

# Run SQL script on server
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    mysql -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" < "$APP_DIR/AddPerformanceIndexes.sql"
ENDSSH

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Performance indexes added successfully!${NC}"
else
    echo -e "${RED}❌ Failed to add performance indexes${NC}"
    exit 1
fi

echo -e "\n${BLUE}Step 4: Verifying indexes...${NC}"

# Verify indexes were created
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$DROPLET_IP << ENDSSH
    mysql -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -e "
    SELECT 
        table_name,
        index_name,
        GROUP_CONCAT(column_name ORDER BY seq_in_index) AS columns
    FROM information_schema.statistics
    WHERE table_schema = '$DB_NAME'
    AND table_name IN ('Users', 'UserAssignments')
    AND index_name LIKE 'IX_%'
    GROUP BY table_name, index_name
    ORDER BY table_name, index_name;
    "
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Performance Indexes Migration Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "The following indexes have been added:"
echo "  - Users: RoleId, IsActive, RoleId+IsActive (composite), FirstName, LastName"
echo "  - UserAssignments: AssignerId, AssigneeId, IsActive, AssignerId+IsActive (composite), AssigneeId+IsActive (composite)"
echo ""
echo "These indexes will significantly improve query performance on large datasets."

