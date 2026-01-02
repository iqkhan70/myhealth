-- ============================================================================
-- Service Request Charges and Company Billing Protection
-- ============================================================================
-- This script implements a charge-based billing system that prevents double
-- billing of the same company for the same Service Request, even if multiple
-- SMEs from that company worked on it.
--
-- Key Safety Features:
-- 1. Unique constraint: Only one charge per SR per BillingAccount (company or individual)
-- 2. Charge-based billing: Bills charges, not assignments directly
-- 3. Company grouping: Multiple SMEs from same company = one charge
-- 4. Database-level protection: Physically impossible to create duplicate charges
-- ============================================================================

USE `customerhealthdb`;

-- ============================================================================
-- Step 1: Add CompanyId to Users table (for SME company association)
-- ============================================================================

-- Check if CompanyId column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'CompanyId');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` 
     ADD COLUMN `CompanyId` INT NULL
     COMMENT ''Company ID for SMEs - if set, billing goes to company, else to individual SME'',
     ADD INDEX `IX_Users_CompanyId` (`CompanyId`);',
    'SELECT "CompanyId column already exists in Users" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 2: Create ServiceRequestCharges table
-- ============================================================================
-- This table ensures ONE charge per SR per BillingAccount (company or individual)
-- The unique constraint (ServiceRequestId, BillingAccountId) physically prevents duplicates

CREATE TABLE IF NOT EXISTS `ServiceRequestCharges` (
    `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `ServiceRequestId` INT NOT NULL,
    `BillingAccountId` INT NOT NULL COMMENT 'User ID if individual, Company ID if company',
    `BillingAccountType` VARCHAR(20) NOT NULL DEFAULT 'Individual' COMMENT 'Individual or Company',
    `Amount` DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    `Status` VARCHAR(20) NOT NULL DEFAULT 'Ready' COMMENT 'Ready, Invoiced, Paid, Voided',
    `InvoiceId` BIGINT NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `InvoicedAt` DATETIME NULL,
    `PaidAt` DATETIME NULL,
    `VoidedAt` DATETIME NULL,
    `Notes` TEXT NULL,
    PRIMARY KEY (`Id`),
    -- CRITICAL: This unique constraint prevents double billing
    -- Only ONE charge per SR per BillingAccount (company or individual)
    UNIQUE INDEX `UQ_SR_BillingAccount` (`ServiceRequestId`, `BillingAccountId`) 
        COMMENT 'Prevents duplicate charges: one charge per SR per company/individual',
    INDEX `IX_ServiceRequestCharges_ServiceRequestId` (`ServiceRequestId`),
    INDEX `IX_ServiceRequestCharges_BillingAccountId` (`BillingAccountId`),
    INDEX `IX_ServiceRequestCharges_Status` (`Status`),
    INDEX `IX_ServiceRequestCharges_InvoiceId` (`InvoiceId`),
    CONSTRAINT `FK_ServiceRequestCharges_ServiceRequests_ServiceRequestId` 
        FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- Step 3: Add BillingAccountId to SmeInvoice table
-- ============================================================================
-- This allows invoices to be addressed to either a company or an individual SME

-- Check if BillingAccountId column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'SmeInvoices' 
    AND COLUMN_NAME = 'BillingAccountId');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `SmeInvoices` 
     ADD COLUMN `BillingAccountId` INT NOT NULL DEFAULT 0
         COMMENT ''Billing account ID (CompanyId if company, SmeUserId if individual)'',
     ADD COLUMN `BillingAccountType` VARCHAR(20) NOT NULL DEFAULT ''Individual''
         COMMENT ''Individual or Company'',
     ADD INDEX `IX_SmeInvoices_BillingAccountId` (`BillingAccountId`);',
    'SELECT "BillingAccountId column already exists in SmeInvoices" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 4: Add ChargeId to SmeInvoiceLine table
-- ============================================================================
-- Link invoice lines to charges instead of (or in addition to) assignments

-- Check if ChargeId column exists
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'SmeInvoiceLines' 
    AND COLUMN_NAME = 'ChargeId');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `SmeInvoiceLines` 
     ADD COLUMN `ChargeId` BIGINT NULL
         COMMENT ''Reference to ServiceRequestCharge (if billing by charge)'',
     ADD INDEX `IX_SmeInvoiceLines_ChargeId` (`ChargeId`),
     ADD CONSTRAINT `FK_SmeInvoiceLines_ServiceRequestCharges_ChargeId` 
         FOREIGN KEY (`ChargeId`) REFERENCES `ServiceRequestCharges` (`Id`) ON DELETE SET NULL;',
    'SELECT "ChargeId column already exists in SmeInvoiceLines" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 5: Create initial charges from existing billable assignments
-- ============================================================================
-- This migration script creates charges for existing Ready assignments
-- Groups by ServiceRequestId and BillingAccountId (CompanyId or SmeUserId)

INSERT INTO `ServiceRequestCharges` 
    (`ServiceRequestId`, `BillingAccountId`, `BillingAccountType`, `Amount`, `Status`, `CreatedAt`)
SELECT 
    a.ServiceRequestId,
    -- BillingAccountId: Use CompanyId if exists, else SmeUserId
    COALESCE(u.CompanyId, a.SmeUserId) AS BillingAccountId,
    -- BillingAccountType: Company if CompanyId exists, else Individual
    CASE WHEN u.CompanyId IS NOT NULL THEN 'Company' ELSE 'Individual' END AS BillingAccountType,
    100.00 AS Amount, -- Default amount (adjust as needed)
    'Ready' AS Status,
    NOW() AS CreatedAt
FROM `ServiceRequestAssignments` a
INNER JOIN `Users` u ON a.SmeUserId = u.Id
WHERE a.IsBillable = 1
    AND a.BillingStatus = 'Ready'
    AND a.InvoiceId IS NULL
    AND (a.Status = 'InProgress' OR a.Status = 'Completed')
GROUP BY 
    a.ServiceRequestId,
    COALESCE(u.CompanyId, a.SmeUserId),
    CASE WHEN u.CompanyId IS NOT NULL THEN 'Company' ELSE 'Individual' END
ON DUPLICATE KEY UPDATE 
    -- If charge already exists (shouldn't happen, but safe), update status
    `Status` = 'Ready',
    `Amount` = 100.00;

-- ============================================================================
-- Step 6: Verification queries
-- ============================================================================

-- Verify CompanyId column was added
SELECT 
    'Users.CompanyId column' AS CheckItem,
    COLUMN_NAME,
    COLUMN_TYPE,
    IS_NULLABLE,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME = 'Users'
    AND COLUMN_NAME = 'CompanyId';

-- Verify ServiceRequestCharges table was created
SELECT 
    'ServiceRequestCharges table' AS CheckItem,
    TABLE_NAME,
    TABLE_ROWS,
    CREATE_TIME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME = 'ServiceRequestCharges';

-- Verify unique constraint exists
SELECT 
    'Unique constraint check' AS CheckItem,
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'customerhealthdb'
    AND TABLE_NAME = 'ServiceRequestCharges'
    AND CONSTRAINT_NAME = 'UQ_SR_BillingAccount';

-- Show charge distribution
SELECT 
    'Charge distribution' AS CheckItem,
    Status,
    BillingAccountType,
    COUNT(*) AS ChargeCount,
    SUM(Amount) AS TotalAmount
FROM `ServiceRequestCharges`
GROUP BY Status, BillingAccountType
ORDER BY Status, BillingAccountType;

-- Show charges per SR (should be max 1 per company per SR)
SELECT 
    'Charges per SR per Company' AS CheckItem,
    ServiceRequestId,
    BillingAccountId,
    BillingAccountType,
    COUNT(*) AS ChargeCount
FROM `ServiceRequestCharges`
GROUP BY ServiceRequestId, BillingAccountId, BillingAccountType
HAVING COUNT(*) > 1; -- This should return 0 rows if constraint is working

SELECT 'Migration completed successfully!' AS Status;
SELECT 'IMPORTANT: Review the charge distribution query above to verify data integrity.' AS Note;

