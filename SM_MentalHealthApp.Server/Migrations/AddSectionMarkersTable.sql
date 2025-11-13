-- Migration SQL for AddSectionMarkersTable
-- This creates the SectionMarkers table for database-driven section marker detection

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

-- Create indexes for performance (MySQL version independent)
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

