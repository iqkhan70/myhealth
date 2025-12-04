# DateOfBirth Encryption Deployment Guide

This guide explains how to deploy the DateOfBirth encryption feature to DigitalOcean.

## Overview

The DateOfBirth encryption feature:

- Encrypts `DateOfBirth` fields in the `Users` and `UserRequests` tables
- Uses AES-256 encryption via `PiiEncryptionService`
- Automatically encrypts/decrypts data when accessed through the application
- Requires database migrations and encryption key configuration

## Prerequisites

1. SSH access to DigitalOcean server (159.65.242.79)
2. SSH key configured (`~/.ssh/id_rsa`)
3. Database access credentials in `appsettings.Production.json`
4. .NET SDK and EF Core tools installed on the server

## Deployment Steps

### Step 1: Update appsettings.Production.json

The encryption key must be configured in `appsettings.Production.json`:

```bash
./deploy/create-appsettingprod.sh
```

This script will:

- Create/update `appsettings.Production.json` on the server
- Add `PiiEncryption` and `Encryption` sections with the encryption key
- Preserve existing `ConnectionStrings` section

**Note:** The encryption key is: `ThisIsAStrongEncryptionKeyForPIIData1234567890`

### Step 2: Apply Database Migrations

Run the migration script to apply the database schema changes:

```bash
./deploy/apply-dob-encryption-migration.sh
```

This script will:

1. Verify encryption key is set in `appsettings.Production.json`
2. Check current database schema
3. Apply EF Core migrations:
   - `20251129161350_EncryptDateOfBirth` (Users table)
   - `20251129162652_EncryptUserRequestDateOfBirth` (UserRequests table)
4. Verify migrations were applied successfully

**What the migrations do:**

- Add `DateOfBirthEncrypted` column (varchar(500)) to `Users` table
- Copy existing `DateOfBirth` data to `DateOfBirthEncrypted` (as plain text initially)
- Drop the old `DateOfBirth` column from `Users` table
- Repeat the same process for `UserRequests` table

### Step 3: Encrypt Existing Data (Optional)

The application will automatically encrypt plain text dates when they are accessed. However, if you want to encrypt all existing data immediately:

```bash
./deploy/encrypt-existing-dob-data.sh
```

This script will:

- Check for plain text dates in the database
- Report how many records need encryption
- Note: The application will encrypt dates automatically when accessed

**Alternative:** The application's `UserEncryptionHelper` will automatically encrypt plain text dates when they are loaded, so manual encryption is not strictly necessary.

### Step 4: Restart the Application

After migrations are applied, restart the application:

```bash
ssh -i ~/.ssh/id_rsa root@159.65.242.79 "systemctl restart mental-health-app"
```

### Step 5: Verify

Check the application logs to ensure everything is working:

```bash
ssh -i ~/.ssh/id_rsa root@159.65.242.79 "journalctl -u mental-health-app -f --no-pager"
```

Look for:

- No encryption/decryption errors
- DateOfBirth values displaying correctly in the UI
- New records being encrypted automatically

## Quick Deployment (All Steps)

To run all steps in sequence:

```bash
# 1. Update appsettings with encryption key
./deploy/create-appsettingprod.sh

# 2. Apply migrations
./deploy/apply-dob-encryption-migration.sh

# 3. Restart application
ssh -i ~/.ssh/id_rsa root@159.65.242.79 "systemctl restart mental-health-app"

# 4. Check logs
ssh -i ~/.ssh/id_rsa root@159.65.242.79 "journalctl -u mental-health-app -n 50 --no-pager"
```

## Troubleshooting

### Migration Fails

If migrations fail:

1. Check database connection string in `appsettings.Production.json`
2. Verify EF Core tools are installed: `dotnet ef --version`
3. Check database permissions for the MySQL user
4. Review migration errors in the script output

### Encryption Key Not Found

If you see "Encryption key not found" errors:

1. Run `./deploy/create-appsettingprod.sh` to add the key
2. Verify the key exists: `grep -A 2 "PiiEncryption" /opt/mental-health-app/server/appsettings.Production.json`
3. Restart the application after updating the config

### Dates Show as 01/01/0001

If dates display as `01/01/0001`:

1. Check that migrations were applied: `SELECT * FROM __EFMigrationsHistory WHERE MigrationId LIKE '%Encrypt%';`
2. Verify encryption key matches between local and production
3. Check application logs for decryption errors
4. Ensure `UserEncryptionHelper.DecryptUserData` is being called in service methods

### Plain Text Dates Not Encrypting

The application automatically encrypts plain text dates when:

- A user record is loaded and decrypted
- A user record is updated
- The `UserEncryptionHelper.DecryptUserData` method is called

If dates remain unencrypted:

1. Access user records through the application (UI or API)
2. The encryption will happen automatically on the next access
3. Or run the encryption script manually

## Database Schema

After migrations:

**Users table:**

- `DateOfBirthEncrypted` (varchar(500)) - Encrypted date of birth
- `DateOfBirth` (computed property) - Decrypted date (not stored in DB)

**UserRequests table:**

- `DateOfBirthEncrypted` (varchar(500)) - Encrypted date of birth
- `DateOfBirth` (computed property) - Decrypted date (not stored in DB)

## Security Notes

1. **Encryption Key:** The encryption key is stored in `appsettings.Production.json`. In production, consider:

   - Using environment variables
   - Using a secrets management service (Azure Key Vault, AWS Secrets Manager, etc.)
   - Rotating keys periodically

2. **Key Rotation:** If you need to rotate the encryption key:

   - Decrypt all data with the old key
   - Re-encrypt with the new key
   - Update `appsettings.Production.json`
   - Restart the application

3. **Backup:** Always backup the database before running migrations:
   ```bash
   mysqldump -u mentalhealth_user -p mentalhealthdb > backup_before_encryption.sql
   ```

## Rollback

If you need to rollback the encryption:

1. **Restore database backup** (if available)
2. **Or manually reverse migrations:**
   - Add back `DateOfBirth` column
   - Decrypt `DateOfBirthEncrypted` and copy to `DateOfBirth`
   - Drop `DateOfBirthEncrypted` column
   - Remove migration entries from `__EFMigrationsHistory`

**Note:** Rollback is complex and not recommended. Always test migrations in a staging environment first.
