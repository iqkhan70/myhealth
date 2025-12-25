#!/bin/bash
# Helper script to check files inside containers on the droplet

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Load droplet IP
source "$SCRIPT_DIR/load-droplet-ip.sh" "${1:-staging}"

# Default SSH key path
SSH_KEY_PATH="${SSH_KEY_PATH:-$HOME/.ssh/id_rsa}"
DROPLET_USER="${DROPLET_USER:-root}"

if [ ! -f "$SSH_KEY_PATH" ]; then
    echo "Error: SSH key not found at $SSH_KEY_PATH" >&2
    exit 1
fi

echo "Connecting to $DROPLET_USER@$DROPLET_IP..."
echo ""
echo "Available commands:"
echo "1. Check API container appsettings files:"
echo "   docker exec mental-health-app-api-1 ls -la /app/appsettings*.json"
echo "   docker exec mental-health-app-api-1 cat /app/appsettings.json"
echo "   docker exec mental-health-app-api-1 cat /app/appsettings.Production.json"
echo ""
echo "2. Check API container environment variables:"
echo "   docker exec mental-health-app-api-1 env | grep ASPNETCORE"
echo ""
echo "3. Check API container logs:"
echo "   docker logs mental-health-app-api-1 --tail 100"
echo ""
echo "4. Check docker-compose .env file:"
echo "   cat /opt/mental-health-app/.env"
echo ""
echo "5. List all containers:"
echo "   docker ps -a"
echo ""
echo "6. Check API container status:"
echo "   docker inspect mental-health-app-api-1 | grep -A 10 'State'"
echo ""
echo "Opening SSH session. Run the commands above:"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP"

