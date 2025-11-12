-- Migration SQL for AddGenericQuestionPatternTable
-- This creates the GenericQuestionPatterns table for database-driven generic question pattern detection

CREATE TABLE IF NOT EXISTS `GenericQuestionPatterns` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Pattern` varchar(500) NOT NULL,
    `Description` varchar(500) NULL,
    `Priority` int NOT NULL DEFAULT 0,
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    `UpdatedAt` datetime(6) NULL,
    PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

-- Create indexes for performance
-- For GenericQuestionPatterns

-- Index: IX_GenericQuestionPatterns_IsActive
SET @db = DATABASE();
SET @idx_exists = (
  SELECT COUNT(*) 
  FROM information_schema.statistics 
  WHERE table_schema = @db 
    AND table_name = 'GenericQuestionPatterns' 
    AND index_name = 'IX_GenericQuestionPatterns_IsActive'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_GenericQuestionPatterns_IsActive` ON `GenericQuestionPatterns` (`IsActive`);',
  'SELECT 1;'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;


-- Index: IX_GenericQuestionPatterns_Priority
SET @idx_exists = (
  SELECT COUNT(*) 
  FROM information_schema.statistics 
  WHERE table_schema = @db 
    AND table_name = 'GenericQuestionPatterns' 
    AND index_name = 'IX_GenericQuestionPatterns_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_GenericQuestionPatterns_Priority` ON `GenericQuestionPatterns` (`Priority`);',
  'SELECT 1;'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;


-- Index: IX_GenericQuestionPatterns_IsActive_Priority
SET @idx_exists = (
  SELECT COUNT(*) 
  FROM information_schema.statistics 
  WHERE table_schema = @db 
    AND table_name = 'GenericQuestionPatterns' 
    AND index_name = 'IX_GenericQuestionPatterns_IsActive_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_GenericQuestionPatterns_IsActive_Priority` ON `GenericQuestionPatterns` (`IsActive`, `Priority`);',
  'SELECT 1;'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
