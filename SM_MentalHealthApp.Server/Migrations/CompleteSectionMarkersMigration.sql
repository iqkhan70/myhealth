-- =====================================================
-- Complete Section Markers Migration Script (MySQL-safe)
-- =====================================================
-- This script creates the SectionMarkers table, creates indexes, and seeds initial data

-- Quiet some notes (optional)
SET @OLD_SQL_NOTES = @@sql_notes; 
SET sql_notes = 0;

-- 1) Create table (with useful keys/constraints)
CREATE TABLE IF NOT EXISTS `SectionMarkers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Marker` varchar(500) NOT NULL,
    `Description` varchar(500) NULL,
    `Category` varchar(100) NULL,
    `Priority` int NOT NULL DEFAULT 0,
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    `UpdatedAt` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_SectionMarkers_Marker` (`Marker`)
) CHARACTER SET utf8mb4;

-- 2) Create indexes for performance (MySQL version independent)
SET @db = DATABASE();

-- IX_SectionMarkers_IsActive
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'SectionMarkers'
    AND index_name   = 'IX_SectionMarkers_IsActive'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_SectionMarkers_IsActive` ON `SectionMarkers` (`IsActive`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_SectionMarkers_Category
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'SectionMarkers'
    AND index_name   = 'IX_SectionMarkers_Category'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_SectionMarkers_Category` ON `SectionMarkers` (`Category`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_SectionMarkers_Priority
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'SectionMarkers'
    AND index_name   = 'IX_SectionMarkers_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_SectionMarkers_Priority` ON `SectionMarkers` (`Priority`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_SectionMarkers_IsActive_Priority
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'SectionMarkers'
    AND index_name   = 'IX_SectionMarkers_IsActive_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_SectionMarkers_IsActive_Priority` ON `SectionMarkers` (`IsActive`, `Priority`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- 3) Seed initial data
INSERT INTO SectionMarkers (Marker, Description, Category, Priority, IsActive, CreatedAt) VALUES
-- Major section markers (high priority)
('=== RECENT JOURNAL ENTRIES ===', 'Marks the start of recent journal entries section', 'Patient Data', 100, TRUE, NOW()),
('=== MEDICAL DATA SUMMARY ===', 'Marks the start of medical data summary section', 'Patient Data', 100, TRUE, NOW()),
('=== CURRENT MEDICAL STATUS ===', 'Marks the start of current medical status section', 'Patient Data', 100, TRUE, NOW()),
('=== HISTORICAL MEDICAL CONCERNS ===', 'Marks the start of historical medical concerns section', 'Patient Data', 100, TRUE, NOW()),
('=== HEALTH TREND ANALYSIS ===', 'Marks the start of health trend analysis section', 'Patient Data', 100, TRUE, NOW()),
('=== RECENT CLINICAL NOTES ===', 'Marks the start of recent clinical notes section', 'Patient Data', 100, TRUE, NOW()),
('=== RECENT CHAT HISTORY ===', 'Marks the start of recent chat history section', 'Patient Data', 100, TRUE, NOW()),
('=== RECENT EMERGENCY INCIDENTS ===', 'Marks the start of recent emergency incidents section', 'Emergency', 100, TRUE, NOW()),
('=== USER QUESTION ===', 'Marks the start of user question section', 'Instructions', 100, TRUE, NOW()),
('=== PROGRESSION ANALYSIS ===', 'Marks the start of progression analysis section', 'Patient Data', 100, TRUE, NOW()),
('=== INSTRUCTIONS FOR AI HEALTH CHECK ANALYSIS ===', 'Marks the start of AI health check instructions', 'Instructions', 100, TRUE, NOW()),

-- Secondary section markers (medium priority)
('Recent Patient Activity:', 'Alternative marker for recent patient activity', 'Patient Data', 50, TRUE, NOW()),
('Current Test Results', 'Alternative marker for current test results', 'Patient Data', 50, TRUE, NOW()),
('Latest Update:', 'Alternative marker for latest update', 'Patient Data', 50, TRUE, NOW()),
('Doctor asks:', 'Marks doctor questions in chat', 'Patient Data', 50, TRUE, NOW()),
('Patient asks:', 'Marks patient questions in chat', 'Patient Data', 50, TRUE, NOW()),

-- Medical resource markers
('**Medical Resource Information', 'Marks medical resource information section', 'Resources', 50, TRUE, NOW()),
('**Medical Facilities Search', 'Marks medical facilities search section', 'Resources', 50, TRUE, NOW()),

-- Emergency and data detection markers (lower priority, used for pattern matching)
('Fall', 'Detects fall incidents in emergency data', 'Emergency', 30, TRUE, NOW()),
('Session:', 'Marks session information', 'Patient Data', 30, TRUE, NOW()),
('Summary:', 'Marks summary sections', 'Patient Data', 30, TRUE, NOW()),
('Clinical Notes', 'Alternative marker for clinical notes', 'Patient Data', 30, TRUE, NOW()),
('Journal Entries', 'Alternative marker for journal entries', 'Patient Data', 30, TRUE, NOW()),
('Chat History', 'Alternative marker for chat history', 'Patient Data', 30, TRUE, NOW()),

-- Mood and activity patterns
('MOOD PATTERNS (Last 30 days):', 'Marks mood patterns section', 'Patient Data', 40, TRUE, NOW()),
('RECENT JOURNAL ENTRIES (Last 14 days):', 'Alternative marker for recent journal entries', 'Patient Data', 40, TRUE, NOW()),

-- AI Health Check marker
('AI Health Check for Patient', 'Default prompt for AI health check', 'Instructions', 20, TRUE, NOW())

ON DUPLICATE KEY UPDATE
    Description = VALUES(Description),
    Category = VALUES(Category),
    Priority = VALUES(Priority),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();

-- 4) Verify table exists
SELECT 'Table Verification:' AS Status;
SELECT 'SectionMarkers' AS TableName,
       COUNT(*) AS TableExists
FROM information_schema.tables
WHERE table_schema = @db AND table_name = 'SectionMarkers';

-- 5) Show table structure
SELECT 'SectionMarkers Structure:' AS Info;
DESCRIBE `SectionMarkers`;

-- 6) Show seeded data
SELECT 'Seeded Section Markers:' AS Info;
SELECT `Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`
FROM `SectionMarkers`
ORDER BY `Priority` DESC, `Category`, `Marker`;

-- Restore notes
SET sql_notes = @OLD_SQL_NOTES;
