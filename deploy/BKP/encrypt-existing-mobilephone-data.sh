#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# Script to encrypt existing plain text MobilePhone data on DigitalOcean
# This script runs the C# encryption script on the server
# Run this AFTER apply-mobilephone-encryption-migration.sh

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
SSH_KEY="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app/server"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Encrypt Existing MobilePhone Data${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check SSH key
if [ ! -f "$SSH_KEY" ]; then
    echo -e "${RED}❌ SSH key not found: $SSH_KEY${NC}"
    exit 1
fi

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY" 2>/dev/null || stat -c "%a" "$SSH_KEY" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY"
    fi
fi

echo -e "${YELLOW}⚠️  This will encrypt all plain text phone numbers in the database${NC}"
echo -e "${YELLOW}   Make sure the migration has been applied first!${NC}"
echo ""
read -p "Do you want to continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo -e "${YELLOW}Encryption cancelled.${NC}"
    exit 0
fi

echo ""
echo -e "${BLUE}Step 1: Checking if application is running...${NC}"

# Check if service is running
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << 'ENDSSH'
    if systemctl is-active --quiet mental-health-app; then
        echo "✅ Application service is running"
    else
        echo "⚠️  Application service is not running. Starting it..."
        systemctl start mental-health-app || echo "❌ Failed to start service"
    fi
ENDSSH

echo ""
echo -e "${BLUE}Step 2: Copying Python encryption script to server (fallback)...${NC}"

# Copy Python encryption script as fallback
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
    "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/encrypt-mobilephone-python.py" \
    root@$SERVER_IP:/tmp/encrypt-mobilephone.py

if [ $? -ne 0 ]; then
    echo -e "${YELLOW}⚠️  Failed to copy Python script, will try .NET method only${NC}"
fi

echo ""
echo -e "${BLUE}Step 3: Running encryption script on server...${NC}"
echo -e "${YELLOW}This may take a few minutes depending on the amount of data...${NC}"

# Run the encryption script on the server
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no root@$SERVER_IP << ENDSSH
    APP_DIR="$APP_DIR"
    cd "\$APP_DIR"
    
    # Find dotnet runtime path (server has runtime, not SDK)
    # Check common locations and also check systemd service for the path
    DOTNET_PATH=\$(which dotnet 2>/dev/null || \
        grep "^ExecStart=" /etc/systemd/system/mental-health-app.service 2>/dev/null | cut -d' ' -f1 | sed 's|ExecStart=||' || \
        find /usr/share/dotnet -name dotnet -type f 2>/dev/null | head -1 || \
        find /root/.dotnet -name dotnet -type f 2>/dev/null | head -1 || \
        find /usr/local/bin -name dotnet -type f 2>/dev/null | head -1 || \
        find /opt -name dotnet -type f 2>/dev/null | head -1 || \
        echo "")
    
    if [ -z "\$DOTNET_PATH" ]; then
        echo "❌ dotnet runtime not found. Please ensure .NET runtime is installed."
        exit 1
    fi
    
    # If it's a symlink, resolve it
    if [ -L "\$DOTNET_PATH" ]; then
        DOTNET_PATH=\$(readlink -f "\$DOTNET_PATH" || echo "\$DOTNET_PATH")
    fi
    
    export PATH="\$(dirname \$DOTNET_PATH):\$PATH"
    echo "✅ Using dotnet: \$DOTNET_PATH"
    
    # Check if we have the published application
    # First check the APP_DIR
    PUBLISHED_APP="\$APP_DIR/SM_MentalHealthApp.Server.dll"
    if [ ! -f "\$PUBLISHED_APP" ]; then
        echo "ℹ️  Not found at \$PUBLISHED_APP, looking for alternative locations..."
        # Check systemd service for the actual path
        SERVICE_EXEC=\$(grep "^ExecStart=" /etc/systemd/system/mental-health-app.service 2>/dev/null | sed 's|.*ExecStart=.*dotnet ||' | sed 's| .*||' || echo "")
        if [ -n "\$SERVICE_EXEC" ] && [ -f "\$SERVICE_EXEC" ]; then
            PUBLISHED_APP="\$SERVICE_EXEC"
            echo "✅ Found via systemd service: \$PUBLISHED_APP"
        else
            # Search in common locations
            PUBLISHED_APP=\$(find "\$APP_DIR" -name "SM_MentalHealthApp.Server.dll" -type f 2>/dev/null | head -1 || \
                find /opt/mental-health-app -name "SM_MentalHealthApp.Server.dll" -type f 2>/dev/null | head -1 || \
                echo "")
            if [ -z "\$PUBLISHED_APP" ]; then
                echo "❌ Published application not found. Cannot run encryption."
                echo "ℹ️  The server needs the published application to run encryption."
                echo "ℹ️  Checked: \$APP_DIR/SM_MentalHealthApp.Server.dll"
                echo "ℹ️  Checked systemd service path: \$SERVICE_EXEC"
                echo "ℹ️  Alternatively, you can use a Python-based encryption script."
                exit 1
            fi
        fi
    fi
    
    echo "✅ Found published application: \$PUBLISHED_APP"
    
    # Stop the service temporarily to avoid conflicts
    echo "Stopping application service..."
    systemctl stop mental-health-app || true
    
    # Check .NET version compatibility
    DOTNET_VERSION=\$(\$DOTNET_PATH --version 2>&1 | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -1 || echo "")
    REQUIRED_VERSION="9.0"
    
    # Check if we can run the .NET app or need Python fallback
    if [ -z "\$DOTNET_VERSION" ] || [ "\$(echo \$DOTNET_VERSION | cut -d. -f1)" != "9" ]; then
        echo "⚠️  .NET 9.0 runtime not found (found: \$DOTNET_VERSION)"
        echo "ℹ️  Using Python-based encryption script instead..."
        echo ""
        
        # Use Python-based encryption
        PYTHON_SCRIPT="/tmp/encrypt-mobilephone.py"
        
        # Check if Python script exists (should be copied by script)
        if [ ! -f "\$PYTHON_SCRIPT" ]; then
            echo "❌ Python encryption script not found at \$PYTHON_SCRIPT"
            echo "ℹ️  The script should have been copied to the server."
            exit 1
        fi
        
        # Install dependencies if needed
        if ! python3 -c "import pymysql" 2>/dev/null; then
            echo "Installing pymysql..."
            pip3 install pymysql --quiet || {
                echo "❌ Failed to install pymysql. Please install manually: pip3 install pymysql"
                exit 1
            }
        fi
        
        if ! python3 -c "from Cryptodome.Cipher import AES" 2>/dev/null; then
            echo "Installing pycryptodome..."
            pip3 install pycryptodome --quiet || {
                echo "❌ Failed to install pycryptodome. Please install manually: pip3 install pycryptodome"
                exit 1
            }
        fi
        
        # Run Python encryption script
        echo "Running Python encryption script..."
        python3 "\$PYTHON_SCRIPT" "\$APP_DIR/appsettings.Production.json" 2>&1 | tee /tmp/mobilephone-encryption.log
        ENCRYPT_EXIT=\${PIPESTATUS[0]}
    else
        # Use .NET application
        echo "Running encryption script..."
        echo "Command: \$DOTNET_PATH \"\$PUBLISHED_APP\" --encrypt-mobilephones"
        
        # Get the directory of the published app
        APP_DIR_PATH=\$(dirname "\$PUBLISHED_APP")
        cd "\$APP_DIR_PATH"
        
        # Run with proper working directory and environment
        \$DOTNET_PATH "\$PUBLISHED_APP" --encrypt-mobilephones 2>&1 | tee /tmp/mobilephone-encryption.log
        ENCRYPT_EXIT=\${PIPESTATUS[0]}
    fi
    
    # Check exit code
    if [ \$ENCRYPT_EXIT -eq 0 ]; then
        echo "✅ Encryption completed successfully"
    else
        echo "❌ Encryption failed with exit code \$ENCRYPT_EXIT"
        echo "Check /tmp/mobilephone-encryption.log for details"
        echo ""
        echo "Last 20 lines of log:"
        tail -20 /tmp/mobilephone-encryption.log || true
        exit 1
    fi
    
    # Restart the service
    echo "Restarting application service..."
    systemctl start mental-health-app
    
    # Wait a moment for service to start
    sleep 3
    
    # Check if service started successfully
    if systemctl is-active --quiet mental-health-app; then
        echo "✅ Application service restarted successfully"
    else
        echo "⚠️  Service may not have started. Check status with: systemctl status mental-health-app"
    fi
ENDSSH

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✅ MobilePhone Encryption Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    echo -e "${YELLOW}Verification:${NC}"
    echo -e "Check the logs on the server: ${BLUE}/tmp/mobilephone-encryption.log${NC}"
    echo -e "Or view service logs: ${BLUE}journalctl -u mental-health-app -f${NC}"
else
    echo ""
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}❌ Encryption Failed!${NC}"
    echo -e "${RED}========================================${NC}"
    echo ""
    echo -e "Check the logs on the server: ${BLUE}/tmp/mobilephone-encryption.log${NC}"
    exit 1
fi

