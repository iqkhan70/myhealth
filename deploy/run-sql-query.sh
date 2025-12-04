#!/bin/bash

# Script to run SQL queries against the server database

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

# Get DB password from server
DB_PASS=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "grep -A 1 '\"MySQL\"' $APP_DIR/server/appsettings.Production.json | grep -o 'password=[^;}\"]*' | cut -d'=' -f2 | tr -d '\"' | tr -d '}'")

if [ -z "$1" ]; then
    echo "Usage: $0 \"SQL_QUERY\""
    echo ""
    echo "Examples:"
    echo "  $0 \"SELECT * FROM JournalEntries;\""
    echo "  $0 \"DESCRIBE JournalEntries;\""
    echo "  $0 \"SHOW TABLES;\""
    exit 1
fi

SQL_QUERY="$1"

echo "🔍 Running query on server database..."
echo "Query: $SQL_QUERY"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP \
    "mysql -u mentalhealth_user -p$DB_PASS mentalhealthdb -e \"$SQL_QUERY\""

