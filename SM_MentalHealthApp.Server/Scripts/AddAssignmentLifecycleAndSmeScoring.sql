-- ============================================================================
-- Assignment Lifecycle and SME Scoring Migration
-- ============================================================================
-- This script adds assignment lifecycle tracking, SME scoring, and billing
-- capabilities to the ServiceRequestAssignment system.
--
-- Phase 1: Add columns to ServiceRequestAssignments table
-- Phase 2: Add SmeScore column to Users table
-- Phase 3: Initialize default values
-- ============================================================================

USE `customerhealthdb`;

-- ============================================================================
-- Step 1: Add Assignment Lifecycle columns to ServiceRequestAssignments
-- ============================================================================

-- Check if Status column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'Status');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `Status` VARCHAR(30) NOT NULL DEFAULT ''Assigned''
     COMMENT ''Assignment status: Assigned, Accepted, Rejected, InProgress, Completed, Abandoned'';',
    'SELECT "Status column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if OutcomeReason column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'OutcomeReason');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `OutcomeReason` VARCHAR(50) NULL
     COMMENT ''Reason for assignment outcome: SME_NoResponse, SME_Rejected, SME_Overloaded, Client_NoResponse, Client_Cancelled, etc.'';',
    'SELECT "OutcomeReason column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if ResponsibilityParty column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'ResponsibilityParty');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `ResponsibilityParty` VARCHAR(30) NULL
     COMMENT ''Who is responsible for the outcome: SME, Client, System, Coordinator, Unknown'';',
    'SELECT "ResponsibilityParty column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if AcceptedAt column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'AcceptedAt');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `AcceptedAt` DATETIME NULL
     COMMENT ''When the SME accepted the assignment'';',
    'SELECT "AcceptedAt column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if StartedAt column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'StartedAt');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `StartedAt` DATETIME NULL
     COMMENT ''When the SME started working on the assignment'';',
    'SELECT "StartedAt column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if CompletedAt column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'CompletedAt');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `CompletedAt` DATETIME NULL
     COMMENT ''When the assignment was completed'';',
    'SELECT "CompletedAt column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if IsBillable column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'IsBillable');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `IsBillable` TINYINT(1) NOT NULL DEFAULT 0
     COMMENT ''Whether this assignment is billable to the SME'';',
    'SELECT "IsBillable column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index on Status for filtering
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND INDEX_NAME = 'IX_ServiceRequestAssignments_Status');

SET @sql = IF(@idx_exists = 0, 
    'CREATE INDEX `IX_ServiceRequestAssignments_Status` ON `ServiceRequestAssignments` (`Status`);',
    'SELECT "Index IX_ServiceRequestAssignments_Status already exists" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index on IsBillable for billing queries
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND INDEX_NAME = 'IX_ServiceRequestAssignments_IsBillable');

SET @sql = IF(@idx_exists = 0, 
    'CREATE INDEX `IX_ServiceRequestAssignments_IsBillable` ON `ServiceRequestAssignments` (`IsBillable`);',
    'SELECT "Index IX_ServiceRequestAssignments_IsBillable already exists" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 2: Add SmeScore column to Users table
-- ============================================================================

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'SmeScore');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` 
     ADD COLUMN `SmeScore` INT NOT NULL DEFAULT 100
     COMMENT ''SME behavior-based score (0-150). Default 100. Used for assignment prioritization.'';',
    'SELECT "SmeScore column already exists in Users" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 3: Initialize existing assignments
-- ============================================================================

-- Set Status to 'Assigned' for all existing active assignments
UPDATE `ServiceRequestAssignments`
SET `Status` = 'Assigned'
WHERE `Status` IS NULL OR `Status` = '';

-- Set IsBillable = false for all existing assignments (they haven't started work yet)
UPDATE `ServiceRequestAssignments`
SET `IsBillable` = 0
WHERE `IsBillable` IS NULL;

-- ============================================================================
-- Step 4: Verification queries
-- ============================================================================

-- Verify columns were added
SELECT 
    'ServiceRequestAssignments columns' AS TableName,
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME = 'ServiceRequestAssignments'
    AND COLUMN_NAME IN ('Status', 'OutcomeReason', 'ResponsibilityParty', 'AcceptedAt', 'StartedAt', 'CompletedAt', 'IsBillable')
ORDER BY ORDINAL_POSITION;

-- Verify SmeScore column
SELECT 
    'Users SmeScore column' AS TableName,
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME = 'Users'
    AND COLUMN_NAME = 'SmeScore';

-- Show assignment status distribution
SELECT 
    Status,
    COUNT(*) AS Count,
    SUM(CASE WHEN IsBillable = 1 THEN 1 ELSE 0 END) AS BillableCount
FROM `ServiceRequestAssignments`
GROUP BY Status
ORDER BY Count DESC;

-- Show SME score distribution
SELECT 
    CASE 
        WHEN u.RoleId IN (2, 4) THEN 'SME' -- Doctor or Attorney
        ELSE 'Other'
    END AS UserType,
    COUNT(*) AS UserCount,
    AVG(u.SmeScore) AS AvgScore,
    MIN(u.SmeScore) AS MinScore,
    MAX(u.SmeScore) AS MaxScore
FROM `Users` u
WHERE u.IsActive = 1
GROUP BY UserType;

SELECT 'Migration completed successfully!' AS Status;

