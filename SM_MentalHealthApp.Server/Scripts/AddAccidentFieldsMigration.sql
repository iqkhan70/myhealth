-- =============================================
-- Migration Script: Add Accident Fields
-- Description: Adds accident-related fields to UserRequests and Users tables
-- Tables: UserRequests, Users
-- Note: The deployment script checks for existing columns before running this
-- =============================================

-- =============================================
-- Step 1: Add accident fields to UserRequests table
-- =============================================
ALTER TABLE `UserRequests`
ADD COLUMN `Age` INT NULL AFTER `Notes`,
ADD COLUMN `Race` VARCHAR(100) NULL AFTER `Age`,
ADD COLUMN `AccidentAddress` VARCHAR(500) NULL AFTER `Race`,
ADD COLUMN `AccidentDate` DATETIME NULL AFTER `AccidentAddress`,
ADD COLUMN `VehicleDetails` VARCHAR(1000) NULL AFTER `AccidentDate`,
ADD COLUMN `DateReported` DATETIME NULL AFTER `VehicleDetails`,
ADD COLUMN `PoliceCaseNumber` VARCHAR(100) NULL AFTER `DateReported`,
ADD COLUMN `AccidentDetails` VARCHAR(2000) NULL AFTER `PoliceCaseNumber`,
ADD COLUMN `RoadConditions` VARCHAR(200) NULL AFTER `AccidentDetails`,
ADD COLUMN `DoctorsInformation` VARCHAR(1000) NULL AFTER `RoadConditions`,
ADD COLUMN `LawyersInformation` VARCHAR(1000) NULL AFTER `DoctorsInformation`,
ADD COLUMN `AdditionalNotes` VARCHAR(2000) NULL AFTER `LawyersInformation`;

-- =============================================
-- Step 2: Add accident fields to Users table
-- =============================================
ALTER TABLE `Users`
ADD COLUMN `Age` INT NULL AFTER `LicenseNumber`,
ADD COLUMN `Race` VARCHAR(100) NULL AFTER `Age`,
ADD COLUMN `AccidentAddress` VARCHAR(500) NULL AFTER `Race`,
ADD COLUMN `AccidentDate` DATETIME NULL AFTER `AccidentAddress`,
ADD COLUMN `VehicleDetails` VARCHAR(1000) NULL AFTER `AccidentDate`,
ADD COLUMN `DateReported` DATETIME NULL AFTER `VehicleDetails`,
ADD COLUMN `PoliceCaseNumber` VARCHAR(100) NULL AFTER `DateReported`,
ADD COLUMN `AccidentDetails` VARCHAR(2000) NULL AFTER `PoliceCaseNumber`,
ADD COLUMN `RoadConditions` VARCHAR(200) NULL AFTER `AccidentDetails`,
ADD COLUMN `DoctorsInformation` VARCHAR(1000) NULL AFTER `RoadConditions`,
ADD COLUMN `LawyersInformation` VARCHAR(1000) NULL AFTER `DoctorsInformation`,
ADD COLUMN `AdditionalNotes` VARCHAR(2000) NULL AFTER `LawyersInformation`;

-- =============================================
-- Verification Queries (run these to verify the migration)
-- =============================================
-- SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
-- FROM INFORMATION_SCHEMA.COLUMNS
-- WHERE TABLE_SCHEMA = DATABASE()
--   AND TABLE_NAME = 'UserRequests'
--   AND COLUMN_NAME IN ('Age', 'Race', 'AccidentAddress', 'AccidentDate', 'VehicleDetails', 
--                       'DateReported', 'PoliceCaseNumber', 'AccidentDetails', 'RoadConditions',
--                       'DoctorsInformation', 'LawyersInformation', 'AdditionalNotes')
-- ORDER BY ORDINAL_POSITION;

-- SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
-- FROM INFORMATION_SCHEMA.COLUMNS
-- WHERE TABLE_SCHEMA = DATABASE()
--   AND TABLE_NAME = 'Users'
--   AND COLUMN_NAME IN ('Age', 'Race', 'AccidentAddress', 'AccidentDate', 'VehicleDetails', 
--                       'DateReported', 'PoliceCaseNumber', 'AccidentDetails', 'RoadConditions',
--                       'DoctorsInformation', 'LawyersInformation', 'AdditionalNotes')
-- ORDER BY ORDINAL_POSITION;
