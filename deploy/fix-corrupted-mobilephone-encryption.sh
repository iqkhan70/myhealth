#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to fix corrupted MobilePhone encryption on DigitalOcean
# This script identifies phone numbers that can't be decrypted with the current key
# and sets them to NULL so they can be re-entered through the UI

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

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Fix Corrupted MobilePhone Encryption${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}⚠️  This script will:${NC}"
echo -e "${YELLOW}   1. Identify phone numbers that can't be decrypted with current key${NC}"
echo -e "${YELLOW}   2. Set those corrupted values to NULL${NC}"
echo -e "${YELLOW}   3. Users will need to re-enter phone numbers through the UI${NC}"
echo ""
read -p "Do you want to continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo -e "${YELLOW}Operation cancelled.${NC}"
    exit 0
fi

echo ""
echo -e "${BLUE}Step 1: Copying Python fix script to server...${NC}"

# Copy Python fix script
cat > /tmp/fix-mobilephone-encryption.py << 'PYTHON_SCRIPT'
#!/usr/bin/env python3
"""
Fix corrupted MobilePhone encryption by identifying values that can't be decrypted
and setting them to NULL so they can be re-entered through the UI.
"""

import sys
import json
import base64
import hashlib
import pymysql
from Cryptodome.Cipher import AES
from Cryptodome.Util.Padding import pad, unpad

def get_encryption_key_and_iv(config_file):
    """Get encryption key and IV from appsettings.Production.json"""
    try:
        with open(config_file, 'r') as f:
            config = json.load(f)
            
        encryption_key = config.get('PiiEncryption', {}).get('Key', '')
        if not encryption_key:
            encryption_key = config.get('Encryption', {}).get('Key', '')
        
        if not encryption_key:
            encryption_key = "DefaultEncryptionKey32BytesLong!!"
        
        key_hash = hashlib.sha256(encryption_key.encode('utf-8')).digest()
        iv_hash = hashlib.sha256((encryption_key + "IV").encode('utf-8')).digest()
        iv = iv_hash[:16]
        
        return key_hash, iv
    except Exception as e:
        print(f"ERROR: Failed to read config file: {e}", file=sys.stderr)
        sys.exit(1)

def get_db_connection(config_file):
    """Get database connection from appsettings.Production.json"""
    try:
        with open(config_file, 'r') as f:
            config = json.load(f)
        
        connection_strings = config.get('ConnectionStrings', {})
        mysql_conn = connection_strings.get('MySQL', '')
        
        parts = {}
        for part in mysql_conn.split(';'):
            if '=' in part:
                key, value = part.split('=', 1)
                parts[key.strip().lower()] = value.strip()
        
        return {
            'host': parts.get('server', 'localhost'),
            'port': int(parts.get('port', 3306)),
            'user': parts.get('user', 'mentalhealth_user'),
            'password': parts.get('password', ''),
            'database': parts.get('database', 'mentalhealthdb')
        }
    except Exception as e:
        print(f"ERROR: Failed to parse connection string: {e}", file=sys.stderr)
        sys.exit(1)

def decrypt_phone(phone_encrypted, key, iv):
    """Try to decrypt phone number"""
    if not phone_encrypted:
        return None
    
    try:
        # Check if it looks like base64
        if '=' not in phone_encrypted or len(phone_encrypted) < 20:
            return None
        
        ct = base64.b64decode(phone_encrypted)
        cipher = AES.new(key, AES.MODE_CBC, iv)
        pt = unpad(cipher.decrypt(ct), AES.block_size)
        decrypted = pt.decode('utf-8')
        
        # Check if decryption actually worked
        if decrypted == phone_encrypted or not decrypted.strip():
            return None
        
        # Check if it looks like a phone number
        if len(decrypted) >= 10 and any(c.isdigit() for c in decrypted):
            return decrypted
        
        return None
    except Exception:
        return None

def is_likely_phone(phone):
    """Check if string looks like a phone number"""
    if not phone:
        return False
    # Phone numbers should have at least 10 digits
    digits = sum(1 for c in phone if c.isdigit())
    return digits >= 10

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 fix-mobilephone-encryption.py <path-to-appsettings.Production.json>")
        sys.exit(1)
    
    config_file = sys.argv[1]
    
    print("=" * 50)
    print("Fix Corrupted MobilePhone Encryption")
    print("=" * 50)
    print()
    
    # Get encryption key and IV
    print("Step 1: Loading encryption key and IV...")
    key, iv = get_encryption_key_and_iv(config_file)
    print(f"✅ Encryption key loaded")
    print()
    
    # Get database connection
    print("Step 2: Connecting to database...")
    db_config = get_db_connection(config_file)
    
    try:
        conn = pymysql.connect(**db_config)
        cursor = conn.cursor()
        print("✅ Database connection established")
        print()
    except Exception as e:
        print(f"❌ Database connection failed: {e}", file=sys.stderr)
        sys.exit(1)
    
    # Fix Users table
    print("Step 3: Checking Users table...")
    try:
        cursor.execute("""
            SELECT Id, MobilePhoneEncrypted 
            FROM Users 
            WHERE MobilePhoneEncrypted IS NOT NULL 
            AND MobilePhoneEncrypted != ''
        """)
        users = cursor.fetchall()
        
        fixed_count = 0
        ok_count = 0
        
        for user_id, phone_encrypted in users:
            try:
                # Try to decrypt
                decrypted = decrypt_phone(phone_encrypted, key, iv)
                
                if decrypted is None:
                    # Can't decrypt - might be corrupted or encrypted with wrong key
                    # Check if it looks like plain text
                    if is_likely_phone(phone_encrypted):
                        # It's plain text, encrypt it properly
                        cipher = AES.new(key, AES.MODE_CBC, iv)
                        padded_data = pad(phone_encrypted.encode('utf-8'), AES.block_size)
                        ciphertext = cipher.encrypt(padded_data)
                        encrypted = base64.b64encode(ciphertext).decode('utf-8')
                        cursor.execute("""
                            UPDATE Users 
                            SET MobilePhoneEncrypted = %s 
                            WHERE Id = %s
                        """, (encrypted, user_id))
                        fixed_count += 1
                        print(f"  ✅ User {user_id}: Re-encrypted plain text phone")
                    else:
                        # Can't decrypt and doesn't look like phone - set to NULL
                        cursor.execute("""
                            UPDATE Users 
                            SET MobilePhoneEncrypted = NULL 
                            WHERE Id = %s
                        """, (user_id,))
                        fixed_count += 1
                        print(f"  ⚠️  User {user_id}: Set to NULL (corrupted encryption)")
                else:
                    ok_count += 1
            except Exception as e:
                print(f"  ⚠️  Error processing user {user_id}: {e}", file=sys.stderr)
        
        conn.commit()
        print(f"✅ Users table: {ok_count} OK, {fixed_count} fixed/set to NULL")
        print()
    except Exception as e:
        print(f"❌ Error processing Users table: {e}", file=sys.stderr)
        conn.rollback()
    
    # Fix UserRequests table
    print("Step 4: Checking UserRequests table...")
    try:
        cursor.execute("""
            SELECT Id, MobilePhoneEncrypted 
            FROM UserRequests 
            WHERE MobilePhoneEncrypted IS NOT NULL 
            AND MobilePhoneEncrypted != ''
        """)
        requests = cursor.fetchall()
        
        fixed_count = 0
        ok_count = 0
        
        for request_id, phone_encrypted in requests:
            try:
                # Try to decrypt
                decrypted = decrypt_phone(phone_encrypted, key, iv)
                
                if decrypted is None:
                    # Can't decrypt - might be corrupted or encrypted with wrong key
                    # Check if it looks like plain text
                    if is_likely_phone(phone_encrypted):
                        # It's plain text, encrypt it properly
                        cipher = AES.new(key, AES.MODE_CBC, iv)
                        padded_data = pad(phone_encrypted.encode('utf-8'), AES.block_size)
                        ciphertext = cipher.encrypt(padded_data)
                        encrypted = base64.b64encode(ciphertext).decode('utf-8')
                        cursor.execute("""
                            UPDATE UserRequests 
                            SET MobilePhoneEncrypted = %s 
                            WHERE Id = %s
                        """, (encrypted, request_id))
                        fixed_count += 1
                        print(f"  ✅ Request {request_id}: Re-encrypted plain text phone")
                    else:
                        # Can't decrypt and doesn't look like phone - set to NULL
                        cursor.execute("""
                            UPDATE UserRequests 
                            SET MobilePhoneEncrypted = NULL 
                            WHERE Id = %s
                        """, (request_id,))
                        fixed_count += 1
                        print(f"  ⚠️  Request {request_id}: Set to NULL (corrupted encryption)")
                else:
                    ok_count += 1
            except Exception as e:
                print(f"  ⚠️  Error processing request {request_id}: {e}", file=sys.stderr)
        
        conn.commit()
        print(f"✅ UserRequests table: {ok_count} OK, {fixed_count} fixed/set to NULL")
        print()
    except Exception as e:
        print(f"❌ Error processing UserRequests table: {e}", file=sys.stderr)
        conn.rollback()
    
    cursor.close()
    conn.close()
    
    print("=" * 50)
    print("✅ Fix completed!")
    print("=" * 50)
    print()
    print("Note: Phone numbers set to NULL need to be re-entered through the UI.")

if __name__ == "__main__":
    main()
PYTHON_SCRIPT

scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    /tmp/fix-mobilephone-encryption.py \
    root@$SERVER_IP:/tmp/fix-mobilephone-encryption.py

echo ""
echo -e "${BLUE}Step 2: Running fix script on server...${NC}"

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << ENDSSH
    APP_DIR="$APP_DIR"
    
    # Install dependencies if needed
    if ! python3 -c "import pymysql" 2>/dev/null; then
        echo "Installing pymysql..."
        pip3 install pymysql --quiet || {
            echo "❌ Failed to install pymysql"
            exit 1
        }
    fi
    
    if ! python3 -c "from Cryptodome.Cipher import AES" 2>/dev/null; then
        echo "Installing pycryptodome..."
        pip3 install pycryptodome --quiet || {
            echo "❌ Failed to install pycryptodome"
            exit 1
        }
    fi
    
    # Run the fix script
    python3 /tmp/fix-mobilephone-encryption.py "\$APP_DIR/appsettings.Production.json" 2>&1 | tee /tmp/fix-mobilephone-encryption.log
    EXIT_CODE=\${PIPESTATUS[0]}
    
    if [ \$EXIT_CODE -eq 0 ]; then
        echo ""
        echo "✅ Fix completed successfully!"
    else
        echo ""
        echo "❌ Fix failed with exit code \$EXIT_CODE"
        echo "Check /tmp/fix-mobilephone-encryption.log for details"
        exit 1
    fi
ENDSSH

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✅ Fix Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo -e "${YELLOW}Next steps:${NC}"
    echo -e "1. Phone numbers that couldn't be decrypted have been set to NULL"
    echo -e "2. Users need to re-enter their phone numbers through the UI"
    echo -e "3. New phone numbers will be encrypted correctly with the current key"
    echo ""
    echo -e "Check the logs: ${BLUE}/tmp/fix-mobilephone-encryption.log${NC}"
else
    echo ""
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}❌ Fix Failed!${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi

