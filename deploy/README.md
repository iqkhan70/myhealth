# DigitalOcean Deployment Guide

This guide will help you deploy your Blazor Mental Health App to DigitalOcean.

## Prerequisites

1. **DigitalOcean Account**: Sign up at [digitalocean.com](https://www.digitalocean.com)
2. **Domain Name** (optional but recommended): You can use a free domain from Freenom or purchase one
3. **SSH Key**: Generate one if you don't have it:
   ```bash
   ssh-keygen -t rsa -b 4096 -C "your_email@example.com"
   ```

## Step 1: Create a DigitalOcean Droplet

1. Log in to DigitalOcean
2. Click "Create" → "Droplets"
3. Choose:
   - **Image**: Ubuntu 22.04 LTS
   - **Plan**: Basic, Regular Intel with SSD (at least 2GB RAM, 1 vCPU)
   - **Region**: Choose closest to your users
   - **Authentication**: Add your SSH key
   - **Hostname**: mental-health-app (or your choice)
4. Click "Create Droplet"
5. Wait for droplet to be created and note the **IP address**

## Step 2: Configure Deployment Scripts

Edit `digitalocean-deploy.sh` and update these variables:

```bash
DROPLET_IP="YOUR_DROPLET_IP_HERE"  # e.g., "157.230.123.45"
DROPLET_USER="root"
SSH_KEY_PATH="$HOME/.ssh/id_rsa"  # Path to your SSH private key
DOMAIN=""  # Your domain (optional, leave empty to use IP)
```

## Step 3: Update Application Configuration

Before deploying, update your configuration:

1. **Database Connection String**: Update in `appsettings.Production.json` (the script creates this)
2. **Agora Credentials**: Add your App ID and Certificate
3. **JWT Secret Key**: Generate a strong secret key (minimum 32 characters)
4. **Redis**: Should work with default localhost:6379

## Step 4: Deploy the Application

Make scripts executable and run:

```bash
cd deploy
chmod +x digitalocean-deploy.sh setup-ssl.sh
./digitalocean-deploy.sh
```

The script will:

- Install .NET 8 runtime, Redis, Nginx, and other dependencies
- Build and publish your application
- Copy files to the server
- Set up systemd service
- Configure Nginx as reverse proxy
- Start the application

## Step 5: Set Up SSL (If You Have a Domain)

If you have a domain name:

1. **Point DNS to your droplet**:

   - Add an A record: `@` → `YOUR_DROPLET_IP`
   - Add an A record: `www` → `YOUR_DROPLET_IP`
   - Wait for DNS propagation (can take a few minutes to hours)

2. **Edit `setup-ssl.sh`**:

   ```bash
   DOMAIN="yourdomain.com"
   EMAIL="your_email@example.com"
   ```

3. **Run SSL setup**:
   ```bash
   ./setup-ssl.sh
   ```

## Step 6: Update Client Configuration

After deployment, you need to update the client to point to your server:

1. **Update `ServerUrlService.cs`** or your server URL configuration
2. **Rebuild and redeploy** the client

Or update the client's `appsettings.json` to use your production URL.

## Post-Deployment Checklist

- [ ] Application is accessible at `http://YOUR_IP` or `https://YOUR_DOMAIN`
- [ ] SSL certificate is installed (if using domain)
- [ ] **Database is set up and migrations are run**: `./setup-database.sh` or manually
- [ ] Database connection is working: Check application logs for connection errors
- [ ] MySQL is running: `ssh root@YOUR_IP 'systemctl status mysql'`
- [ ] Redis is running: `ssh root@YOUR_IP 'redis-cli ping'` (should return PONG)
- [ ] Application logs show no errors: `ssh root@YOUR_IP 'journalctl -u mental-health-app -f'`
- [ ] SignalR connections work (test call functionality)

## Useful Commands

### View Application Logs

```bash
ssh root@YOUR_IP 'journalctl -u mental-health-app -f'
```

### Restart Application

```bash
ssh root@YOUR_IP 'systemctl restart mental-health-app'
```

### Check Application Status

```bash
ssh root@YOUR_IP 'systemctl status mental-health-app'
```

### Check Nginx Status

```bash
ssh root@YOUR_IP 'systemctl status nginx'
```

### Check Redis Status

```bash
ssh root@YOUR_IP 'redis-cli ping'
```

### Check MySQL Status

```bash
ssh root@YOUR_IP 'systemctl status mysql'
```

### Connect to MySQL Database

```bash
# Get the password from appsettings.Production.json or deployment output
ssh root@YOUR_IP 'mysql -u mentalhealth_user -p mentalhealthdb'
```

### Run Database Migrations

```bash
# Use the setup-database.sh script
./setup-database.sh

# Or manually:
ssh root@YOUR_IP 'cd /opt/mental-health-app/server && dotnet ef database update'
```

### Update Application (After Code Changes)

```bash
# Build locally
dotnet publish SM_MentalHealthApp.Server/SM_MentalHealthApp.Server.csproj -c Release -o ./publish/server
dotnet publish SM_MentalHealthApp.Client/SM_MentalHealthApp.Client.csproj -c Release -o ./publish/client

# Copy to server
scp -r ./publish/server/* root@YOUR_IP:/opt/mental-health-app/server/
scp -r ./publish/client/* root@YOUR_IP:/opt/mental-health-app/client/

# Restart service
ssh root@YOUR_IP 'systemctl restart mental-health-app'
```

## Troubleshooting

### Application Won't Start

1. Check logs: `journalctl -u mental-health-app -n 50`
2. Verify configuration: Check `appsettings.Production.json`
3. Check database connection
4. Verify Redis is running

### Nginx 502 Bad Gateway

- Check if application is running: `systemctl status mental-health-app`
- Check application logs for errors
- Verify port 5262 is not blocked

### SSL Certificate Issues

- Ensure DNS is pointing to your droplet
- Check firewall allows port 80 and 443
- Verify domain is accessible via HTTP before running certbot

### SignalR/WebSocket Issues

- Ensure Nginx config includes WebSocket upgrade headers
- Check firewall allows WebSocket connections
- Verify application is listening on correct port

## Security Recommendations

1. **Change default SSH port** (optional but recommended)
2. **Set up fail2ban** for SSH protection
3. **Regularly update system**: `apt-get update && apt-get upgrade`
4. **Use strong JWT secret keys**
5. **Enable firewall**: Already done by script (UFW)
6. **Regular backups**: Set up automated backups for database

## Cost Estimation

- **Droplet**: $12-24/month (2GB RAM, 1 vCPU)
- **Domain**: $10-15/year (optional)
- **Total**: ~$12-25/month

## Support

If you encounter issues:

1. Check application logs
2. Check Nginx error logs: `/var/log/nginx/error.log`
3. Verify all services are running
4. Check firewall rules
