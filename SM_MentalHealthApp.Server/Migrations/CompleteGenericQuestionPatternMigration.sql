-- =====================================================
-- Complete Generic Question Pattern Migration Script (MySQL-safe)
-- =====================================================

-- Quiet some notes (optional)
SET @OLD_SQL_NOTES = @@sql_notes; 
SET sql_notes = 0;

-- 1) Create table (with useful keys/constraints)
CREATE TABLE IF NOT EXISTS `GenericQuestionPatterns` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Pattern` VARCHAR(500) NOT NULL,
  `Description` VARCHAR(500) NULL,
  `Priority` INT NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` DATETIME(6) NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_GenericQuestionPatterns_Pattern` (`Pattern`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Helper var
SET @db = DATABASE();

-- 2) Create missing indexes (compat-safe: no IF NOT EXISTS)

-- IX_GenericQuestionPatterns_IsActive
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'GenericQuestionPatterns'
    AND index_name   = 'IX_GenericQuestionPatterns_IsActive'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_GenericQuestionPatterns_IsActive` ON `GenericQuestionPatterns` (`IsActive`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_GenericQuestionPatterns_Priority
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'GenericQuestionPatterns'
    AND index_name   = 'IX_GenericQuestionPatterns_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_GenericQuestionPatterns_Priority` ON `GenericQuestionPatterns` (`Priority`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_GenericQuestionPatterns_IsActive_Priority
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'GenericQuestionPatterns'
    AND index_name   = 'IX_GenericQuestionPatterns_IsActive_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_GenericQuestionPatterns_IsActive_Priority` ON `GenericQuestionPatterns` (`IsActive`, `Priority`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- 3) Seed data (ON DUPLICATE works via UNIQUE on Pattern)
INSERT INTO `GenericQuestionPatterns` (`Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`)
VALUES
  ('what are normal',  'Matches questions asking about normal values',                                   10, TRUE, NOW()),
  ('what are the normal','Matches questions asking about normal values (with "the")',                    10, TRUE, NOW()),
  ('what is normal',   'Matches questions asking what is normal',                                        10, TRUE, NOW()),
  ('what are critical','Matches questions asking about critical values',                                 10, TRUE, NOW()),
  ('what are serious', 'Matches questions asking about serious values',                                  10, TRUE, NOW()),
  ('what is a normal', 'Matches questions asking what is a normal value',                                10, TRUE, NOW()),
  ('what are typical', 'Matches questions asking about typical values',                                  10, TRUE, NOW()),
  ('what is typical',  'Matches questions asking what is typical',                                       10, TRUE, NOW()),
  ('normal values of', 'Matches questions about normal values of something',                             10, TRUE, NOW()),
  ('normal range of',  'Matches questions about normal range of something',                              10, TRUE, NOW()),
  ('normal levels of', 'Matches questions about normal levels of something',                             10, TRUE, NOW()),
  ('what does',        'Matches questions asking what something does',                                   5,  TRUE, NOW()),
  ('how does',         'Matches questions asking how something works',                                   5,  TRUE, NOW()),
  ('explain',          'Matches questions asking to explain something',                                  5,  TRUE, NOW()),
  ('tell me about',    'Matches questions asking to tell about something',                               5,  TRUE, NOW()),
  ('what is',          'Matches questions asking what something is',                                     5,  TRUE, NOW()),
  ('what are',         'Matches questions asking what things are',                                       5,  TRUE, NOW())
ON DUPLICATE KEY UPDATE
  `Description` = VALUES(`Description`),
  `Priority`    = VALUES(`Priority`),
  `IsActive`    = VALUES(`IsActive`),
  `UpdatedAt`   = NOW();

-- 4) Verify table exists
SELECT 'Table Verification:' AS Status;
SELECT 'GenericQuestionPatterns' AS TableName,
       COUNT(*) AS TableExists
FROM information_schema.tables
WHERE table_schema = @db AND table_name = 'GenericQuestionPatterns';

-- 5) Show table structure
SELECT 'GenericQuestionPatterns Structure:' AS Info;
DESCRIBE `GenericQuestionPatterns`;

-- 6) Show seeded data
SELECT 'Seeded Patterns:' AS Info;
SELECT `Id`, `Pattern`, `Description`, `Priority`, `IsActive`
FROM `GenericQuestionPatterns`
ORDER BY `Priority` DESC, `Pattern`;

-- Restore notes
SET sql_notes = @OLD_SQL_NOTES;
