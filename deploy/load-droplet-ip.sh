#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Default environment if not provided
ENVIRONMENT="${1:-staging}"   # staging | production

case "$ENVIRONMENT" in
  staging)
    DROPLET_IP_FILE="${SCRIPT_DIR}/DROPLET_IP_STAGING"
    ;;
  production)
    DROPLET_IP_FILE="${SCRIPT_DIR}/DROPLET_IP_PRODUCTION"
    ;;
  *)
    echo "Error: Unknown environment '$ENVIRONMENT'. Use: staging | production" >&2
    exit 1
    ;;
esac

if [ -f "$DROPLET_IP_FILE" ]; then
    DROPLET_IP=$(cat "$DROPLET_IP_FILE" | tr -d '[:space:]')
    export DROPLET_IP
    export SERVER_IP="$DROPLET_IP"  # Alias for compatibility
else
    echo "Error: DROPLET_IP file not found at $DROPLET_IP_FILE" >&2
    exit 1
fi
