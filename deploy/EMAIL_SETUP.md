# Email Configuration for DigitalOcean Deployment

## Quick Setup: Mailgun API (Recommended)

Mailgun is recommended because it:

- ✅ Works from anywhere (no SMTP port issues)
- ✅ Free tier: 5,000 emails/month for 3 months, then 1,000/month
- ✅ No firewall/port configuration needed

### Step 1: Get Mailgun API Key and Domain

1. Sign up at https://signup.mailgun.com/ (free)
2. Go to https://app.mailgun.com/app/dashboard
3. Note your domain (e.g., `sandbox12345.mailgun.org` for free tier)
4. Go to Settings → API Keys
5. Copy your Private API key

### Step 2: Authorize Recipients (Free Tier)

For sandbox domain (free tier):

1. Go to Sending → Authorized Recipients
2. Add email addresses you want to send to
3. Verify them via email

For production (custom domain):

1. Go to Sending → Domains
2. Add your domain
3. Configure DNS records as shown
4. Wait for verification

### Step 3: Configure Deployment

#### Option A: Add to secrets.env (Recommended)

Create or edit `deploy/secrets.env`:

```bash
EMAIL_PROVIDER="Mailgun"
EMAIL_MAILGUN_API_KEY="your_mailgun_api_key_here"
EMAIL_MAILGUN_DOMAIN="sandbox12345.mailgun.org"
EMAIL_FROMEMAIL="noreply@healthapp.com"
EMAIL_FROMNAME="Customer Support App"
APP_BASE_URL="https://caseflowstage.store"  # For staging
# APP_BASE_URL="https://caseflow.store"     # For production
```

The deployment script will automatically load this file.

#### Option B: Set Environment Variables

Before running deployment:

```bash
export EMAIL_PROVIDER="Mailgun"
export EMAIL_MAILGUN_API_KEY="your_api_key_here"
export EMAIL_MAILGUN_DOMAIN="sandbox12345.mailgun.org"
export EMAIL_FROMEMAIL="noreply@healthapp.com"
export EMAIL_FROMNAME="Customer Support App"
export APP_BASE_URL="https://caseflowstage.store"  # For staging
# export APP_BASE_URL="https://caseflow.store"      # For production
./consolidated-container-deploy.sh staging
```

#### Option C: Manual .env Edit on Server

After deployment, SSH to your server and edit `/opt/mental-health-app/.env`:

```bash
EMAIL_PROVIDER=Mailgun
EMAIL_MAILGUN_API_KEY=your_api_key_here
EMAIL_MAILGUN_DOMAIN=sandbox12345.mailgun.org
EMAIL_FROMEMAIL=noreply@healthapp.com
EMAIL_FROMNAME=Customer Support App
AppSettings__BaseUrl=https://caseflowstage.store
```

Then restart containers:

```bash
cd /opt/mental-health-app
docker-compose restart api
```

## Fallback to SMTP (If Needed)

If you prefer SMTP or Mailgun isn't working, set:

```bash
EMAIL_PROVIDER="SMTP"
EMAIL_SMTPHOST="smtp.gmail.com"
EMAIL_SMTPUSERNAME="your-email@gmail.com"
EMAIL_SMTPPASSWORD="your-app-password"
EMAIL_FROMEMAIL="your-email@gmail.com"
```

**Note**: SMTP may have port/firewall issues on DigitalOcean.

## Testing

After configuration, test email sending:

1. Use the "Forgot Password" feature
2. Check your email inbox
3. Check Mailgun dashboard: https://app.mailgun.com/app/logs

## Troubleshooting

**"Mailgun API key or domain is not configured"**

- Make sure both `EMAIL_MAILGUN_API_KEY` and `EMAIL_MAILGUN_DOMAIN` are set in `.env` file
- Restart containers after updating `.env`

**"Unauthorized" error**

- Verify API key is correct
- Check API key hasn't been revoked in Mailgun dashboard
- Make sure you're using Private API key (not Public)

**"Forbidden" error (sandbox domain)**

- Add recipient to Authorized Recipients list in Mailgun dashboard
- Verify recipient email address

**Emails not received**

- Check Mailgun logs dashboard
- Check spam folder
- For sandbox: Make sure recipient is in authorized list
