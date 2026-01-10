-- =============================================
-- Migration Script: Add Password Reset Fields
-- Description: Adds PasswordResetToken and PasswordResetTokenExpiry columns to Users table
-- Table: Users
-- =============================================

-- =============================================
-- Step 1: Add PasswordResetToken column to Users table
-- =============================================
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'PasswordResetToken');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` ADD COLUMN `PasswordResetToken` VARCHAR(500) NULL AFTER `MustChangePassword`;',
    'SELECT "PasswordResetToken column already exists in Users" AS message;');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- Step 2: Add PasswordResetTokenExpiry column to Users table
-- =============================================
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'PasswordResetTokenExpiry');

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `Users` ADD COLUMN `PasswordResetTokenExpiry` DATETIME(6) NULL AFTER `PasswordResetToken`;',
    'SELECT "PasswordResetTokenExpiry column already exists in Users" AS message;');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- Step 3: Add index on PasswordResetToken for faster lookups
-- =============================================
SET @index_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Users' 
    AND INDEX_NAME = 'IX_Users_PasswordResetToken');

SET @sql = IF(@index_exists = 0, 
    'CREATE INDEX `IX_Users_PasswordResetToken` ON `Users` (`PasswordResetToken`);',
    'SELECT "IX_Users_PasswordResetToken index already exists" AS message;');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- Migration Complete
-- =============================================
SELECT 'Password reset fields migration completed successfully' AS message;

