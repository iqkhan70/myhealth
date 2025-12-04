#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Install Ollama on DigitalOcean Server
# This script installs Ollama and sets it up as a systemd service

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DROPLET_IP="${DROPLET_IP:-${DROPLET_IP}}"
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Ollama Installation Script${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Installing Ollama on: $DROPLET_IP"
echo ""

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

# Install Ollama on the server
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    echo "Step 1: Downloading and installing Ollama..."
    
    # Download and install Ollama
    curl -fsSL https://ollama.com/install.sh | sh
    
    echo ""
    echo "Step 2: Creating systemd service for Ollama..."
    
    # Create systemd service file
    cat > /etc/systemd/system/ollama.service << 'EOFSERVICE'
[Unit]
Description=Ollama Service
After=network-online.target

[Service]
ExecStart=/usr/local/bin/ollama serve
User=ollama
Group=ollama
Restart=always
RestartSec=3
Environment="OLLAMA_HOST=0.0.0.0:11434"
Environment="OLLAMA_ORIGINS=*"

[Install]
WantedBy=default.target
EOFSERVICE
    
    # Create ollama user if it doesn't exist
    if ! id "ollama" &>/dev/null; then
        useradd -r -s /bin/false -m -d /usr/share/ollama ollama
    fi
    
    # Set up Ollama data directory
    mkdir -p /usr/share/ollama
    chown -R ollama:ollama /usr/share/ollama
    
    echo ""
    echo "Step 3: Starting Ollama service..."
    
    # Reload systemd and start service
    systemctl daemon-reload
    systemctl enable ollama
    systemctl start ollama
    
    # Wait a moment for service to start
    sleep 3
    
    echo ""
    echo "Step 4: Verifying installation..."
    
    # Check service status
    systemctl status ollama --no-pager | head -10
    
    # Test Ollama
    if command -v ollama &> /dev/null; then
        echo ""
        echo "✅ Ollama installed successfully!"
        echo ""
        echo "Ollama version:"
        ollama --version || echo "Version check failed (service may still be starting)"
    else
        echo "⚠️  Ollama command not found in PATH"
    fi
    
    echo ""
    echo "Step 5: Opening firewall port..."
    
    # Open port 11434 for Ollama
    ufw allow 11434/tcp || echo "UFW not configured or already allowed"
    
    echo ""
    echo "=========================================="
    echo "Ollama Installation Complete!"
    echo "=========================================="
    echo ""
    echo "Ollama is running on: http://0.0.0.0:11434"
    echo ""
    echo "To pull a model, SSH to the server and run:"
    echo "  ollama pull llama2"
    echo "  ollama pull mistral"
    echo "  ollama pull codellama"
    echo ""
    echo "To check service status:"
    echo "  systemctl status ollama"
    echo ""
    echo "To view logs:"
    echo "  journalctl -u ollama -f"
ENDSSH

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Installation Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Ollama should now be installed and running on the server."
echo ""
echo "To test from your local machine:"
echo "  curl http://$DROPLET_IP:11434/api/tags"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "ollama pull tinyllama:latest"


ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no root@$DROPLET_IP "systemctl restart ollama"