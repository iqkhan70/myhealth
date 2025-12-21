#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to create/update /opt/mental-health-app/server/appsettings.Staging.json on DigitalOcean server
# Preserves ConnectionStrings section if it exists, overwrites everything else

SSH_KEY="$HOME/.ssh/id_rsa"

echo "🔧 Creating/updating appsettings.Staging.json file on DigitalOcean server..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    APP_SETTINGS_FILE="/opt/mental-health-app/server/appsettings.Staging.json"
    APP_DIR="/opt/mental-health-app/server"

    echo "Creating/updating appsettings.Staging.json file on server..."

    # Create server directory if it doesn't exist
    if [ ! -d "$APP_DIR" ]; then
        echo "Creating server directory..."
        mkdir -p "$APP_DIR"
        chmod 755 "$APP_DIR"
    fi

    # New JSON content (without ConnectionStrings)
    NEW_JSON='{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "HuggingFace": {
    "ApiKey": "",
    "BioMistralModelUrl": "https://api-inference.huggingface.co/models/medalpaca/medalpaca-7b",
    "MeditronModelUrl": "https://api-inference.huggingface.co/models/epfl-llm/meditron-7b"
  },
  "OpenAI": {
    "ApiKey": "sk-your-actual-openai-api-key-here"
  },
  "S3": {
    "AccessKey": "DO00Z6VU8Q38KXLFZ7V4",
    "SecretKey": "ZDK61LfGdaqu5FpTcKnUfK8GNSW+cTSSbK8vK8GnMno",
    "BucketName": "mentalhealth-content",
    "Region": "sfo3",
    "ServiceUrl": "https://sfo3.digitaloceanspaces.com",
    "Folder": "content/"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "SM_MentalHealthApp",
    "Audience": "SM_MentalHealthApp_Users"
  },
  "Vonage": {
    "Enabled": true,
    "ApiKey": "c7dc2f50",
    "ApiSecret": "ZsknGg4RbD1fBI4B",
    "FromNumber": "+16148122119"
  },
  "Email": {
    "Enabled": false,
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@healthapp.com",
    "FromName": "Health App",
    "EnableSsl": true
  },
  "Agora": {
    "AppId": "efa11b3a7d05409ca979fb25a5b489ae",
    "UseTokens": true,
    "AppCertificate": "89ab54068fae46aeaf930ffd493e977b"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Kestrel": {
    "Limits": {
      "KeepAliveTimeout": 900,
      "RequestHeadersTimeout": 900
    }
  },
  "RequestTimeout": "00:15:00",
  "PiiEncryption": {
    "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
  },
  "Encryption": {
    "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
  }
}'

    # Check if file already exists - if so, don't overwrite it
    if [ -f "$APP_SETTINGS_FILE" ]; then
        echo "✅ appsettings.Staging.json already exists - preserving your custom settings"
        echo "   If you need to recreate it, delete the file first and run this script again"
        exit 0
    fi

    # Use Python to merge JSON, preserving ConnectionStrings if it exists
    python3 << 'PYTHON'
import json
import sys
import os

app_settings_file = "/opt/mental-health-app/server/appsettings.Staging.json"
new_json_str = """{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "HuggingFace": {
    "ApiKey": "",
    "BioMistralModelUrl": "https://api-inference.huggingface.co/models/medalpaca/medalpaca-7b",
    "MeditronModelUrl": "https://api-inference.huggingface.co/models/epfl-llm/meditron-7b"
  },
  "OpenAI": {
    "ApiKey": "sk-your-actual-openai-api-key-here"
  },
  "S3": {
    "AccessKey": "DO00Z6VU8Q38KXLFZ7V4",
    "SecretKey": "ZDK61LfGdaqu5FpTcKnUfK8GNSW+cTSSbK8vK8GnMno",
    "BucketName": "mentalhealth-content",
    "Region": "sfo3",
    "ServiceUrl": "https://sfo3.digitaloceanspaces.com",
    "Folder": "content/"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "SM_MentalHealthApp",
    "Audience": "SM_MentalHealthApp_Users"
  },
  "Vonage": {
    "Enabled": true,
    "ApiKey": "c7dc2f50",
    "ApiSecret": "ZsknGg4RbD1fBI4B",
    "FromNumber": "+16148122119"
  },
  "Email": {
    "Enabled": false,
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@healthapp.com",
    "FromName": "Health App",
    "EnableSsl": true
  },
  "Agora": {
    "AppId": "efa11b3a7d05409ca979fb25a5b489ae",
    "UseTokens": true,
    "AppCertificate": "89ab54068fae46aeaf930ffd493e977b"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Kestrel": {
    "Limits": {
      "KeepAliveTimeout": 900,
      "RequestHeadersTimeout": 900
    }
  },
  "RequestTimeout": "00:15:00",
  "PiiEncryption": {
    "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
  },
  "Encryption": {
    "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
  }
}"""

try:
    # CRITICAL: Check if file already exists - if so, don't overwrite it
    if os.path.exists(app_settings_file):
        print("✅ appsettings.Staging.json already exists - preserving your custom settings")
        print("   If you need to recreate it, delete the file first and run this script again")
        sys.exit(0)
    
    # Parse new JSON
    new_config = json.loads(new_json_str)
    
    # Try to read existing file and preserve ConnectionStrings (shouldn't happen due to check above, but kept for safety)
    existing_connection_strings = None
    try:
        with open(app_settings_file, 'r') as f:
            existing_config = json.load(f)
            if "ConnectionStrings" in existing_config:
                existing_connection_strings = existing_config["ConnectionStrings"]
                print("✅ Found existing ConnectionStrings section, preserving it")
    except FileNotFoundError:
        print("ℹ️  Staging file doesn't exist yet, checking base appsettings.json for ConnectionStrings")
    except json.JSONDecodeError as e:
        print(f"⚠️  Existing Staging file has invalid JSON, will create new one: {e}")
    
    # If not found in Staging file, try base appsettings.json (always loaded by ASP.NET Core)
    if not existing_connection_strings:
        try:
            base_file = "/opt/mental-health-app/server/appsettings.json"
            with open(base_file, 'r') as f:
                base_config = json.load(f)
                if "ConnectionStrings" in base_config:
                    existing_connection_strings = base_config["ConnectionStrings"]
                    print("✅ Found ConnectionStrings in base appsettings.json, copying to Staging")
        except FileNotFoundError:
            print("⚠️  Base appsettings.json not found")
        except json.JSONDecodeError:
            print("⚠️  Base appsettings.json has invalid JSON")
    
    # If still not found, try Production file as last resort (in case both staging and production are on same server)
    if not existing_connection_strings:
        try:
            prod_file = "/opt/mental-health-app/server/appsettings.Production.json"
            with open(prod_file, 'r') as f:
                prod_config = json.load(f)
                if "ConnectionStrings" in prod_config:
                    existing_connection_strings = prod_config["ConnectionStrings"]
                    print("✅ Found ConnectionStrings in Production file, copying to Staging")
        except FileNotFoundError:
            pass  # Production file not existing is fine for staging server
        except json.JSONDecodeError:
            pass
    
    # Merge: add ConnectionStrings if it existed (from staging file or production file)
    if existing_connection_strings:
        new_config["ConnectionStrings"] = existing_connection_strings
    else:
        print("⚠️  WARNING: No ConnectionStrings found! The file will be created without database connection.")
        print("   You must manually add ConnectionStrings section to the file.")
    
    # Write the merged config
    with open(app_settings_file, 'w') as f:
        json.dump(new_config, f, indent=2)
    
    # Set proper permissions
    os.chmod(app_settings_file, 0o644)
    
    print(f"✅ Successfully created/updated {app_settings_file}")
    if existing_connection_strings:
        print("✅ ConnectionStrings section preserved")
    
except Exception as e:
    print(f"❌ Error: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
PYTHON

    echo ""
    echo "File contents:"
    cat "$APP_SETTINGS_FILE"
    echo ""
    echo "File permissions:"
    ls -la "$APP_SETTINGS_FILE"
ENDSSH

echo ""
echo "✅ Script complete!"

