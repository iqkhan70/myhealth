-- Add Location Matching System for SMEs and Service Requests
-- This allows coordinators to find SMEs based on proximity to client location
-- Uses ZIP codes with latitude/longitude for distance calculation

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

