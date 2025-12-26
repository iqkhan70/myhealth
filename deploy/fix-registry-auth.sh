#!/bin/bash
# Quick script to fix registry authentication on the droplet

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/load-droplet-ip.sh" "${1:-staging}"

SSH_KEY_PATH="${SSH_KEY_PATH:-$HOME/.ssh/id_rsa}"
DROPLET_USER="${DROPLET_USER:-root}"

if [ ! -f "$SSH_KEY_PATH" ]; then
    echo "Error: SSH key not found at $SSH_KEY_PATH" >&2
    exit 1
fi

echo "Fixing registry authentication on $DROPLET_USER@$DROPLET_IP..."
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    echo "1. Clearing old registry authentication..."
    docker logout registry.digitalocean.com 2>/dev/null || true
    
    echo "2. Attempting to login via doctl..."
    if command -v doctl >/dev/null 2>&1; then
        if doctl registry login; then
            echo "✅ Successfully logged in via doctl"
        else
            echo "❌ doctl registry login failed"
            echo ""
            echo "Please run on the server:"
            echo "  doctl auth init"
            echo "  doctl registry login"
            exit 1
        fi
    else
        echo "⚠️  doctl not found"
        echo ""
        echo "Please manually login:"
        echo "  docker login registry.digitalocean.com"
        echo ""
        echo "You'll need a DigitalOcean API token from:"
        echo "  https://cloud.digitalocean.com/account/api/tokens"
        exit 1
    fi
    
    echo ""
    echo "3. Verifying authentication..."
    if docker pull registry.digitalocean.com/cha-registry/mental-health-app:api-staging >/dev/null 2>&1 || \
       docker manifest inspect registry.digitalocean.com/cha-registry/mental-health-app:api-staging >/dev/null 2>&1; then
        echo "✅ Authentication verified - can access registry"
    else
        echo "⚠️  Could not verify (image might not exist yet, which is OK)"
    fi
    
    echo ""
    echo "✅ Registry authentication fixed!"
ENDSSH

