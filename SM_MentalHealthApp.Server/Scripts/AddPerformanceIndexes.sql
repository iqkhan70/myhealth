-- =============================================
-- Performance Indexes Migration Script
-- Description: Adds critical indexes for Users and UserAssignments tables to optimize queries on large datasets
-- This script is idempotent - safe to run multiple times
-- =============================================

-- =============================================
-- Step 1: Add indexes to Users table
-- =============================================

-- Index on RoleId (critical for filtering patients - RoleId = 1)
-- Check if index exists before creating
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'Users' 
    AND index_name = 'IX_Users_RoleId'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_Users_RoleId ON Users(RoleId)',
    'SELECT "Index IX_Users_RoleId already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index on IsActive (critical for filtering active users)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'Users' 
    AND index_name = 'IX_Users_IsActive'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_Users_IsActive ON Users(IsActive)',
    'SELECT "Index IX_Users_IsActive already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Composite index on RoleId and IsActive (most common filter combination)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'Users' 
    AND index_name = 'IX_Users_RoleId_IsActive'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_Users_RoleId_IsActive ON Users(RoleId, IsActive)',
    'SELECT "Index IX_Users_RoleId_IsActive already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index on FirstName (for name searches)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'Users' 
    AND index_name = 'IX_Users_FirstName'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_Users_FirstName ON Users(FirstName)',
    'SELECT "Index IX_Users_FirstName already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index on LastName (for name searches)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'Users' 
    AND index_name = 'IX_Users_LastName'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_Users_LastName ON Users(LastName)',
    'SELECT "Index IX_Users_LastName already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- Step 2: Add indexes to UserAssignments table
-- =============================================

-- Index on AssignerId (for finding patients assigned to a doctor/coordinator)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'UserAssignments' 
    AND index_name = 'IX_UserAssignments_AssignerId'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_UserAssignments_AssignerId ON UserAssignments(AssignerId)',
    'SELECT "Index IX_UserAssignments_AssignerId already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index on AssigneeId (for finding doctors/coordinators assigned to a patient)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'UserAssignments' 
    AND index_name = 'IX_UserAssignments_AssigneeId'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_UserAssignments_AssigneeId ON UserAssignments(AssigneeId)',
    'SELECT "Index IX_UserAssignments_AssigneeId already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index on IsActive (for filtering active assignments)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'UserAssignments' 
    AND index_name = 'IX_UserAssignments_IsActive'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_UserAssignments_IsActive ON UserAssignments(IsActive)',
    'SELECT "Index IX_UserAssignments_IsActive already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Composite index on AssignerId and IsActive (most common query pattern)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'UserAssignments' 
    AND index_name = 'IX_UserAssignments_AssignerId_IsActive'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_UserAssignments_AssignerId_IsActive ON UserAssignments(AssignerId, IsActive)',
    'SELECT "Index IX_UserAssignments_AssignerId_IsActive already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Composite index on AssigneeId and IsActive (for reverse lookups)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
    AND table_name = 'UserAssignments' 
    AND index_name = 'IX_UserAssignments_AssigneeId_IsActive'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX IX_UserAssignments_AssigneeId_IsActive ON UserAssignments(AssigneeId, IsActive)',
    'SELECT "Index IX_UserAssignments_AssigneeId_IsActive already exists" AS message'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =============================================
-- Verification: Show all indexes created
-- =============================================
SELECT 'Performance indexes migration completed successfully!' AS status;

SELECT 
    table_name,
    index_name,
    GROUP_CONCAT(column_name ORDER BY seq_in_index) AS columns
FROM information_schema.statistics
WHERE table_schema = DATABASE()
AND table_name IN ('Users', 'UserAssignments')
AND index_name LIKE 'IX_%'
GROUP BY table_name, index_name
ORDER BY table_name, index_name;

