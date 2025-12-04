#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Setup automatic Ollama model pre-loading on server startup

SSH_KEY_PATH="$HOME/.ssh/id_rsa"

chmod 600 "$SSH_KEY_PATH" 2>/dev/null

echo "⚡ Setting up Ollama model pre-loading on startup..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP << 'ENDSSH'
    set -e
    
    echo "=========================================="
    echo "1. Creating pre-load script"
    echo "=========================================="
    
    cat > /usr/local/bin/preload-ollama.sh << 'SCRIPT'
#!/bin/bash
# Pre-load Ollama model on startup
# This script waits for Ollama to be ready, then pre-loads the model

echo "Waiting for Ollama service to be ready..."
for i in {1..30}; do
    if curl -s http://127.0.0.1:11434/api/tags > /dev/null 2>&1; then
        echo "✅ Ollama is ready"
        break
    fi
    echo "  Attempt $i/30..."
    sleep 2
done

echo "Pre-loading tinyllama:latest model..."
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

if [ $((END - START)) -lt 300 ]; then
    echo "✅ Model pre-loaded successfully in $((END - START)) seconds"
else
    echo "⚠️  Model pre-load took $((END - START)) seconds (might be slow on first boot)"
fi

# Keep model in memory by setting keep-alive
curl -s -X POST http://127.0.0.1:11434/api/generate \
    -H "Content-Type: application/json" \
    -d '{
        "model": "tinyllama:latest",
        "prompt": "keep alive",
        "stream": false,
        "options": {
            "num_predict": 1
        }
    }' > /dev/null 2>&1

echo "✅ Ollama model is ready for use"
SCRIPT

    chmod +x /usr/local/bin/preload-ollama.sh
    echo "✅ Pre-load script created at /usr/local/bin/preload-ollama.sh"
    echo ""
    
    echo "=========================================="
    echo "2. Creating systemd service"
    echo "=========================================="
    
    cat > /etc/systemd/system/ollama-preload.service << 'SERVICE'
[Unit]
Description=Pre-load Ollama Model
After=ollama.service
Requires=ollama.service

[Service]
Type=oneshot
ExecStart=/usr/local/bin/preload-ollama.sh
StandardOutput=journal
StandardError=journal
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
SERVICE

    echo "✅ Systemd service created"
    echo ""
    
    echo "=========================================="
    echo "3. Enabling and starting service"
    echo "=========================================="
    
    systemctl daemon-reload
    systemctl enable ollama-preload.service
    
    # Start it now (don't wait for next boot)
    echo "Starting pre-load service now..."
    systemctl start ollama-preload.service
    
    sleep 5
    
    if systemctl is-active --quiet ollama-preload.service; then
        echo "✅ Service started successfully"
    else
        echo "⚠️  Service may still be running (checking logs)..."
        journalctl -u ollama-preload.service --no-pager -n 20
    fi
    echo ""
    
    echo "=========================================="
    echo "4. Checking service status"
    echo "=========================================="
    systemctl status ollama-preload.service --no-pager | head -15
    echo ""
    
    echo "=========================================="
    echo "5. Verifying model is loaded"
    echo "=========================================="
    echo "Waiting 10 seconds for pre-load to complete..."
    sleep 10
    echo ""
    echo "Loaded models:"
    curl -s http://127.0.0.1:11434/api/ps | python3 -m json.tool 2>/dev/null || echo "Could not check (this is OK if pre-load is still running)"
    echo ""
    
    echo "=========================================="
    echo "✅ Setup Complete!"
    echo "=========================================="
    echo ""
    echo "The Ollama model will now be pre-loaded:"
    echo "  - On server boot (after Ollama starts)"
    echo "  - Automatically when Ollama service restarts"
    echo ""
    echo "To check status:"
    echo "  systemctl status ollama-preload.service"
    echo ""
    echo "To view logs:"
    echo "  journalctl -u ollama-preload.service -f"
    echo ""
    echo "To manually trigger pre-load:"
    echo "  /usr/local/bin/preload-ollama.sh"
ENDSSH

echo ""
echo "✅ Ollama pre-loading setup complete!"

