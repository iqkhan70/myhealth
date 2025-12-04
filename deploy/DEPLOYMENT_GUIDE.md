# Complete Deployment Guide

## 🚀 Quick Start (One Script)

For a complete automated deployment, run:

```bash
cd deploy
chmod +x deploy-all.sh
./deploy-all.sh
```

This script will:
1. ✅ Deploy the application (server + client)
2. ✅ Set up HTTPS for the server
3. ✅ Set up HTTPS for Nginx
4. ✅ Run database migrations
5. ✅ Verify everything is running

## 📋 Manual Step-by-Step Deployment

If you prefer to run steps manually or need more control:

### Step 1: Initial Deployment

```bash
cd deploy
chmod +x digitalocean-deploy.sh
./digitalocean-deploy.sh
```

**What it does:**
- Installs .NET, MySQL, Redis, Nginx
- Builds and publishes the application
- Creates database and user
- Sets up systemd service
- Configures Nginx (HTTP only)

**Time:** ~10-15 minutes

### Step 2: Set Up HTTPS for Server

```bash
./setup-https-server.sh
```

**What it does:**
- Generates self-signed SSL certificate
- Configures .NET server to use HTTPS on port 5262
- Restarts the service

**Time:** ~1 minute

### Step 3: Set Up HTTPS for Nginx

```bash
./setup-nginx-https.sh
```

**What it does:**
- Generates self-signed SSL certificate for Nginx
- Configures Nginx to serve HTTPS on port 443
- Sets up HTTP to HTTPS redirect
- Restarts Nginx

**Time:** ~1 minute

### Step 4: Run Database Migrations

Choose one method:

**Option A: Generate SQL and Apply (Recommended)**
```bash
./generate-migration-sql.sh
```
- Generates SQL from local migrations
- Applies to remote database
- No .NET SDK needed on server

**Option B: Copy Files and Run on Server**
```bash
./copy-migration-files.sh
ssh root@YOUR_IP 'cd /opt/mental-health-app/server && dotnet ef database update'
```
- Requires .NET SDK on server

**Option C: Direct Connection**
```bash
./apply-migration-direct.sh
```
- Connects local dotnet ef to remote database

**Time:** ~2-5 minutes

### Step 5: Update Configuration

SSH to the server and update `appsettings.Production.json`:

```bash
ssh root@YOUR_IP
nano /opt/mental-health-app/server/appsettings.Production.json
```

**Update these values:**
- `Agora:AppId` - Your Agora App ID
- `Agora:AppCertificate` - Your Agora App Certificate
- `Jwt:Key` - Generate a strong secret (min 32 chars)
- `S3:AccessKey` - DigitalOcean Spaces access key
- `S3:SecretKey` - DigitalOcean Spaces secret key
- Other API keys as needed

**Restart the service:**
```bash
systemctl restart mental-health-app
```

### Step 6: Import Data (Optional)

If you want to transfer data from your local database:

```bash
./export-import-data.sh
```

**What it does:**
- Exports data from local MySQL
- Imports to DigitalOcean MySQL
- Creates missing tables if needed

**Time:** Depends on data size

## 🔄 Updating After Code Changes

For quick updates (code changes only, no infrastructure changes):

```bash
./update-app.sh
```

**What it does:**
- Builds and publishes locally
- Copies files to server
- Restarts the service

**Time:** ~2-3 minutes

## 🔍 Verification & Troubleshooting

### Check Service Status

```bash
ssh root@YOUR_IP 'systemctl status mental-health-app'
```

### Check Logs

```bash
ssh root@YOUR_IP 'journalctl -u mental-health-app -f'
```

### Check Nginx

```bash
ssh root@YOUR_IP 'systemctl status nginx'
ssh root@YOUR_IP 'nginx -t'  # Test configuration
```

### Test Endpoints

```bash
# Health check
curl -k https://YOUR_IP/api/health

# Login page
curl -k https://YOUR_IP/login
```

### Check Database

```bash
./query-database.sh
```

## 📝 Configuration Files

### Server Configuration
- **Location:** `/opt/mental-health-app/server/appsettings.Production.json`
- **Contains:** Database, Agora, S3, JWT, Redis, etc.

### Nginx Configuration
- **Location:** `/etc/nginx/sites-available/mental-health-app`
- **Contains:** Reverse proxy rules, SSL settings

### Systemd Service
- **Location:** `/etc/systemd/system/mental-health-app.service`
- **Contains:** Service definition, environment variables

## 🔐 Security Notes

1. **Self-Signed Certificates:** The deployment uses self-signed certificates. Users will see a security warning. For production, use Let's Encrypt (see `setup-ssl.sh`).

2. **Database Password:** Saved during deployment. Keep it secure.

3. **SSH Keys:** Ensure your SSH key has proper permissions (600).

4. **Firewall:** The deployment opens necessary ports (80, 443, 5262, 22).

## 🆘 Common Issues

### Application Not Starting
```bash
# Check logs
ssh root@YOUR_IP 'journalctl -u mental-health-app -n 50'

# Check if port is in use
ssh root@YOUR_IP 'netstat -tlnp | grep 5262'
```

### 502 Bad Gateway
- Application might not be running
- Check service status
- Verify Nginx can reach the backend

### Database Connection Issues
```bash
# Test MySQL connection
ssh root@YOUR_IP 'mysql -u mentalhealth_user -p mentalhealthdb'
```

### SSL Certificate Warnings
- Normal for self-signed certificates
- Users need to accept the certificate
- For production, use Let's Encrypt

## 📚 Additional Scripts

- `reset-password-now.sh` - Reset user password
- `check-status.sh` - Check application status
- `query-database.sh` - Query database contents
- `fix-ssh-permissions.sh` - Fix SSH key permissions

## 🎯 Deployment Checklist

- [ ] Droplet created and IP noted
- [ ] SSH key added to droplet
- [ ] `digitalocean-deploy.sh` configured with correct IP
- [ ] Initial deployment completed
- [ ] HTTPS configured (server + Nginx)
- [ ] Database migrations run
- [ ] `appsettings.Production.json` updated with real values
- [ ] Service restarted after config changes
- [ ] Application accessible via HTTPS
- [ ] Login working
- [ ] Data imported (if needed)

## 📞 Need Help?

1. Check logs: `journalctl -u mental-health-app -f`
2. Verify service: `systemctl status mental-health-app`
3. Test endpoints: `curl -k https://YOUR_IP/api/health`
4. Review this guide and README.md

