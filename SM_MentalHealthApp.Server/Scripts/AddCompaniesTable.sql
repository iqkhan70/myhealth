-- ============================================================================
-- Companies Table Migration
-- ============================================================================
-- This script creates a Companies table to manage company information
-- for grouping SMEs for billing purposes.
-- ============================================================================

USE `customerhealthdb`;

-- ============================================================================
-- Step 1: Create Companies table
-- ============================================================================

CREATE TABLE IF NOT EXISTS `Companies` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(200) NOT NULL,
    `Description` VARCHAR(1000) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Companies_Name` (`Name`),
    INDEX `IX_Companies_IsActive` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- Step 2: Add foreign key constraint to Users.CompanyId
-- ============================================================================

-- Check if foreign key already exists
SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND CONSTRAINT_NAME = 'FK_Users_Companies_CompanyId');

SET @sql = IF(@fk_exists = 0, 
    'ALTER TABLE `Users` 
     ADD CONSTRAINT `FK_Users_Companies_CompanyId` 
     FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE SET NULL;',
    'SELECT "Foreign key FK_Users_Companies_CompanyId already exists" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 3: Verification queries
-- ============================================================================

-- Verify Companies table was created
SELECT 
    'Companies table' AS CheckItem,
    TABLE_NAME,
    TABLE_ROWS,
    CREATE_TIME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME = 'Companies'
ORDER BY TABLE_NAME;

-- Verify foreign key was added
SELECT 
    'Foreign key check' AS CheckItem,
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME = 'Users'
    AND CONSTRAINT_NAME = 'FK_Users_Companies_CompanyId';

SELECT 'Migration completed successfully!' AS Status;
SELECT 'You can now add companies via the UI or directly in the database.' AS Note;

