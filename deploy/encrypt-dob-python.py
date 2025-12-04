#!/usr/bin/env python3
"""
Script to encrypt existing plain text DateOfBirth data in UserRequests and Users tables.
This implements the same AES-256 encryption logic as PiiEncryptionService.cs
"""

import json
import sys
import hashlib
from datetime import datetime
import base64

# Try different import paths for Crypto
try:
    from Crypto.Cipher import AES
    from Crypto.Util.Padding import pad, unpad
except ImportError:
    try:
        from Cryptodome.Cipher import AES
        from Cryptodome.Util.Padding import pad, unpad
    except ImportError:
        print("❌ Error: pycryptodome not installed. Install with: apt-get install python3-pycryptodome")
        sys.exit(1)

try:
    import pymysql
except ImportError:
    print("❌ Error: pymysql not installed. Install with: apt-get install python3-pymysql")
    sys.exit(1)

def get_encryption_key(config_path):
    """Get encryption key from appsettings.Production.json"""
    with open(config_path, 'r') as f:
        config = json.load(f)
        key = config.get('PiiEncryption', {}).get('Key') or config.get('Encryption', {}).get('Key')
        if not key:
            key = "DefaultEncryptionKey32BytesLong!!"  # Default fallback
        return key

def derive_key_and_iv(encryption_key):
    """Derive AES key and IV from encryption key (same logic as C#)"""
    # Derive 32-byte key using SHA256
    key_hash = hashlib.sha256(encryption_key.encode('utf-8')).digest()
    
    # Derive 16-byte IV from key + "IV"
    iv_hash = hashlib.sha256((encryption_key + "IV").encode('utf-8')).digest()
    iv = iv_hash[:16]
    
    return key_hash, iv

def encrypt_datetime(date_time_str, key, iv):
    """Encrypt a datetime string using AES-256-CBC"""
    try:
        # Parse the date string
        if isinstance(date_time_str, str):
            # Handle ISO format: "2005-02-03T00:00:00.000000"
            if 'T' in date_time_str:
                date_part = date_time_str.split('T')[0]
            else:
                date_part = date_time_str.split(' ')[0]
            
            # Format as "yyyy-MM-dd" for storage
            dt = datetime.strptime(date_part, '%Y-%m-%d')
            plain_text = dt.strftime('%Y-%m-%d')
        else:
            plain_text = str(date_time_str)
        
        # Encrypt using AES-256-CBC
        cipher = AES.new(key, AES.MODE_CBC, iv)
        padded_data = pad(plain_text.encode('utf-8'), AES.block_size)
        encrypted = cipher.encrypt(padded_data)
        
        # Return base64 encoded
        return base64.b64encode(encrypted).decode('utf-8')
    except Exception as e:
        print(f"Error encrypting {date_time_str}: {e}")
        return None

def main():
    config_path = '/opt/mental-health-app/server/appsettings.Production.json'
    
    # Get encryption key
    encryption_key = get_encryption_key(config_path)
    key, iv = derive_key_and_iv(encryption_key)
    
    # Get database connection string
    with open(config_path, 'r') as f:
        config = json.load(f)
        conn_str = config.get('ConnectionStrings', {}).get('MySQL', '')
    
    # Parse connection string
    conn_params = {}
    for part in conn_str.split(';'):
        if '=' in part:
            k, v = part.split('=', 1)
            conn_params[k.strip().lower()] = v.strip()
    
    # Connect to database
    try:
        conn = pymysql.connect(
            host=conn_params.get('server', 'localhost'),
            port=int(conn_params.get('port', 3306)),
            user=conn_params.get('user', 'mentalhealth_user'),
            password=conn_params.get('password', ''),
            database=conn_params.get('database', 'mentalhealthdb'),
            charset='utf8mb4'
        )
        
        cursor = conn.cursor()
        
        # Encrypt UserRequests
        print("Encrypting UserRequests DateOfBirth data...")
        cursor.execute("""
            SELECT Id, DateOfBirthEncrypted 
            FROM UserRequests 
            WHERE DateOfBirthEncrypted IS NOT NULL 
            AND DateOfBirthEncrypted != '' 
            AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000'
            AND DateOfBirthEncrypted LIKE '%-%-%T%:%:%'
        """)
        
        user_requests = cursor.fetchall()
        encrypted_requests = 0
        skipped_requests = 0
        
        for req_id, dob_encrypted in user_requests:
            try:
                # Check if it's plain text (ISO format)
                if 'T' in dob_encrypted or dob_encrypted.count('-') >= 2:
                    # Encrypt it
                    encrypted_value = encrypt_datetime(dob_encrypted, key, iv)
                    if encrypted_value:
                        cursor.execute("""
                            UPDATE UserRequests 
                            SET DateOfBirthEncrypted = %s 
                            WHERE Id = %s
                        """, (encrypted_value, req_id))
                        encrypted_requests += 1
                        print(f"  ✅ Encrypted UserRequest {req_id}")
                    else:
                        skipped_requests += 1
                else:
                    # Already encrypted (base64)
                    skipped_requests += 1
            except Exception as e:
                print(f"  ❌ Error encrypting UserRequest {req_id}: {e}")
                skipped_requests += 1
        
        # Encrypt Users
        print("\nEncrypting Users DateOfBirth data...")
        cursor.execute("""
            SELECT Id, DateOfBirthEncrypted 
            FROM Users 
            WHERE DateOfBirthEncrypted IS NOT NULL 
            AND DateOfBirthEncrypted != '' 
            AND DateOfBirthEncrypted != '0001-01-01T00:00:00.000000'
            AND DateOfBirthEncrypted LIKE '%-%-%T%:%:%'
        """)
        
        users = cursor.fetchall()
        encrypted_users = 0
        skipped_users = 0
        
        for user_id, dob_encrypted in users:
            try:
                # Check if it's plain text (ISO format)
                if 'T' in dob_encrypted or dob_encrypted.count('-') >= 2:
                    # Encrypt it
                    encrypted_value = encrypt_datetime(dob_encrypted, key, iv)
                    if encrypted_value:
                        cursor.execute("""
                            UPDATE Users 
                            SET DateOfBirthEncrypted = %s 
                            WHERE Id = %s
                        """, (encrypted_value, user_id))
                        encrypted_users += 1
                        print(f"  ✅ Encrypted User {user_id}")
                    else:
                        skipped_users += 1
                else:
                    # Already encrypted (base64)
                    skipped_users += 1
            except Exception as e:
                print(f"  ❌ Error encrypting User {user_id}: {e}")
                skipped_users += 1
        
        # Commit changes
        conn.commit()
        
        print(f"\n✅ Encryption complete!")
        print(f"   UserRequests: {encrypted_requests} encrypted, {skipped_requests} skipped")
        print(f"   Users: {encrypted_users} encrypted, {skipped_users} skipped")
        
        cursor.close()
        conn.close()
        
    except Exception as e:
        print(f"❌ Error: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()

