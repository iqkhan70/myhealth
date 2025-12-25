#!/bin/bash
set -e

# Usage:
#   ./generate-self-signed-cert.sh staging
#   ./generate-self-signed-cert.sh production

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${SCRIPT_DIR}/../load-droplet-ip.sh" "${1:-staging}"

ssh root@"$DROPLET_IP" << EOF
set -e
mkdir -p /opt/mental-health-app/certs
cd /opt/mental-health-app/certs

openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout server.key \
  -out server.crt \
  -subj "/C=US/ST=State/L=City/O=CaseFlow/CN=$DROPLET_IP" \
  -addext "subjectAltName=IP:$DROPLET_IP"

chmod 600 server.key
chmod 644 server.crt
echo "✅ Cert created at /opt/mental-health-app/certs/server.crt and server.key"
EOF
