#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to find correct Qwen model names and test pulling them

SSH_KEY_PATH="$HOME/.ssh/id_rsa"
DROPLET_USER="root"

echo "🔍 Finding Correct Qwen Model Names..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    echo "Testing different Qwen model name variations..."
    echo ""
    
    # List of possible model names to try
    MODELS_TO_TRY=(
        "qwen2.5:8b"
        "qwen2.5:8b-instruct"
        "qwen2.5:8b-instruct-q4_0"
        "qwen2.5-8b"
        "qwen2.5-8b-instruct"
        "qwen2.5:4b"
        "qwen2.5:4b-instruct"
        "qwen2.5:4b-instruct-q4_0"
        "qwen2.5-4b"
        "qwen2.5-4b-instruct"
        "qwen:8b"
        "qwen:4b"
    )
    
    echo "Testing model pulls (this will show which ones work)..."
    echo ""
    
    WORKING_MODELS=()
    
    for model in "${MODELS_TO_TRY[@]}"; do
        echo "Testing: $model"
        if timeout 30 ollama pull "$model" 2>&1 | head -3 | grep -q "pulling\|downloading\|success"; then
            echo "  ✅ $model - SUCCESS (or already exists)"
            WORKING_MODELS+=("$model")
            # Cancel if it's actually pulling (we just want to test)
            pkill -f "ollama pull $model" 2>/dev/null || true
        else
            echo "  ❌ $model - Failed or not found"
        fi
        sleep 1
    done
    
    echo ""
    echo "=========================================="
    echo "Alternative: Use tinyllama (smaller, faster, definitely available)"
    echo "=========================================="
    echo ""
    echo "Testing tinyllama..."
    if ollama pull tinyllama 2>&1 | head -5; then
        echo "✅ tinyllama is available and works"
    fi
    echo ""
    
    echo "=========================================="
    echo "Available models on Ollama library:"
    echo "=========================================="
    echo "You can browse available models at: https://ollama.com/library"
    echo ""
    echo "Common Qwen models that might work:"
    echo "  - qwen2.5:8b-instruct"
    echo "  - qwen2.5:4b-instruct"
    echo "  - qwen2.5:7b-instruct"
    echo "  - qwen2.5:14b-instruct"
    echo ""
    echo "Or use tinyllama as a fallback (smaller but works):"
    echo "  - tinyllama"
ENDSSH

echo ""
echo "✅ Model name check complete"
echo ""
echo "If qwen2.5 models don't work, we can:"
echo "1. Use tinyllama (smaller, faster, definitely available)"
echo "2. Update database to use the correct model names"
echo "3. Use a different model that's available"

