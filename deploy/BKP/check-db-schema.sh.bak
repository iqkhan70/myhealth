#!/bin/bash

# Check if database schema matches what code expects

DROPLET_IP="159.65.242.79"
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

