#!/bin/bash

# Diagnostic script for Chained AI timeout issues

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔍 Diagnosing Chained AI Timeout Issues..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    echo "1. Checking Ollama service status..."
    systemctl status ollama --no-pager | head -10
    echo ""
    
    echo "2. Checking if Ollama is listening..."
    ss -tlnp | grep 11434 || echo "❌ Port 11434 not listening"
    echo ""
    
    echo "3. Testing Ollama API directly (quick test)..."
    TEST_RESPONSE=$(curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model":"tinyllama:latest","prompt":"Hello","stream":false}' \
        --max-time 30 2>&1)
    
    if echo "$TEST_RESPONSE" | grep -q '"response"'; then
        echo "   ✅ Ollama API is responding"
        echo "$TEST_RESPONSE" | python3 -m json.tool 2>/dev/null | head -5 || echo "$TEST_RESPONSE" | head -3
    else
        echo "   ❌ Ollama API is NOT responding correctly"
        echo "   Response: $TEST_RESPONSE" | head -10
    fi
    echo ""
    
    echo "4. Checking Ollama configuration in appsettings..."
    CONFIG_FILE="$APP_DIR/server/appsettings.Production.json"
    if [ -f "$CONFIG_FILE" ] && grep -q '"Ollama"' "$CONFIG_FILE"; then
        echo "   ✅ Ollama section found:"
        grep -A 3 '"Ollama"' $APP_DIR/server/appsettings.Production.json
    else
        echo "   ❌ Ollama section NOT found in appsettings"
    fi
    echo ""
    
    echo "5. Checking recent Chained AI logs..."
    journalctl -u mental-health-app --since '1 hour ago' --no-pager | \
        grep -i "chained\|Calling.*Ollama\|Ollama.*API" | tail -20 || echo "   No recent chained AI logs"
    echo ""
    
    echo "6. Checking for timeout errors..."
    journalctl -u mental-health-app --since '1 hour ago' --no-pager | \
        grep -i "timeout\|timed out\|request.*cancel" | tail -10 || echo "   No timeout errors found"
    echo ""
    
    echo "7. Testing Ollama with a longer prompt (simulating real usage)..."
    LONG_TEST=$(curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model":"tinyllama:latest","prompt":"What is blood pressure? Please explain in detail.","stream":false,"options":{"temperature":0.7,"num_predict":500}}' \
        --max-time 60 2>&1)
    
    if echo "$LONG_TEST" | grep -q '"response"'; then
        echo "   ✅ Ollama responds to longer prompts"
        RESPONSE_LEN=$(echo "$LONG_TEST" | python3 -c "import sys, json; print(len(json.load(sys.stdin).get('response', '')))" 2>/dev/null || echo "unknown")
        echo "   Response length: $RESPONSE_LEN characters"
    else
        echo "   ❌ Ollama failed on longer prompt"
        echo "$LONG_TEST" | head -10
    fi
    echo ""
    
    echo "8. Checking if tinyllama model is available..."
    ollama list 2>&1 | grep -i tinyllama || echo "   ⚠️  tinyllama not found in model list"
    echo ""
    
    echo "9. Checking application logs for Chained AI errors..."
    journalctl -u mental-health-app --since '30 minutes ago' --no-pager | \
        grep -A 5 -i "chained.*error\|chained.*exception\|chained.*fail" | tail -30 || echo "   No chained AI errors found"
ENDSSH

echo ""
echo "✅ Diagnostic complete!"

