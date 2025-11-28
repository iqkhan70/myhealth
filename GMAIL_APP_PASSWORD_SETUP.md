# Gmail App Password Setup Guide

## Why You Need an App Password

Gmail requires an **App Password** (not your regular password) for SMTP authentication. This is a security feature that Google requires for third-party applications.

## Steps to Generate a Gmail App Password

1. **Go to your Google Account**: https://myaccount.google.com/
2. **Enable 2-Step Verification** (if not already enabled):
   - Go to Security → 2-Step Verification
   - Follow the prompts to enable it
3. **Generate App Password**:
   - Go to: https://myaccount.google.com/apppasswords
   - Or: Security → 2-Step Verification → App passwords
4. **Create the App Password**:
   - Select "Mail" as the app
   - Select "Other (Custom name)" as the device
   - Enter: `Health App Server`
   - Click "Generate"
5. **Copy the 16-character password**:
   - It will look like: `abcd efgh ijkl mnop`
   - **Important**: Remove all spaces when pasting into appsettings.json
   - Example: `abcdefghijklmnop`

## Update appsettings.json

Replace the `SmtpPassword` value with your App Password:

```json
"Email": {
  "Enabled": true,
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "iqmkhan70@gmail.com",
  "SmtpPassword": "YOUR-16-CHARACTER-APP-PASSWORD-HERE",
  "FromEmail": "iqmkhan70@gmail.com",
  "FromName": "Health App",
  "EnableSsl": true
}
```

## After Updating

1. Restart the server
2. Test by approving a user request
3. Check the logs to verify email was sent

## Troubleshooting

- **Still getting "Authentication Required"**: Make sure you removed all spaces from the App Password
- **Can't generate App Password**: Make sure 2-Step Verification is enabled
- **Password not working**: Generate a new App Password and try again

