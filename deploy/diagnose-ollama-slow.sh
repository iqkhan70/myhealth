#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Diagnose why Ollama is slow on DigitalOcean

SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔍 Diagnosing Ollama Performance Issues..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    echo "=========================================="
    echo "1. DROPLET SPECIFICATIONS"
    echo "=========================================="
    echo "CPU Cores: $(nproc)"
    echo "Total RAM: $(free -h | grep Mem | awk '{print $2}')"
    echo "Available RAM: $(free -h | grep Mem | awk '{print $7}')"
    echo "CPU Model: $(lscpu | grep 'Model name' | cut -d: -f2 | xargs)"
    echo ""
    
    echo "=========================================="
    echo "2. SYSTEM LOAD"
    echo "=========================================="
    uptime
    echo ""
    echo "Top processes by CPU:"
    ps aux --sort=-%cpu | head -6
    echo ""
    echo "Top processes by Memory:"
    ps aux --sort=-%mem | head -6
    echo ""
    
    echo "=========================================="
    echo "3. OLLAMA SERVICE STATUS"
    echo "=========================================="
    systemctl status ollama --no-pager | head -15
    echo ""
    
    echo "=========================================="
    echo "4. OLLAMA CONFIGURATION"
    echo "=========================================="
    if [ -f /etc/systemd/system/ollama.service ]; then
        echo "Service file exists:"
        grep -E "OLLAMA|ExecStart|Environment" /etc/systemd/system/ollama.service
    else
        echo "❌ Service file not found"
    fi
    echo ""
    echo "OLLAMA_HOST environment:"
    systemctl show ollama | grep -i ollama || echo "Not set in service"
    echo ""
    
    echo "=========================================="
    echo "5. OLLAMA MODELS"
    echo "=========================================="
    echo "Installed models:"
    if command -v ollama &> /dev/null; then
        ollama list 2>&1
    else
        curl -s http://127.0.0.1:11434/api/tags | python3 -m json.tool 2>/dev/null | grep -A 3 '"name"' || echo "Could not list models"
    fi
    echo ""
    
    echo "=========================================="
    echo "6. LOADED MODELS (in memory)"
    echo "=========================================="
    curl -s http://127.0.0.1:11434/api/ps | python3 -m json.tool 2>/dev/null || echo "No models currently loaded or API not accessible"
    echo ""
    
    echo "=========================================="
    echo "7. OLLAMA PROCESS RESOURCES"
    echo "=========================================="
    OLLAMA_PID=$(pgrep -f ollama | head -1)
    if [ -n "$OLLAMA_PID" ]; then
        echo "Ollama PID: $OLLAMA_PID"
        ps -p $OLLAMA_PID -o pid,ppid,%cpu,%mem,vsz,rss,cmd
        echo ""
        echo "Open file descriptors:"
        ls -la /proc/$OLLAMA_PID/fd 2>/dev/null | wc -l || echo "Cannot access"
    else
        echo "❌ Ollama process not found"
    fi
    echo ""
    
    echo "=========================================="
    echo "8. DISK I/O & SWAP"
    echo "=========================================="
    echo "Disk usage:"
    df -h / | tail -1
    echo ""
    echo "Swap usage:"
    free -h | grep Swap
    echo ""
    echo "I/O wait:"
    iostat -x 1 2 2>/dev/null | tail -5 || echo "iostat not available"
    echo ""
    
    echo "=========================================="
    echo "9. TEST OLLAMA RESPONSE TIME"
    echo "=========================================="
    echo "Testing with a small prompt..."
    START_TIME=$(date +%s%N)
    curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{
            "model": "tinyllama:latest",
            "prompt": "Say hello",
            "stream": false,
            "options": {
                "num_predict": 10
            }
        }' > /tmp/ollama_test.json 2>&1
    END_TIME=$(date +%s%N)
    DURATION=$((($END_TIME - $START_TIME) / 1000000))
    echo "Response time: ${DURATION}ms"
    echo "Response preview:"
    head -c 200 /tmp/ollama_test.json
    echo ""
    echo ""
    
    echo "=========================================="
    echo "10. RECOMMENDATIONS"
    echo "=========================================="
    RAM_GB=$(free -g | grep Mem | awk '{print $2}')
    CPU_CORES=$(nproc)
    
    if [ "$RAM_GB" -lt 4 ]; then
        echo "⚠️  WARNING: Only ${RAM_GB}GB RAM - TinyLlama needs at least 2GB free"
        echo "   Consider:"
        echo "   - Upgrading to 4GB+ droplet"
        echo "   - Using a smaller model"
        echo "   - Reducing num_predict in code"
    fi
    
    if [ "$CPU_CORES" -eq 1 ]; then
        echo "⚠️  WARNING: Only 1 CPU core - this will be slow"
        echo "   Consider upgrading to 2+ cores"
    fi
    
    echo ""
    echo "Current num_predict in code: 512 tokens"
    echo "This should be manageable, but the model might not be loaded in memory."
    echo ""
    echo "Try pre-loading the model:"
    echo "  ollama run tinyllama:latest 'hello'"
    echo ""
    echo "Or reduce num_predict further in ChainedAIService.cs (currently 512)"
ENDSSH

echo ""
echo "✅ Diagnosis complete!"

