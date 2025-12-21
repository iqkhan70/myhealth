#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Install .NET EF Core tools on the server
# This is the simplest solution - installs the SDK and EF tools on the server

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Install EF Core Tools on Server${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
        echo -e "${GREEN}✅ SSH key permissions fixed${NC}"
    fi
else
    echo -e "${RED}ERROR: SSH key not found at $SSH_KEY_PATH${NC}"
    exit 1
fi

echo -e "\n${YELLOW}Installing .NET SDK and EF Core tools on server...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Install .NET SDK (needed for EF tools)
    if ! dotnet --version &>/dev/null || ! dotnet --list-sdks &>/dev/null; then
        echo "Installing .NET SDK..."
        wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
        chmod +x dotnet-install.sh
        ./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
        export PATH=$PATH:/usr/share/dotnet
        echo 'export PATH=$PATH:/usr/share/dotnet' >> ~/.bashrc
    fi
    
    # Install EF Core tools
    echo "Installing dotnet-ef tool..."
    export PATH=$PATH:/usr/share/dotnet
    
    # Try installing with specific version to avoid package issues
    /usr/share/dotnet/dotnet tool uninstall --global dotnet-ef 2>/dev/null || true
    
    # Install specific version that's known to work
    /usr/share/dotnet/dotnet tool install --global dotnet-ef --version 9.0.0 || {
        echo "Trying alternative installation method..."
        # Alternative: Install as local tool in project directory
        cd $APP_DIR/server
        /usr/share/dotnet/dotnet new tool-manifest 2>/dev/null || true
        /usr/share/dotnet/dotnet tool install dotnet-ef --version 9.0.0 || {
            echo "⚠️ Could not install dotnet-ef tool automatically"
            echo "You can install it manually: dotnet tool install --global dotnet-ef"
        }
    }
    
    # Add dotnet tools to PATH
    export PATH="$PATH:$HOME/.dotnet/tools"
    echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
    
    # Also add to system PATH for root user
    if [ "$USER" = "root" ]; then
        echo 'export PATH="$PATH:/root/.dotnet/tools"' >> ~/.bashrc
    fi
    
    echo "✅ .NET SDK and EF Core tools installed"
    echo ""
    echo "You can now run migrations:"
    echo "  cd $APP_DIR/server"
    echo "  dotnet ef database update"
ENDSSH

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Installation Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Now you can run migrations on the server:"
echo "  ssh $DROPLET_USER@$DROPLET_IP 'cd $APP_DIR/server && dotnet ef database update'"

