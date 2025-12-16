#!/bin/bash

# Script to switch between local and DigitalOcean server configurations
# This makes it easy to switch without manually editing config files

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

CONFIG_FILE="src/config/app.config.js"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Check if config file exists
if [ ! -f "$CONFIG_FILE" ]; then
    echo -e "${RED}❌ Config file not found: $CONFIG_FILE${NC}"
    exit 1
fi

# Get Mac's local IP address
get_local_ip() {
    # Try to get the primary network interface IP
    local ip=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | head -1 | awk '{print $2}')
    if [ -z "$ip" ]; then
        ip="192.168.1.100"  # Fallback
    fi
    echo "$ip"
}

# Get DigitalOcean IP from deploy script
get_droplet_ip() {
    local deploy_dir="../../deploy"
    local ip_file="$deploy_dir/DROPLET_IP"
    
    if [ -f "$ip_file" ]; then
        cat "$ip_file" | tr -d '[:space:]'
    else
        echo "caseflowstage.store"  # Fallback to DNS
    fi
}

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Switch Server Configuration${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Choose server:${NC}"
echo "  1) Local Mac (for development)"
echo "  2) DigitalOcean (staging/production)"
echo ""
read -p "Enter choice [1 or 2]: " choice

case $choice in
    1)
        echo ""
        echo -e "${BLUE}Switching to LOCAL server...${NC}"
        echo ""
        echo -e "${YELLOW}What are you testing on?${NC}"
        echo "  1) iOS Simulator (use localhost)"
        echo "  2) Physical device or Android Emulator (use Mac IP)"
        echo ""
        read -p "Enter choice [1 or 2]: " device_choice
        
        if [ "$device_choice" = "1" ]; then
            LOCAL_IP="localhost"
            echo -e "${GREEN}Using localhost for iOS Simulator${NC}"
        else
            LOCAL_IP=$(get_local_ip)
            echo -e "${YELLOW}Detected Mac IP: $LOCAL_IP${NC}"
            read -p "Use this IP? [Y/n]: " use_ip
            if [[ "$use_ip" =~ ^[Nn]$ ]]; then
                read -p "Enter your Mac's IP address: " LOCAL_IP
            fi
        fi
        
        # Backup current config
        cp "$CONFIG_FILE" "${CONFIG_FILE}.backup.$(date +%Y%m%d_%H%M%S)"
        
        # Update config for local
        sed -i '' \
            -e "s|SERVER_IP: '[^']*'|SERVER_IP: '$LOCAL_IP'|" \
            -e "s|SERVER_PORT: [0-9]*|SERVER_PORT: 5263|" \
            -e "s|USE_HTTPS: [a-z]*|USE_HTTPS: false|" \
            "$CONFIG_FILE"
        
        echo ""
        echo -e "${GREEN}✅ Switched to LOCAL server${NC}"
        echo -e "${GREEN}   SERVER_IP: $LOCAL_IP${NC}"
        echo -e "${GREEN}   SERVER_PORT: 5263${NC}"
        echo -e "${GREEN}   USE_HTTPS: false${NC}"
        echo ""
        echo -e "${YELLOW}📋 Next steps:${NC}"
        echo "  1. Start your server (both HTTP and HTTPS will be available):"
        echo -e "     ${BLUE}cd ../SM_MentalHealthApp.Server${NC}"
        echo -e "     ${BLUE}dotnet run${NC}"
        echo -e "     ${BLUE}Mobile app will use HTTP on port 5263${NC}"
        echo -e "     ${BLUE}Web app will use HTTPS on port 5262${NC}"
        if [ "$device_choice" = "2" ]; then
            echo ""
            echo "  2. Ensure your device is on the same WiFi network as your Mac"
        fi
        echo ""
        echo "  3. Restart Metro: npx expo start --clear"
        ;;
    
    2)
        echo ""
        echo -e "${BLUE}Switching to DIGITALOCEAN server...${NC}"
        
        DROPLET_IP=$(get_droplet_ip)
        echo -e "${YELLOW}Detected server: $DROPLET_IP${NC}"
        read -p "Use this server? [Y/n]: " use_server
        if [[ "$use_server" =~ ^[Nn]$ ]]; then
            read -p "Enter server IP/DNS: " DROPLET_IP
        fi
        
        # Backup current config
        cp "$CONFIG_FILE" "${CONFIG_FILE}.backup.$(date +%Y%m%d_%H%M%S)"
        
        # Update config for DigitalOcean
        sed -i '' \
            -e "s|SERVER_IP: '[^']*'|SERVER_IP: '$DROPLET_IP'|" \
            -e "s|SERVER_PORT: [0-9]*|SERVER_PORT: 443|" \
            -e "s|USE_HTTPS: [a-z]*|USE_HTTPS: true|" \
            "$CONFIG_FILE"
        
        echo ""
        echo -e "${GREEN}✅ Switched to DIGITALOCEAN server${NC}"
        echo -e "${GREEN}   SERVER_IP: $DROPLET_IP${NC}"
        echo -e "${GREEN}   SERVER_PORT: 443${NC}"
        echo -e "${GREEN}   USE_HTTPS: true${NC}"
        echo ""
        echo -e "${YELLOW}📋 Next steps:${NC}"
        echo "  1. Restart Metro: npx expo start --clear"
        echo "  2. Make sure you have internet connection"
        ;;
    
    *)
        echo -e "${RED}❌ Invalid choice. Exiting.${NC}"
        exit 1
        ;;
esac

echo ""
echo -e "${GREEN}========================================${NC}"

