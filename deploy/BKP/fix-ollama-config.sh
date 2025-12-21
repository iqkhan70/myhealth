#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to check and fix Ollama configuration on the server

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
CONFIG_FILE="$APP_DIR/server/appsettings.Production.json"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔍 Checking Ollama configuration and status..."
echo ""

# Check if Ollama service is running
echo "1. Checking Ollama service status:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl is-active ollama && echo '✅ Ollama is running' || echo '❌ Ollama is NOT running'"
echo ""

# Check if Ollama is listening
echo "2. Checking if Ollama is listening on port 11434:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "ss -tlnp | grep 11434 && echo '✅ Port 11434 is listening' || echo '❌ Port 11434 is NOT listening'"
echo ""

# Test Ollama API
echo "3. Testing Ollama API (localhost):"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "curl -s -m 5 http://localhost:11434/api/tags | head -5 || echo '❌ Cannot connect to Ollama'"
echo ""

# Check current configuration
echo "4. Checking appsettings.Production.json for Ollama config:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "grep -A 3 'Ollama' $CONFIG_FILE || echo '❌ Ollama section not found'"
echo ""

# Check if we need to add Ollama config
echo "5. Adding/Updating Ollama configuration..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    CONFIG_FILE="/opt/mental-health-app/server/appsettings.Production.json"
    
    # Check if Ollama section exists
    if grep -q '"Ollama"' "$CONFIG_FILE"; then
        echo "✅ Ollama section already exists"
        # Update BaseUrl if it exists, or add it
        if grep -q '"BaseUrl"' "$CONFIG_FILE"; then
            echo "Updating Ollama BaseUrl to http://127.0.0.1:11434"
            sed -i 's|"BaseUrl":\s*"[^"]*"|"BaseUrl": "http://127.0.0.1:11434"|g' "$CONFIG_FILE"
        else
            echo "Adding BaseUrl to Ollama section"
            sed -i '/"Ollama"/a\    "BaseUrl": "http://127.0.0.1:11434",' "$CONFIG_FILE"
        fi
    else
        echo "Adding Ollama section to appsettings"
        # Find the position after Agora section
        if grep -q '"Agora"' "$CONFIG_FILE"; then
            # Insert after Agora section
            sed -i '/"Agora"/a\  },\n  "Ollama": {\n    "BaseUrl": "http://127.0.0.1:11434"\n  },' "$CONFIG_FILE"
        else
            # Add before Logging section
            sed -i '/"Logging"/i\  "Ollama": {\n    "BaseUrl": "http://127.0.0.1:11434"\n  },' "$CONFIG_FILE"
        fi
    fi
    
    echo ""
    echo "Updated configuration:"
    grep -A 3 '"Ollama"' "$CONFIG_FILE"
ENDSSH

echo ""
echo "6. Restarting application service..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl restart mental-health-app && echo '✅ Service restarted'"
echo ""

echo "✅ Configuration check complete!"
echo ""
echo "Note: Using 127.0.0.1 instead of localhost for better reliability in .NET"

