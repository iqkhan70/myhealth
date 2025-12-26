#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Fix script for "Ollama api error when clicking not found"
# This script:
# 1. Checks Ollama service status
# 2. Verifies available models
# 3. Updates database with correct model names
# 4. Tests model generation
# 5. Restarts the application

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
DROPLET_USER="root"

echo "🔧 Fixing Ollama 'Not Found' Error..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    echo "=========================================="
    echo "1. Checking Ollama Service Status"
    echo "=========================================="
    if ! systemctl is-active --quiet ollama; then
        echo "   Starting Ollama service..."
        systemctl start ollama
        sleep 5
    fi
    
    if systemctl is-active --quiet ollama; then
        echo "   ✅ Ollama is running"
    else
        echo "   ❌ Failed to start Ollama"
        systemctl status ollama --no-pager | head -15
        exit 1
    fi
    echo ""
    
    echo "=========================================="
    echo "2. Waiting for Ollama API to be ready"
    echo "=========================================="
    i=1
    while [ $i -le 30 ]; do
        if curl -s http://127.0.0.1:11434/api/tags > /dev/null 2>&1; then
            echo "   ✅ Ollama API is ready (attempt $i)"
            break
        fi
        if [ $i -eq 30 ]; then
            echo "   ❌ Ollama API did not become ready after 30 attempts"
            exit 1
        fi
        sleep 1
        i=$((i + 1))
    done
    echo ""
    
    echo "=========================================="
    echo "3. Checking Available Models in Ollama"
    echo "=========================================="
    AVAILABLE_MODELS=$(curl -s http://127.0.0.1:11434/api/tags 2>/dev/null | python3 -c "import sys, json; data = json.load(sys.stdin); print('\n'.join([m.get('name', '') for m in data.get('models', [])]))" 2>/dev/null || echo "")
    
    if [ -z "$AVAILABLE_MODELS" ]; then
        echo "   ❌ No models found in Ollama"
        echo "   Available models: (none)"
    else
        echo "   Available models:"
        echo "$AVAILABLE_MODELS" | while read model; do
            if [ -n "$model" ]; then
                echo "     - $model"
            fi
        done
    fi
    echo ""
    
    echo "=========================================="
    echo "4. Checking Database Configuration"
    echo "=========================================="
    if [ -f /root/mysql_root_password.txt ]; then
        MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4 || echo "")
    else
        MYSQL_ROOT_PASS="UthmanBasima70"
    fi
    
    export MYSQL_PWD="$MYSQL_ROOT_PASS"
    
    echo "Current AI Model Configs in database:"
    mysql -u root customerhealthdb -e "SELECT Id, ModelName, Provider, ApiEndpoint, IsActive FROM AIModelConfigs;" 2>&1 | grep -v "Warning" | grep -v "Enter password"
    echo ""
    
    echo "Current AI Model Chains in database:"
    mysql -u root customerhealthdb -e "SELECT Id, ChainName, Context, PrimaryModelId, SecondaryModelId, IsActive FROM AIModelChains;" 2>&1 | grep -v "Warning" | grep -v "Enter password"
    echo ""
    
    echo "=========================================="
    echo "5. Finding Correct Model Names"
    echo "=========================================="
    
    # Determine which models to use
    PRIMARY_MODEL=""
    SECONDARY_MODEL=""
    
    # Try to find qwen2.5:8b-instruct or similar
    if echo "$AVAILABLE_MODELS" | grep -qE "qwen2\.5.*8b.*instruct|qwen.*8b.*instruct"; then
        PRIMARY_MODEL=$(echo "$AVAILABLE_MODELS" | grep -E "qwen2\.5.*8b.*instruct|qwen.*8b.*instruct" | head -1)
        echo "   ✅ Found primary model: $PRIMARY_MODEL"
    elif echo "$AVAILABLE_MODELS" | grep -qE "qwen2\.5.*8b|qwen.*8b"; then
        PRIMARY_MODEL=$(echo "$AVAILABLE_MODELS" | grep -E "qwen2\.5.*8b|qwen.*8b" | head -1)
        echo "   ✅ Found primary model: $PRIMARY_MODEL"
    elif echo "$AVAILABLE_MODELS" | grep -q "tinyllama"; then
        PRIMARY_MODEL="tinyllama"
        echo "   ✅ Using fallback primary model: $PRIMARY_MODEL"
    else
        PRIMARY_MODEL=$(echo "$AVAILABLE_MODELS" | head -1)
        echo "   ⚠️  Using first available model as primary: $PRIMARY_MODEL"
    fi
    
    # Try to find qwen2.5:4b-instruct or similar
    if echo "$AVAILABLE_MODELS" | grep -qE "qwen2\.5.*4b.*instruct|qwen.*4b.*instruct"; then
        SECONDARY_MODEL=$(echo "$AVAILABLE_MODELS" | grep -E "qwen2\.5.*4b.*instruct|qwen.*4b.*instruct" | head -1)
        echo "   ✅ Found secondary model: $SECONDARY_MODEL"
    elif echo "$AVAILABLE_MODELS" | grep -qE "qwen2\.5.*4b|qwen.*4b"; then
        SECONDARY_MODEL=$(echo "$AVAILABLE_MODELS" | grep -E "qwen2\.5.*4b|qwen.*4b" | head -1)
        echo "   ✅ Found secondary model: $SECONDARY_MODEL"
    elif echo "$AVAILABLE_MODELS" | grep -q "tinyllama"; then
        SECONDARY_MODEL="tinyllama"
        echo "   ✅ Using fallback secondary model: $SECONDARY_MODEL"
    else
        SECONDARY_MODEL=$(echo "$AVAILABLE_MODELS" | head -1)
        echo "   ⚠️  Using first available model as secondary: $SECONDARY_MODEL"
    fi
    
    if [ -z "$PRIMARY_MODEL" ] || [ -z "$SECONDARY_MODEL" ]; then
        echo "   ❌ ERROR: No models available in Ollama!"
        echo "   Please pull models first: ollama pull qwen2.5:8b-instruct"
        exit 1
    fi
    echo ""
    
    echo "=========================================="
    echo "6. Updating Database with Correct Model Names"
    echo "=========================================="
    
    # Update primary model (Id=1)
    echo "   Updating primary model (Id=1) to use: $PRIMARY_MODEL"
    mysql -u root customerhealthdb -e "UPDATE AIModelConfigs SET ApiEndpoint='$PRIMARY_MODEL', UpdatedAt=NOW() WHERE Id=1;" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
    
    # Update secondary model (Id=2)
    echo "   Updating secondary model (Id=2) to use: $SECONDARY_MODEL"
    mysql -u root customerhealthdb -e "UPDATE AIModelConfigs SET ApiEndpoint='$SECONDARY_MODEL', UpdatedAt=NOW() WHERE Id=2;" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
    
    # Ensure both models are active
    mysql -u root customerhealthdb -e "UPDATE AIModelConfigs SET IsActive=1 WHERE Id IN (1,2);" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
    
    # Ensure chain is active
    mysql -u root customerhealthdb -e "UPDATE AIModelChains SET IsActive=1 WHERE Id=1;" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
    
    echo "   ✅ Database updated"
    echo ""
    
    echo "Verifying database updates:"
    mysql -u root customerhealthdb -e "SELECT Id, ModelName, Provider, ApiEndpoint, IsActive FROM AIModelConfigs;" 2>&1 | grep -v "Warning" | grep -v "Enter password"
    echo ""
    
    echo "=========================================="
    echo "7. Testing Model Generation"
    echo "=========================================="
    
    echo "   Testing primary model: $PRIMARY_MODEL"
    if curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d "{\"model\": \"$PRIMARY_MODEL\", \"prompt\": \"test\", \"stream\": false, \"options\": {\"num_predict\": 5}}" > /tmp/ollama_test_primary.json 2>&1; then
        if grep -q "response\|error" /tmp/ollama_test_primary.json; then
            if grep -q "error" /tmp/ollama_test_primary.json; then
                echo "   ❌ Primary model test FAILED"
                cat /tmp/ollama_test_primary.json | head -5
            else
                echo "   ✅ Primary model test SUCCESSFUL"
            fi
        else
            echo "   ⚠️  Primary model test returned unexpected response"
            cat /tmp/ollama_test_primary.json | head -5
        fi
    else
        echo "   ❌ Primary model test FAILED (curl error)"
    fi
    rm -f /tmp/ollama_test_primary.json
    
    echo ""
    echo "   Testing secondary model: $SECONDARY_MODEL"
    if curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d "{\"model\": \"$SECONDARY_MODEL\", \"prompt\": \"test\", \"stream\": false, \"options\": {\"num_predict\": 5}}" > /tmp/ollama_test_secondary.json 2>&1; then
        if grep -q "response\|error" /tmp/ollama_test_secondary.json; then
            if grep -q "error" /tmp/ollama_test_secondary.json; then
                echo "   ❌ Secondary model test FAILED"
                cat /tmp/ollama_test_secondary.json | head -5
            else
                echo "   ✅ Secondary model test SUCCESSFUL"
            fi
        else
            echo "   ⚠️  Secondary model test returned unexpected response"
            cat /tmp/ollama_test_secondary.json | head -5
        fi
    else
        echo "   ❌ Secondary model test FAILED (curl error)"
    fi
    rm -f /tmp/ollama_test_secondary.json
    echo ""
    
    echo "=========================================="
    echo "8. Checking appsettings.Production.json"
    echo "=========================================="
    if [ -f /opt/mental-health-app/server/appsettings.Production.json ]; then
        echo "   Checking Ollama configuration:"
        python3 -c "import json; f=open('/opt/mental-health-app/server/appsettings.Production.json'); data=json.load(f); print('Ollama BaseUrl:', data.get('Ollama', {}).get('BaseUrl', 'NOT SET'))" 2>/dev/null || echo "   Could not parse appsettings"
        
        # Ensure Ollama section exists
        if ! python3 -c "import json; f=open('/opt/mental-health-app/server/appsettings.Production.json'); data=json.load(f); assert 'Ollama' in data" 2>/dev/null; then
            echo "   ⚠️  Ollama section missing, adding it..."
            python3 << 'PYTHON_SCRIPT'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
        data = json.load(f)
    
    if 'Ollama' not in data:
        data['Ollama'] = {
            "BaseUrl": "http://127.0.0.1:11434"
        }
        
        with open('/opt/mental-health-app/server/appsettings.Production.json', 'w') as f:
            json.dump(data, f, indent=2)
        
        print("   ✅ Added Ollama configuration")
    else:
        print("   ✅ Ollama configuration already exists")
except Exception as e:
    print(f"   ❌ Error updating appsettings: {e}")
    sys.exit(1)
PYTHON_SCRIPT
        fi
    else
        echo "   ❌ appsettings.Production.json not found"
    fi
    echo ""
    
    echo "=========================================="
    echo "9. Restarting Application"
    echo "=========================================="
    systemctl restart mental-health-app
    sleep 5
    
    if systemctl is-active --quiet mental-health-app; then
        echo "   ✅ Application restarted successfully"
    else
        echo "   ⚠️  Application restart had issues"
        systemctl status mental-health-app --no-pager | head -10
    fi
    echo ""
    
    unset MYSQL_PWD
    
    echo "=========================================="
    echo "✅ Fix Complete!"
    echo "=========================================="
    echo ""
    echo "Summary:"
    echo "  - Primary model: $PRIMARY_MODEL"
    echo "  - Secondary model: $SECONDARY_MODEL"
    echo "  - Database updated with correct model names"
    echo "  - Application restarted"
    echo ""
    echo "If you still get errors, check:"
    echo "  1. Application logs: journalctl -u mental-health-app -n 50"
    echo "  2. Ollama logs: journalctl -u ollama -n 50"
    echo "  3. Test Ollama directly: curl http://127.0.0.1:11434/api/tags"
ENDSSH

echo ""
echo "✅ Fix script complete!"

