-- =============================================
-- Migration Script: Add PreferredSmeUserId to ServiceRequests
-- Description: Adds client's preferred SME field to store preference expressed via Agentic AI
-- =============================================

USE `customerhealthdb`;

-- Check if PreferredSmeUserId column exists before adding it
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'ServiceRequests' 
    AND COLUMN_NAME = 'PreferredSmeUserId');

-- Add PreferredSmeUserId column to store client's preferred SME (nullable)
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE `ServiceRequests` 
     ADD COLUMN `PreferredSmeUserId` INT NULL COMMENT ''Client preferred SME set via Agentic AI. Coordinators should consider this when assigning.'' AFTER `PrimaryExpertiseId`;',
    'SELECT "PreferredSmeUserId column already exists in ServiceRequests" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add foreign key constraint to Users table (optional, for referential integrity)
SET @fk_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'ServiceRequests' 
    AND CONSTRAINT_NAME = 'FK_ServiceRequests_Users_PreferredSmeUserId');

SET @fk_sql = IF(@fk_exists = 0,
    'ALTER TABLE `ServiceRequests` 
     ADD CONSTRAINT `FK_ServiceRequests_Users_PreferredSmeUserId` 
     FOREIGN KEY (`PreferredSmeUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL;',
    'SELECT "Foreign key FK_ServiceRequests_Users_PreferredSmeUserId already exists" AS message;');
PREPARE fk_stmt FROM @fk_sql;
EXECUTE fk_stmt;
DEALLOCATE PREPARE fk_stmt;

-- Update table comment for documentation
ALTER TABLE `ServiceRequests` COMMENT = 'Service requests for clients. PreferredSmeUserId stores client preference expressed via Agentic AI.';

