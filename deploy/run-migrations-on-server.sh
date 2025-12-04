#!/bin/bash

# Run database migrations directly on the server
# Copy this script to the server and run it there: ./run-migrations-on-server.sh

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

APP_DIR="/opt/mental-health-app/server"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Run Database Migrations${NC}"
echo -e "${GREEN}========================================${NC}"

cd "$APP_DIR" || {
    echo -e "${RED}ERROR: Cannot find application directory: $APP_DIR${NC}"
    exit 1
}

# Set up PATH
export PATH=$PATH:/usr/share/dotnet:/root/.dotnet/tools

echo -e "\n${YELLOW}Checking .NET SDK...${NC}"
if ! dotnet --list-sdks &>/dev/null; then
    echo -e "${RED}ERROR: .NET SDK not found!${NC}"
    echo "Please install it first or run: ./install-ef-tool.sh from your local machine"
    exit 1
fi

echo -e "\n${YELLOW}Checking dotnet-ef tool...${NC}"
if ! dotnet ef --version &>/dev/null; then
    echo -e "${YELLOW}Installing dotnet-ef tool...${NC}"
    dotnet tool install --global dotnet-ef --version 9.0.0
    export PATH="$PATH:/root/.dotnet/tools"
fi

echo -e "\n${YELLOW}Running database migrations...${NC}"
echo "Current directory: $(pwd)"
echo "Connection string from appsettings.Production.json:"
grep -A 1 '"MySQL"' appsettings.Production.json || echo "⚠️ Could not find MySQL connection string"

# Run migrations
echo ""
dotnet ef database update --no-build || {
    echo -e "${YELLOW}⚠️ Migration with --no-build failed. Trying with build...${NC}"
    dotnet ef database update
}

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Migrations Complete!${NC}"
echo -e "${GREEN}========================================${NC}"

