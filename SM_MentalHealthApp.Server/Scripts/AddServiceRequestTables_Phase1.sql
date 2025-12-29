-- ============================================================================
-- Phase 1: Add ServiceRequest Tables and Nullable Foreign Keys
-- ============================================================================
-- This script adds the ServiceRequest and ServiceRequestAssignment tables
-- and adds nullable ServiceRequestId columns to content tables.
-- 
-- SAFE TO RUN: This phase does not change any existing behavior.
-- All ServiceRequestId columns are nullable, so existing data remains unchanged.
-- ============================================================================

USE customerhealthdb;

-- ============================================================================
-- Step 1: Create ServiceRequest table
-- ============================================================================
CREATE TABLE IF NOT EXISTS `ServiceRequests` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientId` INT NOT NULL,
    `Title` VARCHAR(200) NOT NULL,
    `Type` VARCHAR(100) NULL,
    `Status` VARCHAR(50) NOT NULL DEFAULT 'Active',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NULL,
    `CreatedByUserId` INT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `Description` VARCHAR(1000) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_ServiceRequests_ClientId` (`ClientId`),
    INDEX `IX_ServiceRequests_Status` (`Status`),
    INDEX `IX_ServiceRequests_IsActive` (`IsActive`),
    INDEX `IX_ServiceRequests_CreatedAt` (`CreatedAt`),
    INDEX `IX_ServiceRequests_ClientId_IsActive` (`ClientId`, `IsActive`),
    CONSTRAINT `FK_ServiceRequests_Users_ClientId` FOREIGN KEY (`ClientId`) 
        REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ServiceRequests_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) 
        REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- Step 2: Create ServiceRequestAssignment table
-- ============================================================================
CREATE TABLE IF NOT EXISTS `ServiceRequestAssignments` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ServiceRequestId` INT NOT NULL,
    `SmeUserId` INT NOT NULL,
    `AssignedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UnassignedAt` DATETIME(6) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `AssignedByUserId` INT NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_ServiceRequestAssignments_ServiceRequestId` (`ServiceRequestId`),
    INDEX `IX_ServiceRequestAssignments_SmeUserId` (`SmeUserId`),
    INDEX `IX_ServiceRequestAssignments_IsActive` (`IsActive`),
    INDEX `IX_ServiceRequestAssignments_ServiceRequestId_IsActive` (`ServiceRequestId`, `IsActive`),
    INDEX `IX_ServiceRequestAssignments_SmeUserId_IsActive` (`SmeUserId`, `IsActive`),
    CONSTRAINT `FK_ServiceRequestAssignments_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) 
        REFERENCES `ServiceRequests` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ServiceRequestAssignments_Users_SmeUserId` FOREIGN KEY (`SmeUserId`) 
        REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ServiceRequestAssignments_Users_AssignedByUserId` FOREIGN KEY (`AssignedByUserId`) 
        REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- Step 3: Add nullable ServiceRequestId to content tables
-- ============================================================================

-- ClinicalNotes
ALTER TABLE `ClinicalNotes` 
    ADD COLUMN `ServiceRequestId` INT NULL AFTER `DoctorId`,
    ADD INDEX `IX_ClinicalNotes_ServiceRequestId` (`ServiceRequestId`),
    ADD CONSTRAINT `FK_ClinicalNotes_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) 
        REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;

-- Contents (ContentItem)
ALTER TABLE `Contents` 
    ADD COLUMN `ServiceRequestId` INT NULL AFTER `ContentTypeModelId`,
    ADD INDEX `IX_Contents_ServiceRequestId` (`ServiceRequestId`),
    ADD CONSTRAINT `FK_Contents_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) 
        REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;

-- JournalEntries
ALTER TABLE `JournalEntries` 
    ADD COLUMN `ServiceRequestId` INT NULL AFTER `EnteredByUserId`,
    ADD INDEX `IX_JournalEntries_ServiceRequestId` (`ServiceRequestId`),
    ADD CONSTRAINT `FK_JournalEntries_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) 
        REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;

-- ChatSessions
ALTER TABLE `ChatSessions` 
    ADD COLUMN `ServiceRequestId` INT NULL AFTER `PatientId`,
    ADD INDEX `IX_ChatSessions_ServiceRequestId` (`ServiceRequestId`),
    ADD CONSTRAINT `FK_ChatSessions_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) 
        REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;

-- Appointments
ALTER TABLE `Appointments` 
    ADD COLUMN `ServiceRequestId` INT NULL AFTER `PatientId`,
    ADD INDEX `IX_Appointments_ServiceRequestId` (`ServiceRequestId`),
    ADD CONSTRAINT `FK_Appointments_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) 
        REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;

-- ContentAlerts
ALTER TABLE `ContentAlerts` 
    ADD COLUMN `ServiceRequestId` INT NULL AFTER `PatientId`,
    ADD INDEX `IX_ContentAlerts_ServiceRequestId` (`ServiceRequestId`),
    ADD CONSTRAINT `FK_ContentAlerts_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) 
        REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL;

-- ============================================================================
-- Verification: Check that all columns were added successfully
-- ============================================================================
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    IS_NULLABLE,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'mentalhealthdb'
    AND COLUMN_NAME = 'ServiceRequestId'
ORDER BY TABLE_NAME, COLUMN_NAME;

-- ============================================================================
-- Phase 1 Complete
-- ============================================================================
-- All tables and columns have been added. The application can now be deployed
-- and will continue to work as before (since all ServiceRequestId columns are nullable).
-- 
-- Next step: Run AddServiceRequestDataMigration_Phase2.sql to create default
-- ServiceRequests and backfill existing data.
-- ============================================================================

