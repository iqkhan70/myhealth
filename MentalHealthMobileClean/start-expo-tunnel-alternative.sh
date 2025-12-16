#!/bin/bash

# Alternative tunnel method - uses Expo's cloud tunnel (no ngrok needed)
# This is more reliable than ngrok tunnel

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Starting Expo with Cloud Tunnel${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}This uses Expo's cloud service (no ngrok needed)${NC}"
echo -e "${YELLOW}More reliable than ngrok tunnel${NC}"
echo ""

# Kill any existing Metro processes
pkill -f "expo start" 2>/dev/null || true
sleep 2

# Start Expo - it will automatically use cloud tunnel if available
echo -e "${GREEN}🚀 Starting Expo...${NC}"
echo -e "${BLUE}Expo will automatically use cloud tunnel if available${NC}"
echo ""

# Use --dev-client flag to ensure proper tunnel setup
EXPO_NO_DOTENV=1 npx expo start --clear --dev-client || npx expo start --clear

