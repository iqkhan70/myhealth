#!/bin/bash
# Quick script to check if customerhealthdb exists on the server

# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
DROPLET_USER="root"
DB_NAME="customerhealthdb"

echo "=========================================="
echo "Checking Database Status on Server"
echo "=========================================="
echo "Server: $DROPLET_IP"
echo "Database: $DB_NAME"
echo ""

# Check if database exists
echo "1. Checking if database '$DB_NAME' exists..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Get MySQL root password
    if [ -f /root/mysql_root_password.txt ]; then
        MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
    else
        MYSQL_ROOT_PASS="UthmanBasima70"
    fi
    
    # Check if database exists
    DB_EXISTS=$(mysql -u root -p$MYSQL_ROOT_PASS -e "SHOW DATABASES LIKE 'customerhealthdb';" 2>/dev/null | grep -c "customerhealthdb" || echo "0")
    
    if [ "$DB_EXISTS" = "1" ]; then
        echo "✅ Database 'customerhealthdb' EXISTS"
        echo ""
        echo "2. Checking tables in database..."
        TABLE_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = 'customerhealthdb' AND table_name != '__EFMigrationsHistory';" 2>/dev/null | tail -1)
        echo "   Tables found: $TABLE_COUNT"
        
        if [ "$TABLE_COUNT" = "0" ] || [ -z "$TABLE_COUNT" ]; then
            echo "   ⚠️  Database exists but has NO TABLES"
        else
            echo "   ✅ Database has $TABLE_COUNT tables"
            echo ""
            echo "3. Listing first 10 tables:"
            mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT table_name FROM information_schema.tables WHERE table_schema = 'customerhealthdb' AND table_name != '__EFMigrationsHistory' LIMIT 10;" 2>/dev/null
        fi
        
        echo ""
        echo "4. Checking for seed data..."
        ROLES_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 || echo "0")
        USERS_COUNT=$(mysql -u root -p$MYSQL_ROOT_PASS customerhealthdb -e "SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -1 || echo "0")
        echo "   Roles: $ROLES_COUNT"
        echo "   Users: $USERS_COUNT"
        
        if [ "$ROLES_COUNT" = "0" ] && [ "$USERS_COUNT" = "0" ]; then
            echo "   ⚠️  Database exists but has NO SEED DATA"
        fi
    else
        echo "❌ Database 'customerhealthdb' DOES NOT EXIST"
        echo ""
        echo "2. Listing all databases on server:"
        mysql -u root -p$MYSQL_ROOT_PASS -e "SHOW DATABASES;" 2>/dev/null | grep -v "information_schema\|performance_schema\|mysql\|sys"
    fi
    
    echo ""
    echo "5. Checking MySQL user 'mentalhealth_user':"
    USER_EXISTS=$(mysql -u root -p$MYSQL_ROOT_PASS -e "SELECT User FROM mysql.user WHERE User='mentalhealth_user' AND Host='localhost';" 2>/dev/null | grep -c "mentalhealth_user" || echo "0")
    if [ "$USER_EXISTS" = "1" ]; then
        echo "   ✅ User 'mentalhealth_user' exists"
        echo "   Checking permissions:"
        mysql -u root -p$MYSQL_ROOT_PASS -e "SHOW GRANTS FOR 'mentalhealth_user'@'localhost';" 2>/dev/null | grep -i customerhealthdb || echo "   ⚠️  No grants found for customerhealthdb"
    else
        echo "   ❌ User 'mentalhealth_user' DOES NOT EXIST"
    fi
ENDSSH

echo ""
echo "=========================================="
echo "Check Complete!"
echo "=========================================="

