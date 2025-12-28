# Deployment Guide

Complete step-by-step guide for deploying the Mental Health App to staging/production environments.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Deployment Workflow](#deployment-workflow)
- [Step-by-Step Instructions](#step-by-step-instructions)
- [Troubleshooting](#troubleshooting)
- [Quick Reference](#quick-reference)

---

## Prerequisites

Before starting, ensure you have:

- ✅ SSH access to your DigitalOcean droplet
- ✅ SSH key configured (`~/.ssh/id_rsa`)
- ✅ Docker installed locally (for building images)
- ✅ DNS records configured (for domain access)
- ✅ DigitalOcean Container Registry access

---

## Deployment Workflow

```
┌─────────────────────────────────────────────────────────┐
│  Step 1: Initial Deployment (Self-Signed Certificates)  │
│  └─> Deploy app, works immediately                       │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│  Step 2: Test Application                               │
│  └─> Verify everything works                            │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│  Step 3: Fix Certificate (Optional)                    │
│  └─> Only if certificate doesn't include domains        │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│  Step 4: Upgrade to Let's Encrypt (When Ready)          │
│  └─> Get trusted SSL certificate (no browser warnings)  │
└─────────────────────────────────────────────────────────┘
```

---

## Step-by-Step Instructions

### Step 1: Initial Deployment (Self-Signed Certificate)

Deploy your application with self-signed certificates. This works immediately, even if DNS isn't fully propagated yet.

```bash
cd deploy
./consolidated-container-deploy.sh staging
```

**What this does:**
- ✅ Builds and pushes Docker images to DigitalOcean Container Registry
- ✅ Sets up the droplet (Docker, Docker Compose, etc.)
- ✅ Generates self-signed SSL certificates (includes your domain names)
- ✅ Creates `.env` file with all configuration
- ✅ Runs database migrations
- ✅ Seeds the database with initial data
- ✅ Sets up Ollama and pulls AI models
- ✅ Starts all containers (API, Web, MySQL, Redis, Ollama)

**Expected Output:**
- Application will be accessible via IP address immediately
- Browser will show certificate warning (this is normal for self-signed certs)
- You can click "Advanced" → "Proceed to site" to continue

**After deployment, you'll see:**
```
Application URL (Domain): https://caseflowstage.store
Application URL (WWW): https://www.caseflowstage.store
Application URL (IP): https://<your-droplet-ip>
```

---

### Step 2: Test Your Application

Verify everything is working correctly before proceeding.

#### 2.1 Test via IP Address (Works Immediately)

1. Open your browser and navigate to:
   ```
   https://<your-droplet-ip>/
   ```

2. You'll see a security warning (expected for self-signed certificates):
   - **Chrome/Edge**: Click "Advanced" → "Proceed to <IP> (unsafe)"
   - **Firefox**: Click "Advanced" → "Accept the Risk and Continue"
   - **Safari**: Click "Show Details" → "visit this website"

3. Verify:
   - ✅ Application loads
   - ✅ Login works
   - ✅ API endpoints respond
   - ✅ Database connections work

#### 2.2 Test via Domain (Once DNS Propagates)

DNS propagation can take 1-24 hours. Test when ready:

1. Navigate to:
   ```
   https://caseflowstage.store/
   https://www.caseflowstage.store/
   ```

2. You'll still see a certificate warning (self-signed), but click through to test

3. Verify:
   - ✅ Domain resolves correctly
   - ✅ Application works via domain
   - ✅ Both `caseflowstage.store` and `www.caseflowstage.store` work

#### 2.3 Check Container Status

SSH into your droplet and verify containers are running:

```bash
ssh root@<your-droplet-ip>
cd /opt/mental-health-app
docker compose ps
```

All containers should show "Up" status.

#### 2.4 Check Logs (If Issues)

```bash
# View all logs
docker compose logs -f

# View specific service logs
docker compose logs -f api
docker compose logs -f web
```

---

### Step 3: Fix Certificate if Needed (Optional)

If the certificate doesn't include your domain names, regenerate it:

```bash
cd deploy
./regenerate-cert-with-domains.sh staging
```

**What this does:**
- ✅ Backs up existing certificates
- ✅ Regenerates certificate with domain names included
- ✅ Restarts nginx container to pick up new certificate
- ✅ Shows certificate details for verification

**When to use:**
- Certificate was generated before domain configuration was added
- Certificate only shows IP address, not domain names
- You're getting certificate errors for your domain

**After running:**
- Certificate will include `caseflowstage.store` and `www.caseflowstage.store`
- You'll still see browser warnings (self-signed cert)
- Click through to proceed

---

### Step 4: Upgrade to Let's Encrypt (When Ready)

Once DNS is fully working and you've tested everything, upgrade to a trusted SSL certificate. This eliminates browser warnings.

**Prerequisites:**
- ✅ DNS fully propagated (test with `nslookup caseflowstage.store`)
- ✅ Domain accessible via HTTP (port 80)
- ✅ Valid email address for Let's Encrypt notifications

**Run the setup:**

```bash
cd deploy
./setup-letsencrypt.sh staging your-email@example.com
```

Replace `your-email@example.com` with your actual email address.

**What this does:**
- ✅ Installs certbot on the droplet (host system)
- ✅ Stops nginx container temporarily
- ✅ Generates Let's Encrypt certificates via HTTP challenge
- ✅ Copies certificates to `/opt/mental-health-app/certs/`
- ✅ Restarts nginx container
- ✅ Sets up auto-renewal (cron job runs twice daily)

**Expected Output:**
```
✅ Let's Encrypt Setup Complete!

Your site now has a trusted SSL certificate!

Access your site:
  https://caseflowstage.store
  https://www.caseflowstage.store

Certificate details:
  Location: /opt/mental-health-app/certs/server.crt
  Auto-renewal: Enabled (runs twice daily)
```

**After setup:**
- ✅ No browser warnings
- ✅ Green padlock in browser
- ✅ Certificates auto-renew every 90 days
- ✅ Email notifications for renewal issues

**Verify certificate:**

```bash
# Check certificate expiration
ssh root@<your-droplet-ip> 'certbot certificates'

# Manually renew (if needed)
ssh root@<your-droplet-ip> '/opt/mental-health-app/renew-cert.sh'
```

---

## Troubleshooting

### Certificate Issues

**Problem:** Browser shows `ERR_CERT_AUTHORITY_INVALID`

**Solution:**
- For self-signed certificates: This is expected. Click "Advanced" → "Proceed to site"
- For Let's Encrypt: Run `./setup-letsencrypt.sh staging your-email@example.com`

**Problem:** Certificate doesn't include domain names

**Solution:**
```bash
./regenerate-cert-with-domains.sh staging
```

### DNS Issues

**Problem:** Domain doesn't resolve

**Solution:**
1. Check DNS records:
   ```bash
   nslookup caseflowstage.store
   nslookup www.caseflowstage.store
   ```

2. Verify DNS points to your droplet IP

3. Wait for DNS propagation (can take 1-24 hours)

### Container Issues

**Problem:** Containers not starting

**Solution:**
```bash
ssh root@<your-droplet-ip>
cd /opt/mental-health-app
docker compose logs -f  # Check logs
docker compose ps       # Check status
docker compose restart  # Restart all containers
```

**Problem:** API container keeps restarting

**Solution:**
```bash
# Check API logs
docker compose logs api --tail 100

# Check database connection
docker compose exec api dotnet ef database update

# Verify .env file
cat /opt/mental-health-app/.env
```

### Let's Encrypt Issues

**Problem:** Certbot fails with "Connection refused"

**Solution:**
- Ensure port 80 is accessible from internet
- Check firewall: `ufw status`
- Verify nginx container is stopped during certificate generation

**Problem:** Certificate renewal fails

**Solution:**
```bash
# Check renewal logs
ssh root@<your-droplet-ip> 'cat /var/log/certbot-renewal.log'

# Manually renew
ssh root@<your-droplet-ip> '/opt/mental-health-app/renew-cert.sh'
```

---

## Quick Reference

### Deployment Commands

| Step | Command | When to Use |
|------|---------|-------------|
| **Deploy** | `./consolidated-container-deploy.sh staging` | First time or when updating code |
| **Fix Cert** | `./regenerate-cert-with-domains.sh staging` | If certificate doesn't include domains |
| **Let's Encrypt** | `./setup-letsencrypt.sh staging your-email@example.com` | When DNS is working and you want trusted cert |

### Useful Commands

```bash
# Check container status
ssh root@<droplet-ip> 'cd /opt/mental-health-app && docker compose ps'

# View logs
ssh root@<droplet-ip> 'cd /opt/mental-health-app && docker compose logs -f'

# Restart containers
ssh root@<droplet-ip> 'cd /opt/mental-health-app && docker compose restart'

# Scale API containers (for production)
ssh root@<droplet-ip> 'cd /opt/mental-health-app && docker compose up -d --scale api=3'

# Check certificate expiration (Let's Encrypt)
ssh root@<droplet-ip> 'certbot certificates'

# Manually renew certificate
ssh root@<droplet-ip> '/opt/mental-health-app/renew-cert.sh'
```

### Environment-Specific Domains

**Staging:**
- Domain: `caseflowstage.store`
- WWW: `www.caseflowstage.store`

**Production:**
- Domain: `caseflow.store`
- WWW: `www.caseflow.store`

---

## Important Notes

### DNS Propagation
- DNS changes can take **1-24 hours** to fully propagate
- Test with IP address first while waiting
- Use `nslookup` or `dig` to check DNS status

### Let's Encrypt Requirements
- ✅ DNS fully propagated
- ✅ Port 80 accessible from internet
- ✅ Valid email address (for expiration notices)
- ✅ Domain must resolve to your droplet IP

### Certificate Types

**Self-Signed (Default):**
- ✅ Works immediately
- ✅ No DNS required
- ⚠️ Browser shows security warning
- ✅ Good for testing/development

**Let's Encrypt:**
- ✅ No browser warnings
- ✅ Trusted by all browsers
- ✅ Auto-renewal setup
- ⚠️ Requires DNS to be working
- ⚠️ Requires port 80 accessible

### Auto-Renewal

Let's Encrypt certificates expire every 90 days. The setup script configures:
- Cron job runs twice daily
- Only renews if certificate expires in < 30 days
- Automatically restarts nginx after renewal
- Logs to `/var/log/certbot-renewal.log`

---

## Next Steps After Deployment

1. **Monitor Logs:**
   ```bash
   ssh root@<droplet-ip> 'cd /opt/mental-health-app && docker compose logs -f'
   ```

2. **Set Up Monitoring:**
   - Configure uptime monitoring
   - Set up error alerting
   - Monitor certificate expiration

3. **Scale for Production:**
   ```bash
   ssh root@<droplet-ip> 'cd /opt/mental-health-app && docker compose up -d --scale api=3'
   ```

4. **Backup Strategy:**
   - Database backups
   - Certificate backups
   - Configuration backups

---

## Support

If you encounter issues:

1. Check container logs: `docker compose logs -f`
2. Verify DNS: `nslookup caseflowstage.store`
3. Check certificate: `certbot certificates`
4. Review this guide's troubleshooting section

For production deployments, always test in staging first!

