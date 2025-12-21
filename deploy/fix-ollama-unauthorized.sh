#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Quick fix script for "unauthorized" Ollama errors

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
DROPLET_USER="root"

echo "🔧 Fixing Ollama Unauthorized Error..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    echo "1. Ensuring Ollama service is running..."
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
    
    echo "2. Waiting for Ollama API to be ready..."
    for i in {1..30}; do
        if curl -s http://127.0.0.1:11434/api/tags > /dev/null 2>&1; then
            echo "   ✅ Ollama API is ready"
            break
        fi
        if [ $i -eq 30 ]; then
            echo "   ❌ Ollama API did not become ready"
            exit 1
        fi
        sleep 1
    done
    echo ""
    
    echo "3. Checking available models..."
    AVAILABLE_MODELS=$(curl -s http://127.0.0.1:11434/api/tags 2>/dev/null | python3 -c "import sys, json; data = json.load(sys.stdin); print(' '.join([m.get('name', '') for m in data.get('models', [])]))" 2>/dev/null || echo "")
    echo "   Available models: $AVAILABLE_MODELS"
    echo ""
    
    echo "4. Pulling missing models..."
    
    if ! echo "$AVAILABLE_MODELS" | grep -q "qwen2.5:8b"; then
        echo "   Pulling qwen2.5:8b (this may take 5-10 minutes)..."
        ollama pull qwen2.5:8b || echo "   ⚠️  Failed to pull qwen2.5:8b"
    else
        echo "   ✅ qwen2.5:8b already available"
    fi
    
    if ! echo "$AVAILABLE_MODELS" | grep -q "qwen2.5:4b"; then
        echo "   Pulling qwen2.5:4b (this may take 3-8 minutes)..."
        ollama pull qwen2.5:4b || echo "   ⚠️  Failed to pull qwen2.5:4b"
    else
        echo "   ✅ qwen2.5:4b already available"
    fi
    echo ""
    
    echo "5. Verifying models are accessible..."
    if curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model": "qwen2.5:8b", "prompt": "test", "stream": false, "options": {"num_predict": 5}}' > /dev/null 2>&1; then
        echo "   ✅ qwen2.5:8b is working"
    else
        echo "   ❌ qwen2.5:8b test failed"
    fi
    
    if curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model": "qwen2.5:4b", "prompt": "test", "stream": false, "options": {"num_predict": 5}}' > /dev/null 2>&1; then
        echo "   ✅ qwen2.5:4b is working"
    else
        echo "   ❌ qwen2.5:4b test failed"
    fi
    echo ""
    
    echo "6. Restarting application to pick up changes..."
    systemctl restart mental-health-app
    sleep 3
    
    if systemctl is-active --quiet mental-health-app; then
        echo "   ✅ Application restarted"
    else
        echo "   ⚠️  Application restart had issues"
        systemctl status mental-health-app --no-pager | head -10
    fi
    echo ""
    
    echo "=========================================="
    echo "✅ Fix Complete!"
    echo "=========================================="
    echo ""
    echo "If you still get 'unauthorized' errors, check:"
    echo "1. Application logs: journalctl -u mental-health-app -n 50"
    echo "2. Ollama logs: journalctl -u ollama -n 50"
    echo "3. Database has correct model names: SELECT * FROM AIModelConfigs;"
ENDSSH

echo ""
echo "✅ Fix script complete!"

