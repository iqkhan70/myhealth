#!/usr/bin/env python3
"""
Encrypt MobilePhone data using AES-256 encryption
This script replicates the C# PiiEncryptionService logic for encrypting phone numbers.
"""

import sys
import json
import base64
import hashlib
import pymysql
from Cryptodome.Cipher import AES
from Cryptodome.Util.Padding import pad, unpad

def get_encryption_key_and_iv(config_file):
    """Get encryption key and IV from appsettings.Production.json
    Matches the C# PiiEncryptionService logic exactly"""
    try:
        with open(config_file, 'r') as f:
            config = json.load(f)
            
        # Try PiiEncryption:Key first
        encryption_key = config.get('PiiEncryption', {}).get('Key', '')
        if not encryption_key:
            # Fallback to Encryption:Key
            encryption_key = config.get('Encryption', {}).get('Key', '')
        
        if not encryption_key:
            # Use default fallback (same as C#)
            encryption_key = "DefaultEncryptionKey32BytesLong!!"
        
        # Derive key and IV exactly like C# code
        # Key: SHA256 hash of encryption key (32 bytes)
        key_hash = hashlib.sha256(encryption_key.encode('utf-8')).digest()
        
        # IV: First 16 bytes of SHA256 hash of (encryption_key + "IV")
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
        
        # Parse connection string
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

def encrypt_phone(phone, key, iv):
    """Encrypt phone number using AES-256-CBC with consistent IV (matches C# logic)"""
    try:
        # Create cipher with consistent IV (derived from key, not random)
        cipher = AES.new(key, AES.MODE_CBC, iv)
        
        # Pad and encrypt (PKCS7 padding)
        padded_data = pad(phone.encode('utf-8'), AES.block_size)
        ciphertext = cipher.encrypt(padded_data)
        
        # Base64 encode (C# doesn't include IV in output, it's derived)
        encrypted = base64.b64encode(ciphertext).decode('utf-8')
        
        return encrypted
    except Exception as e:
        print(f"ERROR: Encryption failed: {e}", file=sys.stderr)
        return None

def is_encrypted(phone_encrypted):
    """Check if phone number is already encrypted"""
    if not phone_encrypted:
        return False
    
    try:
        # Try to decode base64
        decoded = base64.b64decode(phone_encrypted)
        # Encrypted data should be at least 16 bytes (IV) + some ciphertext
        if len(decoded) < 16:
            return False
        # If it decodes successfully and is long enough, assume it's encrypted
        # Plain text phone numbers are typically 10-20 characters
        if len(phone_encrypted) > 30 and phone_encrypted.count('=') > 0:
            return True
        return False
    except:
        # If base64 decode fails, it's likely plain text
        return False

def normalize_phone(phone):
    """Normalize phone number for comparison"""
    if not phone:
        return ""
    # Remove all non-digit characters except leading +
    normalized = ""
    if phone.startswith('+'):
        normalized = '+'
    normalized += ''.join(c for c in phone if c.isdigit())
    return normalized

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 encrypt-mobilephone-python.py <path-to-appsettings.Production.json>")
        sys.exit(1)
    
    config_file = sys.argv[1]
    
    print("=" * 50)
    print("MobilePhone Encryption Script")
    print("=" * 50)
    print()
    
    # Get encryption key and IV
    print("Step 1: Loading encryption key and IV...")
    key, iv = get_encryption_key_and_iv(config_file)
    print(f"✅ Encryption key loaded (length: {len(key)} bytes)")
    print(f"✅ IV loaded (length: {len(iv)} bytes)")
    print()
    
    # Get database connection
    print("Step 2: Connecting to database...")
    db_config = get_db_connection(config_file)
    print(f"✅ Connecting to {db_config['host']}:{db_config['port']}/{db_config['database']}")
    
    try:
        conn = pymysql.connect(**db_config)
        cursor = conn.cursor()
        print("✅ Database connection established")
        print()
    except Exception as e:
        print(f"❌ Database connection failed: {e}", file=sys.stderr)
        sys.exit(1)
    
    # Encrypt Users table
    print("Step 3: Encrypting Users table...")
    try:
        cursor.execute("""
            SELECT Id, MobilePhoneEncrypted 
            FROM Users 
            WHERE MobilePhoneEncrypted IS NOT NULL 
            AND MobilePhoneEncrypted != ''
        """)
        users = cursor.fetchall()
        
        encrypted_count = 0
        skipped_count = 0
        error_count = 0
        
        for user_id, phone_encrypted in users:
            try:
                # Check if already encrypted
                if is_encrypted(phone_encrypted):
                    skipped_count += 1
                    continue
                
                # Encrypt the plain text phone
                encrypted = encrypt_phone(phone_encrypted, key, iv)
                if encrypted:
                    cursor.execute("""
                        UPDATE Users 
                        SET MobilePhoneEncrypted = %s 
                        WHERE Id = %s
                    """, (encrypted, user_id))
                    encrypted_count += 1
                else:
                    error_count += 1
            except Exception as e:
                print(f"⚠️  Error processing user {user_id}: {e}", file=sys.stderr)
                error_count += 1
        
        conn.commit()
        print(f"✅ Users table: {encrypted_count} encrypted, {skipped_count} skipped, {error_count} errors")
        print()
    except Exception as e:
        print(f"❌ Error processing Users table: {e}", file=sys.stderr)
        conn.rollback()
    
    # Encrypt UserRequests table
    print("Step 4: Encrypting UserRequests table...")
    try:
        cursor.execute("""
            SELECT Id, MobilePhoneEncrypted 
            FROM UserRequests 
            WHERE MobilePhoneEncrypted IS NOT NULL 
            AND MobilePhoneEncrypted != ''
        """)
        requests = cursor.fetchall()
        
        encrypted_count = 0
        skipped_count = 0
        error_count = 0
        
        for request_id, phone_encrypted in requests:
            try:
                # Check if already encrypted
                if is_encrypted(phone_encrypted):
                    skipped_count += 1
                    continue
                
                # Encrypt the plain text phone
                encrypted = encrypt_phone(phone_encrypted, key, iv)
                if encrypted:
                    cursor.execute("""
                        UPDATE UserRequests 
                        SET MobilePhoneEncrypted = %s 
                        WHERE Id = %s
                    """, (encrypted, request_id))
                    encrypted_count += 1
                else:
                    error_count += 1
            except Exception as e:
                print(f"⚠️  Error processing user request {request_id}: {e}", file=sys.stderr)
                error_count += 1
        
        conn.commit()
        print(f"✅ UserRequests table: {encrypted_count} encrypted, {skipped_count} skipped, {error_count} errors")
        print()
    except Exception as e:
        print(f"❌ Error processing UserRequests table: {e}", file=sys.stderr)
        conn.rollback()
    
    cursor.close()
    conn.close()
    
    print("=" * 50)
    print("✅ Encryption completed!")
    print("=" * 50)

if __name__ == "__main__":
    main()

