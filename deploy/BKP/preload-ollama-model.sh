#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Pre-load Ollama model to avoid cold starts

SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "⚡ Pre-loading Ollama Model..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    echo "Pre-loading tinyllama:latest into memory..."
    echo "This may take 30-60 seconds on first load..."
    echo ""
    
    START=$(date +%s)
    curl -s -X POST http://127.0.0.1:11434/api/generate \
        -H "Content-Type: application/json" \
        -d '{
            "model": "tinyllama:latest",
            "prompt": "test",
            "stream": false,
            "options": {
                "num_predict": 10
            }
        }' > /dev/null 2>&1
    END=$(date +%s)
    
    echo "✅ Model loaded in $((END - START)) seconds"
    echo ""
    echo "Checking if model stays in memory:"
    curl -s http://127.0.0.1:11434/api/ps | python3 -m json.tool 2>/dev/null || echo "Could not check"
    echo ""
    echo "✅ Model should now be faster on next request!"
ENDSSH

echo ""
echo "✅ Pre-load complete!"

