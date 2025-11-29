# PII Encryption Implementation Guide

## Overview

DateOfBirth is now encrypted in the database to protect PII (Personally Identifiable Information). The encryption uses AES-256 encryption.

## Changes Made

### 1. Encryption Service

- **File**: `SM_MentalHealthApp.Server/Services/PiiEncryptionService.cs`
- Provides `EncryptDateTime()` and `DecryptDateTime()` methods
- Uses AES-256 encryption with a key from configuration

### 2. User Entity Updates

- **File**: `SM_MentalHealthApp.Shared/JournalEntry.cs`
- Added `DateOfBirthEncrypted` property (string) - stored in database
- `DateOfBirth` is now a computed property (DateTime) - not stored in DB
- `DateOfBirth` is automatically decrypted when accessed

### 3. Database Context

- **File**: `SM_MentalHealthApp.Server/Data/JournalDbContext.cs`
- `DateOfBirthEncrypted` is configured as required string (max 500 chars)
- `DateOfBirth` property is ignored by EF Core

### 4. Helper Methods

- **File**: `SM_MentalHealthApp.Server/Helpers/UserEncryptionHelper.cs`
- `EncryptUserData()` - Call before saving User to database
- `DecryptUserData()` - Call after loading User from database

### 5. Configuration

- **File**: `SM_MentalHealthApp.Server/appsettings.json`
- Added `Encryption:Key` setting
- **IMPORTANT**: Change this to a secure 32+ character key in production!

## Required Database Migration

You need to create a migration to:

1. Add `DateOfBirthEncrypted` column (string, max 500)
2. Migrate existing `DateOfBirth` data to encrypted format
3. Remove `DateOfBirth` column (or keep it temporarily for rollback)

### Migration Steps:

1. **Migration Already Created**: `20251129161350_EncryptDateOfBirth.cs`

   - This migration preserves existing data by copying DateOfBirth to DateOfBirthEncrypted
   - Existing data will be stored as plain text initially

2. **Apply Migration**:

   ```bash
   dotnet ef database update --project SM_MentalHealthApp.Server
   ```

3. **Encrypt Existing Data** (After migration):
   - The migration copies data as plain text
   - You need to run a script to encrypt existing DateOfBirthEncrypted values
   - See `SM_MentalHealthApp.Server/Scripts/EncryptExistingDateOfBirthData.cs` for the script
   - New records will be automatically encrypted going forward

## Services That Need Updates

The following services need to be updated to encrypt/decrypt DateOfBirth:

### ✅ Already Updated:

- `AuthService` - Login and Register

### ⚠️ Still Need Updates:

- `UserService` - GetAllUsersAsync, UpdateUserAsync, GetOrCreateDemoUserAsync
- `UserRequestService` - ApproveUserRequestAsync (creates new users)
- `AdminController` - CreatePatient, UpdatePatient, CreateDoctor, UpdateDoctor, CreateCoordinator, UpdateCoordinator
- Any other services that create or update User objects

### Pattern to Follow:

**Before Saving:**

```csharp
// Set DateOfBirth (plain DateTime)
user.DateOfBirth = request.DateOfBirth;

// Encrypt before saving
UserEncryptionHelper.EncryptUserData(user, _encryptionService);

_context.Users.Add(user);
await _context.SaveChangesAsync();
```

**After Loading:**

```csharp
var user = await _context.Users
    .Include(u => u.Role)
    .FirstOrDefaultAsync(u => u.Id == userId);

// Decrypt after loading
UserEncryptionHelper.DecryptUserData(user, _encryptionService);
```

**For Collections:**

```csharp
var users = await _context.Users.ToListAsync();
UserEncryptionHelper.DecryptUserData(users, _encryptionService);
```

## Security Notes

1. **Encryption Key**:

   - Must be at least 32 characters
   - Store in environment variables or secure key vault in production
   - Never commit the actual key to source control

2. **Legacy Data**:

   - The decryption method handles legacy unencrypted data gracefully
   - Consider running a one-time migration script to encrypt all existing data

3. **Performance**:
   - Encryption/decryption adds minimal overhead
   - Consider caching decrypted values if performance becomes an issue

## Testing

After implementation:

1. Test creating new users - DateOfBirth should be encrypted in DB
2. Test loading users - DateOfBirth should be decrypted in UI
3. Test updating users - DateOfBirth should remain encrypted
4. Verify database - DateOfBirthEncrypted should contain encrypted strings, not plain dates
