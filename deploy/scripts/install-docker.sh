#!/bin/bash
set -e

# Usage:
#   ./deploy/scripts/install-docker.sh staging
#   ./deploy/scripts/install-docker.sh production

ENVIRONMENT="${1:-staging}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/../load-droplet-ip.sh" "$ENVIRONMENT"

echo "========================================"
echo "Installing Docker on $ENVIRONMENT"
echo "Droplet IP: $DROPLET_IP"
echo "========================================"

ssh root@"$DROPLET_IP" << 'EOF'
set -e

if command -v docker >/dev/null 2>&1; then
  echo "✅ Docker already installed:"
  docker --version
else
  echo "Installing Docker..."

  apt-get update
  apt-get install -y ca-certificates curl gnupg

  install -m 0755 -d /etc/apt/keyrings

  curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
    | gpg --dearmor -o /etc/apt/keyrings/docker.gpg

  chmod a+r /etc/apt/keyrings/docker.gpg

  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
    https://download.docker.com/linux/ubuntu \
    $(. /etc/os-release && echo $VERSION_CODENAME) stable" \
    > /etc/apt/sources.list.d/docker.list

  apt-get update
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

  systemctl enable docker
  systemctl start docker

  echo "✅ Docker installed:"
  docker --version
fi

if docker compose version >/dev/null 2>&1; then
  echo "✅ Docker Compose plugin available:"
  docker compose version
else
  echo "❌ Docker Compose plugin not found"
  exit 1
fi
EOF

echo "========================================"
echo "✅ Docker setup complete on $DROPLET_IP"
echo "========================================"
