# Data Recovery Guide for ContentTypes Migration

## The Problem

The migration `20251022203336_AddContentTypesTable.cs` was designed to:

1. Rename the `Type` column to `ContentTypeModelId`
2. Add a new `ContentTypes` table
3. Create foreign key relationships

However, this approach would cause **data loss** because:

- The original `Type` column contained enum values (1, 2, 3, 4, 5)
- The migration treats these as foreign keys to a new table
- The data mapping wasn't preserved

## Recovery Steps

### Step 1: Check Current Database State

Run the recovery script to assess the damage:

```bash
mysql -u your_username -p your_database_name < DataRecoveryScript.sql
```

### Step 2: If Migration Hasn't Been Applied Yet

If you haven't run the migration yet, use the fixed migration instead:

1. **Delete the problematic migration:**

   ```bash
   rm SM_MentalHealthApp.Server/Migrations/20251022203336_AddContentTypesTable.cs
   rm SM_MentalHealthApp.Server/Migrations/20251022203336_AddContentTypesTable.Designer.cs
   ```

2. **Use the fixed migration:**

   ```bash
   mv SM_MentalHealthApp.Server/Migrations/20251022203336_AddContentTypesTable_FIXED.cs SM_MentalHealthApp.Server/Migrations/20251022203336_AddContentTypesTable.cs
   ```

3. **Run the fixed migration:**
   ```bash
   dotnet ef database update
   ```

### Step 3: If Migration Has Already Been Applied

If the migration has already been applied and data was lost:

1. **Check if you have a database backup:**

   ```bash
   # Look for recent backups
   ls -la /path/to/your/backups/
   ```

2. **If you have a backup, restore it:**

   ```bash
   mysql -u your_username -p your_database_name < backup_file.sql
   ```

3. **If no backup, try to recover from the current state:**
   - The recovery script will attempt to fix the data
   - Run: `mysql -u your_username -p your_database_name < DataRecoveryScript.sql`

### Step 4: Verify Recovery

After running the recovery, verify that:

1. All records in the `Contents` table have valid `ContentTypeModelId` values (1-5)
2. The `ContentTypes` table has the correct entries
3. Foreign key relationships are working

### Step 5: Test the Application

1. Start your application
2. Check that content uploads work
3. Verify that content types are displayed correctly
4. Test content filtering by type

## Prevention for Future Migrations

1. **Always backup before migrations:**

   ```bash
   mysqldump -u your_username -p your_database_name > backup_$(date +%Y%m%d_%H%M%S).sql
   ```

2. **Test migrations on a copy first:**

   ```bash
   # Create a test database
   mysql -u root -p -e "CREATE DATABASE test_database;"
   mysqldump -u your_username -p your_database_name | mysql -u your_username -p test_database
   ```

3. **Use data-preserving migrations:**
   - Never rename columns that contain data
   - Always copy data before dropping columns
   - Test the migration on a copy of production data

## What the Fixed Migration Does

The fixed migration (`20251022203336_AddContentTypesTable_FIXED.cs`) properly handles the data conversion:

1. **Creates ContentTypes table** with specific IDs (1-5) matching the enum values
2. **Seeds the table** with the correct content types
3. **Adds ContentTypeModelId column** without dropping the original Type column
4. **Copies the data** from Type to ContentTypeModelId
5. **Creates foreign key relationships**
6. **Only then drops the original Type column**

This ensures **zero data loss** during the migration.

## Emergency Recovery Commands

If you need to quickly restore functionality:

```sql
-- Emergency: Add Type column back if needed
ALTER TABLE Contents ADD COLUMN Type INT DEFAULT 1;

-- Emergency: Copy ContentTypeModelId back to Type
UPDATE Contents SET Type = ContentTypeModelId WHERE ContentTypeModelId IN (1,2,3,4,5);

-- Emergency: Drop ContentTypeModelId if causing issues
ALTER TABLE Contents DROP COLUMN ContentTypeModelId;
```

## Contact Information

If you need help with the recovery process, please provide:

1. Output from the recovery script
2. Current database schema (SHOW CREATE TABLE Contents;)
3. Any error messages you're seeing
