-- ============================================================================
-- Billing Accounts and Rates System
-- ============================================================================
-- This script implements a clean pricing system where:
-- 1. BillingAccounts represent "who gets billed" (Company or Individual)
-- 2. BillingRates store pricing per (BillingAccount, Expertise) combination
-- 3. ServiceRequests have a PrimaryExpertiseId for pricing determination
-- 4. ServiceRequestCharges store the ExpertiseId and RateSource used
-- ============================================================================

USE `customerhealthdb`;

-- Temporarily disable safe update mode for this migration
SET SQL_SAFE_UPDATES = 0;

-- ============================================================================
-- Step 1: Create BillingAccounts table
-- ============================================================================
-- Represents "who gets billed" - either a Company or an Individual SME
-- Every SME has exactly one BillingAccountId

CREATE TABLE IF NOT EXISTS `BillingAccounts` (
    `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `Type` VARCHAR(20) NOT NULL COMMENT 'Company or Individual',
    `CompanyId` INT NULL COMMENT 'If Type=Company, this is the Company ID',
    `UserId` INT NULL COMMENT 'If Type=Individual, this is the User ID',
    `Name` VARCHAR(255) NULL COMMENT 'Convenience field for display',
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_BillingAccounts_Company` (`CompanyId`),
    UNIQUE KEY `UQ_BillingAccounts_User` (`UserId`),
    INDEX `IX_BillingAccounts_Type` (`Type`),
    INDEX `IX_BillingAccounts_IsActive` (`IsActive`),
    CONSTRAINT `FK_BillingAccounts_Companies` 
        FOREIGN KEY (`CompanyId`) REFERENCES `Companies` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_BillingAccounts_Users` 
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- Step 2: Add BillingAccountId to Users table
-- ============================================================================

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'BillingAccountId');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` 
     ADD COLUMN `BillingAccountId` BIGINT NULL
     COMMENT ''Billing account for this user. If SME belongs to company, points to company billing account, else to individual billing account.'',
     ADD INDEX `IX_Users_BillingAccountId` (`BillingAccountId`),
     ADD CONSTRAINT `FK_Users_BillingAccounts` 
         FOREIGN KEY (`BillingAccountId`) REFERENCES `BillingAccounts` (`Id`) ON DELETE SET NULL;',
    'SELECT "BillingAccountId column already exists in Users" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 3: Create BillingRates table
-- ============================================================================
-- Stores pricing per (BillingAccount, Expertise) combination
-- Allows different prices for different expertise types per billing account

CREATE TABLE IF NOT EXISTS `BillingRates` (
    `Id` BIGINT NOT NULL AUTO_INCREMENT,
    `BillingAccountId` BIGINT NOT NULL,
    `ExpertiseId` INT NOT NULL,
    `Amount` DECIMAL(10,2) NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UQ_BillingRates_Account_Expertise` (`BillingAccountId`, `ExpertiseId`),
    INDEX `IX_BillingRates_ExpertiseId` (`ExpertiseId`),
    INDEX `IX_BillingRates_IsActive` (`IsActive`),
    CONSTRAINT `FK_BillingRates_BillingAccounts` 
        FOREIGN KEY (`BillingAccountId`) REFERENCES `BillingAccounts` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_BillingRates_Expertise` 
        FOREIGN KEY (`ExpertiseId`) REFERENCES `Expertise` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- Step 4: Add PrimaryExpertiseId to ServiceRequests
-- ============================================================================
-- ServiceRequests can have multiple expertise tags, but pricing needs a single "billing category"
-- Coordinator must choose PrimaryExpertiseId when making it billable

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequests' 
    AND COLUMN_NAME = 'PrimaryExpertiseId');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequests` 
     ADD COLUMN `PrimaryExpertiseId` INT NULL
     COMMENT ''Primary expertise used for billing/pricing. If NULL and SR has exactly 1 expertise, use that. Otherwise coordinator must select.'',
     ADD INDEX `IX_ServiceRequests_PrimaryExpertiseId` (`PrimaryExpertiseId`),
     ADD CONSTRAINT `FK_ServiceRequests_PrimaryExpertise` 
         FOREIGN KEY (`PrimaryExpertiseId`) REFERENCES `Expertise` (`Id`) ON DELETE SET NULL;',
    'SELECT "PrimaryExpertiseId column already exists in ServiceRequests" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 5: Update ServiceRequestCharges table
-- ============================================================================
-- Add ExpertiseId and RateSource to track which expertise and rate was used

SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequestCharges' 
    AND COLUMN_NAME = 'ExpertiseId');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequestCharges` 
     ADD COLUMN `ExpertiseId` INT NULL
     COMMENT ''Primary expertise used for pricing this charge'',
     ADD COLUMN `RateSource` VARCHAR(50) NOT NULL DEFAULT ''Default''
     COMMENT ''Source of the rate: BillingRate or Default (system default $100)'',
     ADD INDEX `IX_ServiceRequestCharges_ExpertiseId` (`ExpertiseId`),
     ADD CONSTRAINT `FK_ServiceRequestCharges_Expertise` 
         FOREIGN KEY (`ExpertiseId`) REFERENCES `Expertise` (`Id`) ON DELETE SET NULL;',
    'SELECT "ExpertiseId and RateSource columns already exist in ServiceRequestCharges" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Step 6: Backfill - Create BillingAccounts for existing companies
-- ============================================================================

INSERT INTO `BillingAccounts` (`Type`, `CompanyId`, `Name`, `IsActive`, `CreatedAt`)
SELECT 'Company', c.Id, c.Name, c.IsActive, NOW()
FROM `Companies` c
LEFT JOIN `BillingAccounts` ba ON ba.CompanyId = c.Id
WHERE ba.Id IS NULL;

-- ============================================================================
-- Step 7: Backfill - Create BillingAccounts for SMEs without a company
-- ============================================================================
-- SME roles: RoleId 6 (SME), 2 (Doctor), 5 (Attorney)

INSERT INTO `BillingAccounts` (`Type`, `UserId`, `Name`, `IsActive`, `CreatedAt`)
SELECT 'Individual', u.Id, CONCAT(u.FirstName, ' ', u.LastName), u.IsActive, NOW()
FROM `Users` u
LEFT JOIN `BillingAccounts` ba ON ba.UserId = u.Id
WHERE u.RoleId IN (2, 5, 6) 
    AND u.CompanyId IS NULL 
    AND ba.Id IS NULL;

-- ============================================================================
-- Step 8: Backfill - Set Users.BillingAccountId
-- ============================================================================
-- For SMEs with company: point to company's billing account
-- For independent SMEs: point to their individual billing account
-- Note: Added u.Id > 0 to satisfy MySQL safe update mode requirement

UPDATE `Users` u
JOIN `BillingAccounts` ba ON (
    (u.CompanyId IS NOT NULL AND ba.CompanyId = u.CompanyId AND ba.Type = 'Company')
    OR
    (u.CompanyId IS NULL AND ba.UserId = u.Id AND ba.Type = 'Individual')
)
SET u.BillingAccountId = ba.Id
WHERE u.Id > 0  -- Required for MySQL safe update mode (uses primary key)
    AND u.RoleId IN (2, 5, 6) 
    AND u.BillingAccountId IS NULL;

-- ============================================================================
-- Step 9: Verify migration
-- ============================================================================

SELECT 
    'BillingAccounts' AS TableName,
    COUNT(*) AS RecordCount
FROM `BillingAccounts`

UNION ALL

SELECT 
    'BillingRates' AS TableName,
    COUNT(*) AS RecordCount
FROM `BillingRates`

UNION ALL

SELECT 
    'Users with BillingAccountId' AS TableName,
    COUNT(*) AS RecordCount
FROM `Users`
WHERE `BillingAccountId` IS NOT NULL AND `RoleId` IN (2, 5, 6);

-- Re-enable safe update mode
SET SQL_SAFE_UPDATES = 1;

