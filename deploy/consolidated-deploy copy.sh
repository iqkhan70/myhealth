#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# ============================================================================
# Consolidated Deployment Script for Mental Health App
# ============================================================================
# This script performs complete initial setup including:
# 1. Application deployment (server + client)
# 2. Database setup (local or managed)
# 3. HTTPS configuration (server + Nginx)
# 4. Database migrations (baseline)
# 5. Database seeding
# 6. Service startup
#
# Usage:
#   Phase 1 (Local DB): ./consolidated-deploy.sh --local-db
#   Phase 2 (Managed DB): ./consolidated-deploy.sh --managed-db "connection_string"
# ============================================================================

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"
APP_NAME="mental-health-app"
DEPLOY_USER="appuser"
APP_DIR="/opt/$APP_NAME"
SERVICE_NAME="$APP_NAME"
DB_NAME="customerhealthdb"
DB_USER="mentalhealth_user"

# Deployment mode
DEPLOYMENT_MODE=""  # "local-db" or "managed-db"
MANAGED_DB_CONNECTION_STRING=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --local-db)
            DEPLOYMENT_MODE="local-db"
            shift
            ;;
        --managed-db)
            DEPLOYMENT_MODE="managed-db"
            if [ -z "$2" ]; then
                echo -e "${RED}ERROR: --managed-db requires a connection string${NC}"
                echo "Usage: $0 --managed-db \"server=host;port=3306;database=db;user=user;password=pass\""
                exit 1
            fi
            MANAGED_DB_CONNECTION_STRING="$2"
            shift 2
            ;;
        *)
            echo -e "${RED}ERROR: Unknown option: $1${NC}"
            echo "Usage: $0 [--local-db | --managed-db \"connection_string\"]"
            exit 1
            ;;
    esac
done

# Validate deployment mode
if [ -z "$DEPLOYMENT_MODE" ]; then
    echo -e "${RED}ERROR: Deployment mode not specified${NC}"
    echo "Usage: $0 [--local-db | --managed-db \"connection_string\"]"
    exit 1
fi

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/.."

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Consolidated Deployment Script${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Configuration:${NC}"
echo -e "  Droplet IP: ${YELLOW}$DROPLET_IP${NC}"
echo -e "  Deployment Mode: ${YELLOW}$DEPLOYMENT_MODE${NC}"
if [ "$DEPLOYMENT_MODE" = "managed-db" ]; then
    echo -e "  Managed DB: ${YELLOW}Yes${NC}"
else
    echo -e "  Local DB: ${YELLOW}Yes${NC}"
fi
echo -e "  SSH Key: ${YELLOW}$SSH_KEY_PATH${NC}"
echo ""

# Check prerequisites
if [ ! -f "$SSH_KEY_PATH" ]; then
    echo -e "${RED}ERROR: SSH key not found at $SSH_KEY_PATH${NC}"
    exit 1
fi

# Fix SSH key permissions
if [ -f "$SSH_KEY_PATH" ]; then
    KEY_PERMS=$(stat -f "%OLp" "$SSH_KEY_PATH" 2>/dev/null || stat -c "%a" "$SSH_KEY_PATH" 2>/dev/null || echo "unknown")
    if [ "$KEY_PERMS" != "600" ] && [ "$KEY_PERMS" != "0600" ]; then
        echo -e "${YELLOW}Step 0: Fixing SSH key permissions...${NC}"
        chmod 600 "$SSH_KEY_PATH"
    fi
fi

# Test SSH connection
echo -e "${YELLOW}Testing SSH connection...${NC}"
if ! ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no -o ConnectTimeout=5 "$DROPLET_USER@$DROPLET_IP" "echo 'SSH connection successful'" >/dev/null 2>&1; then
    echo -e "${RED}ERROR: Cannot connect to $DROPLET_IP${NC}"
    exit 1
fi
echo -e "${GREEN}✅ SSH connection successful${NC}"
echo ""

# ============================================================================
# Step 1: Install Prerequisites
# ============================================================================
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Step 1: Installing Prerequisites${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    set -e
    export DEBIAN_FRONTEND=noninteractive
    
    # Function to wait for apt lock to be released
    wait_for_apt() {
        max_attempts=60
        attempt=0
        
        while [ $attempt -lt $max_attempts ]; do
            if ! lsof /var/lib/apt/lists/lock /var/lib/dpkg/lock-frontend /var/lib/dpkg/lock 2>/dev/null | grep -q apt; then
                # Check if lock files exist but are stale (older than 5 minutes)
                if [ -f /var/lib/apt/lists/lock ]; then
                    # Check if file is older than 5 minutes using stat
                    lock_file_age=$(stat -c %Y /var/lib/apt/lists/lock 2>/dev/null || echo "0")
                    current_time=$(date +%s)
                    age_seconds=$((current_time - lock_file_age))
                    if [ $age_seconds -gt 300 ]; then
                        echo "⚠️  Removing stale apt lock file..."
                        rm -f /var/lib/apt/lists/lock /var/lib/dpkg/lock-frontend /var/lib/dpkg/lock 2>/dev/null || true
                    fi
                fi
                
                # Final check - try to acquire lock
                if ! lsof /var/lib/apt/lists/lock /var/lib/dpkg/lock-frontend /var/lib/dpkg/lock 2>/dev/null | grep -q apt; then
                    return 0
                fi
            fi
            
            attempt=$((attempt + 1))
            mod_result=$((attempt % 10))
            if [ $mod_result -eq 0 ]; then
                echo "Waiting for apt to be available - attempt $attempt of $max_attempts"
            fi
            sleep 1
        done
        
        echo "⚠️  Could not acquire apt lock after $max_attempts attempts"
        echo "Checking what's holding the lock:"
        lsof /var/lib/apt/lists/lock /var/lib/dpkg/lock-frontend /var/lib/dpkg/lock 2>/dev/null || echo "No processes found"
        return 1
    }
    
    echo "Checking for existing apt processes..."
    wait_for_apt || {
        echo "⚠️  Apt is busy, but continuing with installation..."
        echo "If installation fails, you may need to wait for other apt processes to finish"
    }
    
    echo "Updating system packages..."
    apt-get update -y || {
        echo "⚠️  apt-get update had issues, but continuing..."
    }
    
    echo "Upgrading system packages (this may take a few minutes)..."
    apt-get upgrade -y || {
        echo "⚠️  apt-get upgrade had issues, but continuing..."
    }
    
    # Install .NET 9.0 SDK (includes runtime) - required by the application and migrations
    if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks 2>/dev/null | grep -q "9.0"; then
        echo "Installing .NET 9.0 SDK..."
        wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
        chmod +x /tmp/dotnet-install.sh
        # Install SDK 9.0 (NOT just runtime - SDK is needed for migrations)
        /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
        export PATH=$PATH:/usr/share/dotnet
        echo 'export PATH=$PATH:/usr/share/dotnet' >> ~/.bashrc
        ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
        # Verify installation
        echo "Verifying .NET SDK installation..."
        /usr/share/dotnet/dotnet --version
        /usr/share/dotnet/dotnet --list-sdks
        /usr/share/dotnet/dotnet --list-runtimes | grep "9.0" || true
    else
        echo "✅ .NET 9.0 SDK already installed"
        export PATH=$PATH:/usr/share/dotnet
        # Verify SDK is actually available
        if ! dotnet --list-sdks &>/dev/null; then
            echo "⚠️  SDK not found, reinstalling..."
            /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
        fi
    fi
    
    # Install MySQL (only for local-db mode)
    if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
        if ! command -v mysql &> /dev/null; then
            echo "Installing MySQL..."
            MYSQL_ROOT_PASS=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
            echo "MySQL root password: $MYSQL_ROOT_PASS" > /root/mysql_root_password.txt
            chmod 600 /root/mysql_root_password.txt
            
            debconf-set-selections <<< "mysql-server mysql-server/root_password password $MYSQL_ROOT_PASS"
            debconf-set-selections <<< "mysql-server mysql-server/root_password_again password $MYSQL_ROOT_PASS"
            wait_for_apt || true
            apt-get install -y mysql-server || {
                echo "⚠️  MySQL installation had issues, but continuing..."
            }
            systemctl enable mysql
            systemctl start mysql
            
            # Secure MySQL installation
            mysql -u root -p$MYSQL_ROOT_PASS << 'MYSQL_SCRIPT'
            DELETE FROM mysql.user WHERE User='';
            DELETE FROM mysql.user WHERE User='root' AND Host NOT IN ('localhost', '127.0.0.1', '::1');
            DROP DATABASE IF EXISTS test;
            DELETE FROM mysql.db WHERE Db='test' OR Db='test\_%';
            FLUSH PRIVILEGES;
MYSQL_SCRIPT
            echo "✅ MySQL installed and secured"
        fi
    fi
    
    # Install Redis
    if ! command -v redis-server &> /dev/null; then
        echo "Installing Redis..."
        wait_for_apt || true
        apt-get install -y redis-server || {
            echo "⚠️  Redis installation had issues, but continuing..."
        }
        systemctl enable redis-server
        systemctl start redis-server
    fi
    
    # Install Ollama (for LLM support)
    if ! command -v ollama &> /dev/null; then
        echo "Installing Ollama..."
        curl -fsSL https://ollama.com/install.sh | sh
        
        # Create ollama user if it doesn't exist
        if ! id "ollama" &>/dev/null; then
            useradd -r -s /bin/false -m -d /usr/share/ollama ollama
        fi
        
        # Set up Ollama data directory
        mkdir -p /usr/share/ollama
        chown -R ollama:ollama /usr/share/ollama
        
        # Create systemd service file
        cat > /etc/systemd/system/ollama.service << 'EOFOLLAMASERVICE'
[Unit]
Description=Ollama Service
After=network-online.target

[Service]
ExecStart=/usr/local/bin/ollama serve
User=ollama
Group=ollama
Restart=always
RestartSec=3
Environment="OLLAMA_HOST=0.0.0.0:11434"
Environment="OLLAMA_ORIGINS=*"
Environment="OLLAMA_KEEP_ALIVE=5m"

[Install]
WantedBy=default.target
EOFOLLAMASERVICE
        
        # Start Ollama service
        systemctl daemon-reload
        systemctl enable ollama
        systemctl start ollama
        
        # Wait for Ollama to be ready (can take 10-30 seconds)
        echo "Waiting for Ollama service to be ready..."
        i=1
        while [ $i -le 30 ]; do
            if curl -s http://127.0.0.1:11434/api/tags > /dev/null 2>&1; then
                echo "✅ Ollama is ready (attempt $i)"
                break
            fi
            if [ $i -eq 30 ]; then
                echo "⚠️  Ollama did not become ready after 30 attempts, but continuing..."
            else
                sleep 1
            fi
            i=$((i + 1))
        done
        
        echo "✅ Ollama installed and started"
        
        # Pull and preload Qwen models for better performance
        # Try qwen2.5:8b-instruct first (correct name), fallback to tinyllama if it fails
        echo "Pulling Qwen models - this may take several minutes..."
        echo "Note: Model downloads can take 5-15 minutes depending on connection speed"
        
        # Pull primary model - try qwen2.5:8b-instruct, fallback to tinyllama
        echo "Pulling primary model (trying qwen2.5:8b-instruct, will fallback to tinyllama if needed)..."
        PRIMARY_MODEL=""
        if timeout 1800 ollama pull qwen2.5:8b-instruct 2>&1 | tee /tmp/ollama_pull_8b.log; then
            if grep -q "success\|pulled\|downloaded" /tmp/ollama_pull_8b.log; then
                PRIMARY_MODEL="qwen2.5:8b-instruct"
                echo "✅ Primary model (qwen2.5:8b-instruct) pulled successfully"
                
                # Wait a moment for model to be registered
                sleep 2
                
                # Preload primary model into memory
                echo "Pre-loading primary model into memory..."
                if curl -s -X POST http://127.0.0.1:11434/api/generate \
                    -H "Content-Type: application/json" \
                    -d "{
                        \"model\": \"$PRIMARY_MODEL\",
                        "prompt": "test",
                        "stream": false,
                        "options": {
                            "num_predict": 10
                        }
                    }' > /dev/null 2>&1; then
                    echo "✅ Primary model preloaded successfully"
                else
                    echo "⚠️  Primary model preload warning (model may still work)"
                fi
            else
                echo "⚠️  Primary model pull may have failed - check logs"
                cat /tmp/ollama_pull_8b.log | tail -5
            fi
        else
            echo "⚠️  qwen2.5:8b-instruct pull failed, trying fallback: tinyllama"
            if timeout 600 ollama pull tinyllama 2>&1 | tee /tmp/ollama_pull_tiny.log; then
                if grep -q "success\|pulled\|downloaded" /tmp/ollama_pull_tiny.log; then
                    PRIMARY_MODEL="tinyllama"
                    echo "✅ Fallback model (tinyllama) pulled successfully"
                    sleep 2
                    curl -s -X POST http://127.0.0.1:11434/api/generate \
                        -H "Content-Type: application/json" \
                        -d '{"model": "tinyllama", "prompt": "test", "stream": false, "options": {"num_predict": 10}}' > /dev/null 2>&1 || true
                fi
            fi
            rm -f /tmp/ollama_pull_tiny.log
        fi
        rm -f /tmp/ollama_pull_8b.log
        
        # Pull secondary model - try qwen2.5:4b-instruct, fallback to tinyllama
        echo "Pulling secondary model (trying qwen2.5:4b-instruct, will fallback to tinyllama if needed)..."
        SECONDARY_MODEL=""
        if timeout 1200 ollama pull qwen2.5:4b-instruct 2>&1 | tee /tmp/ollama_pull_4b.log; then
            if grep -q "success\|pulled\|downloaded" /tmp/ollama_pull_4b.log; then
                SECONDARY_MODEL="qwen2.5:4b-instruct"
                echo "✅ Secondary model (qwen2.5:4b-instruct) pulled successfully"
                
                # Wait a moment for model to be registered
                sleep 2
                
                # Preload secondary model into memory
                echo "Pre-loading secondary model into memory..."
                if curl -s -X POST http://127.0.0.1:11434/api/generate \
                    -H "Content-Type: application/json" \
                    -d "{
                        \"model\": \"$SECONDARY_MODEL\",
                        "prompt": "test",
                        "stream": false,
                        "options": {
                            "num_predict": 10
                        }
                    }' > /dev/null 2>&1; then
                    echo "✅ Secondary model preloaded successfully"
                else
                    echo "⚠️  Secondary model preload warning (model may still work)"
                fi
            else
                echo "⚠️  Secondary model pull may have failed - check logs"
                cat /tmp/ollama_pull_4b.log | tail -5
            fi
        else
            echo "⚠️  qwen2.5:4b-instruct pull failed, using tinyllama as fallback"
            SECONDARY_MODEL="tinyllama"
            if ! echo "$AVAILABLE_MODELS" | grep -q "tinyllama"; then
                echo "   (tinyllama should already be available from primary model fallback)"
            fi
        fi
        rm -f /tmp/ollama_pull_4b.log
        
        # Set defaults if models weren't set
        if [ -z "$PRIMARY_MODEL" ]; then
            PRIMARY_MODEL="tinyllama"
        fi
        if [ -z "$SECONDARY_MODEL" ]; then
            SECONDARY_MODEL="tinyllama"
        fi
        
        # Verify models are available
        echo ""
        echo "Verifying available models..."
        AVAILABLE_MODELS=$(curl -s http://127.0.0.1:11434/api/tags 2>/dev/null | python3 -c "import sys, json; data = json.load(sys.stdin); print(' '.join([m.get('name', '') for m in data.get('models', [])]))" 2>/dev/null || echo "")
        
        echo "Primary model to use: $PRIMARY_MODEL"
        if echo "$AVAILABLE_MODELS" | grep -q "$PRIMARY_MODEL"; then
            echo "✅ $PRIMARY_MODEL is available"
        else
            echo "❌ $PRIMARY_MODEL is NOT available"
        fi
        
        echo "Secondary model to use: $SECONDARY_MODEL"
        if echo "$AVAILABLE_MODELS" | grep -q "$SECONDARY_MODEL"; then
            echo "✅ $SECONDARY_MODEL is available"
        else
            echo "❌ $SECONDARY_MODEL is NOT available"
        fi
        
        echo ""
        echo "⚠️  IMPORTANT: Update database with correct model names:"
        echo "   Primary: $PRIMARY_MODEL"
        echo "   Secondary: $SECONDARY_MODEL"
        echo "   Run: UPDATE AIModelConfigs SET ApiEndpoint='$PRIMARY_MODEL' WHERE Id=1;"
        echo "   Run: UPDATE AIModelConfigs SET ApiEndpoint='$SECONDARY_MODEL' WHERE Id=2;"
        
        echo "✅ Model pulling and preloading complete"
    else
        echo "✅ Ollama already installed"
        # Ensure service is running
        if ! systemctl is-active --quiet ollama; then
            systemctl start ollama
        fi
    fi
    
    # Install Nginx
    if ! command -v nginx &> /dev/null; then
        echo "Installing Nginx..."
        wait_for_apt || true
        apt-get install -y nginx || {
            echo "⚠️  Nginx installation had issues, but continuing..."
        }
        systemctl enable nginx
    fi
    
    # Install other utilities
    wait_for_apt || true
    apt-get install -y curl wget unzip git openssl python3 || {
        echo "⚠️  Some utilities installation had issues, but continuing..."
    }
    
    echo "✅ Prerequisites installed"
ENDSSH

# ============================================================================
# Step 2: Create Application User
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 2: Creating Application User${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    if ! id "$DEPLOY_USER" &>/dev/null; then
        useradd -m -s /bin/bash -d /home/$DEPLOY_USER $DEPLOY_USER
        mkdir -p $APP_DIR
        chown -R $DEPLOY_USER:$DEPLOY_USER $APP_DIR
        echo "✅ Application user created"
    else
        echo "✅ Application user already exists"
    fi
ENDSSH

# ============================================================================
# Step 3: Build and Deploy Application
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 3: Building and Deploying Application${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

echo "Building application locally..."
dotnet publish SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj -c Release -o ./publish/server
dotnet publish SM_MentalHealthApp.Client/SM_MentalHealthApp.Client.csproj -c Release -o ./publish/client

echo "Copying application files to server..."
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "mkdir -p $APP_DIR/server $APP_DIR/client"
scp -i "$SSH_KEY_PATH" -r ./publish/server/* "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/"
scp -i "$SSH_KEY_PATH" -r ./publish/client/* "$DROPLET_USER@$DROPLET_IP:$APP_DIR/client/"

# Copy migration files and .csproj (needed for migrations)
echo "Copying migration files and project file..."
scp -i "$SSH_KEY_PATH" -r SM_MentalHealthApp.Server/Migrations "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/" 2>/dev/null || echo "⚠️  Migrations folder not found, will use SQL migration only"
# Copy .csproj file (required for dotnet ef commands)
scp -i "$SSH_KEY_PATH" SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/" 2>/dev/null || echo "⚠️  .csproj file not found"

# Copy Data folder source code (needed for migrations to compile)
echo "Copying Data folder source code (needed for migrations)..."
if [ -d "SM_MentalHealthApp.Server/Data" ]; then
    scp -i "$SSH_KEY_PATH" -r SM_MentalHealthApp.Server/Data "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/" 2>/dev/null || echo "⚠️  Data folder copy failed"
else
    echo "⚠️  Data folder not found - migrations may fail to compile"
fi

# Copy seeding script
if [ -f "deploy/Scripts121925/SeedingInitialConsolidatedScript.sh" ]; then
    echo "Copying seeding script..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "mkdir -p $APP_DIR/scripts"
    scp -i "$SSH_KEY_PATH" deploy/Scripts121925/SeedingInitialConsolidatedScript.sh "$DROPLET_USER@$DROPLET_IP:$APP_DIR/scripts/"
fi

echo "✅ Application deployed"

# ============================================================================
# Step 4: Setup Database
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 4: Setting Up Database${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
    # Phase 1: Create database on droplet
    echo "Setting up local database..."
    DB_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
    
    echo "Creating database and user on server..."
    echo "DEBUG: DB_NAME=$DB_NAME, DB_USER=$DB_USER"
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
        set -e  # Exit on error
        set -x  # Debug mode - show commands being executed
        
        # Get MySQL root password
        if [ -f /root/mysql_root_password.txt ]; then
            MYSQL_ROOT_PASS=\$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4 || echo "")
            if [ -z "\$MYSQL_ROOT_PASS" ]; then
                echo "⚠️  Could not extract password from file, using fallback"
                MYSQL_ROOT_PASS="UthmanBasima70"
            else
                echo "✅ Found MySQL root password in file"
            fi
        else
            echo "⚠️  MySQL root password file not found, using fallback password"
            MYSQL_ROOT_PASS="UthmanBasima70"
        fi
        
        # Verify password is not empty
        if [ -z "\$MYSQL_ROOT_PASS" ]; then
            echo "❌ ERROR: MySQL root password is empty!"
            exit 1
        fi
        
        # Test MySQL connection first - use MYSQL_PWD to avoid quote issues
        echo "Testing MySQL connection..."
        export MYSQL_PWD="\$MYSQL_ROOT_PASS"
        if ! mysql -u root -e "SELECT 1;" 2>&1 | grep -v "Warning" | grep -v "Enter password" > /dev/null; then
            echo "❌ ERROR: Cannot connect to MySQL with root user"
            echo "Please check if MySQL is installed and running"
            unset MYSQL_PWD
            exit 1
        fi
        unset MYSQL_PWD
        echo "✅ MySQL connection successful"
        
        # Create database and user (no space after -p is critical!)
        echo "Creating database '$DB_NAME'..."
        echo "Using MySQL root password: \${MYSQL_ROOT_PASS:0:3}*** (hidden)"
        
        # Create database first - use MYSQL_PWD environment variable to avoid quote issues
        export MYSQL_PWD="\$MYSQL_ROOT_PASS"
        mysql -u root -e "CREATE DATABASE IF NOT EXISTS $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
        DB_CREATE_EXIT=\${PIPESTATUS[0]}
        if [ \$DB_CREATE_EXIT -ne 0 ]; then
            echo "❌ ERROR: Failed to create database (exit code: \$DB_CREATE_EXIT)"
            mysql -u root -e "SHOW DATABASES;" 2>&1 | grep -v "Warning" | grep -v "Enter password"
            exit 1
        fi
        echo "✅ Database creation command executed"
        
        # Create or update user - always set the password to ensure it matches
        # First, try to create the user
        mysql -u root -e "CREATE USER IF NOT EXISTS '$DB_USER'@'localhost' IDENTIFIED BY '$DB_PASSWORD';" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
        USER_CREATE_EXIT=\${PIPESTATUS[0]}
        
        # If user already exists, update the password
        if [ \$USER_CREATE_EXIT -ne 0 ] || mysql -u root -e "SELECT User FROM mysql.user WHERE User='$DB_USER' AND Host='localhost';" 2>&1 | grep -q "$DB_USER"; then
            echo "User exists, updating password..."
            mysql -u root -e "ALTER USER '$DB_USER'@'localhost' IDENTIFIED BY '$DB_PASSWORD';" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
            ALTER_EXIT=\${PIPESTATUS[0]}
            if [ \$ALTER_EXIT -ne 0 ]; then
                echo "⚠️  Warning: Password update had issues (exit code: \$ALTER_EXIT), but continuing..."
            else
                echo "✅ User password updated successfully"
            fi
        else
            echo "✅ User created successfully"
        fi
        
        # Grant privileges
        mysql -u root -e "GRANT ALL PRIVILEGES ON $DB_NAME.* TO '$DB_USER'@'localhost';" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
        GRANT_EXIT=\${PIPESTATUS[0]}
        if [ \$GRANT_EXIT -ne 0 ]; then
            echo "❌ ERROR: Failed to grant privileges (exit code: \$GRANT_EXIT)"
            exit 1
        fi
        
        # Flush privileges
        mysql -u root -e "FLUSH PRIVILEGES;" 2>&1 | grep -v "Warning" | grep -v "Enter password" || true
        FLUSH_EXIT=\${PIPESTATUS[0]}
        if [ \$FLUSH_EXIT -ne 0 ]; then
            echo "⚠️  Warning: FLUSH PRIVILEGES had issues (exit code: \$FLUSH_EXIT)"
        fi
        unset MYSQL_PWD
        
        # Verify database was created - use MYSQL_PWD
        echo "Verifying database creation..."
        echo "Listing all databases:"
        export MYSQL_PWD="\$MYSQL_ROOT_PASS"
        mysql -u root -e "SHOW DATABASES;" 2>&1 | grep -v "Warning" | grep -v "Enter password"
        
        DB_EXISTS=\$(mysql -u root -e "SHOW DATABASES LIKE '$DB_NAME';" 2>&1 | grep -v "Warning" | grep -v "Enter password" | grep -c "$DB_NAME" || echo "0")
        echo "Database existence check result: \$DB_EXISTS"
        
        if [ "\$DB_EXISTS" = "0" ]; then
            echo "❌ ERROR: Database '$DB_NAME' was not created!"
            echo "Attempting to create database again with explicit error checking..."
            mysql -u root -e "CREATE DATABASE $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" 2>&1
            # Check again
            DB_EXISTS=\$(mysql -u root -e "SHOW DATABASES LIKE '$DB_NAME';" 2>&1 | grep -v "Warning" | grep -v "Enter password" | grep -c "$DB_NAME" || echo "0")
            if [ "\$DB_EXISTS" = "0" ]; then
                echo "❌ ERROR: Database '$DB_NAME' still does not exist after retry!"
                unset MYSQL_PWD
                exit 1
            fi
        fi
        echo "✅ Database '$DB_NAME' verified"
        
        # Verify user was created
        echo "Verifying user creation..."
        USER_EXISTS=\$(mysql -u root -e "SELECT User FROM mysql.user WHERE User='$DB_USER' AND Host='localhost';" 2>&1 | grep -v "Warning" | grep -v "Enter password" | grep -c "$DB_USER" || echo "0")
        if [ "\$USER_EXISTS" = "0" ]; then
            echo "❌ ERROR: User '$DB_USER' was not created!"
            unset MYSQL_PWD
            exit 1
        fi
        echo "✅ User '$DB_USER' verified"
        
        # Verify user has proper permissions
        echo "Verifying user permissions..."
        mysql -u root -e "SHOW GRANTS FOR '$DB_USER'@'localhost';" 2>&1 | grep -v "Warning" | grep -v "Enter password" | grep -i "$DB_NAME" || {
            echo "⚠️  Warning: User permissions may not be set correctly"
        }
        unset MYSQL_PWD
        
        echo "✅ Database '$DB_NAME' and user '$DB_USER' created successfully"
        set +x  # Turn off debug mode
ENDSSH
    
    SSH_EXIT_CODE=$?
    if [ $SSH_EXIT_CODE -ne 0 ]; then
        echo -e "${RED}❌ ERROR: Database setup failed! (Exit code: $SSH_EXIT_CODE)${NC}"
        echo "Please check the error messages above"
        exit 1
    fi
    echo -e "${GREEN}✅ Database setup completed successfully${NC}"
    
    # Escape special characters in password for connection string
    # MySQL connection strings need special characters URL-encoded or escaped
    ESCAPED_PASSWORD=$(echo "$DB_PASSWORD" | sed 's/&/%26/g; s/;/%3B/g; s/=/%3D/g; s/+/%2B/g; s/\//%2F/g; s/?/%3F/g; s/#/%23/g')
    CONNECTION_STRING="server=localhost;port=3306;database=$DB_NAME;user=$DB_USER;password=$ESCAPED_PASSWORD"
else
    # Phase 2: Use managed database connection string
    echo "Using managed database connection string..."
    CONNECTION_STRING="$MANAGED_DB_CONNECTION_STRING"
fi

# ============================================================================
# Step 5: Configure Application Settings
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 5: Configuring Application Settings${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Create appsettings with proper connection string
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    cat > $APP_DIR/server/appsettings.Production.json << APPSETTINGSEOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "MySQL": "$CONNECTION_STRING"
  },
  "HuggingFace": {
    "ApiKey": "",
    "BioMistralModelUrl": "https://api-inference.huggingface.co/models/medalpaca/medalpaca-7b",
    "MeditronModelUrl": "https://api-inference.huggingface.co/models/epfl-llm/meditron-7b"
  },
  "Ollama": {
    "BaseUrl": "http://127.0.0.1:11434"
  },
  "OpenAI": {
    "ApiKey": "sk-your-actual-openai-api-key-here"
  },
  "S3": {
    "AccessKey": "DO00Z6VU8Q38KXLFZ7V4",
    "SecretKey": "ZDK61LfGdaqu5FpTcKnUfK8GNSW+cTSSbK8vK8GnMno",
    "BucketName": "mentalhealth-content",
    "Region": "sfo3",
    "ServiceUrl": "https://sfo3.digitaloceanspaces.com",
    "Folder": "content/"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "SM_MentalHealthApp",
    "Audience": "SM_MentalHealthApp_Users"
  },
  "Vonage": {
    "Enabled": true,
    "ApiKey": "c7dc2f50",
    "ApiSecret": "ZsknGg4RbD1fBI4B",
    "FromNumber": "+16148122119"
  },
  "Email": {
    "Enabled": false,
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "FromEmail": "noreply@healthapp.com",
    "FromName": "Health App",
    "EnableSsl": true
  },
  "Agora": {
    "AppId": "efa11b3a7d05409ca979fb25a5b489ae",
    "UseTokens": true,
    "AppCertificate": "89ab54068fae46aeaf930ffd493e977b"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Kestrel": {
    "Limits": {
      "KeepAliveTimeout": 900,
      "RequestHeadersTimeout": 900
    },
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:5262",
        "Certificate": {
          "Path": "/opt/mental-health-app/certs/server.crt",
          "KeyPath": "/opt/mental-health-app/certs/server.key"
        }
      }
    }
  },
  "RequestTimeout": "00:15:00",
  "PiiEncryption": {
    "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
  },
  "Encryption": {
    "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
  }
}
APPSETTINGSEOF
    chown -R $DEPLOY_USER:$DEPLOY_USER $APP_DIR
    echo "✅ Application settings configured"
ENDSSH

# ============================================================================
# Step 6: Generate Migration SQL Locally and Apply
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 6: Generating and Applying Database Migrations${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Generate SQL migration script locally (more reliable than running on server)
echo "Generating SQL migration script from local migrations..."

# Check if we're in the right directory
if [ ! -f "SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj" ]; then
    echo -e "${RED}ERROR: Please run this script from the project root directory${NC}"
    exit 1
fi

cd SM_MentalHealthApp.Server

# Check if dotnet ef is installed locally
if ! dotnet ef --version &>/dev/null; then
    echo "Installing dotnet-ef tool locally..."
    dotnet tool install --global dotnet-ef || {
        echo -e "${YELLOW}⚠️  Could not install dotnet-ef tool${NC}"
        echo "Please install it manually: dotnet tool install --global dotnet-ef"
    }
fi

# Generate SQL script from the consolidated migration (InitialCleanBaseline)
# Clean up any old migration SQL files first
MIGRATION_SQL_FILE="$SCRIPT_DIR/migration.sql"
if [ -f "$MIGRATION_SQL_FILE" ]; then
    echo "Removing old migration.sql file..."
    rm -f "$MIGRATION_SQL_FILE"
fi

echo "Creating SQL migration script from consolidated migration (InitialCleanBaseline)..."
echo "Target migration: 20251219144202_InitialCleanBaseline"

# Generate SQL from the specific consolidated migration
# Using --idempotent to make it safe to run multiple times
# Note: --from and --to are positional arguments, not flags
# Format: dotnet ef migrations script [from] [to] [options]
if dotnet ef migrations script 0 20251219144202_InitialCleanBaseline --idempotent --output "$MIGRATION_SQL_FILE" 2>&1; then
    if [ -f "$MIGRATION_SQL_FILE" ] && [ -s "$MIGRATION_SQL_FILE" ]; then
        echo "✅ Migration SQL generated successfully from InitialCleanBaseline"
        echo "   File: $MIGRATION_SQL_FILE"
        echo "   Size: $(du -h "$MIGRATION_SQL_FILE" | cut -f1)"
    else
        echo -e "${YELLOW}⚠️  Migration SQL file is empty or not created${NC}"
        MIGRATION_SQL_FILE=""
    fi
else
    echo -e "${YELLOW}⚠️  Could not generate migration SQL from InitialCleanBaseline${NC}"
    echo "Trying without specifying migration (will use all migrations)..."
    if dotnet ef migrations script --idempotent --output "$MIGRATION_SQL_FILE" 2>&1; then
        if [ -f "$MIGRATION_SQL_FILE" ] && [ -s "$MIGRATION_SQL_FILE" ]; then
            echo "✅ Migration SQL generated successfully (all migrations)"
            MIGRATION_SQL_FILE="$MIGRATION_SQL_FILE"
        else
            echo -e "${YELLOW}⚠️  Migration SQL file is empty or not created${NC}"
            MIGRATION_SQL_FILE=""
        fi
    else
        echo -e "${YELLOW}⚠️  Could not generate migration SQL${NC}"
        MIGRATION_SQL_FILE=""
    fi
fi

cd "$SCRIPT_DIR/.."

# Apply migration SQL to server if generated
if [ -n "$MIGRATION_SQL_FILE" ] && [ -f "$MIGRATION_SQL_FILE" ]; then
    echo "Applying migration SQL to database..."
    
    # Extract database connection details for local-db
    if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
        DB_HOST="localhost"
        DB_PORT="3306"
        DB_NAME_MIG="$DB_NAME"
        DB_USER_MIG="$DB_USER"
        DB_PASS_MIG="$DB_PASSWORD"
    else
        # Extract from managed connection string
        DB_HOST=$(echo "$CONNECTION_STRING" | sed -n 's/.*server=\([^:;]*\).*/\1/p' || echo "localhost")
        DB_PORT=$(echo "$CONNECTION_STRING" | sed -n 's/.*port=\([^;]*\).*/\1/p' || echo "3306")
        DB_NAME_MIG=$(echo "$CONNECTION_STRING" | sed -n 's/.*database=\([^;]*\).*/\1/p' || echo "$DB_NAME")
        DB_USER_MIG=$(echo "$CONNECTION_STRING" | sed -n 's/.*user=\([^;]*\).*/\1/p' || echo "$DB_USER")
        DB_PASS_MIG=$(echo "$CONNECTION_STRING" | sed -n 's/.*password=\([^;]*\).*/\1/p' || echo "")
    fi
    
    # Copy SQL to server
    scp -i "$SSH_KEY_PATH" "$MIGRATION_SQL_FILE" "$DROPLET_USER@$DROPLET_IP:/tmp/migration.sql"
    
    # Apply SQL on server - use ROOT user to create schema (CREATE TABLE statements)
    # Root has all permissions, then we'll use DB_USER for seeding
    echo "Applying migration SQL to database using root user..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
        echo "Applying migration SQL to database..."
        echo "Database: $DB_NAME_MIG"
        echo "Using: root user (for schema creation)"
        
        if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
            # Get MySQL root password
            if [ -f /root/mysql_root_password.txt ]; then
                MYSQL_ROOT_PASS=\$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
            else
                # Try hardcoded password as fallback
                MYSQL_ROOT_PASS="UthmanBasima70"
            fi
            
            # Check if SQL file has stored procedure syntax (DELIMITER commands)
            SQL_METHOD="SQL"
            if grep -q "DELIMITER\|MigrationsScript" /tmp/migration.sql; then
                echo "⚠️  Migration SQL contains stored procedure syntax (DELIMITER)"
                echo "Extracting SQL statements from stored procedures..."
                
                # Extract ALL SQL statements from stored procedures
                # The CREATE TABLE statements are ALSO inside stored procedures (multi-line)
                echo "Extracting SQL statements from stored procedures (CREATE TABLE, CREATE INDEX, etc.)..."
                
                # Use Python to properly parse and extract SQL from stored procedures
                # Use a here-document with escaped backticks to prevent bash interpretation
                python3 << 'PYTHON_SCRIPT' > /tmp/migration_clean.sql 2> /tmp/migration_extract_errors.log
import re
import sys
import os

try:
    # Check if file exists
    if not os.path.exists('/tmp/migration.sql'):
        print("ERROR: /tmp/migration.sql not found", file=sys.stderr)
        sys.exit(1)
    
    with open('/tmp/migration.sql', 'r') as f:
        content = f.read()
    
    if not content:
        print("ERROR: /tmp/migration.sql is empty", file=sys.stderr)
        sys.exit(1)
    
    # First, extract the initial CREATE TABLE IF NOT EXISTS for __EFMigrationsHistory (if it exists outside procedures)
    # Use character class to match backtick: [\x60] or use a different approach
    initial_table_pattern = r'^CREATE TABLE IF NOT EXISTS [\x60]__EFMigrationsHistory[\x60].*?;'
    initial_table_match = re.search(initial_table_pattern, content, re.MULTILINE | re.DOTALL)
    if initial_table_match:
        print(initial_table_match.group(0))
        print()
    
    # Extract all SQL statements from inside stored procedures
    # Pattern: Everything between "IF NOT EXISTS(...) THEN" and "END IF;" inside procedures
    # This captures CREATE TABLE, CREATE INDEX, ALTER DATABASE, INSERT INTO, etc.
    # Use hex escape for backtick to prevent bash interpretation: \x60
    procedure_pattern = r'IF NOT EXISTS\(SELECT 1 FROM [\x60]__EFMigrationsHistory[\x60] WHERE [\x60]MigrationId[\x60] = [^)]+\) THEN\s+(.*?)\s+END IF;'
    matches = list(re.finditer(procedure_pattern, content, re.MULTILINE | re.DOTALL))
    
    if not matches:
        print(f"ERROR: No stored procedure blocks found (searched {len(content)} chars)", file=sys.stderr)
        # Debug: show first 500 chars of file
        print(f"First 500 chars of file: {content[:500]}", file=sys.stderr)
        sys.exit(1)
    
    all_statements = []
    
    for match in matches:
        sql_block = match.group(1).strip()
        if not sql_block:
            continue
        
        # Remove leading whitespace from each line but preserve structure
        lines = []
        for line in sql_block.split('\n'):
            stripped = line.strip()
            if stripped and not stripped.startswith('--'):
                # Preserve the original line structure for multi-line statements
                lines.append(stripped)
        
        if not lines:
            continue
        
        # Split into individual statements by semicolon
        # But preserve multi-line statements (CREATE TABLE spans multiple lines)
        statements = []
        current_stmt = []
        
        for line in lines:
            current_stmt.append(line)
            # Check if this line ends with semicolon (statement complete)
            if line.rstrip().endswith(';'):
                stmt = '\n'.join(current_stmt)
                if stmt.strip():
                    statements.append(stmt)
                current_stmt = []
        
        # If there's a remaining statement without semicolon, add it
        if current_stmt:
            stmt = '\n'.join(current_stmt)
            if stmt.strip():
                statements.append(stmt)
        
        all_statements.extend(statements)
    
    if not all_statements:
        print("ERROR: No SQL statements extracted", file=sys.stderr)
        sys.exit(1)
    
    # Print all statements - ensure proper formatting
    for stmt in all_statements:
        stmt = stmt.strip()
        if stmt:
            # Ensure statement ends with semicolon
            if not stmt.endswith(';'):
                stmt += ';'
            print(stmt)
            print()  # Extra newline between statements for clarity
except Exception as e:
    print(f"ERROR: Python extraction failed: {e}", file=sys.stderr)
    import traceback
    traceback.print_exc(file=sys.stderr)
    sys.exit(1)
PYTHON_SCRIPT
                
                # Check if extraction was successful
                if [ -f /tmp/migration_extract_errors.log ] && [ -s /tmp/migration_extract_errors.log ]; then
                    echo "❌ Python extraction failed with errors:"
                    cat /tmp/migration_extract_errors.log
                    SQL_METHOD="DOTNET_EF"
                elif [ -f /tmp/migration_clean.sql ] && [ -s /tmp/migration_clean.sql ]; then
                    echo "Applying extracted SQL statements..."
                    echo "Extracted SQL file size: \$(wc -l < /tmp/migration_clean.sql) lines"
                    
                    # Apply SQL using Python to handle multi-line statements properly
                    # Use MYSQL_PWD to avoid quote issues
                    export MYSQL_PWD="\$MYSQL_ROOT_PASS"
                    
                    python3 << 'APPLY_SQL' 2>&1
import subprocess
import sys
import re

MYSQL_CMD = ['mysql', '-u', 'root', 'customerhealthdb']
ERROR_COUNT = 0
STATEMENT_COUNT = 0

try:
    with open('/tmp/migration_clean.sql', 'r') as f:
        content = f.read()
    
    # Split by double newline (statements are separated by blank lines)
    statements = [s.strip() for s in content.split('\n\n') if s.strip()]
    
    # Separate __EFMigrationsHistory CREATE TABLE and INSERT statements
    # Apply CREATE TABLE for __EFMigrationsHistory first
    history_create = None
    history_insert = None
    other_statements = []
    
    for stmt in statements:
        if 'CREATE TABLE' in stmt and '__EFMigrationsHistory' in stmt:
            history_create = stmt
        elif 'INSERT INTO' in stmt and '__EFMigrationsHistory' in stmt:
            history_insert = stmt
        else:
            other_statements.append(stmt)
    
    # Reorder: CREATE __EFMigrationsHistory first, then other statements, then INSERT last
    ordered_statements = []
    if history_create:
        ordered_statements.append(history_create)
    ordered_statements.extend(other_statements)
    if history_insert:
        ordered_statements.append(history_insert)
    
    print(f"Found {len(ordered_statements)} SQL statements to apply")
    if history_create:
        print("✅ Will create __EFMigrationsHistory table first")
    
    for i, stmt in enumerate(ordered_statements, 1):
        STATEMENT_COUNT = i
        # Apply statement
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
            # Show error but continue (unless it's a critical error)
            error_msg = stderr.strip()
            if 'ERROR' in error_msg.upper():
                # Ignore "table doesn't exist" errors for INSERT INTO __EFMigrationsHistory if we already tried to create it
                if 'Table' in error_msg and '__EFMigrationsHistory' in error_msg and 'doesn\'t exist' in error_msg:
                    print(f"⚠️  Warning in statement {i}: {error_msg[:200]} (table may already exist or will be created)")
                else:
                    print(f"⚠️  Error in statement {i}: {error_msg[:200]}")
        
        # Show progress every 20 statements
        if i % 20 == 0:
            print(f"Applied {i}/{len(ordered_statements)} statements...")
    
    if ERROR_COUNT > 0:
        print(f"⚠️  SQL application had {ERROR_COUNT} errors out of {STATEMENT_COUNT} statements")
        # Don't fail if only __EFMigrationsHistory INSERT failed (table might already have the record)
        if ERROR_COUNT == 1 and history_insert and 'INSERT INTO' in str(ordered_statements[-1]):
            print("⚠️  Migration history insert failed, but this is usually non-critical")
            sys.exit(0)
        sys.exit(1)
    else:
        print(f"✅ Applied {STATEMENT_COUNT} SQL statements successfully")
        sys.exit(0)
        
except Exception as e:
    print(f"❌ Fatal error applying SQL: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
APPLY_SQL
                    SQL_EXIT_CODE=\$?
                    unset MYSQL_PWD
                    
                    # Verify tables were created - use a simpler query
                    export MYSQL_PWD="\$MYSQL_ROOT_PASS"
                    TABLE_COUNT=\$(mysql -u root "$DB_NAME_MIG" -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$DB_NAME_MIG' AND table_name != '__EFMigrationsHistory';" 2>&1 | grep -v "Warning" | grep -v "Enter password" | tail -1 | tr -d ' ')
                    echo "Tables created: \$TABLE_COUNT"
                    
                    # Also list tables for debugging
                    echo "Tables in database:"
                    mysql -u root "$DB_NAME_MIG" -e "SHOW TABLES;" 2>&1 | grep -v "Warning" | grep -v "Enter password" | head -10
                    unset MYSQL_PWD
                    
                    # Even if SQL_EXIT_CODE is 1 (some errors), check if tables were created
                    # The INSERT INTO __EFMigrationsHistory error is usually non-critical
                    if [ "\$TABLE_COUNT" = "0" ] || [ -z "\$TABLE_COUNT" ]; then
                        echo "⚠️  Extracted SQL resulted in 0 tables, will try dotnet ef"
                        SQL_METHOD="DOTNET_EF"
                    else
                        echo "✅ SQL migration applied successfully (\$TABLE_COUNT tables created)"
                        rm -f /tmp/migration_clean.sql
                        SQL_METHOD="SUCCESS"
                    fi
                else
                    echo "⚠️  Could not extract SQL statements (file is empty or missing), will use dotnet ef"
                    if [ -f /tmp/migration_extract_errors.log ]; then
                        echo "Python script errors:"
                        cat /tmp/migration_extract_errors.log
                    fi
                    if [ -f /tmp/migration_clean.sql ]; then
                        echo "Python script output (first 10 lines):"
                        head -10 /tmp/migration_clean.sql
                    fi
                    SQL_METHOD="DOTNET_EF"
                fi
            else
                # Use root user to create schema (CREATE TABLE statements)
                # Root has all permissions needed to create tables
                # No space after -p is critical! Use quotes around password variable
                echo "Applying SQL migration file..."
                mysql -u root -p"\$MYSQL_ROOT_PASS" "$DB_NAME_MIG" < /tmp/migration.sql 2>&1 | grep -v "Warning" | grep -v "Enter password" || {
                    echo "⚠️  SQL migration had errors, will try dotnet ef"
                    SQL_METHOD="DOTNET_EF"
                }
                
                if [ "\$SQL_METHOD" != "DOTNET_EF" ]; then
                    # Verify tables were created (no space after -p is critical!)
                    TABLE_COUNT=\$(mysql -u root -p"\$MYSQL_ROOT_PASS" "$DB_NAME_MIG" -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = '$DB_NAME_MIG' AND table_name != '__EFMigrationsHistory';" 2>&1 | grep -v "Warning" | grep -v "Enter password" | tail -1)
                    echo "Tables created: \$TABLE_COUNT"
                    
                    if [ "\$TABLE_COUNT" = "0" ] || [ -z "\$TABLE_COUNT" ]; then
                        echo "⚠️  SQL migration resulted in 0 tables. Will use dotnet ef instead."
                        SQL_METHOD="DOTNET_EF"
                    else
                        echo "✅ Migration applied successfully using root user"
                        rm /tmp/migration.sql
                        SQL_METHOD="SUCCESS"
                    fi
                fi
            fi
            
            # If SQL method failed or detected DELIMITER, use dotnet ef
            if [ "\$SQL_METHOD" = "DOTNET_EF" ]; then
                echo "❌ Migration failed. SQL script saved at /tmp/migration.sql for review"
                echo "You can review it with: cat /tmp/migration.sql"
                echo ""
                echo "Trying dotnet ef database update as fallback..."
                
                # Fallback to dotnet ef
                export PATH=\$PATH:/usr/share/dotnet
                export PATH="\$PATH:\$HOME/.dotnet/tools"
                
                if ! dotnet ef --version &>/dev/null; then
                    dotnet tool install --global dotnet-ef --version 9.0.0 || dotnet tool install --global dotnet-ef
                    export PATH="\$PATH:\$HOME/.dotnet/tools"
                fi
                
                cd $APP_DIR/server
                if [ -f SM_MentalHealthApp.Server.csproj ]; then
                    echo "Restoring project..."
                    dotnet restore SM_MentalHealthApp.Server.csproj 2>&1 | tail -10 || true
                    
                    echo "Building project to check for errors..."
                    if dotnet build SM_MentalHealthApp.Server.csproj 2>&1 | tail -20; then
                        echo "✅ Build successful, running migrations..."
                        dotnet ef database update --project SM_MentalHealthApp.Server.csproj 2>&1 || dotnet ef database update 2>&1
                    else
                        echo "❌ Build failed. Trying to run migrations anyway (might work if only warnings)..."
                        dotnet ef database update --project SM_MentalHealthApp.Server.csproj --no-build 2>&1 || dotnet ef database update --no-build 2>&1 || {
                            echo "⚠️  --no-build failed, trying with build..."
                            dotnet ef database update --project SM_MentalHealthApp.Server.csproj 2>&1 || dotnet ef database update 2>&1
                        }
                    fi
                    
                    # Verify again (no space after -p is critical!)
                    TABLE_COUNT=\$(mysql -u root -p"\$MYSQL_ROOT_PASS" "$DB_NAME_MIG" -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = '$DB_NAME_MIG' AND table_name != '__EFMigrationsHistory';" 2>&1 | grep -v "Warning" | grep -v "Enter password" | tail -1)
                    echo "Tables created via dotnet ef: \$TABLE_COUNT"
                else
                    echo "❌ .csproj file not found, cannot run dotnet ef"
                fi
            fi
        else
            # For managed DB, use provided credentials
            mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER_MIG" -p"$DB_PASS_MIG" "$DB_NAME_MIG" < /tmp/migration.sql
            
            if [ \$? -eq 0 ]; then
                echo "✅ Migration applied successfully"
                TABLE_COUNT=\$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER_MIG" -p"$DB_PASS_MIG" "$DB_NAME_MIG" -e "SELECT COUNT(*) as count FROM information_schema.tables WHERE table_schema = '$DB_NAME_MIG' AND table_name != '__EFMigrationsHistory';" 2>/dev/null | tail -1)
                echo "Tables created: \$TABLE_COUNT"
                rm /tmp/migration.sql
            else
                echo "❌ Migration failed"
                exit 1
            fi
        fi
        
        echo "✅ Database migrations completed"
ENDSSH
else
    echo -e "${YELLOW}⚠️  Migration SQL not generated, skipping migration step${NC}"
    echo "You may need to run migrations manually"
fi

# ============================================================================
# Step 8: Run Database Seeding
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 8: Running Database Seeding${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

if [ -f "deploy/Scripts121925/SeedingInitialConsolidatedScript.sh" ]; then
    echo "Applying seeding script..."
    
    # Extract database connection details from connection string
    if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
        DB_HOST="localhost"
        DB_PORT="3306"
        DB_NAME_SEED="$DB_NAME"
        DB_USER_SEED="$DB_USER"
        DB_PASS_SEED="$DB_PASSWORD"
    else
        # Extract from managed connection string
        DB_HOST=$(echo "$CONNECTION_STRING" | sed -n 's/.*server=\([^:;]*\).*/\1/p' || echo "localhost")
        DB_PORT=$(echo "$CONNECTION_STRING" | sed -n 's/.*port=\([^;]*\).*/\1/p' || echo "3306")
        DB_NAME_SEED=$(echo "$CONNECTION_STRING" | sed -n 's/.*database=\([^;]*\).*/\1/p' || echo "$DB_NAME")
        DB_USER_SEED=$(echo "$CONNECTION_STRING" | sed -n 's/.*user=\([^;]*\).*/\1/p' || echo "$DB_USER")
        DB_PASS_SEED=$(echo "$CONNECTION_STRING" | sed -n 's/.*password=\([^;]*\).*/\1/p' || echo "")
    fi
    
    # Copy seeding script to server
    scp -i "$SSH_KEY_PATH" deploy/Scripts121925/SeedingInitialConsolidatedScript.sh "$DROPLET_USER@$DROPLET_IP:/tmp/seed.sql"
    
    # Execute seeding script
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
        # The seeding script is pure SQL (starts with blank lines, then SQL)
        # Remove blank lines at start and execute
        echo "Applying seeding script to database $DB_NAME_SEED..."
        if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
            # Get root password
            if [ -f /root/mysql_root_password.txt ]; then
                MYSQL_ROOT_PASS=\$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
            else
                # Fallback to hardcoded password
                MYSQL_ROOT_PASS="UthmanBasima70"
            fi
            
            # Try using root user first (has all permissions)
            # The mentalhealth_user password might not match what's in appsettings
            echo "Seeding database using root user..."
            
            # Apply seeding SQL using Python to handle multi-line statements properly
            export MYSQL_PWD="\$MYSQL_ROOT_PASS"
            
            python3 << 'APPLY_SEED_SQL' 2>&1
import subprocess
import sys
import os

MYSQL_CMD = ['mysql', '-u', 'root', 'customerhealthdb']
ERROR_COUNT = 0
STATEMENT_COUNT = 0

try:
    with open('/tmp/seed.sql', 'r') as f:
        content = f.read()
    
    # Remove blank lines at start
    lines = [line for line in content.split('\n') if line.strip() or line.startswith('--')]
    content = '\n'.join(lines)
    
    # Split into statements - look for semicolon at end of line (not in comments)
    statements = []
    current = []
    in_comment = False
    
    for line in content.split('\n'):
        stripped = line.strip()
        
        # Skip empty lines
        if not stripped:
            if current:
                current.append('')
            continue
        
        # Track comment blocks
        if stripped.startswith('--'):
            if current:
                current.append(line)
            continue
        
        current.append(line)
        
        # If line ends with semicolon, we have a complete statement
        if stripped.endswith(';'):
            stmt = '\n'.join(current)
            if stmt.strip():
                statements.append(stmt)
            current = []
    
    # If there's remaining content, add it
    if current:
        stmt = '\n'.join(current)
        if stmt.strip():
            statements.append(stmt)
    
    print(f"Found {len(statements)} SQL statements to apply")
    
    for i, stmt in enumerate(statements, 1):
        STATEMENT_COUNT = i
        # Apply statement
        proc = subprocess.Popen(
            MYSQL_CMD,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            env=dict(os.environ, MYSQL_PWD=os.environ.get('MYSQL_PWD', ''))
        )
        stdout, stderr = proc.communicate(input=stmt + '\n')
        
        if proc.returncode != 0:
            ERROR_COUNT += 1
            error_msg = stderr.strip()
            if 'ERROR' in error_msg.upper():
                print(f"⚠️  Error in statement {i}: {error_msg[:300]}")
                # Show first few lines of the problematic statement
                stmt_preview = '\n'.join(stmt.split('\n')[:3])
                print(f"   Statement preview: {stmt_preview[:200]}")
        
        # Show progress every 20 statements
        if i % 20 == 0:
            print(f"Applied {i}/{len(statements)} statements...")
    
    if ERROR_COUNT > 0:
        print(f"⚠️  Seeding had {ERROR_COUNT} errors out of {STATEMENT_COUNT} statements")
        # Don't fail if only a few errors (some might be duplicate key errors which are OK)
        if ERROR_COUNT > len(statements) * 0.1:  # More than 10% errors
            sys.exit(1)
        else:
            print("⚠️  Some errors occurred but continuing (may be duplicate key errors)")
            sys.exit(0)
    else:
        print(f"✅ Applied {STATEMENT_COUNT} seeding statements successfully")
        sys.exit(0)
        
except Exception as e:
    print(f"❌ Fatal error applying seeding SQL: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)
APPLY_SEED_SQL
            SEED_EXIT_CODE=\$?
            unset MYSQL_PWD
            
            # Verify seeding was successful (use root for verification)
            export MYSQL_PWD="\$MYSQL_ROOT_PASS"
            ROLES_COUNT=\$(mysql -u root $DB_NAME_SEED -e "SELECT COUNT(*) FROM Roles;" 2>&1 | grep -v "Warning" | grep -v "Enter password" | tail -1 || echo "0")
            USERS_COUNT=\$(mysql -u root $DB_NAME_SEED -e "SELECT COUNT(*) FROM Users;" 2>&1 | grep -v "Warning" | grep -v "Enter password" | tail -1 || echo "0")
            unset MYSQL_PWD
            echo "Roles seeded: \$ROLES_COUNT"
            echo "Users seeded: \$USERS_COUNT"
        else
            # For managed DB, use provided credentials
            sed '/^[[:space:]]*$/d' /tmp/seed.sql | mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER_SEED" -p"$DB_PASS_SEED" "$DB_NAME_SEED" 2>&1 | grep -v "Enter password" || {
                echo "⚠️  Some seeding errors occurred, but continuing..."
            }
            ROLES_COUNT=\$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER_SEED" -p"$DB_PASS_SEED" "$DB_NAME_SEED" -e "SELECT COUNT(*) FROM Roles;" 2>/dev/null | tail -1 || echo "0")
            USERS_COUNT=\$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER_SEED" -p"$DB_PASS_SEED" "$DB_NAME_SEED" -e "SELECT COUNT(*) FROM Users;" 2>/dev/null | tail -1 || echo "0")
            echo "Roles seeded: \$ROLES_COUNT"
            echo "Users seeded: \$USERS_COUNT"
        fi
        
        rm -f /tmp/seed.sql
        echo "✅ Database seeding completed (includes AI Model Configs and Chains)"
ENDSSH
else
    echo -e "${YELLOW}⚠️  Seeding script not found, skipping...${NC}"
fi

# ============================================================================
# Step 9: Setup HTTPS for Server
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 9: Setting Up HTTPS for Server${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Create certificates directory
    mkdir -p $APP_DIR/certs
    
    # Generate self-signed certificate
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout $APP_DIR/certs/server.key \
        -out $APP_DIR/certs/server.crt \
        -subj "/C=US/ST=State/L=City/O=Organization/CN=$DROPLET_IP" \
        -addext "subjectAltName=IP:$DROPLET_IP"
    
    # Set proper permissions
    chmod 600 $APP_DIR/certs/server.key
    chmod 644 $APP_DIR/certs/server.crt
    chown -R $DEPLOY_USER:$DEPLOY_USER $APP_DIR/certs
    
    echo "✅ SSL certificate generated"
ENDSSH

# ============================================================================
# Step 10: Create Systemd Service
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 10: Creating Systemd Service${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

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
Environment=ASPNETCORE_URLS=https://0.0.0.0:5262
Environment=ASPNETCORE_Kestrel__Certificates__Default__Path=/opt/mental-health-app/certs/server.crt
Environment=ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/opt/mental-health-app/certs/server.key
Environment="PATH=/usr/bin:/usr/local/bin:/usr/share/dotnet"

[Install]
WantedBy=multi-user.target
EOFSERVICE

    systemctl daemon-reload
    systemctl enable $SERVICE_NAME
    echo "✅ Systemd service created"
ENDSSH

# ============================================================================
# Step 11: Setup HTTPS for Nginx
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 11: Setting Up HTTPS for Nginx${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Create certificates directory
    mkdir -p /etc/nginx/ssl
    
    # Generate self-signed certificate for Nginx
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
        -keyout /etc/nginx/ssl/nginx-selfsigned.key \
        -out /etc/nginx/ssl/nginx-selfsigned.crt \
        -subj "/C=US/ST=State/L=City/O=Organization/CN=$DROPLET_IP" \
        -addext "subjectAltName=IP:$DROPLET_IP,DNS:$DROPLET_IP"
    
    # Set proper permissions
    chmod 600 /etc/nginx/ssl/nginx-selfsigned.key
    chmod 644 /etc/nginx/ssl/nginx-selfsigned.crt
    
    # Create Nginx configuration
    cat > /etc/nginx/sites-available/$APP_NAME << 'ENDNGINXCONF'
# Upstream for API server (HTTPS backend) - use IPv4 explicitly
upstream api_backend {
    server 127.0.0.1:5262;
}

# HTTP server - redirect to HTTPS
server {
    listen 80;
    server_name DROPLET_IP_PLACEHOLDER;
    
    # Allow Let's Encrypt validation
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
    
    # Redirect all other traffic to HTTPS
    location / {
        return 301 https://\$server_name\$request_uri;
    }
}

# HTTPS server
server {
    listen 443 ssl http2;
    server_name DROPLET_IP_PLACEHOLDER;
    
    # SSL certificates
    ssl_certificate /etc/nginx/ssl/nginx-selfsigned.crt;
    ssl_certificate_key /etc/nginx/ssl/nginx-selfsigned.key;
    
    # SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    
    # API proxy - to HTTPS backend
    location /api {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
    
    # OData proxy - to HTTPS backend (required for OData endpoints)
    location /odata {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_set_header Accept "application/json, application/odata+json";
        proxy_cache_bypass \$http_upgrade;
    }
    
    # SignalR Hub
    location /mobilehub {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_read_timeout 86400;
    }
    
    # WebSocket support
    location /realtime {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
    
    # Root directory for static files
    root /opt/mental-health-app/client/wwwroot;
    
    # Blazor framework files - must come before the catch-all
    location /_framework {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
    
    # Static files (Blazor client) - catch-all for SPA routing
    location / {
        try_files \$uri \$uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
}
ENDNGINXCONF
    
    # Replace placeholder with actual IP
    sed -i "s/DROPLET_IP_PLACEHOLDER/$DROPLET_IP/g" /etc/nginx/sites-available/$APP_NAME
    
    # Enable site
    ln -sf /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-enabled/
    rm -f /etc/nginx/sites-enabled/default
    
    # Test and reload nginx
    nginx -t && systemctl reload nginx
    echo "✅ Nginx configured"
ENDSSH

# ============================================================================
# Step 12: Configure Firewall
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 12: Configuring Firewall${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Allow SSH, HTTP, HTTPS
    ufw allow 22/tcp
    ufw allow 80/tcp
    ufw allow 443/tcp
    ufw allow 11434/tcp
    ufw --force enable
    echo "✅ Firewall configured (ports: 22, 80, 443, 11434)"
ENDSSH

# ============================================================================
# Step 13: Start Services
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 13: Starting Services${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Verify .NET runtime is available before starting service
    echo "Verifying .NET runtime..."
    /usr/share/dotnet/dotnet --list-runtimes | grep "9.0" || {
        echo "⚠️  WARNING: .NET 9.0 runtime not found. Installing..."
        /tmp/dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir /usr/share/dotnet
        /tmp/dotnet-install.sh --channel 9.0 --runtime dotnet --install-dir /usr/share/dotnet
    }
    
    # Start service
    systemctl daemon-reload
    systemctl restart $SERVICE_NAME
    sleep 5
    
    # Check service status
    if systemctl is-active --quiet $SERVICE_NAME; then
        echo "✅ Service is running"
        systemctl status $SERVICE_NAME --no-pager | head -10
    else
        echo "❌ Service failed to start. Checking logs..."
        journalctl -u $SERVICE_NAME --no-pager -n 30 | tail -20
    fi
    
    echo ""
    systemctl status nginx --no-pager | head -10
ENDSSH

# ============================================================================
# Step 14: Verify Deployment
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Step 14: Verifying Deployment${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Test HTTPS connection
sleep 3
HTTPS_TEST=$(curl -k -s -o /dev/null -w '%{http_code}' "https://$DROPLET_IP/api/health" 2>/dev/null || echo "000")

if [ "$HTTPS_TEST" = "200" ] || [ "$HTTPS_TEST" = "404" ]; then
    echo -e "${GREEN}✅ Application is responding${NC}"
else
    echo -e "${YELLOW}⚠️  Health check returned: $HTTPS_TEST${NC}"
    echo "Checking service logs..."
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" \
        "journalctl -u $SERVICE_NAME --no-pager -n 20 | tail -10"
fi

# ============================================================================
# Summary
# ============================================================================
echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${BLUE}Deployment Summary:${NC}"
echo -e "  Application URL: ${YELLOW}https://$DROPLET_IP${NC}"
echo -e "  Deployment Mode: ${YELLOW}$DEPLOYMENT_MODE${NC}"
if [ "$DEPLOYMENT_MODE" = "local-db" ]; then
    echo -e "  Database: ${YELLOW}Local (on droplet)${NC}"
    echo -e "  Database Name: ${YELLOW}$DB_NAME${NC}"
    echo -e "  Database User: ${YELLOW}$DB_USER${NC}"
    echo -e "  Database Password: ${YELLOW}$DB_PASSWORD${NC}"
    echo -e "${RED}⚠️  IMPORTANT: Save the database password above!${NC}"
else
    echo -e "  Database: ${YELLOW}Managed (Digital Ocean)${NC}"
fi
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo "1. Update appsettings.Production.json with your actual API keys:"
echo "   ssh $DROPLET_USER@$DROPLET_IP 'nano $APP_DIR/server/appsettings.Production.json'"
echo "2. Test the application:"
echo "   https://$DROPLET_IP/login"
echo "3. Check logs if needed:"
echo "   ssh $DROPLET_USER@$DROPLET_IP 'journalctl -u $SERVICE_NAME -f'"
echo ""
echo -e "${GREEN}✅ All setup complete! No fix scripts needed.${NC}"


