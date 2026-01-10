# Mailgun Email Setup

## Why Mailgun?

✅ **No SMTP port issues** - Works from anywhere (local, DigitalOcean, etc.)  
✅ **Free tier**: 5,000 emails/month for 3 months, then 1,000 emails/month  
✅ **Reliable**: Industry-standard email API  
✅ **Easy setup**: Just an API key and domain, no firewall/port configuration  

## Quick Setup (5 minutes)

### 1. Create Mailgun Account
1. Go to https://signup.mailgun.com/
2. Sign up (free account)
3. Verify your email

### 2. Get API Key and Domain
1. Go to https://app.mailgun.com/app/dashboard
2. Your domain will be shown (e.g., `sandbox12345.mailgun.org` for free tier)
3. Go to Settings → API Keys
4. Copy your Private API key

### 3. Verify Domain (For Production)

**For Free Tier (Sandbox Domain):**
- You can send to up to 5 authorized recipients
- Go to Sending → Authorized Recipients
- Add email addresses you want to send to

**For Production (Custom Domain):**
1. Go to Sending → Domains
2. Click "Add New Domain"
3. Enter your domain (e.g., `mail.yourdomain.com`)
4. Add DNS records as instructed
5. Wait for verification (usually a few minutes)

### 4. Configure Your App

#### For Local Development:
Add to `appsettings.Development.json`:
```json
"Email": {
  "Enabled": true,
  "Provider": "Mailgun",
  "MailgunApiKey": "your_api_key_here",
  "MailgunDomain": "sandbox12345.mailgun.org",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "Health App"
}
```

#### For Production/Staging:
Set environment variables:
```bash
export Email__MailgunApiKey="your_api_key_here"
export Email__MailgunDomain="your_domain.mailgun.org"
```

Or in Docker `.env`:
```
EMAIL_MAILGUN_API_KEY=your_api_key_here
EMAIL_MAILGUN_DOMAIN=your_domain.mailgun.org
EMAIL_FROMEMAIL=noreply@yourdomain.com
EMAIL_PROVIDER=Mailgun
```

## Configuration Options

The app supports two email providers:

### Mailgun (Recommended - Default)
```json
"Email": {
  "Provider": "Mailgun",
  "MailgunApiKey": "your_api_key",
  "MailgunDomain": "your_domain.mailgun.org",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "Health App"
}
```

### SMTP (Fallback - for local dev)
```json
"Email": {
  "Provider": "SMTP",
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "your-email@gmail.com",
  "SmtpPassword": "your-app-password",
  "FromEmail": "your-email@gmail.com",
  "FromName": "Health App"
}
```

## Testing

After setup, test email sending:
1. Use the forgot password feature
2. Check your email inbox
3. Check Mailgun dashboard: https://app.mailgun.com/app/logs

## Troubleshooting

**"Mailgun API key or domain is not configured"**
- Make sure both `MailgunApiKey` and `MailgunDomain` are set
- Check the domain format (e.g., `sandbox12345.mailgun.org`)

**"Unauthorized" error**
- Verify API key is correct
- Check API key hasn't been revoked
- Make sure you're using the Private API key (not Public)

**"Forbidden" error**
- For sandbox domain: Add recipient to Authorized Recipients list
- For custom domain: Verify domain DNS records are correct

**Emails not received**
- Check Mailgun logs dashboard
- Check spam folder
- For sandbox: Make sure recipient is authorized

## Cost

- **Free tier**: 
  - 5,000 emails/month for first 3 months
  - Then 1,000 emails/month forever
- **Paid plans**: Start at $35/month for 50,000 emails

For most apps, the free tier is sufficient!

