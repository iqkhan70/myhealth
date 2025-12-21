#!/bin/bash

# Configure Ollama to keep models in memory

DROPLET_IP="159.65.242.79"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "⚙️  Configuring Ollama Keep-Alive..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    OLLAMA_SERVICE="/etc/systemd/system/ollama.service"
    
    if [ ! -f "$OLLAMA_SERVICE" ]; then
        echo "❌ Ollama service file not found at $OLLAMA_SERVICE"
        exit 1
    fi
    
    echo "1. Checking current Ollama service configuration..."
    grep -E "OLLAMA|Environment" "$OLLAMA_SERVICE" || echo "   (No environment variables currently set)"
    echo ""
    
    # Backup
    cp "$OLLAMA_SERVICE" "${OLLAMA_SERVICE}.backup.$(date +%Y%m%d_%H%M%S)"
    
    # Check if keep-alive is already set
    if grep -q "OLLAMA_KEEP_ALIVE" "$OLLAMA_SERVICE"; then
        echo "✅ OLLAMA_KEEP_ALIVE already configured"
        grep "OLLAMA_KEEP_ALIVE" "$OLLAMA_SERVICE"
    else
        echo "2. Adding OLLAMA_KEEP_ALIVE setting..."
        
        # Add Environment line after [Service]
        if grep -q "\[Service\]" "$OLLAMA_SERVICE"; then
            sed -i '/\[Service\]/a Environment="OLLAMA_KEEP_ALIVE=5m"' "$OLLAMA_SERVICE"
            echo "   ✅ Added OLLAMA_KEEP_ALIVE=5m"
        else
            echo "   ❌ Could not find [Service] section"
            exit 1
        fi
    fi
    
    echo ""
    echo "3. Updated service configuration:"
    grep -A 5 "\[Service\]" "$OLLAMA_SERVICE" | head -10
    echo ""
    
    echo "4. Reloading systemd and restarting Ollama..."
    systemctl daemon-reload
    systemctl restart ollama
    
    sleep 3
    
    if systemctl is-active --quiet ollama; then
        echo "   ✅ Ollama restarted successfully"
    else
        echo "   ❌ Ollama failed to start"
        systemctl status ollama --no-pager | head -15
        exit 1
    fi
    
    echo ""
    echo "=========================================="
    echo "✅ Keep-Alive Configured!"
    echo "=========================================="
    echo ""
    echo "Ollama will now keep models in memory for 5 minutes"
    echo "after the last request, making subsequent requests faster."
    echo ""
    echo "To verify:"
    echo "  systemctl show ollama | grep OLLAMA"
ENDSSH

echo ""
echo "✅ Keep-alive configuration complete!"

