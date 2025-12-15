-- ============================
-- SAFE ROLLBACK (MySQL 5.7+)
-- ============================

SET @db := DATABASE();

-- ----------------------------
-- 1) Drop FK(s) related to SymptomOngoingStatus lookup (if they exist)
--    (handles both custom FK name and auto-generated FK name)
-- ----------------------------
SELECT kcu.CONSTRAINT_NAME INTO @fk_symptom
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
WHERE kcu.TABLE_SCHEMA = @db
  AND kcu.TABLE_NAME = 'Users'
  AND kcu.COLUMN_NAME = 'SymptomOngoingStatusId'
  AND kcu.REFERENCED_TABLE_NAME = 'SymptomOngoingStatus'
LIMIT 1;

SET @sql := IF(@fk_symptom IS NULL,
  'SELECT ''No SymptomOngoingStatus FK found'';',
  CONCAT('ALTER TABLE `Users` DROP FOREIGN KEY `', @fk_symptom, '`;')
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ----------------------------
-- 2) Drop index on SymptomOngoingStatusId (if it exists)
-- ----------------------------
SELECT s.INDEX_NAME INTO @idx_symptom
FROM INFORMATION_SCHEMA.STATISTICS s
WHERE s.TABLE_SCHEMA = @db
  AND s.TABLE_NAME = 'Users'
  AND s.INDEX_NAME = 'IX_Users_SymptomOngoingStatusId'
LIMIT 1;

SET @sql := IF(@idx_symptom IS NULL,
  'SELECT ''No IX_Users_SymptomOngoingStatusId index found'';',
  CONCAT('DROP INDEX `', @idx_symptom, '` ON `Users`;')
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ----------------------------
-- 3) Drop columns added for requirements (only those that exist)
--    Includes BOTH SymptomsOngoingStatus (varchar) and SymptomOngoingStatusId (FK version)
-- ----------------------------
SELECT GROUP_CONCAT(CONCAT('DROP COLUMN `', c.col, '`') SEPARATOR ', ')
INTO @drop_cols
FROM (
  SELECT 'SymptomsOngoingStatus' col UNION ALL
  SELECT 'SymptomOngoingStatusId' UNION ALL
  SELECT 'SymptomsHeadaches' UNION ALL
  SELECT 'SymptomsDizziness' UNION ALL
  SELECT 'SymptomsNeckPain' UNION ALL
  SELECT 'SymptomsBackPain' UNION ALL
  SELECT 'SymptomsJointPain' UNION ALL
  SELECT 'SymptomsNumbnessTingling' UNION ALL

  SELECT 'WentToEmergencyRoom' UNION ALL
  SELECT 'ERHospitalName' UNION ALL
  SELECT 'ERVisitDate' UNION ALL
  SELECT 'TreatingInjurySpecialist' UNION ALL
  SELECT 'InjurySpecialistDetails' UNION ALL

  SELECT 'InsuranceAdjusterContacted' UNION ALL
  SELECT 'ProvidedRecordedStatement' UNION ALL
  SELECT 'ReceivedSettlementOffer' UNION ALL
  SELECT 'SettlementOfferAmount' UNION ALL
  SELECT 'ClaimInsuranceCompany' UNION ALL

  SELECT 'SignedDocumentsRelatedToAccident' UNION ALL
  SELECT 'SignedDocumentsNotes' UNION ALL
  SELECT 'AttorneyName' UNION ALL
  SELECT 'AttorneyFirm' UNION ALL

  SELECT 'VehicleCurrentLocation' UNION ALL
  SELECT 'InsuranceEstimateCompleted' UNION ALL
  SELECT 'EstimatedRepairAmount' UNION ALL
  SELECT 'VehicleTotalLoss' UNION ALL

  SELECT 'MissedWork' UNION ALL
  SELECT 'MissedWorkDays' UNION ALL
  SELECT 'WorkingWithRestrictions' UNION ALL
  SELECT 'WorkRestrictionDetails' UNION ALL
  SELECT 'DailyActivitiesAffected' UNION ALL
  SELECT 'DailyActivitiesNotes'
) c
JOIN INFORMATION_SCHEMA.COLUMNS ic
  ON ic.TABLE_SCHEMA = @db
 AND ic.TABLE_NAME = 'Users'
 AND ic.COLUMN_NAME = c.col;

SET @sql := IF(@drop_cols IS NULL OR @drop_cols = '',
  'SELECT ''No matching columns to drop'';',
  CONCAT('ALTER TABLE `Users` ', @drop_cols, ';')
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ----------------------------
-- 4) Drop lookup table SymptomOngoingStatus (if it exists)
-- ----------------------------
SELECT t.TABLE_NAME INTO @tbl_symptom
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_SCHEMA = @db
  AND t.TABLE_NAME = 'SymptomOngoingStatus'
LIMIT 1;

SET @sql := IF(@tbl_symptom IS NULL,
  'SELECT ''No SymptomOngoingStatus table found'';',
  'DROP TABLE `SymptomOngoingStatus`;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Done
SELECT 'SAFE ROLLBACK COMPLETE' AS Status;


-- =========UserRequests Table=========
-- ============================
-- SAFE ROLLBACK (MySQL 5.7+)
-- ============================

SET @db := DATABASE();

-- ----------------------------
-- 1) Drop FK(s) related to SymptomOngoingStatus lookup (if they exist)
--    (handles both custom FK name and auto-generated FK name)
-- ----------------------------
SELECT kcu.CONSTRAINT_NAME INTO @fk_symptom
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
WHERE kcu.TABLE_SCHEMA = @db
  AND kcu.TABLE_NAME = 'UserRequests'
  AND kcu.COLUMN_NAME = 'SymptomOngoingStatusId'
  AND kcu.REFERENCED_TABLE_NAME = 'SymptomOngoingStatus'
LIMIT 1;

SET @sql := IF(@fk_symptom IS NULL,
  'SELECT ''No SymptomOngoingStatus FK found'';',
  CONCAT('ALTER TABLE `UserRequests` DROP FOREIGN KEY `', @fk_symptom, '`;')
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ----------------------------
-- 2) Drop index on SymptomOngoingStatusId (if it exists)
-- ----------------------------
SELECT s.INDEX_NAME INTO @idx_symptom
FROM INFORMATION_SCHEMA.STATISTICS s
WHERE s.TABLE_SCHEMA = @db
  AND s.TABLE_NAME = 'UserRequests'
  AND s.INDEX_NAME = 'IX_UserRequests_SymptomOngoingStatusId'
LIMIT 1;

SET @sql := IF(@idx_symptom IS NULL,
  'SELECT ''No IX_UserRequests_SymptomOngoingStatusId index found'';',
  CONCAT('DROP INDEX `', @idx_symptom, '` ON `UserRequests`;')
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ----------------------------
-- 3) Drop columns added for requirements (only those that exist)
--    Includes BOTH SymptomsOngoingStatus (varchar) and SymptomOngoingStatusId (FK version)
-- ----------------------------
SELECT GROUP_CONCAT(CONCAT('DROP COLUMN `', c.col, '`') SEPARATOR ', ')
INTO @drop_cols
FROM (
  SELECT 'SymptomsOngoingStatus' col UNION ALL
  SELECT 'SymptomOngoingStatusId' UNION ALL
  SELECT 'SymptomsHeadaches' UNION ALL
  SELECT 'SymptomsDizziness' UNION ALL
  SELECT 'SymptomsNeckPain' UNION ALL
  SELECT 'SymptomsBackPain' UNION ALL
  SELECT 'SymptomsJointPain' UNION ALL
  SELECT 'SymptomsNumbnessTingling' UNION ALL

  SELECT 'WentToEmergencyRoom' UNION ALL
  SELECT 'ERHospitalName' UNION ALL
  SELECT 'ERVisitDate' UNION ALL
  SELECT 'TreatingInjurySpecialist' UNION ALL
  SELECT 'InjurySpecialistDetails' UNION ALL

  SELECT 'InsuranceAdjusterContacted' UNION ALL
  SELECT 'ProvidedRecordedStatement' UNION ALL
  SELECT 'ReceivedSettlementOffer' UNION ALL
  SELECT 'SettlementOfferAmount' UNION ALL
  SELECT 'ClaimInsuranceCompany' UNION ALL

  SELECT 'SignedDocumentsRelatedToAccident' UNION ALL
  SELECT 'SignedDocumentsNotes' UNION ALL
  SELECT 'AttorneyName' UNION ALL
  SELECT 'AttorneyFirm' UNION ALL

  SELECT 'VehicleCurrentLocation' UNION ALL
  SELECT 'InsuranceEstimateCompleted' UNION ALL
  SELECT 'EstimatedRepairAmount' UNION ALL
  SELECT 'VehicleTotalLoss' UNION ALL

  SELECT 'MissedWork' UNION ALL
  SELECT 'MissedWorkDays' UNION ALL
  SELECT 'WorkingWithRestrictions' UNION ALL
  SELECT 'WorkRestrictionDetails' UNION ALL
  SELECT 'DailyActivitiesAffected' UNION ALL
  SELECT 'DailyActivitiesNotes'
) c
JOIN INFORMATION_SCHEMA.COLUMNS ic
  ON ic.TABLE_SCHEMA = @db
 AND ic.TABLE_NAME = 'UserRequests'
 AND ic.COLUMN_NAME = c.col;

SET @sql := IF(@drop_cols IS NULL OR @drop_cols = '',
  'SELECT ''No matching columns to drop'';',
  CONCAT('ALTER TABLE `UserRequests` ', @drop_cols, ';')
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ----------------------------
-- 4) Drop lookup table SymptomOngoingStatus (if it exists)
-- ----------------------------
SELECT t.TABLE_NAME INTO @tbl_symptom
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_SCHEMA = @db
  AND t.TABLE_NAME = 'SymptomOngoingStatus'
LIMIT 1;

SET @sql := IF(@tbl_symptom IS NULL,
  'SELECT ''No SymptomOngoingStatus table found'';',
  'DROP TABLE `SymptomOngoingStatus`;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Done
SELECT 'SAFE ROLLBACK COMPLETE' AS Status;

