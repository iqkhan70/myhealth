#!/bin/bash
# Helper script to access containers on the droplet

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

CONTAINER_NAME="${2:-mental-health-app-api-1}"

echo "Accessing container: $CONTAINER_NAME on $DROPLET_USER@$DROPLET_IP"
echo ""
echo "Available commands:"
echo "1. Check container status:"
echo "   docker ps -a | grep $CONTAINER_NAME"
echo ""
echo "2. Access container shell (even if crashed):"
echo "   docker exec -it $CONTAINER_NAME /bin/bash"
echo "   # Or if bash doesn't exist:"
echo "   docker exec -it $CONTAINER_NAME /bin/sh"
echo ""
echo "3. Check container logs:"
echo "   docker logs $CONTAINER_NAME --tail 100"
echo ""
echo "4. Check appsettings files in container:"
echo "   docker exec $CONTAINER_NAME ls -la /app/appsettings*.json"
echo "   docker exec $CONTAINER_NAME cat /app/appsettings.json"
echo "   docker exec $CONTAINER_NAME cat /app/appsettings.Production.json"
echo ""
echo "5. Check environment variables:"
echo "   docker exec $CONTAINER_NAME env | grep ASPNETCORE"
echo "   docker exec $CONTAINER_NAME env | grep Kestrel"
echo ""
echo "Opening SSH session. Run the commands above:"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP"

