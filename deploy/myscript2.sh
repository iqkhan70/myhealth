#!/bin/bash
# Load centralized DROPLET_IP
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/load-droplet-ip.sh"

cd /Users/mohammedkhan/iq/health && ./deploy/generate-migration-sql.sh

cd /Users/mohammedkhan/iq/health && ./deploy/query-database.sh 2>&1 | head -80

./deploy/export-import-data.sh

./deploy/query-database.sh 

./deploy/fix-ssh-permissions.sh

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "which dotnet && dotnet --version" 2>/dev/null


cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "cat /etc/systemd/system/mental-health-app.service" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "test -f /root/.dotnet/dotnet && echo 'exists' || echo 'NOT FOUND'; test -f /usr/bin/dotnet && echo '/usr/bin/dotnet exists' || echo '/usr/bin/dotnet NOT FOUND'" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "ls -la /root/.dotnet/dotnet && /root/.dotnet/dotnet --version" 2>/dev/null


cd /Users/mohammedkhan/iq/health && sleep 2 && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "systemctl status mental-health-app --no-pager | head -15" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} 'mkdir -p /usr/local/bin
ln -sf /root/.dotnet/dotnet /usr/local/bin/dotnet
chmod +x /usr/local/bin/dotnet
which dotnet
dotnet --version' 2>/dev/null

cd /Users/mohammedkhan/iq/health && sleep 3 && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "journalctl -u mental-health-app --no-pager -n 20 | tail -20" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} 'wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet
ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
export PATH="$PATH:/usr/share/dotnet"
dotnet --version' 2>/dev/null


cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} 'su - appuser -c "dotnet --version" 2>&1' 2>/dev/null


cd /Users/mohammedkhan/iq/health && sleep 3 && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "journalctl -u mental-health-app --no-pager -n 15 | tail -15" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "journalctl -u mental-health-app --no-pager -n 30 | grep -i -E '(certificate|cert|ssl|kestrel|error|exception)' | head -20" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "cat /etc/systemd/system/mental-health-app.service | grep -A 5 'Environment='" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ./deploy/setup-https-server.sh 2>&1 | tail -30

cd /Users/mohammedkhan/iq/health && sleep 3 && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "systemctl status mental-health-app --no-pager | head -15" 2>/dev/null

cd /Users/mohammedkhan/iq/health && curl -k https://${DROPLET_IP}/api/health 2>&1


cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "netstat -tlnp | grep -E ':(80|443|5262)'" 2>/dev/null

#still unable to connect
cd /Users/mohammedkhan/iq/health && curl -k https://${DROPLET_IP}:5262/api/health 2>&1

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "systemctl status nginx --no-pager | head -10" 2>/dev/null

cd /Users/mohammedkhan/iq/health && curl -k http://${DROPLET_IP}/api/health 2>&1

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "ss -tlnp | grep 5262" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "cat /etc/systemd/system/mental-health-app.service | grep ASPNETCORE_URLS" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "curl -k https://localhost:5262/api/health 2>&1" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "cat /etc/nginx/sites-available/mental-health-app | head -60" 2>/dev/null


cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} 'cat > /etc/systemd/system/mental-health-app.service << "EOFSERVICE"
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
systemctl restart mental-health-app
sleep 3
systemctl status mental-health-app --no-pager | head -10' 2>/dev/null


cd /Users/mohammedkhan/iq/health && ./deploy/setup-nginx-https.sh 2>&1 | tail -20


cd /Users/mohammedkhan/iq/health && sleep 3 && curl -k https://${DROPLET_IP}/api/health 2>&1


cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "ss -tlnp | grep -E ':(80|443|5262)'" 2>/dev/null


cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} 'cat > /etc/nginx/sites-available/mental-health-app << "ENDOFFILE"
# Upstream for API server (HTTPS backend) - use IPv4 explicitly
upstream api_backend {
    server 127.0.0.1:5262;
}

# HTTP server - redirect to HTTPS
server {
    listen 80;
    server_name ${DROPLET_IP};
    
    # Allow Let'\''s Encrypt validation
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
    
    # Redirect all other traffic to HTTPS
    location / {
        return 301 https://$server_name$request_uri;
    }
}

# HTTPS server
server {
    listen 443 ssl http2;
    server_name ${DROPLET_IP};
    
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
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
    
    # SignalR Hub
    location /mobilehub {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400;
    }
    
    # WebSocket support
    location /realtime {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Root directory for static files (corrected path)
    root /opt/mental-health-app/client/wwwroot/wwwroot;
    
    # Blazor framework files - must come before the catch-all
    location /_framework {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
    
    # Static files (Blazor client) - catch-all for SPA routing
    location / {
        try_files $uri $uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
}
ENDOFFILE
nginx -t && systemctl reload nginx && echo "✅ Nginx reloaded"' 2>/dev/null


cd /Users/mohammedkhan/iq/health && sleep 2 && curl -k https://${DROPLET_IP}/api/health 2>&1

cd /Users/mohammedkhan/iq/health && curl -k https://${DROPLET_IP}/login 2>&1 | head -20 
cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "find /opt/mental-health-app/client -name 'index.html' -type f 2>/dev/null" 2>/dev/null


cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "tail -10 /var/log/nginx/error.log" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} "ls -la /opt/mental-health-app/client/wwwroot/wwwroot/index.html 2>/dev/null || ls -la /opt/mental-health-app/client/wwwroot/index.html 2>/dev/null || echo 'index.html not found'" 2>/dev/null

cd /Users/mohammedkhan/iq/health && ssh -i "$HOME/.ssh/id_rsa" -o StrictHostKeyChecking=no root@${DROPLET_IP} 'cat > /etc/nginx/sites-available/mental-health-app << "ENDOFFILE"
# Upstream for API server (HTTPS backend) - use IPv4 explicitly
upstream api_backend {
    server 127.0.0.1:5262;
}

# HTTP server - redirect to HTTPS
server {
    listen 80;
    server_name ${DROPLET_IP};
    
    # Allow Let'\''s Encrypt validation
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
    
    # Redirect all other traffic to HTTPS
    location / {
        return 301 https://$server_name$request_uri;
    }
}

# HTTPS server
server {
    listen 443 ssl http2;
    server_name ${DROPLET_IP};
    
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
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "keep-alive";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
    
    # SignalR Hub
    location /mobilehub {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400;
    }
    
    # WebSocket support
    location /realtime {
        proxy_pass https://api_backend;
        proxy_http_version 1.1;
        proxy_ssl_verify off;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Root directory for static files
    root /opt/mental-health-app/client/wwwroot;
    
    # Blazor framework files - must come before the catch-all
    location /_framework {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
    
    # Static files (Blazor client) - catch-all for SPA routing
    location / {
        try_files $uri $uri/ /index.html;
        add_header Cache-Control "no-cache";
    }
}
ENDOFFILE
nginx -t && systemctl reload nginx && echo "✅ Nginx reloaded"' 2>/dev/null




cd /Users/mohammedkhan/iq/health && sleep 2 && curl -k https://${DROPLET_IP}/login 2>&1 | head -20


cd /Users/mohammedkhan/iq/health && curl -k https://${DROPLET_IP}/ 2>&1 | head -20



./deploy/fix-all-timeouts.sh

./deploy/optimize-ollama-performance.sh

./deploy/preload-ollama-model.sh

./deploy/add-isactive-column.sh

./deploy/add-ollama-config.sh   

./deploy/add-clinicalnotes-columns.sh

./deploy/preload-ollama-model.sh

./deploy/configure-ollama-keepalive.sh

./deploy/diagnose-502.sh

./deploy/install-ollama.sh

./deploy/fix-ollama-config-correct.sh

./deploy/fix-production-certificates.sh

./deploy/export-import-data.sh

./deploy/apply-dob-encryption-migration.sh

./deploy/encrypt-existing-dob-data.sh

./deploy/apply-userrequest-dob-migration.sh

./deploy/encrypt-dob-data.sh

./deploy/apply-mobilephone-encryption-migration.sh

./deploy/apply-accident-fields-migration.sh

./deploy/encrypt-existing-mobilephone-data.sh

./deploy/fix-nginx-odata.sh

echo "this is for reference in case you want to see the logs"
echo "ssh -i ~/.ssh/id_rsa root@${DROPLET_IP} 'journalctl -u mental-health-app -f --no-pager'"

echo "if you are gettin connection issue, refer mynotes.md"

echo "make sure you push certificate to digital ocean machine for git build to work"
