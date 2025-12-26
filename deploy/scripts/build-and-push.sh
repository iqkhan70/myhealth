#!/bin/bash
set -euo pipefail

# ============================================================
# build-and-push.sh
# ============================================================
# Builds and pushes the API + Web images to DigitalOcean Container Registry (DOCR).
#
# Usage:
#   ./deploy/scripts/build-and-push.sh staging
#   ./deploy/scripts/build-and-push.sh production
#
# Requirements (on your Mac / build machine):
#   - docker installed and running
#   - doctl installed + authenticated (doctl auth init)
#   - doctl registry login (or this script can attempt it)
#
# This script does NOT deploy to the droplet by default.
# After push, it prints the next command to run:
#   ./deploy/scripts/container-deploy.sh <env>
#
# Optional:
#   Set DOCR_REGISTRY in your shell OR in deploy/docker/.env.<env>
#   Example:
#     export DOCR_REGISTRY="caseflow-registry"
# ============================================================

ENVIRONMENT="${1:-staging}"  # staging | production

# Resolve script + repo root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Load droplet ip env var (optional for later steps / consistency)
# Not required for build/push, but keeps your workflow uniform.
source "$SCRIPT_DIR/../load-droplet-ip.sh" "$ENVIRONMENT" || true

# Pick env file path
ENV_FILE="$REPO_ROOT/deploy/docker/.env.${ENVIRONMENT}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "❌ Missing env file: $ENV_FILE"
  echo "Create it from template:"
  echo "  cp $REPO_ROOT/deploy/docker/.env.template $ENV_FILE"
  exit 1
fi

# Helper: read KEY=value from env file (without executing arbitrary code)
get_env_value() {
  local key="$1"
  # shellcheck disable=SC2002
  cat "$ENV_FILE" \
    | grep -E "^${key}=" \
    | tail -n 1 \
    | cut -d'=' -f2- \
    | tr -d '\r' \
    | sed 's/^"\(.*\)"$/\1/' \
    | sed "s/^'\(.*\)'$/\1/"
}

API_IMAGE="$(get_env_value "API_IMAGE")"
WEB_IMAGE="$(get_env_value "WEB_IMAGE")"

if [[ -z "${API_IMAGE}" || -z "${WEB_IMAGE}" ]]; then
  echo "❌ API_IMAGE or WEB_IMAGE is missing in $ENV_FILE"
  echo "Make sure your file includes lines like:"
  echo "  API_IMAGE=registry.digitalocean.com/<REGISTRY>/mental-health-app-api:${ENVIRONMENT}"
  echo "  WEB_IMAGE=registry.digitalocean.com/<REGISTRY>/mental-health-app-web:${ENVIRONMENT}"
  exit 1
fi

echo "========================================"
echo "Build & Push to DOCR"
echo "========================================"
echo "Environment:  $ENVIRONMENT"
echo "Env file:     $ENV_FILE"
echo "API_IMAGE:    $API_IMAGE"
echo "WEB_IMAGE:    $WEB_IMAGE"
echo "Repo root:    $REPO_ROOT"
echo "========================================"
echo

# Basic checks
command -v docker >/dev/null 2>&1 || { echo "❌ docker not found"; exit 1; }

# Attempt to ensure DOCR auth is available
if command -v doctl >/dev/null 2>&1; then
  echo "✅ doctl found"
  echo "Ensuring docker is logged into DOCR..."
  # This is safe to run repeatedly
  doctl registry login >/dev/null 2>&1 || {
    echo "⚠️  doctl registry login failed."
    echo "If push fails, run manually:"
    echo "  doctl auth init"
    echo "  doctl registry login"
  }
else
  echo "⚠️  doctl not found."
  echo "If your docker push fails, install & login:"
  echo "  brew install doctl"
  echo "  doctl auth init"
  echo "  doctl registry login"
fi

# Build images
cd "$REPO_ROOT"

echo
echo "----------------------------------------"
echo "Building API image..."
echo "----------------------------------------"
docker build -f Dockerfile.api -t "$API_IMAGE" .

echo
echo "----------------------------------------"
echo "Building WEB image..."
echo "----------------------------------------"
docker build -f Dockerfile.web -t "$WEB_IMAGE" .

# Push images
echo
echo "----------------------------------------"
echo "Pushing API image..."
echo "----------------------------------------"
docker push "$API_IMAGE"

echo
echo "----------------------------------------"
echo "Pushing WEB image..."
echo "----------------------------------------"
docker push "$WEB_IMAGE"

echo
echo "✅ Build & push complete."
echo
echo "Next step (deploy to droplet):"
echo "  $REPO_ROOT/deploy/scripts/container-deploy.sh $ENVIRONMENT"
echo
