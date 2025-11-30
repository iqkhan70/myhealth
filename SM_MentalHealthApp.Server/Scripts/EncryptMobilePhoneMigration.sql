-- =============================================
-- Migration Script: Encrypt MobilePhone
-- Description: Adds MobilePhoneEncrypted column, migrates existing data, and drops old MobilePhone column
-- Tables: Users, UserRequests
-- =============================================

-- =============================================
-- Step 1: Add MobilePhoneEncrypted column to Users table
-- =============================================
ALTER TABLE `Users`
ADD COLUMN `MobilePhoneEncrypted` VARCHAR(500) NULL AFTER `Gender`;

-- =============================================
-- Step 2: Copy existing MobilePhone data to MobilePhoneEncrypted (as plain text initially)
-- =============================================
UPDATE `Users`
SET `MobilePhoneEncrypted` = `MobilePhone`
WHERE `MobilePhone` IS NOT NULL AND `MobilePhone` != '';

-- =============================================
-- Step 3: Drop the old MobilePhone column from Users table
-- =============================================
ALTER TABLE `Users`
DROP COLUMN `MobilePhone`;

-- =============================================
-- Step 4: Add MobilePhoneEncrypted column to UserRequests table
-- =============================================
ALTER TABLE `UserRequests`
ADD COLUMN `MobilePhoneEncrypted` VARCHAR(500) NOT NULL DEFAULT '' AFTER `Gender`;

-- =============================================
-- Step 5: Copy existing MobilePhone data to MobilePhoneEncrypted (as plain text initially)
-- =============================================
UPDATE `UserRequests`
SET `MobilePhoneEncrypted` = `MobilePhone`
WHERE `MobilePhone` IS NOT NULL AND `MobilePhone` != '';

-- =============================================
-- Step 6: Drop the old MobilePhone column from UserRequests table
-- =============================================
ALTER TABLE `UserRequests`
DROP COLUMN `MobilePhone`;

-- =============================================
-- Step 7: Drop the index on MobilePhone from UserRequests (if it exists)
-- Note: Indexes on encrypted data are not useful, so we remove it
-- =============================================
-- Check if index exists and drop it
-- MySQL doesn't have a direct "IF EXISTS" for indexes, so we use a stored procedure approach
-- Or you can manually check and drop:
-- SHOW INDEX FROM `UserRequests` WHERE Key_name = 'IX_UserRequests_MobilePhone';
-- DROP INDEX `IX_UserRequests_MobilePhone` ON `UserRequests`; -- Uncomment if index exists

-- =============================================
-- Verification Queries (run these to verify the migration)
-- =============================================
-- SELECT COUNT(*) as TotalUsers, 
--        COUNT(MobilePhoneEncrypted) as UsersWithPhone 
-- FROM `Users`;

-- SELECT COUNT(*) as TotalRequests, 
--        COUNT(MobilePhoneEncrypted) as RequestsWithPhone 
-- FROM `UserRequests`;

-- SELECT Id, FirstName, LastName, MobilePhoneEncrypted 
-- FROM `Users` 
-- WHERE MobilePhoneEncrypted IS NOT NULL 
-- LIMIT 10;

