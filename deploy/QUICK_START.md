# Quick Start Deployment Guide

## 🚀 Fast Track to Production

### 1. Create DigitalOcean Droplet (5 minutes)

- Go to [digitalocean.com](https://www.digitalocean.com)
- Create → Droplets
- Ubuntu 22.04, 2GB RAM, $12/month
- Add your SSH key
- Note the IP address

### 2. Configure Scripts (2 minutes)

Edit `digitalocean-deploy.sh`:

```bash
DROPLET_IP="YOUR_IP_HERE"  # e.g., "157.230.123.45"
```

### 3. Deploy (10-15 minutes)

```bash
cd deploy
./digitalocean-deploy.sh
```

### 4. Run Database Migrations

The database is created automatically, but you need to run migrations:

```bash
./setup-database.sh
```

Or manually:

```bash
ssh root@YOUR_IP 'cd /opt/mental-health-app/server && dotnet ef database update'
```

### 5. Update Configuration

After deployment, SSH to your server and edit:

```bash
ssh root@YOUR_IP
nano /opt/mental-health-app/server/appsettings.Production.json
```

Update:

- Agora App ID and Certificate
- JWT Secret Key (generate a strong one)
- Other API keys (HuggingFace, OpenAI, etc.)
- **Note**: Database connection string is already configured!

Then restart:

```bash
systemctl restart mental-health-app
```

### 6. Test

Open in browser: `http://YOUR_IP`

## 🔒 SSL Setup (Optional - If you have a domain)

1. Point DNS to your droplet IP
2. Edit `setup-ssl.sh`:
   ```bash
   DOMAIN="yourdomain.com"
   EMAIL="your@email.com"
   ```
3. Run: `./setup-ssl.sh`

## 📝 Important Notes

- **First deployment takes 10-15 minutes** (installing dependencies including MySQL)
- **Database is automatically created** with secure credentials
- **Run database migrations** after deployment: `./setup-database.sh`
- **Update appsettings.Production.json** with your actual API keys
- **Save the database password** shown at the end of deployment
- **Test the application** before going live
- **Monitor logs**: `ssh root@YOUR_IP 'journalctl -u mental-health-app -f'`

## 🔄 Updating After Code Changes

Use the quick update script:

```bash
./update-app.sh
```

This is much faster than full deployment.

## ❓ Troubleshooting

**App not accessible?**

- Check if running: `systemctl status mental-health-app`
- Check logs: `journalctl -u mental-health-app -n 50`
- Check Nginx: `systemctl status nginx`

**502 Bad Gateway?**

- Application might not be running
- Check application logs
- Verify port 5262 is accessible

**Need help?** Check the full README.md for detailed troubleshooting.
