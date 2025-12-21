#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to check Ollama status and configuration on the server

SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "🔍 Checking Ollama status on server..."
echo ""

echo "1. Checking Ollama service status:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl status ollama --no-pager | head -15"
echo ""

echo "2. Checking if Ollama is listening on port 11434:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "ss -tlnp | grep 11434 || echo '❌ Port 11434 not listening'"
echo ""

echo "3. Testing Ollama API from server (localhost):"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "curl -s http://localhost:11434/api/tags | head -20 || echo '❌ Cannot connect to Ollama on localhost:11434'"
echo ""

echo "4. Checking appsettings.Production.json for Ollama configuration:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "grep -A 2 'Ollama' /opt/mental-health-app/server/appsettings.Production.json || echo '❌ Ollama section not found in appsettings'"
echo ""

echo "5. Checking if tinyllama model is available:"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "ollama list 2>&1 | grep -i tinyllama || echo '⚠️  tinyllama model not found. Run: ollama pull tinyllama'"
echo ""

echo "✅ Status check complete!"

