#!/bin/bash
# ============================================================================
# Consolidated Containerized Deployment Script
# ============================================================================
# This script performs complete containerized deployment including:
# 1. Building Docker images (API + Web)
# 2. Pushing to DigitalOcean Container Registry
# 3. Setting up droplet with Docker/Docker Compose
# 4. Generating SSL certificates
# 5. Creating .env file
# 6. Running database migrations
# 7. Seeding database
# 8. Setting up Ollama and pulling models
# 9. Starting all containers
#
# Usage:
#   Staging (with local DB container):   ./consolidated-container-deploy.sh staging
#   Production (with managed DB):        ./consolidated-container-deploy.sh production --managed-db "connection_string"
# ============================================================================

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
ENVIRONMENT="${1:-staging}"  # staging | production
DEPLOYMENT_MODE="local-db"   # local-db | managed-db
MANAGED_DB_CONNECTION_STRING=""
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_DIR="/opt/mental-health-app"
# Registry configuration - update this to match your DigitalOcean registry
REGISTRY_NAME="${DOCR_REGISTRY:-cha-registry}"  # Set DOCR_REGISTRY env var or update default

# Parse arguments
shift || true
while [[ $# -gt 0 ]]; do
    case $1 in
        --managed-db)
            DEPLOYMENT_MODE="managed-db"
            if [ -z "$2" ]; then
                echo -e "${RED}ERROR: --managed-db requires a connection string${NC}"
                echo "Usage: $0 $ENVIRONMENT --managed-db \"server=host;port=3306;database=db;user=user;password=pass\""
                exit 1
            fi
            MANAGED_DB_CONNECTION_STRING="$2"
            shift 2
            ;;
        *)
            echo -e "${RED}ERROR: Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

# Load droplet IP
source "$SCRIPT_DIR/load-droplet-ip.sh" "$ENVIRONMENT"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Consolidated Containerized Deployment${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Configuration:${NC}"
echo -e "  Environment: ${YELLOW}$ENVIRONMENT${NC}"
echo -e "  Droplet IP: ${YELLOW}$DROPLET_IP${NC}"
echo -e "  Deployment Mode: ${YELLOW}$DEPLOYMENT_MODE${NC}"
if [ "$DEPLOYMENT_MODE" = "managed-db" ]; then
    echo -e "  Managed DB: ${YELLOW}Yes${NC}"
else
    echo -e "  Local DB Container: ${YELLOW}Yes${NC}"
fi
echo -e "  Registry: ${YELLOW}registry.digitalocean.com/$REGISTRY_NAME${NC}"
echo ""

# Check prerequisites
if [ ! -f "$SSH_KEY_PATH" ]; then
    echo -e "${RED}ERROR: SSH key not found at $SSH_KEY_PATH${NC}"
    exit 1
fi

command -v docker >/dev/null 2>&1 || { echo -e "${RED}ERROR: docker not found. Please install Docker.${NC}"; exit 1; }

# Test SSH connection
echo -e "${YELLOW}Testing SSH connection...${NC}"
if ! ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no -o ConnectTimeout=5 "$DROPLET_USER@$DROPLET_IP" "echo 'SSH connection successful'" >/dev/null 2>&1; then
    echo -e "${RED}ERROR: Cannot connect to $DROPLET_IP${NC}"
    exit 1
fi
echo -e "${GREEN}✅ SSH connection successful${NC}"
echo ""

# ============================================================================
# Step 1: Build and Push Docker Images (on server for speed)
# ============================================================================
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Step 1: Building and Pushing Images${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Image tags - Using single repository with tags (for free tier registry limit of 1 repo)
# Format: registry.digitalocean.com/REGISTRY_NAME/REPO_NAME:TAG
API_IMAGE="registry.digitalocean.com/$REGISTRY_NAME/mental-health-app:api-$ENVIRONMENT"
WEB_IMAGE="registry.digitalocean.com/$REGISTRY_NAME/mental-health-app:web-$ENVIRONMENT"

echo "API Image:  $API_IMAGE"
echo "Web Image:  $WEB_IMAGE"
echo ""
echo -e "${BLUE}Building on server (native AMD64) for much faster builds...${NC}"
echo ""

# Create build directory on server
BUILD_DIR="/tmp/mental-health-app-build"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "rm -rf $BUILD_DIR && mkdir -p $BUILD_DIR"

echo "Copying source code to server (this may take a minute)..."
# Use tar for efficient transfer (exclude macOS metadata and respect .dockerignore)
# Note: We need deploy/nginx/nginx.conf for Dockerfile.web, so we exclude deploy but include nginx.conf
cd "$(dirname "$0")/.."
tar --exclude-from=.dockerignore \
    --exclude='._*' \
    --exclude='.DS_Store' \
    --exclude='.AppleDouble' \
    --exclude='deploy' \
    -czf - . | ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "cd $BUILD_DIR && tar -xzf -"

# Copy nginx.conf separately (needed for Dockerfile.web)
echo "Copying nginx.conf (needed for web image)..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "mkdir -p $BUILD_DIR/deploy/nginx"
scp -i "$SSH_KEY_PATH" deploy/nginx/nginx.conf "$DROPLET_USER@$DROPLET_IP:$BUILD_DIR/deploy/nginx/"

echo "Copying Dockerfiles and .dockerignore..."
scp -i "$SSH_KEY_PATH" Dockerfile.api "$DROPLET_USER@$DROPLET_IP:$BUILD_DIR/"
scp -i "$SSH_KEY_PATH" Dockerfile.web "$DROPLET_USER@$DROPLET_IP:$BUILD_DIR/"
scp -i "$SSH_KEY_PATH" .dockerignore "$DROPLET_USER@$DROPLET_IP:$BUILD_DIR/" 2>/dev/null || true

echo "Cleaning up macOS metadata files on server..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    cd $BUILD_DIR
    # Remove macOS metadata files (._* and .DS_Store)
    find . -name "._*" -type f -delete 2>/dev/null || true
    find . -name ".DS_Store" -type f -delete 2>/dev/null || true
    find . -name ".AppleDouble" -type d -exec rm -rf {} + 2>/dev/null || true
    echo "✅ Cleaned up macOS metadata files"
ENDSSH

echo "Logging into DigitalOcean Container Registry on server..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "REGISTRY_NAME=$REGISTRY_NAME bash -s" << 'ENDSSH'
    set -e
    
    # Clear any old registry authentication and cached images (fixes "blob unknown" errors after registry recreation)
    echo "Clearing old registry cache..."
    docker logout registry.digitalocean.com 2>/dev/null || true
    
    # Remove any cached images that reference the old registry
    docker images | grep "registry.digitalocean.com/$REGISTRY_NAME" | awk '{print $3}' | xargs -r docker rmi -f 2>/dev/null || true
    
    # Install doctl if not present
    if ! command -v doctl >/dev/null 2>&1; then
        echo "Installing doctl..."
        cd /tmp
        wget -q https://github.com/digitalocean/doctl/releases/download/v1.104.0/doctl-1.104.0-linux-amd64.tar.gz || \
        wget -q https://github.com/digitalocean/doctl/releases/latest/download/doctl-1.104.0-linux-amd64.tar.gz || {
            echo "⚠️  Failed to download doctl, will try docker login instead"
        }
        if [ -f doctl-*.tar.gz ]; then
            tar xf doctl-*.tar.gz
            mv doctl /usr/local/bin/doctl
            chmod +x /usr/local/bin/doctl
            rm -f doctl-*.tar.gz
            echo "✅ doctl installed"
        fi
    fi
    
    # Try doctl first, then docker login
    echo "Attempting to login to DigitalOcean Container Registry..."
    LOGIN_SUCCESS=false
    
    if command -v doctl >/dev/null 2>&1; then
        echo "Trying doctl registry login..."
        if doctl registry login 2>&1; then
            echo "✅ Successfully logged in via doctl"
            LOGIN_SUCCESS=true
        else
            echo "⚠️  doctl registry login failed"
            echo "   doctl needs to be authenticated first"
            echo "   You can run: doctl auth init (requires API token)"
        fi
    fi
    
    # If doctl failed, check if we can use existing docker credentials
    if [ "$LOGIN_SUCCESS" = false ]; then
        echo "Checking for existing Docker registry credentials..."
        if [ -f /root/.docker/config.json ]; then
            echo "Found existing Docker config, testing authentication..."
            # Try a simple registry operation to verify auth
            if docker manifest inspect "registry.digitalocean.com/$REGISTRY_NAME/mental-health-app:api-staging" >/dev/null 2>&1 || \
               docker pull "registry.digitalocean.com/$REGISTRY_NAME/mental-health-app:api-staging" >/dev/null 2>&1; then
                echo "✅ Existing authentication works"
                LOGIN_SUCCESS=true
            fi
        fi
    fi
    
    # Verify authentication by trying to access the registry
    if [ "$LOGIN_SUCCESS" = true ]; then
        echo "Verifying authentication..."
        # Try a simple registry operation to verify auth
        if docker manifest inspect "registry.digitalocean.com/$REGISTRY_NAME/mental-health-app:api-staging" >/dev/null 2>&1 || \
           docker pull "registry.digitalocean.com/$REGISTRY_NAME/mental-health-app:api-staging" >/dev/null 2>&1; then
            echo "✅ Authentication verified"
        else
            echo "⚠️  Authentication verification failed"
            echo "   This might be OK if the image doesn't exist yet"
        fi
    else
        echo ""
        echo "❌ ERROR: Could not authenticate with DigitalOcean Container Registry"
        echo ""
        echo "You need to manually login. Options:"
        echo ""
        echo "Option 1: Use doctl (recommended)"
        echo "  ssh root@$DROPLET_IP"
        echo "  doctl auth init"
        echo "  doctl registry login"
        echo ""
        echo "Option 2: Use docker login directly"
        echo "  ssh root@$DROPLET_IP"
        echo "  docker login registry.digitalocean.com"
        echo "  (You'll need your DigitalOcean API token)"
        echo ""
        echo "Get your API token from: https://cloud.digitalocean.com/account/api/tokens"
        echo ""
        echo "Then re-run this deployment script."
        exit 1
    fi
ENDSSH

echo ""
echo "Building API image on server (native AMD64 - much faster)..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "API_IMAGE=$API_IMAGE BUILD_DIR=$BUILD_DIR bash -s" << 'ENDSSH'
    set -e
    cd "$BUILD_DIR"
    echo "Building API image (using --no-cache to avoid registry blob issues)..."
    docker build -f Dockerfile.api --no-cache -t "$API_IMAGE" . || {
        echo "❌ Failed to build API image"
        exit 1
    }
    echo "Pushing API image to registry..."
    docker push "$API_IMAGE" || {
        echo "❌ Failed to push API image"
        echo "⚠️  If you see 'blob unknown to registry', try:"
        echo "   docker logout registry.digitalocean.com"
        echo "   docker login registry.digitalocean.com"
        exit 1
    }
    echo "✅ API image built and pushed successfully"
ENDSSH

echo ""
echo "Building Web image on server (native AMD64 - much faster)..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "WEB_IMAGE=$WEB_IMAGE BUILD_DIR=$BUILD_DIR bash -s" << 'ENDSSH'
    set -e
    cd "$BUILD_DIR"
    echo "Building Web image (using --no-cache to avoid registry blob issues)..."
    docker build -f Dockerfile.web --no-cache -t "$WEB_IMAGE" . || {
        echo "❌ Failed to build Web image"
        exit 1
    }
    echo "Pushing Web image to registry..."
    docker push "$WEB_IMAGE" || {
        echo "❌ Failed to push Web image"
        echo "⚠️  If you see 'blob unknown to registry', try:"
        echo "   docker logout registry.digitalocean.com"
        echo "   docker login registry.digitalocean.com"
        exit 1
    }
    echo "✅ Web image built and pushed successfully"
ENDSSH

echo ""
echo "Cleaning up build directory on server..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "rm -rf $BUILD_DIR" || true

echo -e "${GREEN}✅ Images built and pushed successfully${NC}"
echo ""

# ============================================================================
# Step 2: Setup Droplet (Docker, Docker Compose, etc.)
# ============================================================================
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Step 2: Setting Up Droplet${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    export DEBIAN_FRONTEND=noninteractive
    
    # Install Docker if not present
    if ! command -v docker &> /dev/null; then
        echo "Installing Docker..."
        apt-get update -y
        apt-get install -y ca-certificates curl gnupg lsb-release
        install -m 0755 -d /etc/apt/keyrings
        curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
        chmod a+r /etc/apt/keyrings/docker.gpg
        echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
        apt-get update -y
        apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
        systemctl enable docker
        systemctl start docker
        echo "✅ Docker installed"
    else
        echo "✅ Docker already installed"
    fi
    
    # Install Docker Compose if not present (standalone)
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        echo "Installing Docker Compose..."
        apt-get install -y docker-compose-plugin || {
            # Fallback to standalone docker-compose
            curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
            chmod +x /usr/local/bin/docker-compose
        }
        echo "✅ Docker Compose installed"
    else
        echo "✅ Docker Compose already installed"
    fi
    
    # Login to DigitalOcean Container Registry
    echo "Logging into DigitalOcean Container Registry..."
    if command -v doctl >/dev/null 2>&1; then
        echo "Logging in via doctl..."
        doctl registry login || {
            echo "⚠️  doctl registry login failed"
            echo "   Please ensure doctl is configured with: doctl auth init"
            echo "   Or manually login: docker login registry.digitalocean.com"
        }
    elif [ -f /root/.docker/config.json ]; then
        echo "✅ Docker already configured"
    else
        echo "⚠️  Please ensure docker is logged into DOCR on the droplet"
        echo "   Run: docker login registry.digitalocean.com"
        echo "   Or: doctl registry login (if doctl is installed)"
    fi
    
    # Create app directory
    mkdir -p /opt/mental-health-app/certs
    mkdir -p /opt/mental-health-app/data
    
    echo "✅ Droplet setup complete"
ENDSSH

# ============================================================================
# Step 3: Generate SSL Certificates
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 3: Generating SSL Certificates${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    set -e
    
    if [ ! -f /opt/mental-health-app/certs/server.crt ] || [ ! -f /opt/mental-health-app/certs/server.key ]; then
        echo "Generating self-signed SSL certificates..."
        openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
            -keyout /opt/mental-health-app/certs/server.key \
            -out /opt/mental-health-app/certs/server.crt \
            -subj "/C=US/ST=State/L=City/O=Organization/CN=$DROPLET_IP" \
            -addext "subjectAltName=IP:$DROPLET_IP,DNS:$DROPLET_IP"
        
        chmod 600 /opt/mental-health-app/certs/server.key
        chmod 644 /opt/mental-health-app/certs/server.crt
        echo "✅ SSL certificates generated"
    else
        echo "✅ SSL certificates already exist"
    fi
ENDSSH

# ============================================================================
# Step 4: Create .env File
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 4: Creating .env File${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check if .env file exists on droplet - if so, reuse existing passwords
# This prevents password changes on existing deployments
EXISTING_ENV=$(ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "cat /opt/mental-health-app/.env 2>/dev/null" || echo "")

if [ -n "$EXISTING_ENV" ]; then
    echo "✅ Found existing .env file - reusing existing passwords"
    # Extract existing passwords from .env file
    DB_ROOT_PASSWORD=$(echo "$EXISTING_ENV" | grep "^MYSQL_ROOT_PASSWORD=" | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
    DB_PASSWORD=$(echo "$EXISTING_ENV" | grep "^MYSQL_PASSWORD=" | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
    JWT_KEY=$(echo "$EXISTING_ENV" | grep "^JWT_KEY=" | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
    
    # If passwords are empty, generate new ones
    if [ -z "$DB_ROOT_PASSWORD" ]; then
        echo "⚠️  MYSQL_ROOT_PASSWORD not found in existing .env, generating new one"
        DB_ROOT_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
    else
        echo "✅ Reusing existing MYSQL_ROOT_PASSWORD (${DB_ROOT_PASSWORD:0:3}***)"
    fi
    
    if [ -z "$DB_PASSWORD" ]; then
        echo "⚠️  MYSQL_PASSWORD not found in existing .env, generating new one"
        DB_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
    else
        echo "✅ Reusing existing MYSQL_PASSWORD (${DB_PASSWORD:0:3}***)"
    fi
    
    if [ -z "$JWT_KEY" ]; then
        echo "⚠️  JWT_KEY not found in existing .env, generating new one"
        JWT_KEY=$(openssl rand -base64 48 | tr -d "=+/" | cut -c1-48)
    else
        echo "✅ Reusing existing JWT_KEY (${JWT_KEY:0:3}***)"
    fi
else
    echo "📝 No existing .env file found - generating new passwords"
    # Generate new passwords for fresh deployment
    DB_ROOT_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
    DB_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
    JWT_KEY=$(openssl rand -base64 48 | tr -d "=+/" | cut -c1-48)
    
    # For first-time deployment, you can optionally use a fixed password
    # Uncomment the line below if you want to use "UthmanBasima70" for the root password:
    # DB_ROOT_PASSWORD="UthmanBasima70"
fi

# Database configuration
if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
    DB_NAME="customerhealthdb"
    DB_USER="mentalhealth_user"
    MYSQL_CONN="server=mysql;port=3306;database=$DB_NAME;user=$DB_USER;password=$DB_PASSWORD"
else
    # Extract from managed connection string
    DB_NAME=$(echo "$MANAGED_DB_CONNECTION_STRING" | sed -n 's/.*database=\([^;]*\).*/\1/p' || echo "customerhealthdb")
    MYSQL_CONN="$MANAGED_DB_CONNECTION_STRING"
fi

export MYSQL_CONN
export ConnectionStrings__MySQL="$MYSQL_CONN"

# Determine ASPNETCORE_ENVIRONMENT value based on ENVIRONMENT
if [ "$ENVIRONMENT" = "staging" ]; then
    ASPNETCORE_ENV_VALUE="Staging"
else
    ASPNETCORE_ENV_VALUE="Production"
fi

# Load secrets from secrets.env if available (BEFORE creating ENV_CONTENT)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/secrets.env" ]; then
    echo "Loading secrets from secrets.env..."
    source "$SCRIPT_DIR/secrets.env"
fi

# Set defaults if not provided via secrets.env
HUGGINGFACE_API_KEY=${HUGGINGFACE_API_KEY:-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}
HUGGINGFACE_BIOMISTRAL_MODEL_URL=${HUGGINGFACE_BIOMISTRAL_MODEL_URL:-https://api-inference.huggingface.co/models/medalpaca/medalpaca-7b}
HUGGINGFACE_MEDITRON_MODEL_URL=${HUGGINGFACE_MEDITRON_MODEL_URL:-https://api-inference.huggingface.co/models/epfl-llm/meditron-7b}
OPENAI_API_KEY=${OPENAI_API_KEY:-sk-your-actual-openai-api-key-here}

# Create .env file on droplet
# Generate .env content locally first to ensure variables are expanded correctly
ENV_CONTENT="# Environment
ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENV_VALUE
ENVIRONMENT=$ENVIRONMENT

# Docker Images
API_IMAGE=$API_IMAGE
WEB_IMAGE=$WEB_IMAGE

# Database Configuration
MYSQL_CONN=\"$MYSQL_CONN\"
ConnectionStrings__MySQL=\"$MYSQL_CONN\"
MYSQL_ROOT_PASSWORD=$DB_ROOT_PASSWORD
MYSQL_DB=$DB_NAME
MYSQL_USER=$DB_USER
MYSQL_PASSWORD=$DB_PASSWORD

# JWT
JWT_KEY=$JWT_KEY

# Redis (using container name)
REDIS_CONNECTION_STRING=redis:6379

# Ollama (using container name)
OLLAMA_BASE_URL=http://ollama:11434

# S3/DigitalOcean Spaces
S3_ACCESS_KEY=DO00Z6VU8Q38KXLFZ7V4
S3_SECRET_KEY=ZDK61LfGdaqu5FpTcKnUfK8GNSW+cTSSbK8vK8GnMno
S3_BUCKET=mentalhealth-content
S3_REGION=sfo3
S3_SERVICE_URL=https://sfo3.digitaloceanspaces.com
S3_FOLDER=content/

# Vonage
VONAGE_ENABLED=true
VONAGE_API_KEY=c7dc2f50
VONAGE_API_SECRET=ZsknGg4RbD1fBI4B
VONAGE_FROM_NUMBER=+16148122119

# Agora
AGORA_APP_ID=efa11b3a7d05409ca979fb25a5b489ae
AGORA_USE_TOKENS=true
AGORA_APP_CERT=89ab54068fae46aeaf930ffd493e977b

# HuggingFace
HUGGINGFACE_API_KEY=$HUGGINGFACE_API_KEY
HUGGINGFACE_BIOMISTRAL_MODEL_URL=$HUGGINGFACE_BIOMISTRAL_MODEL_URL
HUGGINGFACE_MEDITRON_MODEL_URL=$HUGGINGFACE_MEDITRON_MODEL_URL

# OpenAI
OPENAI_API_KEY=$OPENAI_API_KEY"

# Write .env file to remote server
echo "$ENV_CONTENT" | ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "cat > /opt/mental-health-app/.env && chmod 600 /opt/mental-health-app/.env && echo '✅ .env file created'"

# Verify .env file was created correctly and fix MYSQL_CONN if incomplete
echo "Verifying .env file contents..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    if [ -f /opt/mental-health-app/.env ]; then
        echo "✅ .env file exists"
        echo ""
        echo "Key environment variables in .env file:"
        echo "ASPNETCORE_ENVIRONMENT: \$(grep '^ASPNETCORE_ENVIRONMENT=' /opt/mental-health-app/.env | cut -d'=' -f2 || echo 'NOT FOUND')"
        echo "ENVIRONMENT: \$(grep '^ENVIRONMENT=' /opt/mental-health-app/.env | cut -d'=' -f2 || echo 'NOT FOUND')"
        
        # Read MYSQL_CONN and check if it's complete
        MYSQL_CONN_VALUE=\$(grep '^MYSQL_CONN=' /opt/mental-health-app/.env | cut -d'=' -f2- || echo "")
        echo "MYSQL_CONN: \${MYSQL_CONN_VALUE:0:80}..."
        
        # Check if MYSQL_CONN is incomplete (missing database, user, or password)
        if [ -n "\$MYSQL_CONN_VALUE" ]; then
            if ! echo "\$MYSQL_CONN_VALUE" | grep -q "database=" || \
               ! echo "\$MYSQL_CONN_VALUE" | grep -q "user=" || \
               ! echo "\$MYSQL_CONN_VALUE" | grep -q "password="; then
                echo "⚠️  WARNING: MYSQL_CONN appears incomplete!"
                echo "Reconstructing from individual components..."
                
                # Read individual components
                DB_NAME_VAL=\$(grep '^MYSQL_DB=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "customerhealthdb")
                DB_USER_VAL=\$(grep '^MYSQL_USER=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "mentalhealth_user")
                DB_PASSWORD_VAL=\$(grep '^MYSQL_PASSWORD=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
                
                if [ -n "\$DB_NAME_VAL" ] && [ -n "\$DB_USER_VAL" ] && [ -n "\$DB_PASSWORD_VAL" ]; then
                    # Reconstruct full connection string
                    NEW_MYSQL_CONN="server=mysql;port=3306;database=\$DB_NAME_VAL;user=\$DB_USER_VAL;password=\$DB_PASSWORD_VAL"
                    
                    # Update .env file
                    sed -i "s|^MYSQL_CONN=.*|MYSQL_CONN=\$NEW_MYSQL_CONN|" /opt/mental-health-app/.env
                    sed -i "s|^ConnectionStrings__MySQL=.*|ConnectionStrings__MySQL=\$NEW_MYSQL_CONN|" /opt/mental-health-app/.env
                    
                    echo "✅ Updated MYSQL_CONN in .env file"
                    echo "New MYSQL_CONN: \${NEW_MYSQL_CONN:0:80}..."
                else
                    echo "❌ ERROR: Cannot reconstruct - missing components"
                    echo "  DB_NAME: \${DB_NAME_VAL:-NOT SET}"
                    echo "  DB_USER: \${DB_USER_VAL:-NOT SET}"
                    echo "  DB_PASSWORD: \${DB_PASSWORD_VAL:+SET (hidden)}"
                    exit 1
                fi
            else
                echo "✅ MYSQL_CONN appears complete"
            fi
        else
            echo "❌ ERROR: MYSQL_CONN is empty in .env file!"
            exit 1
        fi
        
        echo ""
        echo "JWT_KEY: \$(grep '^JWT_KEY=' /opt/mental-health-app/.env | cut -d'=' -f2- | head -c 20 || echo 'NOT FOUND')..."
        echo ""
        echo "Verifying MYSQL_CONN after fix:"
        grep '^MYSQL_CONN=' /opt/mental-health-app/.env | cut -d'=' -f2- | head -c 80
        echo "..."
    else
        echo "❌ ERROR: .env file was not created!"
        exit 1
    fi
ENDSSH

# ============================================================================
# Step 5: Copy Docker Compose and Nginx Config
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 5: Copying Configuration Files${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

scp -i "$SSH_KEY_PATH" "$SCRIPT_DIR/docker/docker-compose.yml" "$DROPLET_USER@$DROPLET_IP:/opt/mental-health-app/docker-compose.yml"
# Note: nginx.conf is already copied into the web image via Dockerfile.web
# We don't need to copy it separately, but we can keep this for reference
# scp -i "$SSH_KEY_PATH" "$SCRIPT_DIR/nginx/nginx.conf" "$DROPLET_USER@$DROPLET_IP:/opt/mental-health-app/nginx.conf"

# Update docker-compose.yml to conditionally include mysql service
if [ "$DEPLOYMENT_MODE" = "managed-db" ]; then
    echo "Updating docker-compose.yml to exclude MySQL service (using managed DB)..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
        # Remove mysql from api depends_on
        sed -i '/depends_on:/,/redis:/{ /mysql:/d; }' /opt/mental-health-app/docker-compose.yml
        # Remove mysql service block
        sed -i '/^  mysql:/,/^$/d' /opt/mental-health-app/docker-compose.yml
        # Remove mysql_data volume
        sed -i '/mysql_data:/d' /opt/mental-health-app/docker-compose.yml
ENDSSH
fi

echo -e "${GREEN}✅ Configuration files copied${NC}"

# ============================================================================
# Step 6: Pull Images and Start Containers (Initial)
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 6: Starting Containers${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Before starting containers, ensure .env file has correct MYSQL_CONN
echo "Verifying .env file has complete MYSQL_CONN before starting containers..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    if [ ! -f /opt/mental-health-app/.env ]; then
        echo "❌ ERROR: .env file not found!"
        exit 1
    fi
    
    # Read current MYSQL_CONN
    MYSQL_CONN_CURRENT=$(grep '^MYSQL_CONN=' /opt/mental-health-app/.env | cut -d'=' -f2- || echo "")
    
    # Check if it's complete
    if [ -z "$MYSQL_CONN_CURRENT" ] || ! echo "$MYSQL_CONN_CURRENT" | grep -q "database=" || ! echo "$MYSQL_CONN_CURRENT" | grep -q "user=" || ! echo "$MYSQL_CONN_CURRENT" | grep -q "password="; then
        echo "⚠️  MYSQL_CONN is incomplete: '${MYSQL_CONN_CURRENT:0:80}'"
        echo "Reconstructing from individual components..."
        
        # Read individual components
        DB_NAME_VAL=$(grep '^MYSQL_DB=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "customerhealthdb")
        DB_USER_VAL=$(grep '^MYSQL_USER=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "mentalhealth_user")
        DB_PASSWORD_VAL=$(grep '^MYSQL_PASSWORD=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
        
        if [ -z "$DB_NAME_VAL" ] || [ -z "$DB_USER_VAL" ] || [ -z "$DB_PASSWORD_VAL" ]; then
            echo "❌ ERROR: Cannot reconstruct - missing components"
            exit 1
        fi
        
        # Reconstruct full connection string
        FIXED_MYSQL_CONN="server=mysql;port=3306;database=$DB_NAME_VAL;user=$DB_USER_VAL;password=$DB_PASSWORD_VAL"
        
        # Update .env file
        sed -i "s|^MYSQL_CONN=.*|MYSQL_CONN=$FIXED_MYSQL_CONN|" /opt/mental-health-app/.env
        sed -i "s|^ConnectionStrings__MySQL=.*|ConnectionStrings__MySQL=$FIXED_MYSQL_CONN|" /opt/mental-health-app/.env
        
        echo "✅ Fixed MYSQL_CONN in .env file: ${FIXED_MYSQL_CONN:0:80}..."
    else
        echo "✅ MYSQL_CONN is complete: ${MYSQL_CONN_CURRENT:0:80}..."
    fi
ENDSSH

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    cd /opt/mental-health-app
    
    echo "Verifying .env file exists and is valid..."
    if [ ! -f .env ]; then
        echo "❌ ERROR: .env file not found at /opt/mental-health-app/.env"
        exit 1
    fi
    
    echo "Checking .env file contents..."
    echo "API_IMAGE from .env:"
    grep "^API_IMAGE=" .env || echo "⚠️  API_IMAGE not found in .env"
    echo "WEB_IMAGE from .env:"
    grep "^WEB_IMAGE=" .env || echo "⚠️  WEB_IMAGE not found in .env"
    
    # Export .env variables to current shell
    set -a
    source .env
    set +a
    
    echo "API_IMAGE value: $API_IMAGE"
    echo "WEB_IMAGE value: $WEB_IMAGE"
    
    if [ -z "$API_IMAGE" ] || [ -z "$WEB_IMAGE" ]; then
        echo "❌ ERROR: API_IMAGE or WEB_IMAGE is empty in .env file"
        echo "Contents of .env file:"
        cat .env
        exit 1
    fi
    
    echo "Removing old containers to avoid conflicts..."
    docker compose --env-file .env down 2>/dev/null || true
    docker container prune -f 2>/dev/null || true
    
    echo "Pulling images from registry..."
    docker compose --env-file .env pull || {
        echo "⚠️  Pull failed. Make sure docker is logged into DOCR:"
        echo "   docker login registry.digitalocean.com"
        echo "   Or: doctl registry login"
        exit 1
    }
    
    echo "Starting containers..."
    docker compose --env-file .env up -d --remove-orphans
    
    echo "Waiting for services to be ready..."
    sleep 10
    
    echo "✅ Containers started"
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    
    # Verify ASPNETCORE_ENVIRONMENT is set in API container
    echo ""
    echo "Verifying ASPNETCORE_ENVIRONMENT in API container..."
    API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -E "api" | grep -v "web" | head -1 || echo "")
    if [ -n "$API_CONTAINER" ]; then
        CONTAINER_ENV=$(docker exec "$API_CONTAINER" sh -c 'echo "$ASPNETCORE_ENVIRONMENT"' 2>/dev/null || echo "")
        if [ -z "$CONTAINER_ENV" ]; then
            echo "❌ ERROR: ASPNETCORE_ENVIRONMENT is not set in API container!"
            echo "This will cause the app to not load appsettings.Staging.json or appsettings.Production.json"
            echo ""
            echo "Checking docker-compose environment variables..."
            docker compose --env-file .env config | grep -A 5 "ASPNETCORE_ENVIRONMENT" || echo "Not found in compose config"
            exit 1
        else
            echo "✅ ASPNETCORE_ENVIRONMENT in container: $CONTAINER_ENV"
        fi
    else
        echo "⚠️  WARNING: Could not find API container to verify environment variable"
    fi
    
    echo ""
    echo "Checking for failed/exited containers..."
    FAILED_CONTAINERS=$(docker ps -a --filter "status=exited" --format "{{.Names}}" || echo "")
    if [ -n "$FAILED_CONTAINERS" ]; then
        echo "⚠️  WARNING: Some containers have exited:"
        echo "$FAILED_CONTAINERS"
        echo ""
        
        # Check specifically for API container
        API_CONTAINER=$(echo "$FAILED_CONTAINERS" | grep -i api | head -1 || docker ps -a --format "{{.Names}}" | grep -i api | head -1 || echo "")
        if [ -n "$API_CONTAINER" ]; then
            echo "❌ API container has exited: $API_CONTAINER"
            echo "Container status:"
            docker ps -a | grep "$API_CONTAINER"
            echo ""
            echo "API container logs (last 100 lines):"
            docker logs "$API_CONTAINER" --tail 100 2>&1
            echo ""
            echo "Checking docker compose logs for API service..."
            docker compose --env-file .env logs api --tail 50 2>&1
        fi
    fi
    
    # Verify API container is actually running (not web container)
    if ! docker ps --format "{{.Names}}" | grep -E "api" | grep -v "web" | grep -q .; then
        echo ""
        echo "❌ ERROR: API container is not running!"
        echo "Checking docker compose status..."
        docker compose --env-file .env ps
        echo ""
        echo "Attempting to start API service specifically..."
        docker compose --env-file .env up -d api
        sleep 5
        if ! docker ps --format "{{.Names}}" | grep -qE "api"; then
            echo "❌ API container still not running after start attempt"
            echo "Docker compose logs for API:"
            docker compose --env-file .env logs api --tail 100
            exit 1
        fi
    fi
    
    echo ""
    echo "Checking API container health..."
    # Find API container name (may vary if scaled) - specifically exclude "web" container
    API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -E "api" | grep -v "web" | head -1 || echo "")
    
    if [ -z "$API_CONTAINER" ]; then
        echo "❌ ERROR: No running API container found!"
        echo ""
        echo "Checking for stopped/exited API containers..."
        API_CONTAINER=$(docker ps -a --format "{{.Names}}" | grep -E "api" | grep -v "web" | head -1 || echo "")
        if [ -n "$API_CONTAINER" ]; then
            echo "Found stopped API container: $API_CONTAINER"
            echo "Container status:"
            docker ps -a | grep "$API_CONTAINER"
            echo ""
            echo "API container logs (last 100 lines):"
            docker logs "$API_CONTAINER" --tail 100 2>&1
            echo ""
            echo "Attempting to start API container..."
            docker start "$API_CONTAINER" || echo "Failed to start container"
            sleep 5
            if docker ps | grep -q "$API_CONTAINER"; then
                echo "✅ API container started"
            else
                echo "❌ API container failed to start"
                exit 1
            fi
        else
            echo "❌ No API container found at all!"
            echo "All containers:"
            docker ps -a
            echo ""
            echo "Checking docker compose status..."
            docker compose --env-file .env ps
            echo ""
            echo "Attempting to start API service..."
            docker compose --env-file .env up -d api
            sleep 5
            API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -E "api" | head -1 || echo "")
            if [ -z "$API_CONTAINER" ]; then
                echo "❌ API container still not running after start attempt"
                docker compose --env-file .env logs api --tail 50
                exit 1
            fi
        fi
    fi
    
    echo "Found API container: $API_CONTAINER"
    
    # Wait for API to be ready (up to 60 seconds)
    API_READY=false
    for i in {1..30}; do
        # Try to connect to API health endpoint
        if docker exec "$API_CONTAINER" curl -s http://localhost:5262/api/health > /dev/null 2>&1; then
            echo "✅ API container is healthy"
            API_READY=true
            break
        fi
        if [ $i -eq 30 ]; then
            echo "⚠️  API container may not be ready yet"
            break
        fi
        sleep 2
    done
    
    if [ "$API_READY" = false ]; then
        echo "⚠️  Warning: API health check failed"
        echo "Checking API container logs..."
        docker logs "$API_CONTAINER" --tail 50 2>&1 || echo "Could not get API logs"
        echo ""
        echo "Checking API container status..."
        docker ps -a | grep "$API_CONTAINER" || echo "API container not found"
    fi
    
    echo ""
    echo "Checking nginx can reach API (using service name 'api')..."
    # Test from nginx container using service name (Docker Compose DNS)
    if docker exec mental-health-web curl -s http://api:5262/api/health > /dev/null 2>&1; then
        echo "✅ Nginx can reach API backend via service name"
    else
        echo "⚠️  Warning: Nginx cannot reach API backend via service name"
        echo "This may cause 502 errors. Checking network connectivity..."
        docker exec mental-health-web ping -c 2 api 2>&1 | head -5 || echo "Cannot ping API from nginx"
        echo ""
        echo "Checking if containers are on the same network..."
        docker network ls
        docker network inspect mental-health-app_internal 2>/dev/null | grep -A 10 "Containers" || echo "Could not inspect network"
    fi
    
    echo ""
    echo "Note: To scale API containers, use:"
    echo "   docker compose --env-file .env up -d --scale api=3"
ENDSSH

# ============================================================================
# Step 7: Setup Database (if local-db)
# ============================================================================
if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
    echo -e "\n${GREEN}========================================${NC}"
    echo -e "${GREEN}Step 7: Setting Up Local Database${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo ""
    
    echo "Waiting for MySQL container to be ready..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "DB_NAME=$DB_NAME DB_USER=$DB_USER DB_PASSWORD=$DB_PASSWORD bash -s" << 'ENDSSH'
        set -e
        
        # Read MySQL root password from .env file (this is what the container was initialized with)
        if [ -f /opt/mental-health-app/.env ]; then
            DB_ROOT_PASSWORD=$(grep "^MYSQL_ROOT_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2 | tr -d '"' | tr -d "'")
        else
            echo "❌ ERROR: .env file not found. Cannot get MySQL root password."
            exit 1
        fi
        
        if [ -z "$DB_ROOT_PASSWORD" ]; then
            echo "❌ ERROR: MySQL root password is empty"
            exit 1
        fi
        
        # Wait for MySQL to be ready
        i=1
        while [ $i -le 60 ]; do
            # Try to connect - MySQL container root user can connect without password from within container
            # Don't use -h localhost, use default socket connection
            if docker exec mental-health-mysql mysqladmin ping --silent 2>/dev/null; then
                echo "✅ MySQL is ready"
                break
            fi
            # Try with password if needed
            if docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysqladmin ping --silent 2>/dev/null; then
                echo "✅ MySQL is ready (with password)"
                break
            fi
            if [ $i -eq 60 ]; then
                echo "❌ MySQL did not become ready"
                echo "Checking MySQL container status..."
                docker ps | grep mental-health-mysql || echo "MySQL container not running!"
                docker logs mental-health-mysql --tail 10
                exit 1
            fi
            sleep 2
            i=$((i + 1))
        done
        
        # Create database and user if they don't exist
        # MySQL container allows root without password from within container by default
        # Try without password first (this is the standard MySQL container behavior)
        echo "Creating database and user..."
        
        # Test connection without password first
        if docker exec mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
            # Root can connect without password - use that
            echo "Connecting to MySQL without password (standard container behavior)..."
            docker exec mental-health-mysql mysql -uroot -e "CREATE DATABASE IF NOT EXISTS $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" 2>&1 | grep -v "Warning" || true
            docker exec mental-health-mysql mysql -uroot -e "CREATE USER IF NOT EXISTS '$DB_USER'@'%' IDENTIFIED BY '$DB_PASSWORD';" 2>&1 | grep -v "Warning" || true
            docker exec mental-health-mysql mysql -uroot -e "GRANT ALL PRIVILEGES ON $DB_NAME.* TO '$DB_USER'@'%';" 2>&1 | grep -v "Warning" || true
            docker exec mental-health-mysql mysql -uroot -e "FLUSH PRIVILEGES;" 2>&1 | grep -v "Warning" || true
            # Fix authentication plugin to mysql_native_password for compatibility
            echo "Setting authentication plugin to mysql_native_password..."
            docker exec mental-health-mysql mysql -uroot -e "ALTER USER '$DB_USER'@'%' IDENTIFIED WITH mysql_native_password BY '$DB_PASSWORD';" 2>&1 | grep -v "Warning" || true
            docker exec mental-health-mysql mysql -uroot -e "FLUSH PRIVILEGES;" 2>&1 | grep -v "Warning" || true
            echo "✅ Database and user created (without password)"
        else
            # If that failed, try with password
            echo "Connecting to MySQL with password..."
            docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "CREATE DATABASE IF NOT EXISTS $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" 2>&1 | grep -v "Warning" || true
            docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "CREATE USER IF NOT EXISTS '$DB_USER'@'%' IDENTIFIED BY '$DB_PASSWORD';" 2>&1 | grep -v "Warning" || true
            docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "GRANT ALL PRIVILEGES ON $DB_NAME.* TO '$DB_USER'@'%';" 2>&1 | grep -v "Warning" || true
            docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "FLUSH PRIVILEGES;" 2>&1 | grep -v "Warning" || true
            # Fix authentication plugin to mysql_native_password for compatibility
            echo "Setting authentication plugin to mysql_native_password..."
            docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "ALTER USER '$DB_USER'@'%' IDENTIFIED WITH mysql_native_password BY '$DB_PASSWORD';" 2>&1 | grep -v "Warning" || true
            docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "FLUSH PRIVILEGES;" 2>&1 | grep -v "Warning" || true
            echo "✅ Database and user created (with password)"
        fi
        
        # Verify database was created
        if docker exec mental-health-mysql mysql -uroot -e "SHOW DATABASES LIKE '$DB_NAME';" 2>/dev/null | grep -q "$DB_NAME"; then
            echo "✅ Database '$DB_NAME' verified"
        else
            echo "⚠️  Warning: Could not verify database creation"
        fi
        
        # Ensure authentication plugin is set correctly (even if user already existed)
        # This fixes compatibility issues with MySQL 8.0+ default caching_sha2_password
        # We always run this to ensure the plugin is correct, even on subsequent deployments
        echo "Ensuring authentication plugin is set to mysql_native_password..."
        if docker exec mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
            # Check if user exists first
            if docker exec mental-health-mysql mysql -uroot -e "SELECT User FROM mysql.user WHERE User='$DB_USER' AND Host='%';" 2>/dev/null | grep -q "$DB_USER"; then
                docker exec mental-health-mysql mysql -uroot -e "ALTER USER '$DB_USER'@'%' IDENTIFIED WITH mysql_native_password BY '$DB_PASSWORD';" 2>&1 | grep -v "Warning" || true
                docker exec mental-health-mysql mysql -uroot -e "FLUSH PRIVILEGES;" 2>&1 | grep -v "Warning" || true
                echo "✅ Authentication plugin updated to mysql_native_password"
            else
                echo "⚠️  User '$DB_USER'@'%' does not exist yet, will be set when user is created"
            fi
        else
            # Check if user exists first
            if docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "SELECT User FROM mysql.user WHERE User='$DB_USER' AND Host='%';" 2>/dev/null | grep -q "$DB_USER"; then
                docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "ALTER USER '$DB_USER'@'%' IDENTIFIED WITH mysql_native_password BY '$DB_PASSWORD';" 2>&1 | grep -v "Warning" || true
                docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "FLUSH PRIVILEGES;" 2>&1 | grep -v "Warning" || true
                echo "✅ Authentication plugin updated to mysql_native_password"
            else
                echo "⚠️  User '$DB_USER'@'%' does not exist yet, will be set when user is created"
            fi
        fi
ENDSSH
fi

# ============================================================================
# Step 8: Run Database Migrations
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 8: Running Database Migrations${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Generate migration SQL locally
cd "$REPO_ROOT/SM_MentalHealthApp.Server"

MIGRATION_SQL_FILE="$SCRIPT_DIR/migration.sql"
MIGRATION_APPLIED=false

if command -v dotnet >/dev/null 2>&1 && dotnet ef --version &>/dev/null 2>&1; then
    echo "Generating migration SQL..."
    
    if dotnet ef migrations script 0 20251219144202_InitialCleanBaseline --idempotent --output "$MIGRATION_SQL_FILE" 2>&1; then
        if [ -f "$MIGRATION_SQL_FILE" ] && [ -s "$MIGRATION_SQL_FILE" ]; then
            echo "✅ Migration SQL generated ($(wc -l < "$MIGRATION_SQL_FILE" | tr -d ' ') lines)"
            scp -i "$SSH_KEY_PATH" "$MIGRATION_SQL_FILE" "$DROPLET_USER@$DROPLET_IP:/tmp/migration.sql"
            
            # Apply migration
            echo "Applying migration SQL to database..."
            ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "DB_NAME=$DB_NAME DEPLOYMENT_MODE=$DEPLOYMENT_MODE bash -s" << 'ENDSSH'
                set -e
                
                # Read MySQL root password from .env file
                if [ -f /opt/mental-health-app/.env ]; then
                    DB_ROOT_PASSWORD=$(grep "^MYSQL_ROOT_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2 | tr -d '"' | tr -d "'")
                else
                    echo "❌ ERROR: .env file not found"
                    exit 1
                fi
                
                if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
                    # Check if migration SQL contains DELIMITER (stored procedures)
                    if grep -q "DELIMITER\|MigrationsScript" /tmp/migration.sql; then
                        echo "⚠️  Migration SQL contains DELIMITER statements (stored procedures)"
                        echo "Extracting SQL statements from stored procedures..."
                        
                        # Determine which password to use for MySQL connection
                        MYSQL_PWD_TO_USE=""
                        if docker exec mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
                            echo "Using no-password connection"
                            MYSQL_PWD_TO_USE=""
                        elif docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
                            echo "Using .env password"
                            MYSQL_PWD_TO_USE="$DB_ROOT_PASSWORD"
                        else
                            echo "Using fallback password"
                            MYSQL_PWD_TO_USE="UthmanBasima70"
                        fi
                        
                        # Extract SQL using Python on the server (not in container)
                        python3 << 'PYTHON_SCRIPT' > /tmp/migration_clean.sql 2> /tmp/migration_extract_errors.log
import re
import sys

try:
    with open('/tmp/migration.sql', 'r') as f:
        content = f.read()
    
    if not content:
        print("ERROR: /tmp/migration.sql is empty", file=sys.stderr)
        sys.exit(1)
    
    # Extract initial __EFMigrationsHistory table creation
    initial_table_pattern = r'^CREATE TABLE IF NOT EXISTS [`]__EFMigrationsHistory[`].*?;'
    initial_table_match = re.search(initial_table_pattern, content, re.MULTILINE | re.DOTALL)
    if initial_table_match:
        print(initial_table_match.group(0))
        print()
    
    # Extract SQL from stored procedures
    procedure_pattern = r'IF NOT EXISTS\(SELECT 1 FROM [`]__EFMigrationsHistory[`] WHERE [`]MigrationId[`] = [^)]+\) THEN\s+(.*?)\s+END IF;'
    matches = list(re.finditer(procedure_pattern, content, re.MULTILINE | re.DOTALL))
    
    if not matches:
        print(f"ERROR: No stored procedure blocks found", file=sys.stderr)
        sys.exit(1)
    
    all_statements = []
    for match in matches:
        sql_block = match.group(1).strip()
        # Remove IF NOT EXISTS wrapper and extract statements
        statements = re.split(r';\s*(?=\w)', sql_block)
        for stmt in statements:
            stmt = stmt.strip()
            if stmt and not stmt.startswith('IF NOT EXISTS'):
                all_statements.append(stmt)
    
    # Extract INSERT INTO __EFMigrationsHistory
    history_insert_pattern = r'INSERT INTO [`]__EFMigrationsHistory[`].*?;'
    history_insert_match = re.search(history_insert_pattern, content, re.MULTILINE | re.DOTALL)
    if history_insert_match:
        all_statements.append(history_insert_match.group(0))
    
    # Output statements separated by double newline
    for stmt in all_statements:
        if stmt.strip():
            print(stmt.strip())
            print()
            
except Exception as e:
    print(f"ERROR: {str(e)}", file=sys.stderr)
    import traceback
    traceback.print_exc(file=sys.stderr)
    sys.exit(1)
PYTHON_SCRIPT
                        
                        if [ -f /tmp/migration_extract_errors.log ] && [ -s /tmp/migration_extract_errors.log ]; then
                            echo "❌ Python extraction failed:"
                            cat /tmp/migration_extract_errors.log
                            echo "Falling back to dotnet ef database update..."
                            SQL_METHOD="DOTNET_EF"
                        elif [ -f /tmp/migration_clean.sql ] && [ -s /tmp/migration_clean.sql ]; then
                            echo "✅ Extracted SQL statements ($(wc -l < /tmp/migration_clean.sql | tr -d ' ') lines)"
                            echo "Applying extracted SQL statements..."
                            
                            # Apply using Python script on server, connecting to MySQL container
                            python3 << APPLY_SQL
import subprocess
import sys
import os

# Determine MySQL connection command
if '$MYSQL_PWD_TO_USE':
    MYSQL_CMD = ['docker', 'exec', '-i', '-e', 'MYSQL_PWD=$MYSQL_PWD_TO_USE', 'mental-health-mysql', 'mysql', '-uroot', '$DB_NAME']
else:
    MYSQL_CMD = ['docker', 'exec', '-i', 'mental-health-mysql', 'mysql', '-uroot', '$DB_NAME']

ERROR_COUNT = 0
STATEMENT_COUNT = 0

try:
    with open('/tmp/migration_clean.sql', 'r') as f:
        content = f.read()
    
    statements = [s.strip() for s in content.split('\n\n') if s.strip()]
    
    print(f"Found {len(statements)} SQL statements to apply")
    
    for i, stmt in enumerate(statements, 1):
        STATEMENT_COUNT = i
        proc = subprocess.Popen(
            MYSQL_CMD,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        stdout, stderr = proc.communicate(input=stmt + '\n')
        
        if proc.returncode != 0:
            ERROR_COUNT += 1
            error_msg = stderr.strip()
            if 'ERROR' in error_msg.upper():
                # Ignore table already exists errors for __EFMigrationsHistory
                if '__EFMigrationsHistory' not in error_msg or 'already exists' not in error_msg:
                    print(f"⚠️  Error in statement {i}: {error_msg[:200]}")
        
        if i % 20 == 0:
            print(f"Applied {i}/{len(statements)} statements...")
    
    if ERROR_COUNT > 0:
        print(f"⚠️  Had {ERROR_COUNT} errors out of {STATEMENT_COUNT} statements")
    else:
        print(f"✅ Successfully applied {STATEMENT_COUNT} statements")
        
except Exception as e:
    print(f"ERROR: {str(e)}", file=sys.stderr)
    import traceback
    traceback.print_exc(file=sys.stderr)
    sys.exit(1)
APPLY_SQL
                            
                            if [ $? -eq 0 ]; then
                                echo "✅ Migration SQL applied successfully"
                                # Note: "already exists" errors are expected and OK - tables were already created
                            else
                                # Check if the errors are just "already exists" - that's OK
                                if echo "$MIGRATION_OUTPUT" 2>/dev/null | grep -qi "already exists\|duplicate"; then
                                    echo "✅ Migration completed (tables/indexes already exist - this is expected)"
                                else
                                    echo "❌ Migration application failed with unexpected errors"
                                    exit 1
                                fi
                            fi
                        else
                            echo "❌ Extracted SQL file is empty"
                            SQL_METHOD="DOTNET_EF"
                        fi
                    else
                        # No DELIMITER, apply directly
                        echo "Applying migration SQL directly..."
                        MIGRATION_OUTPUT=""
                        
                        # Try without password first
                        if docker exec mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
                            MIGRATION_OUTPUT=$(docker exec -i mental-health-mysql mysql -uroot "$DB_NAME" < /tmp/migration.sql 2>&1 || true)
                        elif docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
                            MIGRATION_OUTPUT=$(docker exec -i -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot "$DB_NAME" < /tmp/migration.sql 2>&1 || true)
                        else
                            MIGRATION_OUTPUT=$(docker exec -i -e MYSQL_PWD="UthmanBasima70" mental-health-mysql mysql -uroot "$DB_NAME" < /tmp/migration.sql 2>&1 || true)
                        fi
                        
                        if echo "$MIGRATION_OUTPUT" | grep -qi "ERROR"; then
                            echo "❌ Migration errors:"
                            echo "$MIGRATION_OUTPUT" | grep -i "ERROR" | head -5
                            exit 1
                        fi
                        echo "✅ Migration SQL executed"
                    fi
                    
                    # Verify tables were created using the same password that worked for migration
                    echo "Verifying tables were created..."
                    TABLE_COUNT="0"
                    
                    # Use the same password method that worked for migration (stored in MYSQL_PWD_TO_USE)
                    # Default to fallback password since that's what worked
                    VERIFY_PWD="${MYSQL_PWD_TO_USE:-UthmanBasima70}"
                    
                    if [ "$VERIFY_PWD" = "" ]; then
                        # No password
                        TABLE_COUNT=$(docker exec mental-health-mysql mysql -uroot -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$DB_NAME';" 2>/dev/null | tail -1 | tr -d ' ' | grep -E '^[0-9]+$' || echo "0")
                    else
                        # With password
                        TABLE_COUNT=$(docker exec -e MYSQL_PWD="$VERIFY_PWD" mental-health-mysql mysql -uroot -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$DB_NAME';" 2>/dev/null | tail -1 | tr -d ' ' | grep -E '^[0-9]+$' || echo "0")
                    fi
                    
                    # If still 0, try direct table listing
                    if [ "$TABLE_COUNT" = "0" ] || [ -z "$TABLE_COUNT" ]; then
                        echo "Trying direct table listing..."
                        if [ "$VERIFY_PWD" = "" ]; then
                            TABLE_LIST=$(docker exec mental-health-mysql mysql -uroot -e "USE $DB_NAME; SHOW TABLES;" 2>/dev/null | tail -n +2 | wc -l | tr -d ' ')
                        else
                            TABLE_LIST=$(docker exec -e MYSQL_PWD="$VERIFY_PWD" mental-health-mysql mysql -uroot -e "USE $DB_NAME; SHOW TABLES;" 2>/dev/null | tail -n +2 | wc -l | tr -d ' ')
                        fi
                        
                        if [ "$TABLE_LIST" -gt 0 ] 2>/dev/null; then
                            TABLE_COUNT="$TABLE_LIST"
                            echo "✅ Found $TABLE_COUNT tables using direct query"
                        fi
                    fi
                    
                    # Ensure TABLE_COUNT is a number
                    if ! echo "$TABLE_COUNT" | grep -qE '^[0-9]+$'; then
                        TABLE_COUNT="0"
                    fi
                    
                    if [ "$TABLE_COUNT" -gt 0 ] 2>/dev/null; then
                        echo "✅ Migrations verified successfully ($TABLE_COUNT tables found in database)"
                    else
                        echo "❌ ERROR: No tables found after migration! (count: '$TABLE_COUNT')"
                        echo "Checking database status with fallback password..."
                        docker exec -e MYSQL_PWD="UthmanBasima70" mental-health-mysql mysql -uroot -e "SHOW DATABASES;" 2>/dev/null || true
                        docker exec -e MYSQL_PWD="UthmanBasima70" mental-health-mysql mysql -uroot -e "USE $DB_NAME; SHOW TABLES;" 2>/dev/null || true
                        echo "Migration may have failed - tables should exist but don't"
                        exit 1
                    fi
                else
                    # For managed DB, we'd need to run from API container or use mysql client
                    echo "⚠️  For managed DB, migrations should be run from API container"
                    docker exec mental-health-api dotnet ef database update || echo "⚠️  Migration may need to be run manually"
                fi
ENDSSH
            MIGRATION_APPLIED=true
        else
            echo -e "${RED}❌ Migration SQL file is empty or not found${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  Could not generate migration SQL${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  dotnet ef not available locally${NC}"
fi

# Fallback: Try running migrations from API container if SQL method failed
if [ "$MIGRATION_APPLIED" = false ] && [ "$DEPLOYMENT_MODE" = "local-db" ]; then
    echo -e "${YELLOW}Attempting to run migrations from API container...${NC}"
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
        set -e
        # Copy migrations to API container
        docker cp /tmp/migrations mental-health-api:/app/Migrations 2>/dev/null || echo "⚠️  Could not copy migrations"
        
        # Run dotnet ef database update in API container
        docker exec mental-health-api dotnet ef database update || {
            echo "❌ Migration from container failed"
            exit 1
        }
        
        # Verify tables
        TABLE_COUNT=$(docker exec mental-health-mysql mysql -uroot -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'customerhealthdb';" 2>/dev/null | tail -1 | tr -d ' ')
        if [ "$TABLE_COUNT" -gt 0 ]; then
            echo "✅ Migrations applied from container ($TABLE_COUNT tables)"
        else
            echo "❌ ERROR: No tables found after migration!"
            exit 1
        fi
ENDSSH
fi

cd "$REPO_ROOT"

# ============================================================================
# Step 9: Seed Database
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 9: Seeding Database${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

if [ -f "$SCRIPT_DIR/Scripts121925/SeedingInitialConsolidatedScript.sh" ]; then
    echo "Copying seeding script..."
    scp -i "$SSH_KEY_PATH" "$SCRIPT_DIR/Scripts121925/SeedingInitialConsolidatedScript.sh" "$DROPLET_USER@$DROPLET_IP:/tmp/seed.sql"
    
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "DB_NAME=$DB_NAME DEPLOYMENT_MODE=$DEPLOYMENT_MODE bash -s" << 'ENDSSH'
        set -e
        
        # Determine which password to use (same method as migration)
        SEED_PWD=""
        if docker exec mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
            echo "Using no-password connection for seeding"
            SEED_PWD=""
        elif [ -f /opt/mental-health-app/.env ]; then
            DB_ROOT_PASSWORD=$(grep "^MYSQL_ROOT_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2 | tr -d '"' | tr -d "'")
            if docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot -e "SELECT 1;" 2>/dev/null > /dev/null; then
                echo "Using .env password for seeding"
                SEED_PWD="$DB_ROOT_PASSWORD"
            else
                echo "Using fallback password for seeding"
                SEED_PWD="UthmanBasima70"
            fi
        else
            echo "Using fallback password for seeding"
            SEED_PWD="UthmanBasima70"
        fi
        
        if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
            echo "Applying seeding SQL statements..."
            
            # Apply using Python script (same as old script) to handle multi-line INSERT statements
            python3 << APPLY_SEED_SQL
import subprocess
import sys
import os

# Determine MySQL connection command
if '$SEED_PWD':
    MYSQL_CMD = ['docker', 'exec', '-i', '-e', 'MYSQL_PWD=$SEED_PWD', 'mental-health-mysql', 'mysql', '-uroot', '$DB_NAME']
else:
    MYSQL_CMD = ['docker', 'exec', '-i', 'mental-health-mysql', 'mysql', '-uroot', '$DB_NAME']

ERROR_COUNT = 0
STATEMENT_COUNT = 0

try:
    with open('/tmp/seed.sql', 'r') as f:
        content = f.read()
    
    # Clean up content - preserve comments and empty lines for readability
    lines = [line for line in content.split('\n') if line.strip() or line.startswith('--')]
    content = '\n'.join(lines)
    
    # Split by semicolon, preserving multi-line statements
    statements = []
    current = []
    
    for line in content.split('\n'):
        stripped = line.strip()
        if not stripped:
            if current:
                current.append('')
            continue
        
        if stripped.startswith('--'):
            if current:
                current.append(line)
            continue
        
        current.append(line)
        
        if stripped.endswith(';'):
            stmt = '\n'.join(current)
            if stmt.strip():
                statements.append(stmt)
            current = []
    
    if current:
        stmt = '\n'.join(current)
        if stmt.strip():
            statements.append(stmt)
    
    print(f"Found {len(statements)} SQL statements to apply")
    
    for i, stmt in enumerate(statements, 1):
        STATEMENT_COUNT = i
        proc = subprocess.Popen(
            MYSQL_CMD,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        stdout, stderr = proc.communicate(input=stmt + '\n')
        
        if proc.returncode != 0:
            ERROR_COUNT += 1
            error_msg = stderr.strip()
            if 'ERROR' in error_msg.upper():
                # Ignore duplicate key errors (data already exists)
                if 'duplicate' not in error_msg.lower():
                    print(f"⚠️  Error in statement {i}: {error_msg[:300]}")
        
        if i % 20 == 0:
            print(f"Applied {i}/{len(statements)} statements...")
    
    if ERROR_COUNT > 0:
        print(f"⚠️  Seeding had {ERROR_COUNT} errors out of {STATEMENT_COUNT} statements")
    else:
        print(f"✅ Successfully applied {STATEMENT_COUNT} statements")
        
except Exception as e:
    print(f"ERROR: {str(e)}", file=sys.stderr)
    import traceback
    traceback.print_exc(file=sys.stderr)
    sys.exit(1)
APPLY_SEED_SQL
            
            if [ $? -eq 0 ]; then
                echo "✅ Seeding SQL applied successfully"
                
                # Verify data was seeded
                echo "Verifying seeded data..."
                if [ -z "$SEED_PWD" ]; then
                    ROLES_COUNT=$(docker exec mental-health-mysql mysql -uroot -e "USE $DB_NAME; SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 | tr -d ' ' || echo "0")
                    USERS_COUNT=$(docker exec mental-health-mysql mysql -uroot -e "USE $DB_NAME; SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -1 | tr -d ' ' || echo "0")
                else
                    ROLES_COUNT=$(docker exec -e MYSQL_PWD="$SEED_PWD" mental-health-mysql mysql -uroot -e "USE $DB_NAME; SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 | tr -d ' ' || echo "0")
                    USERS_COUNT=$(docker exec -e MYSQL_PWD="$SEED_PWD" mental-health-mysql mysql -uroot -e "USE $DB_NAME; SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -1 | tr -d ' ' || echo "0")
                fi
                
                if [ "$ROLES_COUNT" -gt 0 ] 2>/dev/null && [ "$USERS_COUNT" -gt 0 ] 2>/dev/null; then
                    echo "✅ Database seeded successfully (Roles: $ROLES_COUNT, Users: $USERS_COUNT)"
                else
                    echo "⚠️  Warning: Seeding completed but verification shows (Roles: $ROLES_COUNT, Users: $USERS_COUNT)"
                    echo "Data may have been seeded but counts are low, or verification query failed"
                fi
            else
                echo "❌ Seeding application failed"
                exit 1
            fi
        else
            echo "⚠️  For managed DB, seeding should be run from API container or manually"
        fi
ENDSSH
else
    echo -e "${YELLOW}⚠️  Seeding script not found${NC}"
fi

# ============================================================================
# Step 10: Setup Ollama and Pull Models
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 10: Setting Up Ollama${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    echo "Waiting for Ollama container to be ready..."
    i=1
    while [ $i -le 60 ]; do
        if curl -s http://127.0.0.1:11434/api/tags > /dev/null 2>&1; then
            echo "✅ Ollama is ready"
            break
        fi
        if [ $i -eq 60 ]; then
            echo "⚠️  Ollama did not become ready, but continuing..."
            break
        fi
        sleep 2
        i=$((i + 1))
    done
    
    echo "Pulling Ollama models (this may take several minutes)..."
    echo "Pulling tinyllama (fallback model)..."
    docker exec mental-health-ollama ollama pull tinyllama || echo "⚠️  Failed to pull tinyllama"
    
    echo "Attempting to pull qwen2.5:8b-instruct..."
    docker exec mental-health-ollama ollama pull qwen2.5:8b-instruct || echo "⚠️  qwen2.5:8b-instruct not available, using tinyllama"
    
    echo "Attempting to pull qwen2.5:4b-instruct..."
    docker exec mental-health-ollama ollama pull qwen2.5:4b-instruct || echo "⚠️  qwen2.5:4b-instruct not available, using tinyllama"
    
    echo "✅ Ollama models pulled"
ENDSSH

# ============================================================================
# Step 11: Verify API Container Health
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 11: Verifying API Container Health${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    
    echo "Checking API container status..."
    # Find API container name (may vary if scaled) - specifically exclude "web" container
    API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -E "api" | grep -v "web" | head -1 || echo "")
    
    # If not found in running, check stopped containers
    if [ -z "$API_CONTAINER" ]; then
        API_CONTAINER=$(docker ps -a --format "{{.Names}}" | grep -E "api" | grep -v "web" | head -1 || echo "")
        if [ -n "$API_CONTAINER" ]; then
            echo "⚠️  API container found but not running: $API_CONTAINER"
            echo "Container status:"
            docker ps -a | grep "$API_CONTAINER"
            echo ""
            echo "API container logs (why it stopped):"
            docker logs "$API_CONTAINER" --tail 100 2>&1
            echo ""
            echo "Attempting to start API container..."
            docker start "$API_CONTAINER" || {
                echo "❌ Failed to start API container"
                echo "Checking docker compose status..."
                docker compose --env-file .env ps
                exit 1
            }
            sleep 5
        fi
    fi
    
    if [ -z "$API_CONTAINER" ]; then
        echo "❌ ERROR: No API container found!"
        echo "All containers:"
        docker ps -a
        echo ""
        echo "Checking docker compose status..."
        docker compose --env-file .env ps
        exit 1
    fi
    
    echo "✅ API container is running: $API_CONTAINER"
    
    echo "Checking API health endpoint (from within container)..."
    if docker exec "$API_CONTAINER" curl -s http://localhost:5262/api/health > /dev/null 2>&1; then
        echo "✅ API health endpoint is responding"
    else
        echo "⚠️  Warning: API health endpoint not responding from within container"
        echo "Checking API logs..."
        docker logs "$API_CONTAINER" --tail 50 2>&1
    fi
    
    echo "Checking nginx can reach API (from nginx container using service name 'api')..."
    if docker exec mental-health-web curl -s http://api:5262/api/health > /dev/null 2>&1; then
        echo "✅ Nginx can reach API backend"
    else
        echo "❌ ERROR: Nginx cannot reach API backend - this will cause 502 errors!"
        echo ""
        echo "Diagnostics:"
        echo "1. Checking network connectivity from nginx to API..."
        docker exec mental-health-web ping -c 2 api 2>&1 | head -5 || echo "Cannot ping API"
        echo ""
        echo "2. Checking if containers are on the same network..."
        docker network inspect mental-health-app_internal 2>/dev/null | grep -A 10 "Containers" || echo "Network inspection failed"
        echo ""
        echo "3. Checking API container logs (last 50 lines)..."
        docker logs "$API_CONTAINER" --tail 50 2>&1
        echo ""
        echo "4. Checking nginx container logs..."
        docker logs mental-health-web --tail 20 2>&1
        echo ""
        echo "5. Testing direct connection to API from host..."
        docker exec "$API_CONTAINER" netstat -tlnp 2>/dev/null | grep 5262 || echo "Port 5262 not listening"
        exit 1
    fi
    
    echo "✅ API container is healthy and reachable"
ENDSSH

# ============================================================================
# Step 11: Sync Ollama Model Names with Database
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 11: Syncing Ollama Models with Database${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "DB_NAME=$DB_NAME DEPLOYMENT_MODE=$DEPLOYMENT_MODE bash -s" << 'ENDSSH'
    set -e
    
    # Read MySQL root password from .env file
    if [ -f /opt/mental-health-app/.env ]; then
        DB_ROOT_PASSWORD=$(grep "^MYSQL_ROOT_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2 | tr -d '"' | tr -d "'")
    else
        echo "⚠️  .env file not found, skipping model sync"
        exit 0
    fi
    
    # Get available models
    AVAILABLE_MODELS=$(curl -s http://127.0.0.1:11434/api/tags 2>/dev/null | python3 -c "import sys, json; data = json.load(sys.stdin); print('\n'.join([m.get('name', '') for m in data.get('models', [])]))" 2>/dev/null || echo "")
    
    if [ -z "$AVAILABLE_MODELS" ]; then
        echo "⚠️  No models found in Ollama, skipping sync"
        exit 0
    fi
    
    # Find correct models
    PRIMARY_MODEL=""
    if echo "$AVAILABLE_MODELS" | grep -qE "qwen2\.5.*8b.*instruct|qwen.*8b.*instruct"; then
        PRIMARY_MODEL=$(echo "$AVAILABLE_MODELS" | grep -E "qwen2\.5.*8b.*instruct|qwen.*8b.*instruct" | head -1 | tr -d '\n\r')
    elif echo "$AVAILABLE_MODELS" | grep -qE "qwen2\.5.*8b|qwen.*8b"; then
        PRIMARY_MODEL=$(echo "$AVAILABLE_MODELS" | grep -E "qwen2\.5.*8b|qwen.*8b" | head -1 | tr -d '\n\r')
    elif echo "$AVAILABLE_MODELS" | grep -q "tinyllama"; then
        PRIMARY_MODEL="tinyllama"
    else
        PRIMARY_MODEL=$(echo "$AVAILABLE_MODELS" | head -1 | tr -d '\n\r')
    fi
    
    SECONDARY_MODEL=""
    if echo "$AVAILABLE_MODELS" | grep -qE "qwen2\.5.*4b.*instruct|qwen.*4b.*instruct"; then
        SECONDARY_MODEL=$(echo "$AVAILABLE_MODELS" | grep -E "qwen2\.5.*4b.*instruct|qwen.*4b.*instruct" | head -1 | tr -d '\n\r')
    elif echo "$AVAILABLE_MODELS" | grep -qE "qwen2\.5.*4b|qwen.*4b"; then
        SECONDARY_MODEL=$(echo "$AVAILABLE_MODELS" | grep -E "qwen2\.5.*4b|qwen.*4b" | head -1 | tr -d '\n\r')
    elif echo "$AVAILABLE_MODELS" | grep -q "tinyllama"; then
        SECONDARY_MODEL="tinyllama"
    else
        SECONDARY_MODEL=$(echo "$AVAILABLE_MODELS" | head -1 | tr -d '\n\r')
    fi
    
    PRIMARY_MODEL=$(echo "$PRIMARY_MODEL" | sed 's/[^a-zA-Z0-9:._-]//g')
    SECONDARY_MODEL=$(echo "$SECONDARY_MODEL" | sed 's/[^a-zA-Z0-9:._-]//g')
    
    if [ -z "$PRIMARY_MODEL" ] || [ -z "$SECONDARY_MODEL" ]; then
        echo "⚠️  Could not determine model names"
        exit 0
    fi
    
    echo "Updating database with model names: $PRIMARY_MODEL, $SECONDARY_MODEL"
    
    if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
        docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot "$DB_NAME" -e "UPDATE AIModelConfigs SET ApiEndpoint='$PRIMARY_MODEL', UpdatedAt=NOW() WHERE Id=1;" 2>&1 | grep -v "Warning" || true
        docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot "$DB_NAME" -e "UPDATE AIModelConfigs SET ApiEndpoint='$SECONDARY_MODEL', UpdatedAt=NOW() WHERE Id=2;" 2>&1 | grep -v "Warning" || true
        docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot "$DB_NAME" -e "UPDATE AIModelConfigs SET IsActive=1 WHERE Id IN (1,2);" 2>&1 | grep -v "Warning" || true
        docker exec -e MYSQL_PWD="$DB_ROOT_PASSWORD" mental-health-mysql mysql -uroot "$DB_NAME" -e "UPDATE AIModelChains SET IsActive=1 WHERE Id=1;" 2>&1 | grep -v "Warning" || true
    else
        echo "⚠️  For managed DB, update model names manually in the database"
    fi
    
    echo "✅ Model names synced"
ENDSSH

# ============================================================================
# Step 12: Create appsettings.Staging.json or appsettings.Production.json
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 12: Creating appsettings File${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Create appsettings file on droplet and copy into API container
# Read .env file on remote server to get actual values (more reliable than passing via SSH)
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "ENVIRONMENT=$ENVIRONMENT bash -s" << 'ENDSSH'
    set -e
    
    # Read values from .env file on the droplet (most reliable)
    if [ -f /opt/mental-health-app/.env ]; then
        set -a
        source /opt/mental-health-app/.env
        set +a
        echo "✅ Loaded values from .env file"
        echo "MYSQL_CONN from sourcing: '${MYSQL_CONN}'"
        echo "MYSQL_CONN length: ${#MYSQL_CONN}"
        
        # Also read directly from file to compare
        MYSQL_CONN_FROM_FILE=$(grep "^MYSQL_CONN=" /opt/mental-health-app/.env | cut -d'=' -f2- || echo "")
        echo "MYSQL_CONN from file grep: '${MYSQL_CONN_FROM_FILE:0:80}'"
        
        # Use the file value if it's more complete
        if [ ${#MYSQL_CONN_FROM_FILE} -gt ${#MYSQL_CONN} ]; then
            echo "⚠️  File value is longer, using it instead"
            MYSQL_CONN="$MYSQL_CONN_FROM_FILE"
        fi
    else
        echo "❌ ERROR: .env file not found at /opt/mental-health-app/.env"
        exit 1
    fi
    set -e
    
    echo "Finding API container..."
    # Try multiple patterns to find API container
    API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -E "api|mental-health.*api" | grep -v "web" | head -1 || echo "")
    if [ -z "$API_CONTAINER" ]; then
        API_CONTAINER=$(docker ps -a --format "{{.Names}}" | grep -E "api|mental-health.*api" | grep -v "web" | head -1 || echo "")
    fi
    # Try docker compose ps format
    if [ -z "$API_CONTAINER" ]; then
        cd /opt/mental-health-app
        API_CONTAINER=$(docker compose --env-file .env ps --format json 2>/dev/null | python3 -c "import sys, json; containers = json.load(sys.stdin); api = [c for c in containers if 'api' in c.get('Service', '').lower() and 'web' not in c.get('Service', '').lower()]; print(api[0]['Name'] if api else '')" 2>/dev/null || echo "")
    fi
    # Last resort: list all containers
    if [ -z "$API_CONTAINER" ]; then
        echo "All containers:"
        docker ps -a --format "table {{.Names}}\t{{.Status}}"
        echo ""
        echo "Trying to find any container with 'api' in name..."
        API_CONTAINER=$(docker ps -a --format "{{.Names}}" | grep -i api | head -1 || echo "")
    fi
    
    if [ -z "$API_CONTAINER" ]; then
        echo "❌ ERROR: No API container found"
        echo "Available containers:"
        docker ps -a
        exit 1
    fi
    
    echo "Found API container: $API_CONTAINER"
    
    # Verify MYSQL_CONN is not empty and is complete
    if [ -z "$MYSQL_CONN" ]; then
        echo "❌ ERROR: MYSQL_CONN is empty!"
        echo "Reading from .env file..."
        if [ -f /opt/mental-health-app/.env ]; then
            MYSQL_CONN=$(grep "^MYSQL_CONN=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'")
            echo "MYSQL_CONN from .env: $MYSQL_CONN"
        fi
    fi
    
    # Check if MYSQL_CONN is incomplete (only has "server=mysql" without other parts)
    if [ -n "$MYSQL_CONN" ] && echo "$MYSQL_CONN" | grep -q "^server=mysql$" || echo "$MYSQL_CONN" | grep -q "^server=mysql;port=" && ! echo "$MYSQL_CONN" | grep -q "database="; then
        echo "⚠️  WARNING: MYSQL_CONN appears incomplete: '$MYSQL_CONN'"
        echo "Reconstructing from individual components..."
        
        # Read individual components from .env file
        if [ -f /opt/mental-health-app/.env ]; then
            DB_NAME_FROM_ENV=$(grep "^MYSQL_DB=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "customerhealthdb")
            DB_USER_FROM_ENV=$(grep "^MYSQL_USER=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "mentalhealth_user")
            DB_PASSWORD_FROM_ENV=$(grep "^MYSQL_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
            
            if [ -n "$DB_NAME_FROM_ENV" ] && [ -n "$DB_USER_FROM_ENV" ] && [ -n "$DB_PASSWORD_FROM_ENV" ]; then
                MYSQL_CONN="server=mysql;port=3306;database=$DB_NAME_FROM_ENV;user=$DB_USER_FROM_ENV;password=$DB_PASSWORD_FROM_ENV"
                echo "✅ Reconstructed MYSQL_CONN: ${MYSQL_CONN:0:50}..."
            else
                echo "❌ ERROR: Cannot reconstruct MYSQL_CONN - missing components"
                echo "  DB_NAME: ${DB_NAME_FROM_ENV:-NOT SET}"
                echo "  DB_USER: ${DB_USER_FROM_ENV:-NOT SET}"
                echo "  DB_PASSWORD: ${DB_PASSWORD_FROM_ENV:+SET (hidden)}"
                exit 1
            fi
        else
            echo "❌ ERROR: Cannot reconstruct - .env file not found"
            exit 1
        fi
    fi
    
    if [ -z "$MYSQL_CONN" ]; then
        echo "❌ ERROR: MYSQL_CONN is still empty after all attempts"
        exit 1
    fi
    
    # Verify JWT_KEY is not empty
    if [ -z "$JWT_KEY" ]; then
        echo "⚠️  WARNING: JWT_KEY is empty, reading from .env file..."
        if [ -f /opt/mental-health-app/.env ]; then
            JWT_KEY=$(grep "^JWT_KEY=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'")
        fi
    fi
    
    # Determine appsettings filename
    if [ "$ENVIRONMENT" = "staging" ]; then
        APPSETTINGS_FILE="appsettings.Staging.json"
    else
        APPSETTINGS_FILE="appsettings.Production.json"
    fi
    
    echo "Creating $APPSETTINGS_FILE in container..."
    echo "Using connection string: ${MYSQL_CONN:0:80}..." # Show first 80 chars for verification
    
    # Verify MYSQL_CONN has all required parts
    if ! echo "$MYSQL_CONN" | grep -q "database=" || ! echo "$MYSQL_CONN" | grep -q "user=" || ! echo "$MYSQL_CONN" | grep -q "password="; then
        echo "❌ ERROR: MYSQL_CONN is incomplete: '$MYSQL_CONN'"
        echo "It must contain: server, port, database, user, and password"
        exit 1
    fi
    
    # ALWAYS reconstruct MYSQL_CONN from individual components to ensure we have the actual password
    # Don't trust MYSQL_CONN from .env file - it might be incomplete or have CHANGE_ME
    echo "Reading database components from .env file to build connection string..."
    DB_NAME_FINAL=$(grep "^MYSQL_DB=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "customerhealthdb")
    DB_USER_FINAL=$(grep "^MYSQL_USER=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "mentalhealth_user")
    DB_PASSWORD_FINAL=$(grep "^MYSQL_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
    
    if [ -z "$DB_NAME_FINAL" ] || [ -z "$DB_USER_FINAL" ] || [ -z "$DB_PASSWORD_FINAL" ]; then
        echo "❌ ERROR: Cannot read database components from .env file"
        echo "  DB_NAME: ${DB_NAME_FINAL:-NOT SET}"
        echo "  DB_USER: ${DB_USER_FINAL:-NOT SET}"
        echo "  DB_PASSWORD: ${DB_PASSWORD_FINAL:+SET (hidden)}"
        exit 1
    fi
    
    # Build connection string with actual password
    MYSQL_CONN="server=mysql;port=3306;database=$DB_NAME_FINAL;user=$DB_USER_FINAL;password=$DB_PASSWORD_FINAL"
    echo "✅ Built MYSQL_CONN from components: ${MYSQL_CONN:0:80}..."
    
    # Read JWT_KEY directly from .env file
    JWT_KEY=$(grep "^JWT_KEY=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
    if [ -z "$JWT_KEY" ]; then
        echo "❌ ERROR: JWT_KEY is empty in .env file!"
        exit 1
    fi
    echo "✅ Read JWT_KEY from .env file (length: ${#JWT_KEY})"
    
    # Read API keys from .env file (they were set from secrets.env during .env file creation)
    HUGGINGFACE_API_KEY_VAL=$(grep "^HUGGINGFACE_API_KEY=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "*****no need to print api key*****")
    HUGGINGFACE_BIOMISTRAL_URL_VAL=$(grep "^HUGGINGFACE_BIOMISTRAL_MODEL_URL=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "https://api-inference.huggingface.co/models/medalpaca/medalpaca-7b")
    HUGGINGFACE_MEDITRON_URL_VAL=$(grep "^HUGGINGFACE_MEDITRON_MODEL_URL=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "https://api-inference.huggingface.co/models/epfl-llm/meditron-7b")
    OPENAI_API_KEY_VAL=$(grep "^OPENAI_API_KEY=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "sk-your-actual-openai-api-key-here")
    
    # Write values to temp files for Python to read (avoids shell escaping issues)
    echo "$MYSQL_CONN" > /tmp/mysql_conn.txt
    echo "$JWT_KEY" > /tmp/jwt_key.txt
    echo "$DB_NAME_FINAL" > /tmp/db_name.txt
    echo "$HUGGINGFACE_API_KEY_VAL" > /tmp/huggingface_api_key.txt
    echo "$HUGGINGFACE_BIOMISTRAL_URL_VAL" > /tmp/huggingface_biomistral_url.txt
    echo "$HUGGINGFACE_MEDITRON_URL_VAL" > /tmp/huggingface_meditron_url.txt
    echo "$OPENAI_API_KEY_VAL" > /tmp/openai_api_key.txt
    
    # Verify temp files were written correctly
    if [ ! -f /tmp/mysql_conn.txt ] || [ ! -s /tmp/mysql_conn.txt ]; then
        echo "❌ ERROR: Failed to write /tmp/mysql_conn.txt"
        exit 1
    fi
    if [ ! -f /tmp/jwt_key.txt ] || [ ! -s /tmp/jwt_key.txt ]; then
        echo "❌ ERROR: Failed to write /tmp/jwt_key.txt"
        exit 1
    fi
    echo "✅ Temp files created successfully"
    
    # Create JSON file using Python - read from temp files
    python3 << 'PYTHON_SCRIPT'
import json
import sys
import os

# Read values from temp files
try:
    with open('/tmp/mysql_conn.txt', 'r') as f:
        mysql_conn = f.read().strip()
    with open('/tmp/jwt_key.txt', 'r') as f:
        jwt_key = f.read().strip()
    with open('/tmp/db_name.txt', 'r') as f:
        db_name = f.read().strip() or "customerhealthdb"
    with open('/tmp/huggingface_api_key.txt', 'r') as f:
        huggingface_api_key = f.read().strip()
    with open('/tmp/huggingface_biomistral_url.txt', 'r') as f:
        huggingface_biomistral_url = f.read().strip()
    with open('/tmp/huggingface_meditron_url.txt', 'r') as f:
        huggingface_meditron_url = f.read().strip()
    with open('/tmp/openai_api_key.txt', 'r') as f:
        openai_api_key = f.read().strip()
except Exception as e:
    print(f"ERROR: Failed to read temp files: {e}", file=sys.stderr)
    sys.exit(1)

# Debug output
print(f"DEBUG: mysql_conn length: {len(mysql_conn)}", file=sys.stderr)
print(f"DEBUG: mysql_conn preview: {mysql_conn[:80] if mysql_conn else 'EMPTY'}...", file=sys.stderr)
print(f"DEBUG: jwt_key length: {len(jwt_key)}", file=sys.stderr)
print(f"DEBUG: db_name: {db_name}", file=sys.stderr)

# Verify connection string is not empty and complete
if not mysql_conn:
    print("ERROR: MYSQL_CONN is empty!", file=sys.stderr)
    sys.exit(1)

if 'database=' not in mysql_conn or 'user=' not in mysql_conn or 'password=' not in mysql_conn:
    print(f"ERROR: MYSQL_CONN is incomplete: '{mysql_conn[:100]}'", file=sys.stderr)
    print("It must contain: database=, user=, and password=", file=sys.stderr)
    sys.exit(1)

if not jwt_key:
    print("ERROR: JWT_KEY is empty!", file=sys.stderr)
    sys.exit(1)

appsettings = {
    "ConnectionStrings": {
        "MySQL": mysql_conn
    },
    "Kestrel": {
        "Endpoints": {
            "Http": {
                "Url": "http://0.0.0.0:5262"
            }
        }
    },
    "Jwt": {
        "Key": jwt_key,
        "Issuer": "SM_MentalHealthApp",
        "Audience": "SM_MentalHealthApp_Users"
    },
    "Redis": {
        "ConnectionString": "redis:6379"
    },
    "Ollama": {
        "BaseUrl": "http://ollama:11434"
    },
    "S3": {
        "AccessKey": "DO00Z6VU8Q38KXLFZ7V4",
        "SecretKey": "ZDK61LfGdaqu5FpTcKnUfK8GNSW+cTSSbK8vK8GnMno",
        "BucketName": "mentalhealth-content",
        "Region": "sfo3",
        "ServiceUrl": "https://sfo3.digitaloceanspaces.com",
        "Folder": "content/"
    },
    "Vonage": {
        "Enabled": True,
        "ApiKey": "c7dc2f50",
        "ApiSecret": "ZsknGg4RbD1fBI4B",
        "FromNumber": "+16148122119"
    },
    "Agora": {
        "AppId": "efa11b3a7d05409ca979fb25a5b489ae",
        "UseTokens": True,
        "AppCertificate": "89ab54068fae46aeaf930ffd493e977b"
    },
    "Email": {
        "Enabled": True,
        "SmtpHost": "smtp.gmail.com",
        "SmtpPort": 587,
        "SmtpUsername": "iqmkhan70@gmail.com",
        "SmtpPassword": "gbkounbbzauyoujj",
        "FromEmail": "iqmkhan70@gmail.com",
        "FromName": "Health App",
        "EnableSsl": True
    },
    "HuggingFace": {
        "ApiKey": huggingface_api_key,
        "BioMistralModelUrl": huggingface_biomistral_url,
        "MeditronModelUrl": huggingface_meditron_url
    },
    "OpenAI": {
        "ApiKey": openai_api_key
    },
    "PiiEncryption": {
        "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
    },
    "Encryption": {
        "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "AllowedHosts": "*"
}

# Write to temporary file
try:
    with open('/tmp/appsettings.json', 'w') as f:
        json.dump(appsettings, f, indent=2)
    
    # Verify file was written and has all required sections
    with open('/tmp/appsettings.json', 'r') as f:
        written_data = json.load(f)
    
    required_sections = ['ConnectionStrings', 'Kestrel', 'Jwt', 'Redis', 'Ollama', 'S3', 'Vonage', 'Agora', 'Email', 'HuggingFace', 'OpenAI', 'Encryption', 'Logging', 'AllowedHosts']
    missing = [s for s in required_sections if s not in written_data]
    
    if missing:
        print(f"ERROR: Missing sections in appsettings: {missing}", file=sys.stderr)
        sys.exit(1)
    
    # Verify connection string has actual password (not CHANGE_ME)
    conn_str = written_data.get('ConnectionStrings', {}).get('MySQL', '')
    if 'CHANGE_ME' in conn_str or 'password=' not in conn_str or conn_str.count('password=') == 0:
        print(f"ERROR: Connection string appears invalid: {conn_str[:100]}", file=sys.stderr)
        sys.exit(1)
    
    print("✅ appsettings file created with all required sections")
except Exception as e:
    print(f"ERROR: Failed to write appsettings file: {e}", file=sys.stderr)
    sys.exit(1)
PYTHON_SCRIPT
    
    # Verify the JSON file was created and is valid
    if [ ! -f /tmp/appsettings.json ]; then
        echo "❌ ERROR: Python script did not create /tmp/appsettings.json"
        exit 1
    fi
    
    # Verify JSON is valid and has all required sections
    echo "Verifying generated appsettings file..."
    if python3 -m json.tool /tmp/appsettings.json > /dev/null 2>&1; then
        echo "✅ JSON is valid"
        # Check for required sections
        MISSING_SECTIONS=""
        if ! python3 -c "import json; data = json.load(open('/tmp/appsettings.json')); print('OK' if 'ConnectionStrings' in data and 'MySQL' in data.get('ConnectionStrings', {}) else 'MISSING')" | grep -q "OK"; then
            MISSING_SECTIONS="$MISSING_SECTIONS ConnectionStrings"
        fi
        if ! python3 -c "import json; data = json.load(open('/tmp/appsettings.json')); print('OK' if 'Jwt' in data else 'MISSING')" | grep -q "OK"; then
            MISSING_SECTIONS="$MISSING_SECTIONS Jwt"
        fi
        if ! python3 -c "import json; data = json.load(open('/tmp/appsettings.json')); print('OK' if 'S3' in data else 'MISSING')" | grep -q "OK"; then
            MISSING_SECTIONS="$MISSING_SECTIONS S3"
        fi
        
        if [ -n "$MISSING_SECTIONS" ]; then
            echo "⚠️  WARNING: Missing sections in appsettings: $MISSING_SECTIONS"
        else
            echo "✅ All required sections present"
        fi
        
        # Show connection string from generated file
        echo ""
        echo "Connection string in generated file:"
        python3 -c "import json; data = json.load(open('/tmp/appsettings.json')); print(data.get('ConnectionStrings', {}).get('MySQL', 'NOT FOUND')[:100])"
    else
        echo "❌ ERROR: Generated JSON file is invalid!"
        echo "File contents:"
        cat /tmp/appsettings.json
        exit 1
    fi
    
    # Remove old file if it exists to ensure we're not appending
    docker exec "$API_CONTAINER" rm -f "/app/$APPSETTINGS_FILE" 2>/dev/null || true
    
    # Copy file into API container
    docker cp /tmp/appsettings.json "$API_CONTAINER:/app/$APPSETTINGS_FILE"
    
    # Verify file was copied and show connection string
    if docker exec "$API_CONTAINER" test -f "/app/$APPSETTINGS_FILE"; then
        echo "✅ $APPSETTINGS_FILE created in container"
        
        # Verify the file in the container has all required sections
        echo ""
        echo "Verifying file in container has all required sections..."
        # Use a more robust check - verify each section exists
        MISSING_IN_CONTAINER=""
        REQUIRED_SECTIONS="ConnectionStrings Kestrel Jwt Redis Ollama S3 Vonage Agora Email HuggingFace OpenAI Encryption Logging AllowedHosts"
        for section in $REQUIRED_SECTIONS; do
            # Check if section exists in JSON
            if ! docker exec "$API_CONTAINER" python3 -c "import json, sys; data = json.load(open('/app/$APPSETTINGS_FILE')); sys.exit(0 if '$section' in data else 1)" 2>/dev/null; then
                MISSING_IN_CONTAINER="$MISSING_IN_CONTAINER $section"
            fi
        done
        
        if [ -n "$MISSING_IN_CONTAINER" ]; then
            echo "❌ ERROR: Missing sections in container file: $MISSING_IN_CONTAINER"
            echo "File contents:"
            docker exec "$API_CONTAINER" cat "/app/$APPSETTINGS_FILE"
            exit 1
        fi
        
        echo "✅ All required sections present in container file"
        echo ""
        echo "Connection string in container file:"
        docker exec "$API_CONTAINER" python3 -c "import json, sys; data = json.load(open('/app/$APPSETTINGS_FILE')); print(data.get('ConnectionStrings', {}).get('MySQL', 'NOT FOUND')[:100])" || echo "Could not read connection string"
        
        # Verify password is not CHANGE_ME
        CONN_STR=$(docker exec "$API_CONTAINER" python3 -c "import json, sys; data = json.load(open('/app/$APPSETTINGS_FILE')); print(data.get('ConnectionStrings', {}).get('MySQL', ''))" 2>/dev/null || echo "")
        if echo "$CONN_STR" | grep -q "CHANGE_ME"; then
            echo "❌ ERROR: Connection string still contains CHANGE_ME!"
            echo "Connection string: ${CONN_STR:0:100}..."
            exit 1
        fi
        echo "✅ Connection string verified (no CHANGE_ME found)"
    else
        echo "❌ ERROR: Failed to create $APPSETTINGS_FILE in container"
        exit 1
    fi
    
    # Also verify environment variable is set (it might override appsettings)
    # IMPORTANT: Environment variables override appsettings.json, so we must ensure it's correct
    echo ""
    echo "Checking environment variable ConnectionStrings__MySQL in container:"
    ENV_CONN_STR=$(docker exec "$API_CONTAINER" sh -c 'echo "$ConnectionStrings__MySQL"' 2>/dev/null || echo "")
    
    # Check if connection string is incomplete
    if [ -z "$ENV_CONN_STR" ] || [ "$ENV_CONN_STR" = "server=mysql" ] || ! echo "$ENV_CONN_STR" | grep -q "database=" || ! echo "$ENV_CONN_STR" | grep -q "user=" || ! echo "$ENV_CONN_STR" | grep -q "password="; then
        echo "❌ ERROR: ConnectionStrings__MySQL environment variable is empty or incomplete: '$ENV_CONN_STR'"
        echo "This will override the appsettings file value with an empty/incomplete string."
        echo ""
        echo "Fixing .env file and recreating container..."
        
        # Always reconstruct from components to ensure we have the correct value
        DB_NAME_FIX=$(grep "^MYSQL_DB=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "customerhealthdb")
        DB_USER_FIX=$(grep "^MYSQL_USER=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "mentalhealth_user")
        DB_PASSWORD_FIX=$(grep "^MYSQL_PASSWORD=" /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
        
        if [ -z "$DB_NAME_FIX" ] || [ -z "$DB_USER_FIX" ] || [ -z "$DB_PASSWORD_FIX" ]; then
            echo "❌ ERROR: Cannot reconstruct - missing components"
            echo "  DB_NAME: ${DB_NAME_FIX:-NOT SET}"
            echo "  DB_USER: ${DB_USER_FIX:-NOT SET}"
            echo "  DB_PASSWORD: ${DB_PASSWORD_FIX:+SET (hidden)}"
            exit 1
        fi
        
        FIXED_MYSQL_CONN="server=mysql;port=3306;database=$DB_NAME_FIX;user=$DB_USER_FIX;password=$DB_PASSWORD_FIX"
        
        # Update .env file
        sed -i "s|^MYSQL_CONN=.*|MYSQL_CONN=$FIXED_MYSQL_CONN|" /opt/mental-health-app/.env
        sed -i "s|^ConnectionStrings__MySQL=.*|ConnectionStrings__MySQL=$FIXED_MYSQL_CONN|" /opt/mental-health-app/.env
        
        echo "✅ Fixed MYSQL_CONN in .env file"
        echo "New value: ${FIXED_MYSQL_CONN:0:80}..."
        echo ""
        echo "Recreating API container to pick up the fix..."
        cd /opt/mental-health-app
        
        # Verify .env file has the correct value before recreating
        echo "Verifying .env file has correct MYSQL_CONN..."
        ENV_MYSQL_CONN_CHECK=$(grep "^MYSQL_CONN=" /opt/mental-health-app/.env | cut -d'=' -f2- || echo "")
        if [ -z "$ENV_MYSQL_CONN_CHECK" ] || ! echo "$ENV_MYSQL_CONN_CHECK" | grep -q "database=" || ! echo "$ENV_MYSQL_CONN_CHECK" | grep -q "user=" || ! echo "$ENV_MYSQL_CONN_CHECK" | grep -q "password="; then
            echo "❌ ERROR: .env file still has incomplete MYSQL_CONN after fix attempt!"
            echo "MYSQL_CONN in .env: '${ENV_MYSQL_CONN_CHECK:0:80}'"
            exit 1
        fi
        echo "✅ .env file verified: ${ENV_MYSQL_CONN_CHECK:0:80}..."
        
        # Stop and remove the container to ensure fresh start
        echo "Stopping and removing API container..."
        docker compose --env-file .env stop api 2>/dev/null || true
        docker compose --env-file .env rm -f api 2>/dev/null || true
        
        # Wait a moment for cleanup
        sleep 2
        
        echo "Starting API container with updated .env file..."
        docker compose --env-file .env up -d api
        
        echo "Waiting 15 seconds for container to start..."
        sleep 15
        
        # Find the new container name (it might have changed)
        NEW_API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -E "api|mental-health.*api" | grep -v "web" | head -1 || echo "$API_CONTAINER")
        
        if [ -z "$NEW_API_CONTAINER" ]; then
            echo "❌ ERROR: Could not find API container after recreation"
            docker ps -a
            exit 1
        fi
        
        echo "Found API container: $NEW_API_CONTAINER"
        
        # Verify after restart - check both environment variable and that container is running
        NEW_ENV_CONN_STR=$(docker exec "$NEW_API_CONTAINER" sh -c 'echo "$ConnectionStrings__MySQL"' 2>/dev/null || echo "")
        if [ -n "$NEW_ENV_CONN_STR" ] && echo "$NEW_ENV_CONN_STR" | grep -q "database=" && echo "$NEW_ENV_CONN_STR" | grep -q "user=" && echo "$NEW_ENV_CONN_STR" | grep -q "password="; then
            echo "✅ ConnectionStrings__MySQL is now correct: ${NEW_ENV_CONN_STR:0:80}..."
        else
            echo "❌ ERROR: Still incorrect after fix: '$NEW_ENV_CONN_STR'"
            echo "Container environment variables:"
            docker exec "$NEW_API_CONTAINER" env | grep -i "connection\|mysql" || echo "Could not read env vars"
            echo ""
            echo "Checking .env file one more time:"
            grep "^MYSQL_CONN=" /opt/mental-health-app/.env || echo "MYSQL_CONN not found in .env"
            exit 1
        fi
    else
        echo "✅ ConnectionStrings__MySQL env var is set correctly: ${ENV_CONN_STR:0:80}..."
    fi
    
    # Check for multiple API containers (could cause port conflict)
    echo ""
    echo "Checking for multiple API containers..."
    ALL_API_CONTAINERS=$(docker ps -a --format "{{.Names}}" | grep -E "api|mental-health.*api" | grep -v "web" || echo "")
    API_COUNT=$(echo "$ALL_API_CONTAINERS" | wc -l | tr -d ' ')
    if [ "$API_COUNT" -gt 1 ]; then
        echo "⚠️  WARNING: Found $API_COUNT API containers, stopping all to avoid port conflicts..."
        echo "$ALL_API_CONTAINERS" | while read -r container; do
            if [ -n "$container" ]; then
                echo "  Stopping container: $container"
                docker stop "$container" 2>/dev/null || true
            fi
        done
        sleep 3
    fi
    
    # Stop API container properly to release port 5262
    echo ""
    echo "Stopping API container to release port 5262..."
    docker stop "$API_CONTAINER" || {
        echo "⚠️  Warning: Failed to stop container, trying docker compose stop..."
        cd /opt/mental-health-app
        docker compose --env-file .env stop api || echo "⚠️  Could not stop via compose"
    }
    
    # Wait for container to fully stop
    echo "Waiting 5 seconds for container to fully stop..."
    sleep 5
    
    # Kill any processes still using port 5262 inside the container (if container is still running)
    echo "Checking for processes using port 5262..."
    if docker ps --format "{{.Names}}" | grep -q "$API_CONTAINER"; then
        echo "⚠️  Container still running, attempting to kill processes on port 5262..."
        docker exec "$API_CONTAINER" sh -c "lsof -ti:5262 | xargs kill -9 2>/dev/null || true" || true
        docker stop "$API_CONTAINER" --time 10 || true
    fi
    
    # Start API container with new appsettings file
    echo "Starting API container with new configuration..."
    docker start "$API_CONTAINER" || {
        echo "⚠️  Warning: Failed to start container, trying docker compose start..."
        cd /opt/mental-health-app
        docker compose --env-file .env start api || {
            echo "⚠️  Start failed, trying up -d..."
            docker compose --env-file .env up -d api
        }
    }
    
    echo "Waiting 15 seconds for API to start..."
    sleep 15
    
    # Verify API is running with new config
    echo "Checking API container status..."
    if docker ps --format "{{.Names}}" | grep -q "$API_CONTAINER"; then
        echo "✅ API container is running"
        echo ""
        echo "Checking API logs for errors (last 30 lines)..."
        docker logs "$API_CONTAINER" --tail 30 2>&1
        echo ""
        echo "Checking for connection string errors specifically..."
        docker logs "$API_CONTAINER" --tail 50 2>&1 | grep -i "connection\|mysql\|database\|access denied" || echo "No connection-related errors in recent logs"
        echo ""
        echo "Checking if API is listening on port 5262..."
        if docker exec "$API_CONTAINER" netstat -tlnp 2>/dev/null | grep -q ":5262"; then
            echo "✅ API is listening on port 5262"
        else
            echo "⚠️  WARNING: API is not listening on port 5262"
        fi
    else
        echo "❌ ERROR: API container is not running after restart"
        echo "Container logs (last 100 lines):"
        docker logs "$API_CONTAINER" --tail 100 2>&1
        echo ""
        echo "Checking for other API containers..."
        docker ps -a | grep -i api
    fi
    
    # Clean up temp files
    rm -f /tmp/appsettings.json /tmp/mysql_conn.txt /tmp/jwt_key.txt /tmp/db_name.txt /tmp/huggingface_api_key.txt /tmp/huggingface_biomistral_url.txt /tmp/huggingface_meditron_url.txt /tmp/openai_api_key.txt
ENDSSH

# ============================================================================
# Step 13: Final Verification and Container Recreation
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 13: Final Verification and Container Recreation${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Final check: Ensure .env file is correct and recreate container if needed
echo "Performing final verification of connection string..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    cd /opt/mental-health-app
    
    # Find API container
    API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -E "api|mental-health.*api" | grep -v "web" | head -1 || echo "")
    if [ -z "$API_CONTAINER" ]; then
        API_CONTAINER=$(docker ps -a --format "{{.Names}}" | grep -E "api|mental-health.*api" | grep -v "web" | head -1 || echo "")
    fi
    
    if [ -z "$API_CONTAINER" ]; then
        echo "⚠️  No API container found, skipping verification"
        exit 0
    fi
    
    # Check current environment variable in container
    CURRENT_ENV_CONN=$(docker exec "$API_CONTAINER" sh -c 'echo "$ConnectionStrings__MySQL"' 2>/dev/null || echo "")
    
    # Check .env file
    ENV_FILE_CONN=$(grep '^MYSQL_CONN=' /opt/mental-health-app/.env | cut -d'=' -f2- || echo "")
    
    # Verify .env file has complete connection string
    if [ -z "$ENV_FILE_CONN" ] || ! echo "$ENV_FILE_CONN" | grep -q "database=" || ! echo "$ENV_FILE_CONN" | grep -q "user=" || ! echo "$ENV_FILE_CONN" | grep -q "password="; then
        echo "⚠️  .env file has incomplete MYSQL_CONN, fixing..."
        
        # Read individual components
        DB_NAME_VAL=$(grep '^MYSQL_DB=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "customerhealthdb")
        DB_USER_VAL=$(grep '^MYSQL_USER=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "mentalhealth_user")
        DB_PASSWORD_VAL=$(grep '^MYSQL_PASSWORD=' /opt/mental-health-app/.env | cut -d'=' -f2- | tr -d '"' | tr -d "'" || echo "")
        
        if [ -z "$DB_NAME_VAL" ] || [ -z "$DB_USER_VAL" ] || [ -z "$DB_PASSWORD_VAL" ]; then
            echo "❌ ERROR: Cannot reconstruct - missing components"
            exit 1
        fi
        
        FIXED_MYSQL_CONN="server=mysql;port=3306;database=$DB_NAME_VAL;user=$DB_USER_VAL;password=$DB_PASSWORD_VAL"
        
        # Update .env file
        sed -i "s|^MYSQL_CONN=.*|MYSQL_CONN=$FIXED_MYSQL_CONN|" /opt/mental-health-app/.env
        sed -i "s|^ConnectionStrings__MySQL=.*|ConnectionStrings__MySQL=$FIXED_MYSQL_CONN|" /opt/mental-health-app/.env
        
        echo "✅ Fixed .env file: ${FIXED_MYSQL_CONN:0:80}..."
        ENV_FILE_CONN="$FIXED_MYSQL_CONN"
    fi
    
    # Check if container environment variable matches .env file
    if [ -z "$CURRENT_ENV_CONN" ] || [ "$CURRENT_ENV_CONN" = "server=mysql" ] || ! echo "$CURRENT_ENV_CONN" | grep -q "database=" || ! echo "$CURRENT_ENV_CONN" | grep -q "user=" || ! echo "$CURRENT_ENV_CONN" | grep -q "password="; then
        echo "⚠️  Container has incorrect ConnectionStrings__MySQL: '${CURRENT_ENV_CONN:0:80}'"
        echo "Recreating container to pick up correct value from .env file..."
        
        # Stop and remove container completely
        docker compose --env-file .env stop api 2>/dev/null || true
        docker compose --env-file .env rm -f api 2>/dev/null || true
        sleep 2
        
        # Start fresh with updated .env file
        docker compose --env-file .env up -d api
        sleep 15
        
        # Find new container
        NEW_API_CONTAINER=$(docker ps --format "{{.Names}}" | grep -E "api|mental-health.*api" | grep -v "web" | head -1 || echo "")
        
        if [ -n "$NEW_API_CONTAINER" ]; then
            # Verify it's correct now
            NEW_ENV_CONN=$(docker exec "$NEW_API_CONTAINER" sh -c 'echo "$ConnectionStrings__MySQL"' 2>/dev/null || echo "")
            if [ -n "$NEW_ENV_CONN" ] && echo "$NEW_ENV_CONN" | grep -q "database=" && echo "$NEW_ENV_CONN" | grep -q "user=" && echo "$NEW_ENV_CONN" | grep -q "password="; then
                echo "✅ Container recreated successfully with correct connection string: ${NEW_ENV_CONN:0:80}..."
            else
                echo "❌ ERROR: Container still has incorrect connection string: '$NEW_ENV_CONN'"
                echo "This is a critical error - please check the .env file manually"
                exit 1
            fi
        else
            echo "❌ ERROR: Could not find API container after recreation"
            exit 1
        fi
    else
        echo "✅ Container has correct ConnectionStrings__MySQL: ${CURRENT_ENV_CONN:0:80}..."
        echo "Restarting container to ensure all changes are applied..."
        docker compose --env-file .env restart api
        sleep 5
    fi
    
    echo "✅ Services verified and restarted"
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
ENDSSH

# ============================================================================
# Summary
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Deployment Summary:${NC}"
echo -e "  Environment: ${YELLOW}$ENVIRONMENT${NC}"
echo -e "  Application URL: ${YELLOW}https://$DROPLET_IP${NC}"
echo -e "  Deployment Mode: ${YELLOW}$DEPLOYMENT_MODE${NC}"
if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
    echo -e "  Database: ${YELLOW}Local Container${NC}"
    echo -e "  Database Name: ${YELLOW}$DB_NAME${NC}"
    echo -e "  Database User: ${YELLOW}$DB_USER${NC}"
    echo -e "  Database Password: ${YELLOW}$DB_PASSWORD${NC}"
    echo -e "${RED}⚠️  IMPORTANT: Save the database password above!${NC}"
else
    echo -e "  Database: ${YELLOW}Managed (Digital Ocean)${NC}"
fi
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo "1. Test the application:"
echo "   https://$DROPLET_IP/"
echo "2. Check container logs:"
echo "   ssh $DROPLET_USER@$DROPLET_IP 'cd /opt/mental-health-app && docker compose logs -f'"
echo "3. Scale API containers (for production):"
echo "   ssh $DROPLET_USER@$DROPLET_IP 'cd /opt/mental-health-app && docker compose up -d --scale api=3'"
echo ""
echo -e "${GREEN}✅ Containerized deployment complete!${NC}"
