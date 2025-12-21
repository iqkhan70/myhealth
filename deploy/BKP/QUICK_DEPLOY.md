# Quick Deployment Reference

## 🚀 One-Command Deployment

```bash
cd deploy
./deploy-all.sh
```

This runs everything automatically:
1. Full application deployment
2. HTTPS setup (server + Nginx)
3. Database migrations
4. Service verification

## 📋 Manual Steps (If Needed)

### 1. Deploy Application
```bash
./digitalocean-deploy.sh
```

### 2. Setup HTTPS Server
```bash
./setup-https-server.sh
```

### 3. Setup HTTPS Nginx
```bash
./setup-nginx-https.sh
```

### 4. Run Migrations
```bash
./generate-migration-sql.sh  # Recommended
# OR
./copy-migration-files.sh && ssh root@IP 'cd /opt/mental-health-app/server && dotnet ef database update'
```

### 5. Update Config & Restart
```bash
ssh root@YOUR_IP
nano /opt/mental-health-app/server/appsettings.Production.json
# Update: Agora, JWT, S3, etc.
systemctl restart mental-health-app
```

### 6. Test
```bash
curl -k https://YOUR_IP/api/health
# Open in browser: https://YOUR_IP/login
```

## 🔄 Quick Updates (Code Changes Only)

```bash
./update-app.sh
```

## 🆘 Troubleshooting

```bash
# Check status
./check-status.sh

# Check logs
ssh root@YOUR_IP 'journalctl -u mental-health-app -f'

# Reset password
./reset-password-now.sh email@example.com NewPassword123!
```

## 📝 Configuration Location

- **Server Config:** `/opt/mental-health-app/server/appsettings.Production.json`
- **Nginx Config:** `/etc/nginx/sites-available/mental-health-app`
- **Service:** `/etc/systemd/system/mental-health-app.service`

