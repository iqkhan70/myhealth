# MobilePhone Encryption Migration Guide

This guide explains how to migrate the `MobilePhone` field to use encryption, following the same pattern as `DateOfBirth` encryption.

## Overview

The `MobilePhone` field is being encrypted to protect PII (Personally Identifiable Information) in the database. The implementation follows the same pattern as `DateOfBirth` encryption:

- **Database**: Stores encrypted value in `MobilePhoneEncrypted` column
- **Application**: Uses computed `MobilePhone` property that is decrypted on read and encrypted on write
- **SMS Functionality**: Phone numbers are decrypted before being sent to SMS services

## Migration Steps

### Step 1: Apply Database Migration

Run the SQL migration script to:
1. Add `MobilePhoneEncrypted` column to `Users` and `UserRequests` tables
2. Copy existing `MobilePhone` data to `MobilePhoneEncrypted` (as plain text initially)
3. Drop the old `MobilePhone` column

**Option A: Manual SQL Execution**

```bash
# Connect to your database and run:
mysql -u your_user -p your_database < SM_MentalHealthApp.Server/Scripts/EncryptMobilePhoneMigration.sql
```

**Option B: Using Deployment Script**

```bash
./deploy/apply-mobilephone-encryption-migration.sh
```

### Step 2: Encrypt Existing Plain Text Data

After the migration, existing phone numbers will be stored as plain text in `MobilePhoneEncrypted`. You need to encrypt them.

**Option A: Using C# Script (Recommended)**

The script `EncryptExistingMobilePhoneData.cs` can be run to encrypt all existing plain text phone numbers:

```csharp
// Add this to Program.cs temporarily:
if (args.Contains("--encrypt-mobilephones"))
{
    await EncryptExistingMobilePhoneData.RunAsync();
    return;
}
```

Then run:
```bash
dotnet run --project SM_MentalHealthApp.Server --encrypt-mobilephones
```

**Option B: Manual Encryption**

If you prefer to encrypt manually, you can use the application's encryption service to encrypt each phone number individually.

### Step 3: Verify Migration

Run these queries to verify the migration:

```sql
-- Check Users table
SELECT COUNT(*) as TotalUsers, 
       COUNT(MobilePhoneEncrypted) as UsersWithPhone 
FROM Users;

-- Check UserRequests table
SELECT COUNT(*) as TotalRequests, 
       COUNT(MobilePhoneEncrypted) as RequestsWithPhone 
FROM UserRequests;

-- Verify encrypted format (should be base64, 40+ characters)
SELECT Id, FirstName, LastName, 
       LENGTH(MobilePhoneEncrypted) as EncryptedLength,
       CASE 
         WHEN MobilePhoneEncrypted LIKE '%=%' THEN 'Likely Encrypted'
         ELSE 'Likely Plain Text'
       END as EncryptionStatus
FROM Users
WHERE MobilePhoneEncrypted IS NOT NULL
  AND MobilePhoneEncrypted != ''
LIMIT 10;
```

## Important Notes

### SMS Functionality

**SMS functionality remains intact!** The application automatically decrypts phone numbers before sending SMS messages. The SMS service receives plain text phone numbers as before.

### Encryption Key

Make sure the encryption key in `appsettings.json` matches the key used for `DateOfBirth` encryption:

```json
{
  "PiiEncryption": {
    "Key": "ThisIsAStrongEncryptionKeyForPIIData1234567890"
  }
}
```

### Index Removal

The index on `MobilePhone` in `UserRequests` table is removed because:
- Encrypted data cannot be effectively indexed
- Phone number lookups are now done after decryption in the application layer

### Validation Changes

Phone number validation now requires:
- Loading all users with phone numbers
- Decrypting their phone numbers
- Comparing decrypted values

This is necessary because encrypted values cannot be directly compared.

## Rollback Procedure

If you need to rollback the migration:

1. **Restore MobilePhone column**:
   ```sql
   ALTER TABLE Users ADD COLUMN MobilePhone VARCHAR(20) NULL AFTER Gender;
   ALTER TABLE UserRequests ADD COLUMN MobilePhone VARCHAR(20) NOT NULL DEFAULT '' AFTER Gender;
   ```

2. **Decrypt and copy data back**:
   ```sql
   -- You'll need to decrypt MobilePhoneEncrypted and copy to MobilePhone
   -- This requires running a C# script to decrypt the values
   ```

3. **Drop encrypted column**:
   ```sql
   ALTER TABLE Users DROP COLUMN MobilePhoneEncrypted;
   ALTER TABLE UserRequests DROP COLUMN MobilePhoneEncrypted;
   ```

4. **Revert application code** to use `MobilePhone` directly (remove encryption logic)

## Troubleshooting

### Issue: SMS not sending

**Solution**: Check that:
1. Phone numbers are being decrypted before SMS service calls
2. `UserEncryptionHelper.DecryptUserData()` is called before using `MobilePhone`
3. Encryption service is properly configured

### Issue: Phone number validation failing

**Solution**: Ensure that:
1. `ValidateEmailAndPhoneAsync` decrypts all users' phone numbers before comparison
2. User requests are decrypted before validation

### Issue: Migration fails with "Column already exists"

**Solution**: The migration may have been partially applied. Check which columns exist:
```sql
DESCRIBE Users;
DESCRIBE UserRequests;
```

If `MobilePhoneEncrypted` exists but `MobilePhone` also exists, you can manually drop `MobilePhone`:
```sql
ALTER TABLE Users DROP COLUMN MobilePhone;
ALTER TABLE UserRequests DROP COLUMN MobilePhone;
```

## Files Modified

- `SM_MentalHealthApp.Shared/JournalEntry.cs` - User entity
- `SM_MentalHealthApp.Shared/UserRequest.cs` - UserRequest entity
- `SM_MentalHealthApp.Server/Data/JournalDbContext.cs` - Database mapping
- `SM_MentalHealthApp.Server/Helpers/UserEncryptionHelper.cs` - Encryption helper
- `SM_MentalHealthApp.Server/Controllers/MobileController.cs` - SMS functionality
- `SM_MentalHealthApp.Server/Services/NotificationService.cs` - Emergency SMS
- `SM_MentalHealthApp.Server/Services/UserRequestService.cs` - User request validation
- `SM_MentalHealthApp.Server/Controllers/AdminController.cs` - User CRUD operations

## Security Notes

- Phone numbers are encrypted at rest in the database
- Phone numbers are decrypted only when needed (for SMS, validation, display)
- The encryption key should be stored securely and rotated periodically
- Never log decrypted phone numbers
- Use the same encryption key as DateOfBirth for consistency

