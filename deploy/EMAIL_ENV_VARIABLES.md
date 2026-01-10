# Email Environment Variables Reference

This document lists all email-related environment variables used in the application.

## Required Variables for Mailgun

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `EMAIL_PROVIDER` | Email provider to use | `Mailgun` | Yes |
| `EMAIL_MAILGUN_API_KEY` | Mailgun API key | `key-1234567890abcdef` | Yes |
| `EMAIL_MAILGUN_DOMAIN` | Mailgun domain | `sandbox12345.mailgun.org` | Yes |
| `EMAIL_FROMEMAIL` | Sender email address | `noreply@healthapp.com` | Yes |
| `EMAIL_FROMNAME` | Sender display name | `Customer Support App` | No (defaults to "Customer Support App") |
| `APP_BASE_URL` | Base URL for password reset links | `https://caseflowstage.store` | Yes |

## Optional SMTP Fallback Variables

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `EMAIL_SMTPHOST` | SMTP server hostname | `smtp.gmail.com` | No |
| `EMAIL_SMTPPORT` | SMTP server port | `587` | No (defaults to 587) |
| `EMAIL_SMTPUSERNAME` | SMTP username | `your-email@gmail.com` | No |
| `EMAIL_SMTPPASSWORD` | SMTP password/app password | `your-app-password` | No |
| `EMAIL_ENABLESSL` | Enable SSL for SMTP | `true` | No (defaults to true) |

## Configuration Files

### 1. Environment Variables Example
**File**: `SM_MentalHealthApp.Server/environment-variables.example`

Contains all email variables with examples and instructions.

### 2. Deployment Script
**File**: `deploy/consolidated-container-deploy.sh`

Sets defaults and includes all email variables in the `.env` file:
- Lines 521-525: Email configuration defaults
- Lines 581-604: Email variables in .env file

### 3. Docker Compose
**File**: `deploy/docker/docker-compose.yml`

Maps environment variables to container environment:
- Lines 51-63: Email configuration
- Line 64: AppSettings__BaseUrl

### 4. AppSettings Files
**Files**: 
- `SM_MentalHealthApp.Server/appsettings.json`
- `SM_MentalHealthApp.Server/appsettings.Staging.json`
- `SM_MentalHealthApp.Server/appsettings.Production.json`

These files use `USE_ENV_VARIABLE` placeholders and are overridden by environment variables.

## Environment Variable Mapping

ASP.NET Core uses double underscores (`__`) to represent nested configuration:

| Environment Variable | Configuration Path | Example |
|---------------------|-------------------|---------|
| `EMAIL_PROVIDER` | `Email:Provider` | `Mailgun` |
| `EMAIL_MAILGUN_API_KEY` | `Email:MailgunApiKey` | `key-...` |
| `EMAIL_MAILGUN_DOMAIN` | `Email:MailgunDomain` | `sandbox12345.mailgun.org` |
| `EMAIL_FROMEMAIL` | `Email:FromEmail` | `noreply@healthapp.com` |
| `EMAIL_FROMNAME` | `Email:FromName` | `Customer Support App` |
| `EMAIL_ENABLED` | `Email:Enabled` | `true` |
| `EMAIL_SMTPHOST` | `Email:SmtpHost` | `smtp.gmail.com` |
| `EMAIL_SMTPPORT` | `Email:SmtpPort` | `587` |
| `EMAIL_SMTPUSERNAME` | `Email:SmtpUsername` | `user@gmail.com` |
| `EMAIL_SMTPPASSWORD` | `Email:SmtpPassword` | `password` |
| `EMAIL_ENABLESSL` | `Email:EnableSsl` | `true` |
| `APP_BASE_URL` | `AppSettings:BaseUrl` | `https://caseflowstage.store` |

## Setting Variables

### For Staging
```bash
export EMAIL_PROVIDER="Mailgun"
export EMAIL_MAILGUN_API_KEY="your_api_key"
export EMAIL_MAILGUN_DOMAIN="sandbox12345.mailgun.org"
export EMAIL_FROMEMAIL="noreply@healthapp.com"
export EMAIL_FROMNAME="Customer Support App"
export APP_BASE_URL="https://caseflowstage.store"
```

### For Production
```bash
export EMAIL_PROVIDER="Mailgun"
export EMAIL_MAILGUN_API_KEY="your_api_key"
export EMAIL_MAILGUN_DOMAIN="yourdomain.mailgun.org"
export EMAIL_FROMEMAIL="noreply@healthapp.com"
export EMAIL_FROMNAME="Customer Support App"
export APP_BASE_URL="https://caseflow.store"
```

## Verification

After setting variables, verify they're loaded:

1. **Check .env file on server**:
   ```bash
   ssh user@server "cat /opt/mental-health-app/.env | grep EMAIL"
   ```

2. **Check container environment**:
   ```bash
   docker exec mental-health-api env | grep -i email
   ```

3. **Check application logs**:
   ```bash
   docker logs mental-health-api | grep -i email
   ```

## Notes

- The `APP_BASE_URL` is critical for password reset links to work correctly
- Mailgun sandbox domains require adding recipients to "Authorized Recipients" list
- The application automatically falls back to SMTP if Mailgun fails
- All email variables are optional except for Mailgun API key and domain when using Mailgun provider

