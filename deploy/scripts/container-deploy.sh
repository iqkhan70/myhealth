#!/bin/bash
set -euo pipefail

ENVIRONMENT="${1:-staging}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"  # .../deploy

source "$DEPLOY_DIR/load-droplet-ip.sh" "$ENVIRONMENT"

APP_DIR="/opt/mental-health-app"

COMPOSE_FILE="$DEPLOY_DIR/docker/docker-compose.yml"
NGINX_FILE="$DEPLOY_DIR/nginx/nginx.conf"
ENV_FILE_LOCAL="$DEPLOY_DIR/docker/.env.${ENVIRONMENT}"

echo "Deploying to $ENVIRONMENT at $DROPLET_IP"
echo "Using compose: $COMPOSE_FILE"
echo "Using nginx:   $NGINX_FILE"

# Pre-flight checks
[[ -f "$COMPOSE_FILE" ]] || { echo "❌ Missing: $COMPOSE_FILE"; exit 1; }
[[ -f "$NGINX_FILE"   ]] || { echo "❌ Missing: $NGINX_FILE"; exit 1; }

# Ensure remote dir exists
ssh root@"$DROPLET_IP" "mkdir -p $APP_DIR"

# Copy compose + nginx
scp "$COMPOSE_FILE" root@"$DROPLET_IP":"$APP_DIR/docker-compose.yml"
scp "$NGINX_FILE"   root@"$DROPLET_IP":"$APP_DIR/nginx.conf"

# Copy env if you have it locally (recommended)
if [[ -f "$ENV_FILE_LOCAL" ]]; then
  echo "Copying env file: $ENV_FILE_LOCAL"
  scp "$ENV_FILE_LOCAL" root@"$DROPLET_IP":"$APP_DIR/.env"
else
  echo "⚠️  Local env file not found at $ENV_FILE_LOCAL"
  echo "   Make sure /opt/mental-health-app/.env exists on the droplet before compose pull/up."
fi

# Deploy
ssh root@"$DROPLET_IP" << EOF
set -e
cd $APP_DIR

# sanity
ls -la
test -f .env || { echo "❌ Missing .env in $APP_DIR"; exit 1; }

docker compose --env-file .env pull
docker compose --env-file .env up -d --remove-orphans

echo ""
echo "✅ Containers:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
EOF

echo ""
echo "✅ Done. Test:"
echo "   https://$DROPLET_IP/"
