#!/bin/bash
# ============================================================================
# Complete Deployment Script
# This script completes the deployment by running remaining migrations and seeding
# ============================================================================

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
ENVIRONMENT="${1:-staging}"  # staging | production
DEPLOYMENT_MODE="local-db"   # local-db | managed-db
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Load droplet IP
source "$SCRIPT_DIR/load-droplet-ip.sh" "$ENVIRONMENT"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Completing Deployment${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Configuration:${NC}"
echo -e "  Environment: ${YELLOW}$ENVIRONMENT${NC}"
echo -e "  Droplet IP: ${YELLOW}$DROPLET_IP${NC}"
echo -e "  Deployment Mode: ${YELLOW}$DEPLOYMENT_MODE${NC}"
echo ""

# Read database configuration from .env on droplet
echo "Reading database configuration from .env file..."
DB_NAME=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "grep '^MYSQL_DB=' /opt/mental-health-app/.env | cut -d'=' -f2 | tr -d '\"' | tr -d \"'\"" || echo "customerhealthdb")
DB_ROOT_PASSWORD=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "grep '^MYSQL_ROOT_PASSWORD=' /opt/mental-health-app/.env | cut -d'=' -f2 | tr -d '\"' | tr -d \"'\"" || echo "")

echo "Database: $DB_NAME"
echo ""

# Function to run a migration step
run_migration_step() {
    local step_num=$1
    local step_name=$2
    local script_path=$3
    
    echo -e "\n${GREEN}========================================${NC}"
    echo -e "${GREEN}Step $step_num: $step_name${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    
    if [ ! -f "$REPO_ROOT/$script_path" ]; then
        echo -e "${YELLOW}⚠️  Migration script not found: $script_path${NC}"
        return 0
    fi
    
    local script_name=$(basename "$script_path")
    echo "Copying migration script..."
    scp -i "$SSH_KEY_PATH" "$REPO_ROOT/$script_path" "$DROPLET_USER@$DROPLET_IP:/tmp/${script_name}"
    
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "DB_NAME=$DB_NAME DEPLOYMENT_MODE=$DEPLOYMENT_MODE SCRIPT_NAME=$script_name bash -s" << 'ENDSSH'
        set -e
        
        # Read MySQL root password from .env file
        if [ -f /opt/mental-health-app/.env ]; then
            DB_ROOT_PASSWORD=$(grep "^MYSQL_ROOT_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2 | tr -d '"' | tr -d "'")
        else
            echo "❌ ERROR: .env file not found"
            exit 1
        fi
        
        if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
            echo "Applying migration SQL..."
            
            # Determine which password to use for MySQL connection
            MYSQL_PWD_TO_USE=""
            if docker exec mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
                echo "Using no-password connection"
                MYSQL_PWD_TO_USE=""
            elif docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
                echo "Using .env password"
                MYSQL_PWD_TO_USE="$DB_ROOT_PASSWORD"
            else
                echo "Using fallback password"
                MYSQL_PWD_TO_USE="UthmanBasima70"
            fi
            
            # Apply migration - first try to replace hardcoded database name if present
            # Some migration scripts have "USE customerhealthdb;" which needs to be replaced
            SCRIPT_PATH="/tmp/$SCRIPT_NAME"
            sed -i "s/USE \`customerhealthdb\`;/USE \`$DB_NAME\`;/g" "$SCRIPT_PATH"
            sed -i "s/USE customerhealthdb;/USE \`$DB_NAME\`;/g" "$SCRIPT_PATH"
            sed -i "s/TABLE_SCHEMA = 'customerhealthdb'/TABLE_SCHEMA = '$DB_NAME'/g" "$SCRIPT_PATH"
            sed -i "s/TABLE_SCHEMA = \"customerhealthdb\"/TABLE_SCHEMA = \"$DB_NAME\"/g" "$SCRIPT_PATH"
            
            # Apply migration
            if [ -z "$MYSQL_PWD_TO_USE" ]; then
                ERROR_OUTPUT=$(docker exec -i mental-health-mysql mysql -uroot "$DB_NAME" < "$SCRIPT_PATH" 2>&1 || true)
                # Filter out warnings but keep errors
                ERROR_ONLY=$(echo "$ERROR_OUTPUT" | grep -i "error" || true)
                if [ -n "$ERROR_ONLY" ]; then
                    if echo "$ERROR_OUTPUT" | grep -qi "already exists\|duplicate\|already exist"; then
                        echo "✅ Migration completed (some items already exist - this is expected)"
                    else
                        echo "❌ Migration failed with errors:"
                        echo "$ERROR_OUTPUT"
                        exit 1
                    fi
                else
                    echo "✅ Migration applied successfully"
                fi
            else
                ERROR_OUTPUT=$(docker exec -i -e MYSQL_PWD="$MYSQL_PWD_TO_USE" mental-health-mysql mysql -uroot "$DB_NAME" < "$SCRIPT_PATH" 2>&1 || true)
                # Filter out warnings but keep errors
                ERROR_ONLY=$(echo "$ERROR_OUTPUT" | grep -i "error" || true)
                if [ -n "$ERROR_ONLY" ]; then
                    if echo "$ERROR_OUTPUT" | grep -qi "already exists\|duplicate\|already exist"; then
                        echo "✅ Migration completed (some items already exist - this is expected)"
                    else
                        echo "❌ Migration failed with errors:"
                        echo "$ERROR_OUTPUT"
                        exit 1
                    fi
                else
                    echo "✅ Migration applied successfully"
                fi
            fi
        else
            echo "⚠️  For managed DB, migration should be run manually"
        fi
        
        rm -f "$SCRIPT_PATH"
ENDSSH
    
    echo -e "${GREEN}✅ $step_name completed${NC}"
}

# Run remaining migrations (8.7 onwards - in case 8.7 didn't complete)
echo -e "${GREEN}Running remaining migrations...${NC}"

# Step 8.7: Assignment Lifecycle Migration (must run before 8.8)
# This creates IsBillable column which 8.8 depends on
run_migration_step "8.7" "Running Assignment Lifecycle Migration" "SM_MentalHealthApp.Server/Scripts/AddAssignmentLifecycleAndSmeScoring.sql"

# Step 8.8: Billing Status Migration (depends on 8.7 for IsBillable column)
run_migration_step "8.8" "Running Billing Status Migration" "SM_MentalHealthApp.Server/Scripts/AddBillingStatusAndInvoicing.sql"

# Step 8.9: Service Request Charges Migration
run_migration_step "8.9" "Running Service Request Charges Migration" "SM_MentalHealthApp.Server/Scripts/AddServiceRequestChargesAndCompanyBilling.sql"

# Step 8.10: SME Role Migration
run_migration_step "8.10" "Running SME Role Migration" "SM_MentalHealthApp.Server/Scripts/AddSmeRole.sql"

# Step 8.11: Companies Table Migration
run_migration_step "8.11" "Running Companies Table Migration" "SM_MentalHealthApp.Server/Scripts/AddCompaniesTable.sql"

# Step 8.12: Billing Accounts Migration
run_migration_step "8.12" "Running Billing Accounts Migration" "SM_MentalHealthApp.Server/Scripts/AddBillingAccountsAndRates.sql"

# Step 8.13: Password Reset Fields Migration
run_migration_step "8.13" "Running Password Reset Fields Migration" "SM_MentalHealthApp.Server/Scripts/AddPasswordResetFields.sql"

# Step 8.14: Client Profile System Migration
run_migration_step "8.14" "Running Client Profile System Migration" "SM_MentalHealthApp.Server/Scripts/AddClientProfileSystem.sql"

# Step 8.15: Client Agent Session Migration
run_migration_step "8.15" "Running Client Agent Session Migration" "SM_MentalHealthApp.Server/Scripts/AddClientAgentSession.sql"

# Step 8.16: Appointment-ServiceRequest Junction Migration
run_migration_step "8.16" "Running Appointment-ServiceRequest Junction Migration" "SM_MentalHealthApp.Server/Scripts/AddAppointmentServiceRequestsJunction.sql"

# Step 9: Seed Database
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 9: Seeding Database${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

if [ -f "$SCRIPT_DIR/Scripts121925/SeedingInitialConsolidatedScript.sh" ]; then
    echo "Copying seeding script..."
    scp -i "$SSH_KEY_PATH" "$SCRIPT_DIR/Scripts121925/SeedingInitialConsolidatedScript.sh" "$DROPLET_USER@$DROPLET_IP:/tmp/seed.sql"
    
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "DB_NAME=$DB_NAME DEPLOYMENT_MODE=$DEPLOYMENT_MODE bash -s" << 'ENDSSH'
        set -e
        
        # Determine which password to use
        SEED_PWD=""
        if docker exec mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
            echo "Using no-password connection for seeding"
            SEED_PWD=""
        elif [ -f /opt/mental-health-app/.env ]; then
            DB_ROOT_PASSWORD=$(grep "^MYSQL_ROOT_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2 | tr -d '"' | tr -d "'")
            if docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
                echo "Using .env password for seeding"
                SEED_PWD="$DB_ROOT_PASSWORD"
            else
                echo "Using fallback password for seeding"
                SEED_PWD="UthmanBasima70"
            fi
        else
            echo "Using fallback password for seeding"
            SEED_PWD="UthmanBasima70"
        fi
        
        if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
            echo "Applying seeding SQL statements..."
            
            # Apply using Python script
            python3 << APPLY_SEED_SQL
import subprocess
import sys
import os

# Determine MySQL connection command
if '$SEED_PWD':
    MYSQL_CMD = ['docker', 'exec', '-i', '-e', 'MYSQL_PWD=$SEED_PWD', 'mental-health-mysql', 'mysql', '-uroot', '$DB_NAME']
else:
    MYSQL_CMD = ['docker', 'exec', '-i', 'mental-health-mysql', 'mysql', '-uroot', '$DB_NAME']

ERROR_COUNT = 0
STATEMENT_COUNT = 0

try:
    with open('/tmp/seed.sql', 'r') as f:
        content = f.read()
    
    # Split by semicolon, preserving multi-line statements
    statements = []
    current = []
    
    for line in content.split('\n'):
        stripped = line.strip()
        if not stripped or stripped.startswith('--'):
            if current:
                current.append('')
            continue
        
        current.append(line)
        
        if stripped.endswith(';'):
            stmt = '\n'.join(current)
            if stmt.strip():
                statements.append(stmt)
            current = []
    
    if current:
        stmt = '\n'.join(current)
        if stmt.strip():
            statements.append(stmt)
    
    print(f"Found {len(statements)} SQL statements to apply")
    
    for i, stmt in enumerate(statements, 1):
        STATEMENT_COUNT = i
        proc = subprocess.Popen(
            MYSQL_CMD,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        stdout, stderr = proc.communicate(input=stmt + '\n')
        
        if proc.returncode != 0:
            ERROR_COUNT += 1
            error_msg = stderr.strip()
            if 'ERROR' in error_msg.upper():
                if 'duplicate' not in error_msg.lower():
                    print(f"⚠️  Error in statement {i}: {error_msg[:300]}")
        
        if i % 20 == 0:
            print(f"Applied {i}/{len(statements)} statements...")
    
    if ERROR_COUNT > 0:
        print(f"⚠️  Seeding had {ERROR_COUNT} errors out of {STATEMENT_COUNT} statements")
    else:
        print(f"✅ Successfully applied {STATEMENT_COUNT} statements")
        
except Exception as e:
    print(f"ERROR: {str(e)}", file=sys.stderr)
    import traceback
    traceback.print_exc(file=sys.stderr)
    sys.exit(1)
APPLY_SEED_SQL
            
            if [ $? -eq 0 ]; then
                echo "✅ Seeding SQL applied successfully"
                
                # Verify data was seeded
                echo "Verifying seeded data..."
                if [ -z "$SEED_PWD" ]; then
                    ROLES_COUNT=$(docker exec mental-health-mysql mysql -uroot -e "USE $DB_NAME; SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 | tr -d ' ' || echo "0")
                    USERS_COUNT=$(docker exec mental-health-mysql mysql -uroot -e "USE $DB_NAME; SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -1 | tr -d ' ' || echo "0")
                else
                    ROLES_COUNT=$(docker exec -e MYSQL_PWD="$SEED_PWD" mental-health-mysql mysql -uroot -e "USE $DB_NAME; SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 | tr -d ' ' || echo "0")
                    USERS_COUNT=$(docker exec -e MYSQL_PWD="$SEED_PWD" mental-health-mysql mysql -uroot -e "USE $DB_NAME; SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -1 | tr -d ' ' || echo "0")
                fi
                
                echo "Roles count: $ROLES_COUNT"
                echo "Users count: $USERS_COUNT"
                
                if [ "$ROLES_COUNT" -gt 0 ] 2>/dev/null && [ "$USERS_COUNT" -gt 0 ] 2>/dev/null; then
                    echo "✅ Database seeded successfully (Roles: $ROLES_COUNT, Users: $USERS_COUNT)"
                else
                    echo "⚠️  Warning: Seeding completed but verification shows (Roles: $ROLES_COUNT, Users: $USERS_COUNT)"
                    echo "Data may have been seeded but counts are low, or verification query failed"
                fi
            else
                echo "❌ Seeding application failed"
                exit 1
            fi
        else
            echo "⚠️  For managed DB, seeding should be run from API container or manually"
        fi
ENDSSH
    
    echo -e "${GREEN}✅ Database seeding completed${NC}"
else
    echo -e "${YELLOW}⚠️  Seeding script not found${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Completion Finished${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "You can now verify the database by checking:"
echo "  - Users table should have at least 3 users (admin, doctor, patient)"
echo "  - Roles table should have 6 roles"
echo ""

