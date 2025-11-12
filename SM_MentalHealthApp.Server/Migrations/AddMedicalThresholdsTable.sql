-- Migration SQL for AddMedicalThresholdsTable
-- Generated for Medical Threshold system

CREATE TABLE IF NOT EXISTS `MedicalThresholds` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ParameterName` varchar(100) NOT NULL,
    `Unit` varchar(50) NULL,
    `SeverityLevel` varchar(50) NULL,
    `MinValue` double NULL,
    `MaxValue` double NULL,
    `ComparisonOperator` varchar(20) NULL,
    `ThresholdValue` double NULL,
    `SecondaryParameterName` varchar(100) NULL,
    `SecondaryThresholdValue` double NULL,
    `SecondaryComparisonOperator` varchar(20) NULL,
    `Description` varchar(500) NULL,
    `Priority` int NOT NULL DEFAULT 0,
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    `UpdatedAt` datetime(6) NULL,
    PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

-- Create indexes for performance (MySQL version independent)
SET @db = DATABASE();

-- IX_MedicalThresholds_ParameterName
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'MedicalThresholds'
    AND index_name   = 'IX_MedicalThresholds_ParameterName'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_MedicalThresholds_ParameterName` ON `MedicalThresholds` (`ParameterName`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_MedicalThresholds_IsActive
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'MedicalThresholds'
    AND index_name   = 'IX_MedicalThresholds_IsActive'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_MedicalThresholds_IsActive` ON `MedicalThresholds` (`IsActive`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_MedicalThresholds_Priority
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'MedicalThresholds'
    AND index_name   = 'IX_MedicalThresholds_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_MedicalThresholds_Priority` ON `MedicalThresholds` (`Priority`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_MedicalThresholds_SeverityLevel
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'MedicalThresholds'
    AND index_name   = 'IX_MedicalThresholds_SeverityLevel'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_MedicalThresholds_SeverityLevel` ON `MedicalThresholds` (`SeverityLevel`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_MedicalThresholds_ParameterName_IsActive
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'MedicalThresholds'
    AND index_name   = 'IX_MedicalThresholds_ParameterName_IsActive'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_MedicalThresholds_ParameterName_IsActive` ON `MedicalThresholds` (`ParameterName`, `IsActive`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- IX_MedicalThresholds_IsActive_Priority
SET @idx_exists = (
  SELECT COUNT(*)
  FROM information_schema.statistics
  WHERE table_schema = @db
    AND table_name   = 'MedicalThresholds'
    AND index_name   = 'IX_MedicalThresholds_IsActive_Priority'
);
SET @sql = IF(@idx_exists = 0,
  'CREATE INDEX `IX_MedicalThresholds_IsActive_Priority` ON `MedicalThresholds` (`IsActive`, `Priority`);',
  'SELECT 1;'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

