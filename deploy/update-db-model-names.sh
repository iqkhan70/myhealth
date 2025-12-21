#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to update database with correct Ollama model names

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
DROPLET_USER="root"

echo "🔧 Updating Database with Correct Model Names..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    # Get MySQL root password
    if [ -f /root/mysql_root_password.txt ]; then
        MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4 || echo "")
    else
        MYSQL_ROOT_PASS="UthmanBasima70"
    fi
    
    if [ -z "$MYSQL_ROOT_PASS" ]; then
        echo "❌ ERROR: MySQL root password is empty!"
        exit 1
    fi
    
    export MYSQL_PWD="$MYSQL_ROOT_PASS"
    
    # Get available models from Ollama
    echo "Checking available Ollama models..."
    AVAILABLE_MODELS=$(curl -s http://127.0.0.1:11434/api/tags 2>/dev/null | python3 -c "import sys, json; data = json.load(sys.stdin); print(' '.join([m.get('name', '') for m in data.get('models', [])]))" 2>/dev/null || echo "")
    
    echo "Available models: $AVAILABLE_MODELS"
    echo ""
    
    # Determine which models to use
    PRIMARY_MODEL=""
    SECONDARY_MODEL=""
    
    # Try to find qwen models first
    if echo "$AVAILABLE_MODELS" | grep -q "qwen2.5:8b-instruct"; then
        PRIMARY_MODEL="qwen2.5:8b-instruct"
    elif echo "$AVAILABLE_MODELS" | grep -q "qwen2.5:8b"; then
        PRIMARY_MODEL="qwen2.5:8b"
    elif echo "$AVAILABLE_MODELS" | grep -q "tinyllama"; then
        PRIMARY_MODEL="tinyllama"
    else
        echo "❌ No suitable primary model found!"
        echo "Please pull a model first: ollama pull tinyllama"
        exit 1
    fi
    
    if echo "$AVAILABLE_MODELS" | grep -q "qwen2.5:4b-instruct"; then
        SECONDARY_MODEL="qwen2.5:4b-instruct"
    elif echo "$AVAILABLE_MODELS" | grep -q "qwen2.5:4b"; then
        SECONDARY_MODEL="qwen2.5:4b"
    elif echo "$AVAILABLE_MODELS" | grep -q "tinyllama"; then
        SECONDARY_MODEL="tinyllama"
    else
        SECONDARY_MODEL="$PRIMARY_MODEL"  # Use same model if secondary not available
    fi
    
    echo "Using Primary Model: $PRIMARY_MODEL"
    echo "Using Secondary Model: $SECONDARY_MODEL"
    echo ""
    
    # Update database
    echo "Updating AIModelConfigs table..."
    mysql -u root customerhealthdb << EOF
UPDATE AIModelConfigs 
SET ApiEndpoint = '$PRIMARY_MODEL', UpdatedAt = NOW() 
WHERE Id = 1;

UPDATE AIModelConfigs 
SET ApiEndpoint = '$SECONDARY_MODEL', UpdatedAt = NOW() 
WHERE Id = 2;
EOF
    
    echo "✅ Database updated successfully"
    echo ""
    echo "Verifying updates..."
    mysql -u root customerhealthdb -e "SELECT Id, ModelName, ApiEndpoint FROM AIModelConfigs;" 2>&1 | grep -v "Warning" | grep -v "Enter password"
    
    unset MYSQL_PWD
    
    echo ""
    echo "✅ Model names updated in database!"
    echo ""
    echo "Restarting application to pick up changes..."
    systemctl restart mental-health-app
    sleep 3
    
    if systemctl is-active --quiet mental-health-app; then
        echo "✅ Application restarted"
    else
        echo "⚠️  Application restart had issues"
        systemctl status mental-health-app --no-pager | head -10
    fi
ENDSSH

echo ""
echo "✅ Update complete!"

