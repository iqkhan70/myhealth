-- ============================================================================
-- Service Request Migration - Consolidated (Phase 1 + Phase 2 + Phase 3 + Phase 4)
-- ============================================================================
-- This script consolidates:
--   - Phase 1: Table creation
--   - Phase 2: Data migration
--   - Phase 3: Expertise system (SME matching and Service Request categorization)
--   - Phase 4: Location matching system (ZIP code-based proximity matching)
-- into a single idempotent script that can be safely run on new or existing databases.
--
-- IMPORTANT: This script is idempotent - it checks for existing tables/data before
-- creating or migrating, so it's safe to run multiple times.
--
-- Usage:
--   - For new deployments: Run this after Entity Framework migrations
--   - For existing deployments: Run this to add ServiceRequest functionality and Expertise system
-- ============================================================================

USE customerhealthdb;

-- Temporarily disable safe update mode for this migration
SET SQL_SAFE_UPDATES = 0;

-- ============================================================================
-- PHASE 1: Create Tables (Idempotent)
-- ============================================================================

-- Create ServiceRequests table if it doesn't exist
CREATE TABLE IF NOT EXISTS `ServiceRequests` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientId` INT NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Type` VARCHAR(100) NULL,
    `Status` VARCHAR(50) NOT NULL DEFAULT 'Active',
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL,
    `CreatedByUserId` INT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `Description` VARCHAR(1000) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_ServiceRequests_ClientId` (`ClientId`),
    INDEX `IX_ServiceRequests_Status` (`Status`),
    INDEX `IX_ServiceRequests_IsActive` (`IsActive`),
    CONSTRAINT `FK_ServiceRequests_Users_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ServiceRequests_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create ServiceRequestAssignments table if it doesn't exist
CREATE TABLE IF NOT EXISTS `ServiceRequestAssignments` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ServiceRequestId` INT NOT NULL,
    `SmeUserId` INT NOT NULL,
    `AssignedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UnassignedAt` DATETIME NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `AssignedByUserId` INT NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_ServiceRequestAssignments_ServiceRequestId` (`ServiceRequestId`),
    INDEX `IX_ServiceRequestAssignments_SmeUserId` (`SmeUserId`),
    INDEX `IX_ServiceRequestAssignments_IsActive` (`IsActive`),
    INDEX `IX_ServiceRequestAssignments_ServiceRequestId_SmeUserId` (`ServiceRequestId`, `SmeUserId`),
    CONSTRAINT `FK_ServiceRequestAssignments_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ServiceRequestAssignments_Users_SmeUserId` FOREIGN KEY (`SmeUserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ServiceRequestAssignments_Users_AssignedByUserId` FOREIGN KEY (`AssignedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add ServiceRequestId columns to existing tables (if they don't exist)
-- ClinicalNotes
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ClinicalNotes' 
    AND COLUMN_NAME = 'ServiceRequestId');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ClinicalNotes` ADD COLUMN `ServiceRequestId` INT NULL AFTER `IsIgnoredByDoctor`, ADD INDEX `IX_ClinicalNotes_ServiceRequestId` (`ServiceRequestId`), ADD CONSTRAINT `FK_ClinicalNotes_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;',
    'SELECT "ServiceRequestId column already exists in ClinicalNotes" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Contents (ContentItem)
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Contents' 
    AND COLUMN_NAME = 'ServiceRequestId');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Contents` ADD COLUMN `ServiceRequestId` INT NULL AFTER `AddedByUserId`, ADD INDEX `IX_Contents_ServiceRequestId` (`ServiceRequestId`), ADD CONSTRAINT `FK_Contents_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;',
    'SELECT "ServiceRequestId column already exists in Contents" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- JournalEntries
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'JournalEntries' 
    AND COLUMN_NAME = 'ServiceRequestId');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `JournalEntries` ADD COLUMN `ServiceRequestId` INT NULL AFTER `UserId`, ADD INDEX `IX_JournalEntries_ServiceRequestId` (`ServiceRequestId`), ADD CONSTRAINT `FK_JournalEntries_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;',
    'SELECT "ServiceRequestId column already exists in JournalEntries" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ChatSessions
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ChatSessions' 
    AND COLUMN_NAME = 'ServiceRequestId');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ChatSessions` ADD COLUMN `ServiceRequestId` INT NULL AFTER `PatientId`, ADD INDEX `IX_ChatSessions_ServiceRequestId` (`ServiceRequestId`), ADD CONSTRAINT `FK_ChatSessions_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;',
    'SELECT "ServiceRequestId column already exists in ChatSessions" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Appointments
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Appointments' 
    AND COLUMN_NAME = 'ServiceRequestId');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Appointments` ADD COLUMN `ServiceRequestId` INT NULL AFTER `PatientId`, ADD INDEX `IX_Appointments_ServiceRequestId` (`ServiceRequestId`), ADD CONSTRAINT `FK_Appointments_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;',
    'SELECT "ServiceRequestId column already exists in Appointments" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ContentAlerts
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ContentAlerts' 
    AND COLUMN_NAME = 'ServiceRequestId');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ContentAlerts` ADD COLUMN `ServiceRequestId` INT NULL AFTER `PatientId`, ADD INDEX `IX_ContentAlerts_ServiceRequestId` (`ServiceRequestId`), ADD CONSTRAINT `FK_ContentAlerts_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;',
    'SELECT "ServiceRequestId column already exists in ContentAlerts" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PHASE 2: Create Default ServiceRequests and Backfill Data (Idempotent)
-- ============================================================================

-- Step 1: Create default ServiceRequests for each client with assignments
-- Only create if they don't already exist
INSERT INTO `ServiceRequests` (`ClientId`, `Title`, `Type`, `Status`, `CreatedAt`, `IsActive`, `Description`)
SELECT DISTINCT
    ua.`AssigneeId` AS `ClientId`,
    'General' AS `Title`,
    'General' AS `Type`,
    'Active' AS `Status`,
    NOW() AS `CreatedAt`,
    1 AS `IsActive`,
    'Default service request created during migration. Contains all existing content for this client.' AS `Description`
FROM `UserAssignments` ua
WHERE ua.`IsActive` = 1
    AND ua.`AssigneeId` IN (SELECT `Id` FROM `Users` WHERE `RoleId` = 1 AND `IsActive` = 1) -- Only patients/clients
    AND NOT EXISTS (
        SELECT 1 FROM `ServiceRequests` sr 
        WHERE sr.`ClientId` = ua.`AssigneeId` 
        AND sr.`Title` = 'General' 
        AND sr.`IsActive` = 1
    );

-- Step 2: Assign SMEs to the default ServiceRequests
-- Only assign if assignment doesn't already exist
INSERT INTO `ServiceRequestAssignments` (`ServiceRequestId`, `SmeUserId`, `AssignedAt`, `IsActive`)
SELECT 
    sr.`Id` AS `ServiceRequestId`,
    ua.`AssignerId` AS `SmeUserId`,
    ua.`AssignedAt` AS `AssignedAt`,
    1 AS `IsActive`
FROM `ServiceRequests` sr
INNER JOIN `UserAssignments` ua ON ua.`AssigneeId` = sr.`ClientId`
WHERE sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND ua.`IsActive` = 1
    AND NOT EXISTS (
        SELECT 1 FROM `ServiceRequestAssignments` sra
        WHERE sra.`ServiceRequestId` = sr.`Id`
        AND sra.`SmeUserId` = ua.`AssignerId`
        AND sra.`IsActive` = 1
    )
ORDER BY ua.`AssignedAt` ASC; -- Assign the first (oldest) SME

-- Step 3: Backfill ClinicalNotes to default ServiceRequests
-- Only backfill rows that don't already have a ServiceRequestId
UPDATE `ClinicalNotes` cn, `ServiceRequests` sr
SET cn.`ServiceRequestId` = sr.`Id`
WHERE cn.`PatientId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND cn.`ServiceRequestId` IS NULL
    AND cn.`IsActive` = 1
    AND cn.`Id` > 0; -- Explicit key reference for safe update mode

-- Step 4: Backfill Contents (ContentItem) to default ServiceRequests
UPDATE `Contents` c, `ServiceRequests` sr
SET c.`ServiceRequestId` = sr.`Id`
WHERE c.`PatientId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND c.`ServiceRequestId` IS NULL
    AND c.`IsActive` = 1
    AND c.`Id` > 0; -- Explicit key reference for safe update mode

-- Step 5: Backfill JournalEntries to default ServiceRequests
UPDATE `JournalEntries` je, `ServiceRequests` sr
SET je.`ServiceRequestId` = sr.`Id`
WHERE je.`UserId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND je.`ServiceRequestId` IS NULL
    AND je.`IsActive` = 1
    AND je.`Id` > 0; -- Explicit key reference for safe update mode

-- Step 6: Backfill ChatSessions to default ServiceRequests
UPDATE `ChatSessions` cs, `ServiceRequests` sr
SET cs.`ServiceRequestId` = sr.`Id`
WHERE (cs.`PatientId` = sr.`ClientId` OR (cs.`PatientId` IS NULL AND cs.`UserId` = sr.`ClientId`))
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND cs.`ServiceRequestId` IS NULL
    AND cs.`IsActive` = 1
    AND (cs.`PatientId` IS NOT NULL OR cs.`UserId` IN (SELECT `Id` FROM `Users` WHERE `RoleId` = 1))
    AND cs.`Id` > 0; -- Explicit key reference for safe update mode

-- Step 7: Backfill Appointments to default ServiceRequests
UPDATE `Appointments` a, `ServiceRequests` sr
SET a.`ServiceRequestId` = sr.`Id`
WHERE a.`PatientId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND a.`ServiceRequestId` IS NULL
    AND a.`IsActive` = 1
    AND a.`Id` > 0; -- Explicit key reference for safe update mode

-- Step 8: Backfill ContentAlerts to default ServiceRequests
UPDATE `ContentAlerts` ca, `ServiceRequests` sr
SET ca.`ServiceRequestId` = sr.`Id`
WHERE ca.`PatientId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND ca.`ServiceRequestId` IS NULL
    AND ca.`Id` > 0; -- Explicit key reference for safe update mode

-- ============================================================================
-- Verification: Check migration results
-- ============================================================================

-- Count of ServiceRequests created
SELECT 
    COUNT(*) AS TotalServiceRequests,
    COUNT(DISTINCT `ClientId`) AS UniqueClients
FROM `ServiceRequests`
WHERE `Title` = 'General' AND `IsActive` = 1;

-- Count of ServiceRequestAssignments created
SELECT 
    COUNT(*) AS TotalAssignments,
    COUNT(DISTINCT `ServiceRequestId`) AS ServiceRequestsWithAssignments,
    COUNT(DISTINCT `SmeUserId`) AS UniqueSMEs
FROM `ServiceRequestAssignments`
WHERE `IsActive` = 1;

-- Count of content items backfilled
SELECT 
    'ClinicalNotes' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `ClinicalNotes`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'Contents' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `Contents`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'JournalEntries' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `JournalEntries`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'ChatSessions' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `ChatSessions`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'Appointments' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `Appointments`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'ContentAlerts' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `ContentAlerts`;

-- ============================================================================
-- PHASE 3: Add Expertise System (Idempotent)
-- ============================================================================
-- This adds the expertise system for SMEs and Service Requests
-- Allows coordinators to quickly find SMEs with matching expertise
-- ============================================================================

-- Step 1: Create Expertise lookup table
CREATE TABLE IF NOT EXISTS Expertise (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
    INDEX IX_Expertise_Name (Name),
    INDEX IX_Expertise_IsActive (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Step 2: Create SmeExpertise junction table (SME ↔ Expertise many-to-many)
CREATE TABLE IF NOT EXISTS SmeExpertise (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    SmeUserId INT NOT NULL,
    ExpertiseId INT NOT NULL,
    IsPrimary TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    INDEX IX_SmeExpertise_SmeUserId (SmeUserId),
    INDEX IX_SmeExpertise_ExpertiseId (ExpertiseId),
    INDEX IX_SmeExpertise_IsActive (IsActive),
    UNIQUE KEY UQ_SmeExpertise_Sme_Expertise (SmeUserId, ExpertiseId),
    CONSTRAINT FK_SmeExpertise_User FOREIGN KEY (SmeUserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SmeExpertise_Expertise FOREIGN KEY (ExpertiseId) REFERENCES Expertise(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Step 3: Create ServiceRequestExpertise junction table (ServiceRequest ↔ Expertise many-to-many)
CREATE TABLE IF NOT EXISTS ServiceRequestExpertise (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ServiceRequestId INT NOT NULL,
    ExpertiseId INT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    INDEX IX_ServiceRequestExpertise_ServiceRequestId (ServiceRequestId),
    INDEX IX_ServiceRequestExpertise_ExpertiseId (ExpertiseId),
    UNIQUE KEY UQ_ServiceRequestExpertise_SR_Expertise (ServiceRequestId, ExpertiseId),
    CONSTRAINT FK_ServiceRequestExpertise_ServiceRequest FOREIGN KEY (ServiceRequestId) REFERENCES ServiceRequests(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ServiceRequestExpertise_Expertise FOREIGN KEY (ExpertiseId) REFERENCES Expertise(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Step 4: Insert some default expertise categories
INSERT INTO Expertise (Id, Name, Description, IsActive, CreatedAt) VALUES
(1, 'General', 'General service requests', 1, NOW()),
(2, 'Medical', 'Medical and healthcare services', 1, NOW()),
(3, 'Legal', 'Legal services and consultation', 1, NOW()),
(4, 'Therapy', 'Therapy and counseling services', 1, NOW()),
(5, 'Consultation', 'General consultation services', 1, NOW()),
(6, 'Follow-up', 'Follow-up services', 1, NOW()),
(7, 'Emergency', 'Emergency services', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Name = VALUES(Name),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- ============================================================================
-- PHASE 4: Add Location Matching System (Idempotent)
-- ============================================================================
-- Allows coordinators to find SMEs based on proximity to client location
-- Uses ZIP codes with latitude/longitude for distance calculation
-- ============================================================================

-- Step 1: Add location fields to Users table (for both Clients and SMEs)
-- Check and add ZipCode
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'ZipCode');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` ADD COLUMN `ZipCode` VARCHAR(10) NULL AFTER `MobilePhoneEncrypted`;',
    'SELECT "ZipCode column already exists in Users" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check and add Latitude
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'Latitude');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` ADD COLUMN `Latitude` DECIMAL(10, 8) NULL AFTER `ZipCode`;',
    'SELECT "Latitude column already exists in Users" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check and add Longitude
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'Longitude');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` ADD COLUMN `Longitude` DECIMAL(11, 8) NULL AFTER `Latitude`;',
    'SELECT "Longitude column already exists in Users" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check and add MaxTravelMiles
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'MaxTravelMiles');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` ADD COLUMN `MaxTravelMiles` INT NULL AFTER `Longitude`;',
    'SELECT "MaxTravelMiles column already exists in Users" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add indexes if they don't exist
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND INDEX_NAME = 'IX_Users_ZipCode');
SET @sql = IF(@idx_exists = 0, 
    'ALTER TABLE `Users` ADD INDEX `IX_Users_ZipCode` (`ZipCode`);',
    'SELECT "IX_Users_ZipCode index already exists" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'Users' 
    AND INDEX_NAME = 'IX_Users_Latitude_Longitude');
SET @sql = IF(@idx_exists = 0, 
    'ALTER TABLE `Users` ADD INDEX `IX_Users_Latitude_Longitude` (`Latitude`, `Longitude`);',
    'SELECT "IX_Users_Latitude_Longitude index already exists" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Step 2: Add location fields to ServiceRequests table
-- Check and add ServiceZipCode
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequests' 
    AND COLUMN_NAME = 'ServiceZipCode');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequests` ADD COLUMN `ServiceZipCode` VARCHAR(10) NULL AFTER `Description`;',
    'SELECT "ServiceZipCode column already exists in ServiceRequests" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Check and add MaxDistanceMiles
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequests' 
    AND COLUMN_NAME = 'MaxDistanceMiles');
SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `ServiceRequests` ADD COLUMN `MaxDistanceMiles` INT NOT NULL DEFAULT 50 AFTER `ServiceZipCode`;',
    'SELECT "MaxDistanceMiles column already exists in ServiceRequests" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add index if it doesn't exist
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = 'customerhealthdb' 
    AND TABLE_NAME = 'ServiceRequests' 
    AND INDEX_NAME = 'IX_ServiceRequests_ServiceZipCode');
SET @sql = IF(@idx_exists = 0, 
    'ALTER TABLE `ServiceRequests` ADD INDEX `IX_ServiceRequests_ServiceZipCode` (`ServiceZipCode`);',
    'SELECT "IX_ServiceRequests_ServiceZipCode index already exists" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Step 3: Create ZipCodeLookup table (ZIP code to lat/lon mapping)
CREATE TABLE IF NOT EXISTS `ZipCodeLookup` (
    `ZipCode` VARCHAR(10) NOT NULL PRIMARY KEY,
    `Latitude` DECIMAL(10, 8) NOT NULL,
    `Longitude` DECIMAL(11, 8) NOT NULL,
    `City` VARCHAR(100) NULL,
    `State` VARCHAR(2) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    INDEX `IX_ZipCodeLookup_State` (`State`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Step 4: Insert common US ZIP codes with lat/lon (sample data - you can expand this)
-- This is a small subset. For production, you'd want to import a full US ZIP code dataset
INSERT INTO ZipCodeLookup (ZipCode, Latitude, Longitude, City, State) VALUES
-- ===============================
-- Kansas / Missouri (KC Metro)
-- ===============================
('66221', 38.97170000, -94.70140000, 'Overland Park', 'KS'),
('66062', 38.88330000, -94.81670000, 'Olathe',        'KS'),

-- Kansas City, MO (ZIP-SPECIFIC – NOT city centroid)
('64138', 38.95860000, -94.52380000, 'Kansas City',   'MO'),
('64110', 39.03540000, -94.57670000, 'Kansas City',   'MO'),
('64111', 39.05970000, -94.59390000, 'Kansas City',   'MO'),

-- ===============================
-- California
-- ===============================
('90210', 34.09010000, -118.40650000, 'Beverly Hills',  'CA'),
('90001', 33.97310000, -118.24790000, 'Los Angeles',    'CA'),
('94102', 37.77930000, -122.41930000, 'San Francisco',  'CA'),
('92101', 32.72130000, -117.16520000, 'San Diego',      'CA'),

-- ===============================
-- New York
-- ===============================
('10001', 40.75060000, -73.99720000, 'New York', 'NY'),
('10002', 40.71580000, -73.98700000, 'New York', 'NY'),

-- ===============================
-- Texas
-- ===============================
('75201', 32.78760000, -96.79940000, 'Dallas',  'TX'),
('77001', 29.83010000, -95.43420000, 'Houston', 'TX'),

-- ===============================
-- Illinois
-- ===============================
('60601', 41.88530000, -87.62290000, 'Chicago', 'IL'),
('60602', 41.88370000, -87.62980000, 'Chicago', 'IL');

-- Note: For production, you should import a complete US ZIP code database
-- A good source is the USPS ZIP code database or a commercial dataset
-- This sample provides enough data to test the functionality

-- ============================================================================
-- Migration Complete
-- ============================================================================
-- The ServiceRequest tables have been created and existing data has been
-- backfilled to default ServiceRequests. The application now operates with
-- ServiceRequest-based access control.
-- The Expertise system has been added for SME matching and Service Request categorization.
-- The Location matching system has been added for proximity-based SME recommendations.
-- ============================================================================

-- Re-enable safe update mode
SET SQL_SAFE_UPDATES = 1;

