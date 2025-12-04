# AppSettings Configuration

## Setup Instructions

1. **Copy the example file:**
   ```bash
   cp appsettings.example.json appsettings.json
   ```

2. **Update the values:**
   - Database connection string
   - API keys (HuggingFace, OpenAI, Vonage, Agora)
   - S3 credentials
   - JWT secret key
   - Email SMTP settings
   - Encryption keys
   - SSL certificate paths (for HTTPS)

## Important Notes

- ⚠️ **Never commit `appsettings.json`** - It contains sensitive credentials
- ✅ **Commit `appsettings.example.json`** - This is the template file
- 🔒 Keep your actual `appsettings.json` file local only

## Environment-Specific Files

- `appsettings.json` - Local development (not in git)
- `appsettings.Development.json` - Development environment (not in git)
- `appsettings.Production.json` - Production environment (not in git, created on server)

## Required Settings

Make sure to configure:
- ✅ Database connection string
- ✅ JWT secret key (at least 32 characters)
- ✅ Encryption keys (for PII data)
- ✅ S3 credentials (for document storage)
- ✅ Email settings (for notifications)
- ✅ Agora credentials (for video/audio calls)

