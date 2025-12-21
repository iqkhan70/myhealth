#!/bin/bash

# Script to check what LLM models are available on the server

DROPLET_IP="68.183.61.49"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"

echo "🔍 Checking Ollama models on server..."
echo "======================================"
echo ""

# Fix SSH key permissions
chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "1️⃣ Checking if Ollama service is running..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl is-active ollama && echo '✅ Ollama is running' || echo '❌ Ollama is not running'"

echo ""
echo "2️⃣ Checking Ollama API for available models..."
echo ""

# Check if Ollama is accessible
OLLAMA_RESPONSE=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "curl -s http://localhost:11434/api/tags 2>&1")

if echo "$OLLAMA_RESPONSE" | grep -q "models"; then
    echo "✅ Ollama API is accessible"
    echo ""
    echo "📋 Available models:"
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "curl -s http://localhost:11434/api/tags | python3 -m json.tool 2>/dev/null || curl -s http://localhost:11434/api/tags"
else
    echo "❌ Could not connect to Ollama API"
    echo "Response: $OLLAMA_RESPONSE"
    echo ""
    echo "3️⃣ Checking Ollama service status..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl status ollama --no-pager | head -20"
    echo ""
    echo "4️⃣ Checking if Ollama is listening on port 11434..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "ss -tlnp | grep 11434 || echo 'Port 11434 not listening'"
fi

echo ""
echo "5️⃣ Checking Ollama environment variables..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl show ollama | grep -i environment || echo 'No environment variables found'"

echo ""
echo "✅ Check complete!"

