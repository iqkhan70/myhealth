#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Simple test script to verify Ollama connection from the app's perspective

SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "Testing Ollama connection..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    echo "1. Checking appsettings.Production.json for Ollama config:"
    python3 << 'PYTHON'
import json
import sys

try:
    with open('/opt/mental-health-app/server/appsettings.Production.json', 'r') as f:
        config = json.load(f)
    
    if 'Ollama' in config:
        base_url = config['Ollama'].get('BaseUrl', 'NOT SET')
        print(f"   Ollama BaseUrl: {base_url}")
    else:
        print("   ❌ Ollama section not found - adding it...")
        config['Ollama'] = {'BaseUrl': 'http://127.0.0.1:11434'}
        with open('/opt/mental-health-app/server/appsettings.Production.json', 'w') as f:
            json.dump(config, f, indent=2)
        print("   ✅ Added Ollama config: http://127.0.0.1:11434")
except Exception as e:
    print(f"   ❌ Error: {e}")
    sys.exit(1)
PYTHON
    
    echo ""
    echo "2. Testing Ollama API with curl (simulating .NET app call):"
    echo "   Request: POST http://127.0.0.1:11434/api/generate"
    echo "   Model: tinyllama:latest"
    echo ""
    
    RESPONSE=$(curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{"model":"tinyllama:latest","prompt":"What is blood pressure?","stream":false}' \
        --max-time 60 2>&1)
    
    if echo "$RESPONSE" | grep -q '"response"'; then
        echo "   ✅ SUCCESS - Ollama API responded"
        echo "$RESPONSE" | python3 -m json.tool 2>/dev/null | head -10 || echo "$RESPONSE" | head -5
    else
        echo "   ❌ FAILED - Ollama API did not respond correctly"
        echo "   Response:"
        echo "$RESPONSE" | head -10
    fi
    
    echo ""
    echo "3. Checking if Ollama is accessible from the app's user context:"
    # The app runs as appuser, so test as that user
    if id appuser &>/dev/null; then
        sudo -u appuser curl -s -X POST http://127.0.0.1:11434/api/tags --max-time 5 > /dev/null 2>&1
        if [ $? -eq 0 ]; then
            echo "   ✅ App user can access Ollama"
        else
            echo "   ⚠️  App user cannot access Ollama (may need to check permissions)"
        fi
    else
        echo "   ⚠️  appuser not found, skipping user context test"
    fi
    
    echo ""
    echo "4. Checking recent app logs for Ollama calls:"
    journalctl -u mental-health-app --since '10 minutes ago' --no-pager | grep -i "ollama\|Calling.*model" | tail -10 || echo "   No recent Ollama logs"
ENDSSH

echo ""
echo "✅ Test complete!"

