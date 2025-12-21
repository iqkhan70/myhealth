#!/bin/bash

# Script to create/update /opt/mental-health-app/server/appsettings.Production.json on DigitalOcean server
# Preserves ConnectionStrings section if it exists, overwrites everything else

SERVER_IP="159.65.242.79"
SSH_KEY="$HOME/.ssh/id_rsa"

echo "🔧 Creating/updating appsettings.Production.json file on DigitalOcean server..."

ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    APP_SETTINGS_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    APP_DIR="/opt/mental-health-app/server"

    echo "Creating/updating appsettings.Production.json file on server..."

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

    # Use Python to merge JSON, preserving ConnectionStrings if it exists
    python3 << 'PYTHON'
import json
import sys

app_settings_file = "/opt/mental-health-app/server/appsettings.Production.json"
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
    # Parse new JSON
    new_config = json.loads(new_json_str)
    
    # Try to read existing file and preserve ConnectionStrings
    existing_connection_strings = None
    try:
        with open(app_settings_file, 'r') as f:
            existing_config = json.load(f)
            if "ConnectionStrings" in existing_config:
                existing_connection_strings = existing_config["ConnectionStrings"]
                print("✅ Found existing ConnectionStrings section, preserving it")
    except FileNotFoundError:
        print("ℹ️  File doesn't exist yet, creating new one")
    except json.JSONDecodeError as e:
        print(f"⚠️  Existing file has invalid JSON, will create new one: {e}")
    
    # Merge: add ConnectionStrings if it existed
    if existing_connection_strings:
        new_config["ConnectionStrings"] = existing_connection_strings
    
    # Write the merged config
    with open(app_settings_file, 'w') as f:
        json.dump(new_config, f, indent=2)
    
    # Set proper permissions
    import os
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
