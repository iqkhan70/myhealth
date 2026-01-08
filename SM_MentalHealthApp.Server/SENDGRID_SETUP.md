# SendGrid Email Setup

## Why SendGrid?

✅ **No SMTP port issues** - Works from anywhere (local, DigitalOcean, etc.)  
✅ **Free tier**: 100 emails/day forever  
✅ **Reliable**: Industry-standard email API  
✅ **Easy setup**: Just an API key, no firewall/port configuration  

## Quick Setup (5 minutes)

### 1. Create SendGrid Account
1. Go to https://signup.sendgrid.com/
2. Sign up (free account)
3. Verify your email

### 2. Get API Key
1. Go to https://app.sendgrid.com/settings/api_keys
2. Click "Create API Key"
3. Name it: "Health App Production"
4. Select "Full Access" (or "Mail Send" only for security)
5. Copy the API key (starts with `SG.`)

### 3. Configure Your App

#### For Local Development:
Add to `appsettings.Development.json`:
```json
"Email": {
  "Enabled": true,
  "Provider": "SendGrid",
  "SendGridApiKey": "SG.your_api_key_here",
  "FromEmail": "your-email@example.com",
  "FromName": "Health App"
}
```

#### For Production/Staging:
Set environment variable:
```bash
export Email__SendGridApiKey="SG.your_api_key_here"
```

Or in Docker `.env`:
```
Email__SendGridApiKey=SG.your_api_key_here
```

### 4. Verify Sender Identity (Required for Production)

SendGrid requires you to verify your sender email:

1. Go to https://app.sendgrid.com/settings/sender_auth/senders
2. Click "Create New Sender"
3. Enter your email address
4. Verify via email link

**Note**: For testing, you can use the default SendGrid test sender, but for production you must verify your domain or email.

## Configuration Options

The app supports two email providers:

### SendGrid (Recommended - Default)
```json
"Email": {
  "Provider": "SendGrid",
  "SendGridApiKey": "SG.xxx",
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
3. Check SendGrid dashboard: https://app.sendgrid.com/activity

## Troubleshooting

**"API key invalid"**
- Make sure the API key starts with `SG.`
- Check you copied the full key (no spaces)

**"Sender not verified"**
- Verify your sender email in SendGrid dashboard
- For production, verify your domain

**"Email not received"**
- Check SendGrid activity dashboard
- Check spam folder
- Verify sender email is correct

## Cost

- **Free tier**: 100 emails/day forever
- **Paid plans**: Start at $19.95/month for 50,000 emails

For most apps, the free tier is sufficient!

