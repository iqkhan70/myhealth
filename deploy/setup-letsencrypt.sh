#!/bin/bash
# Script to set up Let's Encrypt SSL certificates using certbot
# This installs certbot on the droplet (host) and generates certificates

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
ENVIRONMENT="${1:-staging}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/load-droplet-ip.sh" "$ENVIRONMENT"

DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
CERT_DIR="/opt/mental-health-app/certs"
EMAIL="${2:-your-email@example.com}"  # Pass email as second argument

# Domain configuration
if [ "$ENVIRONMENT" = "staging" ]; then
    DOMAIN_NAME="caseflowstage.store"
    WWW_DOMAIN="www.caseflowstage.store"
elif [ "$ENVIRONMENT" = "production" ]; then
    DOMAIN_NAME="caseflow.store"
    WWW_DOMAIN="www.caseflow.store"
else
    echo -e "${RED}ERROR: Unknown environment. Use 'staging' or 'production'${NC}"
    exit 1
fi

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Let's Encrypt SSL Certificate Setup${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Environment: ${YELLOW}$ENVIRONMENT${NC}"
echo -e "Droplet IP: ${YELLOW}$DROPLET_IP${NC}"
echo -e "Domain: ${YELLOW}$DOMAIN_NAME${NC}"
echo -e "WWW Domain: ${YELLOW}$WWW_DOMAIN${NC}"
echo -e "Email: ${YELLOW}$EMAIL${NC}"
echo ""

if [ "$EMAIL" = "your-email@example.com" ]; then
    echo -e "${YELLOW}⚠️  WARNING: Using default email. Please provide your email address:${NC}"
    echo "Usage: $0 $ENVIRONMENT your-email@example.com"
    echo ""
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo -e "${BLUE}Step 1: Installing certbot on droplet...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    export DEBIAN_FRONTEND=noninteractive
    
    # Update package list
    apt-get update -y
    
    # Install certbot
    if ! command -v certbot &> /dev/null; then
        echo "Installing certbot..."
        apt-get install -y certbot
        echo "✅ Certbot installed"
    else
        echo "✅ Certbot already installed"
    fi
ENDSSH

echo ""
echo -e "${BLUE}Step 2: Stopping nginx container temporarily for certificate generation...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Stop nginx container so certbot can use port 80
    docker stop mental-health-web 2>/dev/null || echo "Nginx container not running or already stopped"
    echo "✅ Nginx container stopped"
ENDSSH

echo ""
echo -e "${BLUE}Step 3: Generating Let's Encrypt certificate...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "DOMAIN_NAME=$DOMAIN_NAME WWW_DOMAIN=$WWW_DOMAIN EMAIL=$EMAIL CERT_DIR=$CERT_DIR bash -s" << 'ENDSSH'
    set -e
    
    echo "Generating certificate for $DOMAIN_NAME and $WWW_DOMAIN..."
    
    # Use standalone mode (requires port 80 to be free)
    certbot certonly --standalone \
        --non-interactive \
        --agree-tos \
        --email "$EMAIL" \
        -d "$DOMAIN_NAME" \
        -d "$WWW_DOMAIN" \
        --preferred-challenges http \
        --cert-name mental-health-app
    
    echo "✅ Certificate generated"
    
    # Copy certificates to the cert directory used by nginx container
    echo "Copying certificates to $CERT_DIR..."
    mkdir -p "$CERT_DIR"
    
    # Let's Encrypt stores certificates in /etc/letsencrypt/live/domain/
    LE_CERT_DIR="/etc/letsencrypt/live/mental-health-app"
    
    if [ -f "$LE_CERT_DIR/fullchain.pem" ] && [ -f "$LE_CERT_DIR/privkey.pem" ]; then
        cp "$LE_CERT_DIR/fullchain.pem" "$CERT_DIR/server.crt"
        cp "$LE_CERT_DIR/privkey.pem" "$CERT_DIR/server.key"
        
        chmod 644 "$CERT_DIR/server.crt"
        chmod 600 "$CERT_DIR/server.key"
        
        echo "✅ Certificates copied to $CERT_DIR"
        echo ""
        echo "Certificate location: $CERT_DIR/server.crt"
        echo "Private key location: $CERT_DIR/server.key"
    else
        echo "❌ ERROR: Certificate files not found in $LE_CERT_DIR"
        exit 1
    fi
ENDSSH

echo ""
echo -e "${BLUE}Step 4: Restarting nginx container...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Restart nginx container
    docker start mental-health-web || {
        echo "⚠️  Could not start nginx container. Starting via docker-compose..."
        cd /opt/mental-health-app
        docker compose --env-file .env up -d web
    }
    echo "✅ Nginx container restarted"
ENDSSH

echo ""
echo -e "${BLUE}Step 5: Setting up auto-renewal...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "CERT_DIR=$CERT_DIR bash -s" << 'ENDSSH'
    set -e
    
    # Create renewal script
    cat > /opt/mental-health-app/renew-cert.sh << 'RENEW_SCRIPT'
#!/bin/bash
# Script to renew Let's Encrypt certificate and restart nginx

set -e

CERT_DIR="/opt/mental-health-app/certs"
LE_CERT_DIR="/etc/letsencrypt/live/mental-health-app"

# Stop nginx container
docker stop mental-health-web

# Renew certificate
certbot renew --standalone --non-interactive

# Copy renewed certificates
if [ -f "$LE_CERT_DIR/fullchain.pem" ] && [ -f "$LE_CERT_DIR/privkey.pem" ]; then
    cp "$LE_CERT_DIR/fullchain.pem" "$CERT_DIR/server.crt"
    cp "$LE_CERT_DIR/privkey.pem" "$CERT_DIR/server.key"
    chmod 644 "$CERT_DIR/server.crt"
    chmod 600 "$CERT_DIR/server.key"
    
    # Restart nginx
    docker start mental-health-web || {
        cd /opt/mental-health-app
        docker compose --env-file .env up -d web
    }
    
    echo "✅ Certificate renewed and nginx restarted"
else
    echo "❌ ERROR: Renewed certificate files not found"
    docker start mental-health-web
    exit 1
fi
RENEW_SCRIPT

    chmod +x /opt/mental-health-app/renew-cert.sh
    echo "✅ Renewal script created at /opt/mental-health-app/renew-cert.sh"
    
    # Add cron job for auto-renewal (runs twice daily, certbot will only renew if needed)
    CRON_JOB="0 0,12 * * * /opt/mental-health-app/renew-cert.sh >> /var/log/certbot-renewal.log 2>&1"
    
    # Remove existing cron job if it exists
    (crontab -l 2>/dev/null | grep -v "/opt/mental-health-app/renew-cert.sh" || true) | crontab -
    
    # Add new cron job
    (crontab -l 2>/dev/null || true; echo "$CRON_JOB") | crontab -
    
    echo "✅ Auto-renewal cron job added (runs twice daily)"
ENDSSH

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✅ Let's Encrypt Setup Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Your site now has a trusted SSL certificate!"
echo ""
echo "Access your site:"
echo "  https://$DOMAIN_NAME"
echo "  https://$WWW_DOMAIN"
echo ""
echo "Certificate details:"
echo "  Location: $CERT_DIR/server.crt"
echo "  Auto-renewal: Enabled (runs twice daily)"
echo ""
echo "To manually renew certificate:"
echo "  ssh $DROPLET_USER@$DROPLET_IP '/opt/mental-health-app/renew-cert.sh'"
echo ""
echo "To check certificate expiration:"
echo "  ssh $DROPLET_USER@$DROPLET_IP 'certbot certificates'"

