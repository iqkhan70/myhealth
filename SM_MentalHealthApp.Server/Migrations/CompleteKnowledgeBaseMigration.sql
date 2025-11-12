-- =====================================================
-- Complete Knowledge Base Migration Script (MySQL-safe)
-- Run this in your target MySQL database
-- =====================================================

-- 0) Quiet some noisy notes (optional)
SET @OLD_SQL_NOTES = @@sql_notes; 
SET sql_notes = 0;

-- 1) Ensure the EF migrations history table exists
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
  `MigrationId`    varchar(150) NOT NULL,
  `ProductVersion` varchar(32)  NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB;

-- 2) Mark the migration as applied
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251112000000_AddKnowledgeBaseTables', '9.0.9')
ON DUPLICATE KEY UPDATE `ProductVersion` = VALUES(`ProductVersion`);

-- Helper: current DB
SET @db = DATABASE();

-- =====================================================
-- 3) Create missing indexes (compat-safe: no IF NOT EXISTS)
--    Pattern for each index:
--    - Check information_schema.statistics
--    - Conditionally CREATE INDEX via prepared statement
-- =====================================================

-- --- KnowledgeBaseCategories indexes ---

-- IX_KnowledgeBaseCategories_Name
SET @idx_exists = (
  SELECT COUNT(*) FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'KnowledgeBaseCategories'
    AND index_name   = 'IX_KnowledgeBaseCategories_Name'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_KnowledgeBaseCategories_Name` ON `KnowledgeBaseCategories` (`Name`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_KnowledgeBaseCategories_IsActive_DisplayOrder
SET @idx_exists = (
  SELECT COUNT(*) FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'KnowledgeBaseCategories'
    AND index_name   = 'IX_KnowledgeBaseCategories_IsActive_DisplayOrder'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_KnowledgeBaseCategories_IsActive_DisplayOrder` ON `KnowledgeBaseCategories` (`IsActive`, `DisplayOrder`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- --- KnowledgeBaseEntries indexes ---

-- IX_KnowledgeBaseEntries_CategoryId
SET @idx_exists = (
  SELECT COUNT(*) FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'KnowledgeBaseEntries'
    AND index_name   = 'IX_KnowledgeBaseEntries_CategoryId'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_KnowledgeBaseEntries_CategoryId` ON `KnowledgeBaseEntries` (`CategoryId`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_KnowledgeBaseEntries_Priority
SET @idx_exists = (
  SELECT COUNT(*) FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'KnowledgeBaseEntries'
    AND index_name   = 'IX_KnowledgeBaseEntries_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_KnowledgeBaseEntries_Priority` ON `KnowledgeBaseEntries` (`Priority`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_KnowledgeBaseEntries_IsActive
SET @idx_exists = (
  SELECT COUNT(*) FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'KnowledgeBaseEntries'
    AND index_name   = 'IX_KnowledgeBaseEntries_IsActive'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_KnowledgeBaseEntries_IsActive` ON `KnowledgeBaseEntries` (`IsActive`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_KnowledgeBaseEntries_CategoryId_IsActive_Priority
SET @idx_exists = (
  SELECT COUNT(*) FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'KnowledgeBaseEntries'
    AND index_name   = 'IX_KnowledgeBaseEntries_CategoryId_IsActive_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_KnowledgeBaseEntries_CategoryId_IsActive_Priority` ON `KnowledgeBaseEntries` (`CategoryId`, `IsActive`, `Priority`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_KnowledgeBaseEntries_CreatedByUserId
SET @idx_exists = (
  SELECT COUNT(*) FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'KnowledgeBaseEntries'
    AND index_name   = 'IX_KnowledgeBaseEntries_CreatedByUserId'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_KnowledgeBaseEntries_CreatedByUserId` ON `KnowledgeBaseEntries` (`CreatedByUserId`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_KnowledgeBaseEntries_UpdatedByUserId
SET @idx_exists = (
  SELECT COUNT(*) FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'KnowledgeBaseEntries'
    AND index_name   = 'IX_KnowledgeBaseEntries_UpdatedByUserId'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_KnowledgeBaseEntries_UpdatedByUserId` ON `KnowledgeBaseEntries` (`UpdatedByUserId`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- =====================================================
-- 4) Verification
-- =====================================================

-- Migration status
SELECT 'Migration Status:' AS Status;
SELECT `MigrationId`, `ProductVersion`
FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20251112000000_AddKnowledgeBaseTables';

-- Table existence
SELECT 'Table Verification:' AS Status;
SELECT 'KnowledgeBaseCategories' AS TableName,
       COUNT(*) AS TableExists
FROM information_schema.tables
WHERE table_schema = @db AND table_name = 'KnowledgeBaseCategories'
UNION ALL
SELECT 'KnowledgeBaseEntries' AS TableName,
       COUNT(*) AS TableExists
FROM information_schema.tables
WHERE table_schema = @db AND table_name = 'KnowledgeBaseEntries';

-- Optional structures (will error if table doesn’t exist)
SELECT 'KnowledgeBaseCategories Structure:' AS Info;
DESCRIBE `KnowledgeBaseCategories`;

SELECT 'KnowledgeBaseEntries Structure:' AS Info;
DESCRIBE `KnowledgeBaseEntries`;

-- Restore notes
SET sql_notes = @OLD_SQL_NOTES;
