#!/bin/bash

# Optimize Ollama performance on DigitalOcean

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "⚡ Optimizing Ollama Performance..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    echo "=========================================="
    echo "1. PRE-LOADING TINYLLAMA MODEL"
    echo "=========================================="
    echo "This will load the model into memory so subsequent requests are faster..."
    echo ""
    
    # Pre-load the model by making a small request
    echo "Pre-loading tinyllama:latest..."
    START=$(date +%s)
    curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{
            "model": "tinyllama:latest",
            "prompt": "test",
            "stream": false,
            "options": {
                "num_predict": 5
            }
        }' > /dev/null 2>&1
    END=$(date +%s)
    echo "✅ Model pre-loaded in $((END - START)) seconds"
    echo ""
    
    echo "=========================================="
    echo "2. CHECKING IF MODEL STAYS IN MEMORY"
    echo "=========================================="
    echo "Current loaded models:"
    curl -s http://127.0.0.1:11434/api/ps | python3 -m json.tool 2>/dev/null || echo "API not accessible"
    echo ""
    
    echo "=========================================="
    echo "3. OPTIMIZING OLLAMA SERVICE"
    echo "=========================================="
    
    # Check if we can set OLLAMA_NUM_PARALLEL or other optimizations
    if [ -f /etc/systemd/system/ollama.service ]; then
        echo "Current service configuration:"
        grep -E "OLLAMA|Environment" /etc/systemd/system/ollama.service || echo "No environment variables set"
        
        # Add keep-alive to prevent model unloading
        if ! grep -q "OLLAMA_KEEP_ALIVE" /etc/systemd/system/ollama.service; then
            echo ""
            echo "Adding OLLAMA_KEEP_ALIVE to keep model in memory..."
            sed -i '/\[Service\]/a Environment="OLLAMA_KEEP_ALIVE=5m"' /etc/systemd/system/ollama.service
            systemctl daemon-reload
            systemctl restart ollama
            sleep 3
            echo "✅ Ollama restarted with keep-alive setting"
        else
            echo "✅ Keep-alive already configured"
        fi
    fi
    echo ""
    
    echo "=========================================="
    echo "4. SYSTEM RESOURCE CHECK"
    echo "=========================================="
    echo "Available RAM: $(free -h | grep Mem | awk '{print $7}')"
    echo "CPU Load: $(uptime | awk -F'load average:' '{print $2}')"
    echo ""
    
    RAM_GB=$(free -g | grep Mem | awk '{print $2}')
    if [ "$RAM_GB" -lt 4 ]; then
        echo "⚠️  WARNING: Only ${RAM_GB}GB RAM"
        echo "   TinyLlama needs ~1.5GB RAM when loaded"
        echo "   Consider reducing num_predict in code or upgrading droplet"
    fi
    echo ""
    
    echo "=========================================="
    echo "5. TESTING RESPONSE TIME"
    echo "=========================================="
    echo "Making a test request (model should be loaded now)..."
    START=$(date +%s%N)
    curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{
            "model": "tinyllama:latest",
            "prompt": "What is 2+2?",
            "stream": false,
            "options": {
                "num_predict": 20
            }
        }' > /tmp/ollama_test2.json 2>&1
    END=$(date +%s%N)
    DURATION=$((($END - $START) / 1000000))
    echo "Response time: ${DURATION}ms"
    
    if [ "$DURATION" -lt 5000 ]; then
        echo "✅ Good! Model is loaded and responding quickly"
    elif [ "$DURATION" -lt 30000 ]; then
        echo "⚠️  Acceptable but could be better"
    else
        echo "❌ Still slow - model might not be staying in memory"
        echo "   Consider:"
        echo "   - Upgrading droplet to 4GB+ RAM"
        echo "   - Using a smaller/faster model"
        echo "   - Reducing num_predict in code"
    fi
    echo ""
    
    echo "=========================================="
    echo "✅ Optimization Complete!"
    echo "=========================================="
    echo ""
    echo "Next steps:"
    echo "1. Monitor the next AI request - it should be faster"
    echo "2. If still slow, consider reducing num_predict from 512 to 256 in ChainedAIService.cs"
    echo "3. Or upgrade droplet to 4GB RAM / 2 vCPU"
ENDSSH

echo ""
echo "✅ Optimization complete!"

