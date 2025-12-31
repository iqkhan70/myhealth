-- ============================================================================
-- Billing Status and Invoicing System Migration
-- ============================================================================
-- This script adds billing status tracking and invoicing tables to prevent
-- re-billing of service requests that have already been invoiced and paid.
--
-- Features:
-- 1. BillingStatus on ServiceRequestAssignments (Ready, Invoiced, Paid, Voided)
-- 2. Invoice tracking (SmeInvoice table)
-- 3. Invoice line items (SmeInvoiceLine table)
-- 4. Unique constraint to prevent duplicate billing
-- ============================================================================

USE `customerhealthdb`;

-- ============================================================================
-- Step 1: Add Billing Status columns to ServiceRequestAssignments
-- ============================================================================

-- Check if BillingStatus column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'BillingStatus');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `BillingStatus` VARCHAR(20) NOT NULL DEFAULT ''NotBillable''
     COMMENT ''Billing status: NotBillable, Ready, Invoiced, Paid, Voided'';',
    'SELECT "BillingStatus column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if InvoiceId column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'InvoiceId');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `InvoiceId` BIGINT NULL
     COMMENT ''Reference to SmeInvoice if this assignment has been invoiced'';',
    'SELECT "InvoiceId column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if BilledAt column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'BilledAt');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `BilledAt` DATETIME NULL
     COMMENT ''When this assignment was included in an invoice'';',
    'SELECT "BilledAt column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check if PaidAt column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND COLUMN_NAME = 'PaidAt');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestAssignments` 
     ADD COLUMN `PaidAt` DATETIME NULL
     COMMENT ''When payment was received for this assignment'';',
    'SELECT "PaidAt column already exists in ServiceRequestAssignments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index on BillingStatus for filtering
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND INDEX_NAME = 'IX_ServiceRequestAssignments_BillingStatus');

SET @sql = IF(@idx_exists = 0, 
    'CREATE INDEX `IX_ServiceRequestAssignments_BillingStatus` ON `ServiceRequestAssignments` (`BillingStatus`);',
    'SELECT "Index IX_ServiceRequestAssignments_BillingStatus already exists" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index on InvoiceId for joining
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestAssignments' 
    AND INDEX_NAME = 'IX_ServiceRequestAssignments_InvoiceId');

SET @sql = IF(@idx_exists = 0, 
    'CREATE INDEX `IX_ServiceRequestAssignments_InvoiceId` ON `ServiceRequestAssignments` (`InvoiceId`);',
    'SELECT "Index IX_ServiceRequestAssignments_InvoiceId already exists" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 2: Create SmeInvoice table
-- ============================================================================

CREATE TABLE IF NOT EXISTS `SmeInvoices` (
    `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `SmeUserId` INT NOT NULL,
    `InvoiceNumber` VARCHAR(50) NOT NULL,
    `PeriodStart` DATETIME NOT NULL,
    `PeriodEnd` DATETIME NOT NULL,
    `Status` VARCHAR(20) NOT NULL DEFAULT 'Draft' COMMENT 'Draft, Sent, Paid, Voided',
    `SubTotal` DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    `TaxAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    `TotalAmount` DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `SentAt` DATETIME NULL,
    `PaidAt` DATETIME NULL,
    `VoidedAt` DATETIME NULL,
    `CreatedByUserId` INT NULL,
    `Notes` TEXT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_SmeInvoices_InvoiceNumber` (`InvoiceNumber`),
    INDEX `IX_SmeInvoices_SmeUserId` (`SmeUserId`),
    INDEX `IX_SmeInvoices_Status` (`Status`),
    INDEX `IX_SmeInvoices_Period` (`PeriodStart`, `PeriodEnd`),
    CONSTRAINT `FK_SmeInvoices_Users_SmeUserId` FOREIGN KEY (`SmeUserId`) 
        REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_SmeInvoices_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) 
        REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- Step 3: Create SmeInvoiceLine table (with unique constraint to prevent duplicates)
-- ============================================================================

CREATE TABLE IF NOT EXISTS `SmeInvoiceLines` (
    `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `InvoiceId` BIGINT NOT NULL,
    `AssignmentId` INT NOT NULL,
    `ServiceRequestId` INT NOT NULL,
    `Description` VARCHAR(500) NULL,
    `Amount` DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UQ_InvoiceLine_Assignment` (`AssignmentId`) COMMENT 'Prevents duplicate billing of same assignment',
    INDEX `IX_SmeInvoiceLines_InvoiceId` (`InvoiceId`),
    INDEX `IX_SmeInvoiceLines_ServiceRequestId` (`ServiceRequestId`),
    CONSTRAINT `FK_SmeInvoiceLines_SmeInvoices_InvoiceId` FOREIGN KEY (`InvoiceId`) 
        REFERENCES `SmeInvoices` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_SmeInvoiceLines_ServiceRequestAssignments_AssignmentId` FOREIGN KEY (`AssignmentId`) 
        REFERENCES `ServiceRequestAssignments` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_SmeInvoiceLines_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) 
        REFERENCES `ServiceRequests` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- Step 4: Initialize existing billable assignments
-- ============================================================================

-- Set BillingStatus = 'Ready' for assignments that are billable but not yet invoiced
UPDATE `ServiceRequestAssignments`
SET `BillingStatus` = 'Ready'
WHERE `IsBillable` = 1
    AND (`Status` = 'InProgress' OR `Status` = 'Completed')
    AND (`BillingStatus` IS NULL OR `BillingStatus` = 'NotBillable' OR `BillingStatus` = '');

-- Set BillingStatus = 'NotBillable' for assignments that are not billable
UPDATE `ServiceRequestAssignments`
SET `BillingStatus` = 'NotBillable'
WHERE `IsBillable` = 0
    AND (`BillingStatus` IS NULL OR `BillingStatus` = '');

-- ============================================================================
-- Step 5: Verification queries
-- ============================================================================

-- Verify columns were added
SELECT 
    'ServiceRequestAssignments billing columns' AS TableName,
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME = 'ServiceRequestAssignments'
    AND COLUMN_NAME IN ('BillingStatus', 'InvoiceId', 'BilledAt', 'PaidAt')
ORDER BY ORDINAL_POSITION;

-- Show billing status distribution
SELECT 
    BillingStatus,
    COUNT(*) AS Count,
    SUM(CASE WHEN IsBillable = 1 THEN 1 ELSE 0 END) AS BillableCount,
    SUM(CASE WHEN InvoiceId IS NOT NULL THEN 1 ELSE 0 END) AS InvoicedCount
FROM `ServiceRequestAssignments`
GROUP BY BillingStatus
ORDER BY Count DESC;

-- Verify tables were created
SELECT 
    TABLE_NAME,
    TABLE_ROWS,
    CREATE_TIME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME IN ('SmeInvoices', 'SmeInvoiceLines')
ORDER BY TABLE_NAME;

SELECT 'Migration completed successfully!' AS Status;

