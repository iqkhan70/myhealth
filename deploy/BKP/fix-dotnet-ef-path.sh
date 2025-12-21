#!/bin/bash
# Quick fix script to make dotnet-ef accessible on the server

set -e

# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"

echo "Fixing dotnet-ef PATH on server..."

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Set up PATH
    export PATH=$PATH:/usr/share/dotnet
    export PATH="$PATH:$HOME/.dotnet/tools"
    
    # Ensure dotnet-ef is installed
    if ! dotnet ef --version &>/dev/null; then
        echo "Installing dotnet-ef tool..."
        dotnet tool install --global dotnet-ef --version 9.0.0 || dotnet tool install --global dotnet-ef
        export PATH="$PATH:$HOME/.dotnet/tools"
    fi
    
    # Add to bashrc for persistence
    if ! grep -q '.dotnet/tools' ~/.bashrc; then
        echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
        echo 'export PATH="$PATH:/usr/share/dotnet"' >> ~/.bashrc
    fi
    
    # Create a symlink in /usr/local/bin for easier access
    if [ -f "$HOME/.dotnet/tools/dotnet-ef" ]; then
        ln -sf "$HOME/.dotnet/tools/dotnet-ef" /usr/local/bin/dotnet-ef 2>/dev/null || true
    fi
    
    # Verify installation
    echo "Verifying dotnet-ef installation..."
    export PATH="$PATH:$HOME/.dotnet/tools"
    dotnet ef --version
    
    echo ""
    echo "✅ dotnet-ef is now accessible"
    echo ""
    echo "To run migrations, use:"
    echo "  export PATH=\$PATH:/usr/share/dotnet"
    echo "  export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
    echo "  cd /opt/mental-health-app/server"
    echo "  dotnet ef database update"
ENDSSH

echo ""
echo "✅ Fix complete!"

