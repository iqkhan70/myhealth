-- =====================================================
-- Complete AI Response Template Migration Script
-- Run this script in your MySQL database to mark the migration as applied
-- =====================================================

-- Step 1: Mark the migration as applied in the migration history
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251113000000_AddAIResponseTemplateTable', '9.0.9')
ON DUPLICATE KEY UPDATE MigrationId = MigrationId;

-- Step 2: Create any missing indexes (in case migration was interrupted)
-- Using IF NOT EXISTS syntax (MySQL 8.0.1+)
-- If you get syntax errors, your MySQL server version may be older - remove "IF NOT EXISTS" from each line

-- Indexes for AIResponseTemplates
CREATE INDEX IF NOT EXISTS IX_AIResponseTemplates_TemplateKey 
ON AIResponseTemplates (TemplateKey);

CREATE INDEX IF NOT EXISTS IX_AIResponseTemplates_Priority 
ON AIResponseTemplates (Priority);

CREATE INDEX IF NOT EXISTS IX_AIResponseTemplates_IsActive 
ON AIResponseTemplates (IsActive);

CREATE INDEX IF NOT EXISTS IX_AIResponseTemplates_IsActive_Priority 
ON AIResponseTemplates (IsActive, Priority);

CREATE INDEX IF NOT EXISTS IX_AIResponseTemplates_CreatedByUserId 
ON AIResponseTemplates (CreatedByUserId);

CREATE INDEX IF NOT EXISTS IX_AIResponseTemplates_UpdatedByUserId 
ON AIResponseTemplates (UpdatedByUserId);

-- Step 3: Verify the migration was applied
SELECT 'Migration Status:' AS Status;
SELECT MigrationId, ProductVersion 
FROM __EFMigrationsHistory 
WHERE MigrationId = '20251113000000_AddAIResponseTemplateTable';

-- Step 4: Verify table exists
SELECT 'Table Verification:' AS Status;
SELECT 
    'AIResponseTemplates' AS TableName,
    COUNT(*) AS TableExists 
FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'AIResponseTemplates';

-- Step 5: Show table structure (optional verification)
SELECT 'AIResponseTemplates Structure:' AS Info;
DESCRIBE AIResponseTemplates;

