#!/bin/bash

# Start Expo with LAN mode - only works on same WiFi network
# Use this when you're at home or on the same network as your Mac

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Starting Expo with LAN Mode${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}This works when your phone is on the same WiFi${NC}"
echo -e "${YELLOW}For remote access, use: ./start-expo-tunnel.sh${NC}"
echo ""

# Kill any existing Metro processes
pkill -f "expo start" 2>/dev/null || true
sleep 2

# Get Mac's IP
MAC_IP=$(ifconfig | grep "inet " | grep -v 127.0.0.1 | head -1 | awk '{print $2}')
if [ -z "$MAC_IP" ]; then
    MAC_IP="unknown"
fi

echo -e "${BLUE}Your Mac's IP: $MAC_IP${NC}"
echo -e "${BLUE}Make sure your phone is on the same WiFi network${NC}"
echo ""

# Start Expo with LAN mode
echo -e "${GREEN}🚀 Starting Expo with LAN mode...${NC}"
echo ""
npx expo start --host lan --clear

