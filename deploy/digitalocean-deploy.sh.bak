#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

# DigitalOcean Deployment Script for Blazor Mental Health App
# This script automates the deployment of the application to a DigitalOcean droplet

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration - UPDATE THESE VALUES
DROPLET_IP="${DROPLET_IP}"  # Your DigitalOcean droplet IP address
DROPLET_USER="root"  # Usually 'root' for new droplets
SSH_KEY_PATH="$HOME/.ssh/id_rsa"  # Path to your SSH private key
DOMAIN=""  # Your domain name (optional, leave empty to use IP)
APP_NAME="mental-health-app"
DEPLOY_USER="appuser"  # User to run the application
APP_DIR="/opt/$APP_NAME"
SERVICE_NAME="$APP_NAME"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}DigitalOcean Deployment Script${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if required variables are set
if [ -z "$DROPLET_IP" ]; then
    echo -e "${RED}ERROR: DROPLET_IP is not set!${NC}"
    echo "Please edit this script and set DROPLET_IP to your droplet's IP address"
    exit 1
fi

# Check if SSH key exists
if [ ! -f "$SSH_KEY_PATH" ]; then
    echo -e "${YELLOW}SSH key not found at $SSH_KEY_PATH${NC}"
    echo "Please generate an SSH key with: ssh-keygen -t rsa -b 4096"
    exit 1
fi

echo -e "${GREEN}Deploying to: $DROPLET_IP${NC}"
echo -e "${GREEN}Domain: ${DOMAIN:-$DROPLET_IP}${NC}"

# Function to run commands on remote server
remote_exec() {
    ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" "$@"
}

# Function to copy files to remote server
remote_copy() {
    scp -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$@" "$DROPLET_USER@$DROPLET_IP:$2"
}

echo -e "\n${YELLOW}Step 1: Installing prerequisites on server...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Update system
    export DEBIAN_FRONTEND=noninteractive
    apt-get update -y
    apt-get upgrade -y
    
    # Install .NET 8 runtime
    if ! command -v dotnet &> /dev/null; then
        wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
        chmod +x dotnet-install.sh
        ./dotnet-install.sh --channel 8.0 --runtime aspnetcore
        export PATH=$PATH:$HOME/.dotnet
        echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
    fi
    
    # Install MySQL
    if ! command -v mysql &> /dev/null; then
        # Generate secure root password
        MYSQL_ROOT_PASS=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
        echo "MySQL root password: $MYSQL_ROOT_PASS" > /root/mysql_root_password.txt
        chmod 600 /root/mysql_root_password.txt
        
        debconf-set-selections <<< "mysql-server mysql-server/root_password password $MYSQL_ROOT_PASS"
        debconf-set-selections <<< "mysql-server mysql-server/root_password_again password $MYSQL_ROOT_PASS"
        apt-get install -y mysql-server
        systemctl enable mysql
        systemctl start mysql
        
        # Secure MySQL installation (remove test DB, etc.)
        mysql -u root -p$MYSQL_ROOT_PASS << 'MYSQL_SCRIPT'
        DELETE FROM mysql.user WHERE User='';
        DELETE FROM mysql.user WHERE User='root' AND Host NOT IN ('localhost', '127.0.0.1', '::1');
        DROP DATABASE IF EXISTS test;
        DELETE FROM mysql.db WHERE Db='test' OR Db='test\_%';
        FLUSH PRIVILEGES;
MYSQL_SCRIPT
        echo "MySQL installed and secured"
        echo "⚠️ MySQL root password saved to /root/mysql_root_password.txt"
    else
        # MySQL already installed, read root password if file exists
        if [ -f /root/mysql_root_password.txt ]; then
            MYSQL_ROOT_PASS=$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
        else
            echo "⚠️ MySQL is installed but root password file not found. You may need to set it manually."
            MYSQL_ROOT_PASS=""
        fi
    fi
    
    # Install Redis
    if ! command -v redis-server &> /dev/null; then
        apt-get install -y redis-server
        systemctl enable redis-server
        systemctl start redis-server
    fi
    
    # Install Nginx
    if ! command -v nginx &> /dev/null; then
        apt-get install -y nginx
        systemctl enable nginx
    fi
    
    # Install certbot for SSL
    if ! command -v certbot &> /dev/null; then
        apt-get install -y certbot python3-certbot-nginx
    fi
    
    # Install other utilities
    apt-get install -y curl wget unzip git
    
    echo "Prerequisites installed successfully"
ENDSSH

echo -e "\n${YELLOW}Step 2: Creating application user...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    if ! id "$DEPLOY_USER" &>/dev/null; then
        useradd -m -s /bin/bash -d /home/$DEPLOY_USER $DEPLOY_USER
        mkdir -p $APP_DIR
        chown -R $DEPLOY_USER:$DEPLOY_USER $APP_DIR
    fi
ENDSSH

echo -e "\n${YELLOW}Step 3: Building application locally...${NC}"
cd "$(dirname "$0")/.."
dotnet publish SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj -c Release -o ./publish/server
dotnet publish SM_MentalHealthApp.Client/SM_MentalHealthApp.Client.csproj -c Release -o ./publish/client

echo -e "\n${YELLOW}Step 4: Copying application files to server...${NC}"
remote_exec "mkdir -p $APP_DIR/server $APP_DIR/client"
scp -i "$SSH_KEY_PATH" -r ./publish/server/* "$DROPLET_USER@$DROPLET_IP:$APP_DIR/server/"
scp -i "$SSH_KEY_PATH" -r ./publish/client/* "$DROPLET_USER@$DROPLET_IP:$APP_DIR/client/"

echo -e "\n${YELLOW}Step 5: Setting up MySQL database...${NC}"
# Generate secure database password
DB_PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)
DB_NAME="mentalhealthdb"
DB_USER="mentalhealth_user"

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    # Get MySQL root password
    if [ -f /root/mysql_root_password.txt ]; then
        MYSQL_ROOT_PASS=\$(grep "MySQL root password:" /root/mysql_root_password.txt | cut -d' ' -f4)
    else
        echo "⚠️ ERROR: MySQL root password file not found!"
        echo "Please set MySQL root password manually or reinstall MySQL"
        exit 1
    fi
    
    # Create database and user
    mysql -u root -p\$MYSQL_ROOT_PASS << MYSQL_SCRIPT
    CREATE DATABASE IF NOT EXISTS $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
    CREATE USER IF NOT EXISTS '$DB_USER'@'localhost' IDENTIFIED BY '$DB_PASSWORD';
    GRANT ALL PRIVILEGES ON $DB_NAME.* TO '$DB_USER'@'localhost';
    FLUSH PRIVILEGES;
MYSQL_SCRIPT
    
    echo "Database '$DB_NAME' and user '$DB_USER' created successfully"
    echo "Database password: $DB_PASSWORD"
    echo "⚠️ IMPORTANT: Save this password! It will be shown again at the end."
ENDSSH

echo -e "\n${YELLOW}Step 6: Setting up application configuration...${NC}"
# Copy appsettings.json (you should review and update this)
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    cat > $APP_DIR/server/appsettings.Production.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "MySQL": "server=localhost;port=3306;database=$DB_NAME;user=$DB_USER;password=$DB_PASSWORD"
  },
  "Agora": {
    "AppId": "YOUR_AGORA_APP_ID",
    "AppCertificate": "YOUR_AGORA_APP_CERTIFICATE",
    "UseTokens": true
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "SM_MentalHealthApp",
    "Audience": "SM_MentalHealthApp_Users"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
EOF
    chown -R $DEPLOY_USER:$DEPLOY_USER $APP_DIR
ENDSSH

echo -e "\n${YELLOW}Step 7: Running database migrations...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    cd $APP_DIR/server
    # Run EF Core migrations if you have them
    # /root/.dotnet/dotnet ef database update --no-build || echo "No migrations found or migration tool not available"
    echo "⚠️ NOTE: If you have Entity Framework migrations, run them manually:"
    echo "   cd $APP_DIR/server"
    echo "   dotnet ef database update"
ENDSSH

echo -e "\n${YELLOW}Step 8: Creating systemd service...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    cat > /etc/systemd/system/$SERVICE_NAME.service << EOF
[Unit]
Description=Mental Health App Server
After=network.target redis-server.service

[Service]
Type=notify
User=$DEPLOY_USER
WorkingDirectory=$APP_DIR/server
ExecStart=/root/.dotnet/dotnet $APP_DIR/server/SM_MentalHealthApp.Server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$SERVICE_NAME
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5262

[Install]
WantedBy=multi-user.target
EOF
    systemctl daemon-reload
    systemctl enable $SERVICE_NAME
ENDSSH

echo -e "\n${YELLOW}Step 9: Configuring Nginx...${NC}"
if [ -n "$DOMAIN" ]; then
    NGINX_SERVER_NAME="$DOMAIN"
else
    NGINX_SERVER_NAME="$DROPLET_IP"
fi

ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << ENDSSH
    cat > /etc/nginx/sites-available/$APP_NAME << EOF
# Upstream for API server
upstream api_backend {
    server localhost:5262;
}

# Server block for API
server {
    listen 80;
    server_name $NGINX_SERVER_NAME;
    
    # API proxy
    location /api {
        proxy_pass http://api_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
    
    # SignalR Hub
    location /mobilehub {
        proxy_pass http://api_backend;
        proxy_http_version 1.1;
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
        proxy_pass http://api_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
    
    # Static files (Blazor client)
    location / {
        root $APP_DIR/client/wwwroot;
        try_files \$uri \$uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
    
    # Blazor framework files
    location /_framework {
        root $APP_DIR/client/wwwroot;
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
}
EOF

    # Enable site
    ln -sf /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-enabled/
    rm -f /etc/nginx/sites-enabled/default
    
    # Test and reload nginx
    nginx -t && systemctl reload nginx
ENDSSH

echo -e "\n${YELLOW}Step 10: Configuring firewall...${NC}"
ssh -i "$SSH_KEY_PATH" -o StrictHostKeyChecking=no "$DROPLET_USER@$DROPLET_IP" << 'ENDSSH'
    # Allow SSH, HTTP, HTTPS
    ufw allow 22/tcp
    ufw allow 80/tcp
    ufw allow 443/tcp
    ufw --force enable
ENDSSH

echo -e "\n${YELLOW}Step 11: Starting application...${NC}"
remote_exec "systemctl start $SERVICE_NAME"
sleep 5
remote_exec "systemctl status $SERVICE_NAME --no-pager"

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Application URL: http://${DOMAIN:-$DROPLET_IP}"
echo ""
echo -e "${YELLOW}Database Information:${NC}"
echo "  Database Name: $DB_NAME"
echo "  Database User: $DB_USER"
echo "  Database Password: $DB_PASSWORD"
echo -e "${RED}⚠️  IMPORTANT: Save the database password above!${NC}"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo "1. Run database migrations (if you have them):"
echo "   ssh $DROPLET_USER@$DROPLET_IP 'cd $APP_DIR/server && dotnet ef database update'"
echo "2. Update appsettings.Production.json with your actual configuration values:"
echo "   - Agora App ID and Certificate"
echo "   - JWT Secret Key"
echo "   - Other API keys (HuggingFace, OpenAI, etc.)"
echo "3. If you have a domain, run SSL setup: ./setup-ssl.sh"
echo "4. Check application logs: ssh $DROPLET_USER@$DROPLET_IP 'journalctl -u $SERVICE_NAME -f'"
echo ""
echo -e "${YELLOW}Useful Commands:${NC}"
echo "  Restart app:    ssh $DROPLET_USER@$DROPLET_IP 'systemctl restart $SERVICE_NAME'"
echo "  View logs:      ssh $DROPLET_USER@$DROPLET_IP 'journalctl -u $SERVICE_NAME -f'"
echo "  Check status:   ssh $DROPLET_USER@$DROPLET_IP 'systemctl status $SERVICE_NAME'"

