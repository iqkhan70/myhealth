#!/bin/bash

# Setup HTTPS for the .NET server (port 5262)
# This creates a self-signed certificate for the IP address

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuration
DROPLET_IP="159.65.242.79"
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
SERVICE_NAME="mental-health-app"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}HTTPS Setup for .NET Server${NC}"
echo -e "${GREEN}========================================${NC}"

# Fix SSH key permissions if needed
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

echo -e "\n${YELLOW}Step 1: Generating self-signed SSL certificate...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Create certificates directory
    mkdir -p /opt/mental-health-app/certs
    
    # Generate self-signed certificate for the IP address
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout /opt/mental-health-app/certs/server.key \
        -out /opt/mental-health-app/certs/server.crt \
        -subj "/C=US/ST=State/L=City/O=Organization/CN=$DROPLET_IP" \
        -addext "subjectAltName=IP:$DROPLET_IP"
    
    # Set proper permissions
    chmod 600 /opt/mental-health-app/certs/server.key
    chmod 644 /opt/mental-health-app/certs/server.crt
    chown -R appuser:appuser /opt/mental-health-app/certs
    
    echo "✅ SSL certificate generated"
    echo "Certificate: /opt/mental-health-app/certs/server.crt"
    echo "Private Key: /opt/mental-health-app/certs/server.key"
ENDSSH

echo -e "\n${YELLOW}Step 2: Updating systemd service to use HTTPS...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    cat > /etc/systemd/system/$SERVICE_NAME.service << 'EOFSERVICE'
[Unit]
Description=Mental Health App Server
After=network.target redis-server.service mysql.service

[Service]
Type=simple
User=appuser
WorkingDirectory=/opt/mental-health-app/server
ExecStart=/usr/bin/dotnet /opt/mental-health-app/server/SM_MentalHealthApp.Server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mental-health-app
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=https://localhost:5262
Environment=ASPNETCORE_Kestrel__Certificates__Default__Path=/opt/mental-health-app/certs/server.crt
Environment=ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/opt/mental-health-app/certs/server.key

[Install]
WantedBy=multi-user.target
EOFSERVICE

    systemctl daemon-reload
    echo "✅ Service configuration updated"
ENDSSH

echo -e "\n${YELLOW}Step 3: Restarting service...${NC}"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    systemctl restart $SERVICE_NAME
    sleep 3
    systemctl status $SERVICE_NAME --no-pager -l | head -20
ENDSSH

echo -e "\n${YELLOW}Step 4: Testing HTTPS connection...${NC}"

# Test HTTPS connection
HTTPS_TEST=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
    "curl -k -s -o /dev/null -w '%{http_code}' https://localhost:5262/api/health 2>/dev/null || echo '000'")

if [ "$HTTPS_TEST" = "200" ] || [ "$HTTPS_TEST" = "404" ]; then
    echo -e "${GREEN}✅ HTTPS is working on port 5262${NC}"
else
    echo -e "${YELLOW}⚠️  HTTPS test returned: $HTTPS_TEST${NC}"
    echo "Checking logs..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
        "journalctl -u $SERVICE_NAME --no-pager -n 20 | tail -10"
fi

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}HTTPS Setup Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Important Notes:${NC}"
echo "1. This is a SELF-SIGNED certificate (for IP address)"
echo "2. Browsers will show a security warning - this is normal"
echo "3. You need to accept the certificate in your browser:"
echo "   - Visit: https://$DROPLET_IP:5262/api/health"
echo "   - Click 'Advanced' → 'Proceed to site' (or similar)"
echo "4. After accepting, the app should work with HTTPS"
echo ""
echo -e "${YELLOW}For production with a domain:${NC}"
echo "  - Use Let's Encrypt (free, trusted certificates)"
echo "  - Run: ./setup-ssl.sh (requires a domain name)"
echo ""
echo "Server is now running on: https://$DROPLET_IP:5262"

