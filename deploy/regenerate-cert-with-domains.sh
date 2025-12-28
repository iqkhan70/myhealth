#!/bin/bash
# Script to regenerate SSL certificate with domain names and restart nginx

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
ENVIRONMENT="${1:-staging}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/load-droplet-ip.sh" "$ENVIRONMENT"

DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"

# Domain configuration
if [ "$ENVIRONMENT" = "staging" ]; then
    DOMAIN_NAME="caseflowstage.store"
    WWW_DOMAIN="www.caseflowstage.store"
elif [ "$ENVIRONMENT" = "production" ]; then
    DOMAIN_NAME="caseflow.store"
    WWW_DOMAIN="www.caseflow.store"
else
    DOMAIN_NAME="$DROPLET_IP"
    WWW_DOMAIN="$DROPLET_IP"
fi

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Regenerating SSL Certificate${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Environment: ${YELLOW}$ENVIRONMENT${NC}"
echo -e "Droplet IP: ${YELLOW}$DROPLET_IP${NC}"
echo -e "Domain: ${YELLOW}$DOMAIN_NAME${NC}"
echo -e "WWW Domain: ${YELLOW}$WWW_DOMAIN${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "DOMAIN_NAME=$DOMAIN_NAME WWW_DOMAIN=$WWW_DOMAIN DROPLET_IP=$DROPLET_IP bash -s" << 'ENDSSH'
    set -e
    
    echo "Backing up old certificate..."
    if [ -f /opt/mental-health-app/certs/server.crt ]; then
        cp /opt/mental-health-app/certs/server.crt /opt/mental-health-app/certs/server.crt.backup.$(date +%Y%m%d_%H%M%S) || true
    fi
    if [ -f /opt/mental-health-app/certs/server.key ]; then
        cp /opt/mental-health-app/certs/server.key /opt/mental-health-app/certs/server.key.backup.$(date +%Y%m%d_%H%M%S) || true
    fi
    
    echo "Generating new SSL certificate with domain names..."
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout /opt/mental-health-app/certs/server.key \
        -out /opt/mental-health-app/certs/server.crt \
        -subj "/C=US/ST=State/L=City/O=Organization/CN=$DOMAIN_NAME" \
        -addext "subjectAltName=IP:$DROPLET_IP,DNS:$DOMAIN_NAME,DNS:$WWW_DOMAIN"
    
    chmod 600 /opt/mental-health-app/certs/server.key
    chmod 644 /opt/mental-health-app/certs/server.crt
    
    echo "✅ SSL certificate generated"
    echo ""
    echo "Certificate details:"
    openssl x509 -in /opt/mental-health-app/certs/server.crt -noout -text | grep -A 2 "Subject:" || true
    openssl x509 -in /opt/mental-health-app/certs/server.crt -noout -text | grep -A 1 "Subject Alternative Name" || true
    
    echo ""
    echo "Restarting nginx container to pick up new certificate..."
    docker restart mental-health-web || {
        echo "⚠️  Could not restart nginx container. You may need to restart it manually:"
        echo "   docker restart mental-health-web"
    }
    
    echo ""
    echo "✅ Certificate regeneration complete!"
    echo ""
    echo "Note: You will still see a browser warning because this is a self-signed certificate."
    echo "To proceed:"
    echo "  1. Click 'Advanced' or 'Show Details'"
    echo "  2. Click 'Proceed to caseflowstage.store (unsafe)' or 'Accept the Risk and Continue'"
    echo ""
    echo "For production, consider using Let's Encrypt for a trusted certificate."
ENDSSH

echo ""
echo -e "${GREEN}✅ Done!${NC}"
echo ""
echo "You can now access your site at:"
echo "  https://$DOMAIN_NAME"
echo "  https://$WWW_DOMAIN"
echo ""
echo "Note: You'll need to accept the browser warning (this is normal for self-signed certificates)"

