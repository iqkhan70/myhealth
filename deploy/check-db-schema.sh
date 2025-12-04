#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Check if database schema matches what code expects

APP_DIR="/opt/mental-health-app"

echo "🔍 Checking database schema on server..."
echo ""

# Get DB password
DB_PASS=$(ssh root@$DROPLET_IP "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

echo "📋 Checking JournalEntries table structure:"
ssh root@$DROPLET_IP "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'DESCRIBE JournalEntries;' 2>&1"

echo ""
echo "📋 Checking if IsActive column exists:"
ssh root@$DROPLET_IP "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'SHOW COLUMNS FROM JournalEntries LIKE \"IsActive\";' 2>&1"

echo ""
echo "📋 Checking applied migrations:"
ssh root@$DROPLET_IP "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e 'SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;' 2>&1"

