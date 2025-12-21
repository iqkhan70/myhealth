#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Diagnostic script to check Ollama status and configuration

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
DROPLET_USER="root"

echo "🔍 Checking Ollama Status on $DROPLET_IP..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    echo "=========================================="
    echo "1. Checking Ollama Service Status"
    echo "=========================================="
    if systemctl is-active --quiet ollama; then
        echo "✅ Ollama service is RUNNING"
        systemctl status ollama --no-pager | head -10
    else
        echo "❌ Ollama service is NOT RUNNING"
        systemctl status ollama --no-pager | head -15
    fi
    echo ""
    
    echo "=========================================="
    echo "2. Checking Ollama Port (11434)"
    echo "=========================================="
    if ss -tlnp | grep -q ":11434"; then
        echo "✅ Port 11434 is LISTENING"
        ss -tlnp | grep ":11434"
    else
        echo "❌ Port 11434 is NOT LISTENING"
    fi
    echo ""
    
    echo "=========================================="
    echo "3. Testing Ollama API Connectivity"
    echo "=========================================="
    if curl -s http://127.0.0.1:11434/api/tags > /dev/null 2>&1; then
        echo "✅ Ollama API is ACCESSIBLE"
        echo "Available models:"
        curl -s http://127.0.0.1:11434/api/tags | python3 -m json.tool 2>/dev/null | grep -E '"name"|"model"' | head -20 || curl -s http://127.0.0.1:11434/api/tags
    else
        echo "❌ Ollama API is NOT ACCESSIBLE"
        echo "Error:"
        curl -v http://127.0.0.1:11434/api/tags 2>&1 | head -10
    fi
    echo ""
    
    echo "=========================================="
    echo "4. Checking Required Models"
    echo "=========================================="
    MODELS=$(curl -s http://127.0.0.1:11434/api/tags 2>/dev/null | python3 -c "import sys, json; data = json.load(sys.stdin); print('\n'.join([m.get('name', '') for m in data.get('models', [])]))" 2>/dev/null || echo "")
    
    if echo "$MODELS" | grep -q "qwen2.5:8b"; then
        echo "✅ qwen2.5:8b is available"
    else
        echo "❌ qwen2.5:8b is NOT available"
    fi
    
    if echo "$MODELS" | grep -q "qwen2.5:4b"; then
        echo "✅ qwen2.5:4b is available"
    else
        echo "❌ qwen2.5:4b is NOT available"
    fi
    
    echo ""
    echo "All available models:"
    echo "$MODELS" | head -10
    echo ""
    
    echo "=========================================="
    echo "5. Checking Database Configuration"
    echo "=========================================="
    if [ -f /root/mysql_root_password.txt ]; then
        MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4 || echo "")
    else
        MYSQL_ROOT_PASS="UthmanBasima70"
    fi
    
    export MYSQL_PWD="$MYSQL_ROOT_PASS"
    
    echo "AI Model Configs in database:"
    mysql -u root customerhealthdb -e "SELECT Id, ModelName, Provider, ApiEndpoint, IsActive FROM AIModelConfigs;" 2>&1 | grep -v "Warning" | grep -v "Enter password"
    
    echo ""
    echo "AI Model Chains in database:"
    mysql -u root customerhealthdb -e "SELECT Id, ChainName, Context, PrimaryModelId, SecondaryModelId, IsActive FROM AIModelChains;" 2>&1 | grep -v "Warning" | grep -v "Enter password"
    
    unset MYSQL_PWD
    echo ""
    
    echo "=========================================="
    echo "6. Checking Ollama Configuration in appsettings"
    echo "=========================================="
    if [ -f /opt/mental-health-app/server/appsettings.Production.json ]; then
        echo "Ollama section:"
        python3 -c "import json; f=open('/opt/mental-health-app/server/appsettings.Production.json'); data=json.load(f); print(json.dumps(data.get('Ollama', {}), indent=2))" 2>/dev/null || echo "Could not parse appsettings"
    else
        echo "❌ appsettings.Production.json not found"
    fi
    echo ""
    
    echo "=========================================="
    echo "7. Testing Model Generation"
    echo "=========================================="
    if curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model": "qwen2.5:8b", "prompt": "test", "stream": false, "options": {"num_predict": 5}}' > /tmp/ollama_test.json 2>&1; then
        echo "✅ Model generation test SUCCESSFUL"
        cat /tmp/ollama_test.json | python3 -m json.tool 2>/dev/null | head -5 || cat /tmp/ollama_test.json | head -5
    else
        echo "❌ Model generation test FAILED"
        cat /tmp/ollama_test.json 2>&1 | head -10
    fi
    rm -f /tmp/ollama_test.json
    echo ""
    
    echo "=========================================="
    echo "✅ Diagnostic Complete"
    echo "=========================================="
ENDSSH

echo ""
echo "To fix issues:"
echo "1. If Ollama is not running: systemctl start ollama"
echo "2. If models are missing: ollama pull qwen2.5:8b && ollama pull qwen2.5:4b"
echo "3. If port is not listening: check firewall and Ollama service"
echo ""

