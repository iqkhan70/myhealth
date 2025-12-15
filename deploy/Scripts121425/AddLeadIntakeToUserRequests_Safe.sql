-- Add Lead Intake columns to UserRequests table (Safe version - handles existing columns/indexes)
-- This script adds the same Lead Intake fields to UserRequests that were added to Users table

-- Add columns (will fail if columns already exist - that's okay)
ALTER TABLE UserRequests
  -- State fields
  ADD COLUMN ResidenceStateCode CHAR(2) NULL AFTER AdditionalNotes,
  ADD COLUMN AccidentStateCode CHAR(2) NULL AFTER ResidenceStateCode,

  -- Dropdown-backed fields
  ADD COLUMN AccidentParticipantRoleId INT NULL AFTER AccidentStateCode,
  ADD COLUMN VehicleDispositionId INT NULL AFTER AccidentParticipantRoleId,
  ADD COLUMN TransportToCareMethodId INT NULL AFTER VehicleDispositionId,
  ADD COLUMN MedicalAttentionTypeId INT NULL AFTER TransportToCareMethodId,

  -- Yes/No fields (use TINYINT(1) for MySQL boolean style)
  ADD COLUMN PoliceInvolvement TINYINT(1) NULL AFTER MedicalAttentionTypeId,
  ADD COLUMN LostConsciousness TINYINT(1) NULL AFTER PoliceInvolvement,
  ADD COLUMN NeuroSymptoms TINYINT(1) NULL AFTER LostConsciousness,
  ADD COLUMN MusculoskeletalSymptoms TINYINT(1) NULL AFTER NeuroSymptoms,
  ADD COLUMN PsychologicalSymptoms TINYINT(1) NULL AFTER MusculoskeletalSymptoms,

  -- Notes for case manager (brief notation)
  ADD COLUMN SymptomsNotes VARCHAR(2000) NULL AFTER PsychologicalSymptoms,

  -- Insurance / attorney
  ADD COLUMN InsuranceContacted TINYINT(1) NULL AFTER SymptomsNotes,
  ADD COLUMN RepresentedByAttorney TINYINT(1) NULL AFTER InsuranceContacted;

-- Create indexes only if they don't exist
-- Note: MySQL doesn't support IF NOT EXISTS for CREATE INDEX, so we use a workaround
SET @index_exists = (SELECT COUNT(*) FROM information_schema.statistics 
                     WHERE table_schema = DATABASE() 
                     AND table_name = 'UserRequests' 
                     AND index_name = 'IX_UserRequests_ResidenceStateCode');
SET @sql = IF(@index_exists = 0, 
              'CREATE INDEX IX_UserRequests_ResidenceStateCode ON UserRequests(ResidenceStateCode)', 
              'SELECT "Index IX_UserRequests_ResidenceStateCode already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @index_exists = (SELECT COUNT(*) FROM information_schema.statistics 
                     WHERE table_schema = DATABASE() 
                     AND table_name = 'UserRequests' 
                     AND index_name = 'IX_UserRequests_AccidentStateCode');
SET @sql = IF(@index_exists = 0, 
              'CREATE INDEX IX_UserRequests_AccidentStateCode ON UserRequests(AccidentStateCode)', 
              'SELECT "Index IX_UserRequests_AccidentStateCode already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @index_exists = (SELECT COUNT(*) FROM information_schema.statistics 
                     WHERE table_schema = DATABASE() 
                     AND table_name = 'UserRequests' 
                     AND index_name = 'IX_UserRequests_AccidentParticipantRoleId');
SET @sql = IF(@index_exists = 0, 
              'CREATE INDEX IX_UserRequests_AccidentParticipantRoleId ON UserRequests(AccidentParticipantRoleId)', 
              'SELECT "Index IX_UserRequests_AccidentParticipantRoleId already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @index_exists = (SELECT COUNT(*) FROM information_schema.statistics 
                     WHERE table_schema = DATABASE() 
                     AND table_name = 'UserRequests' 
                     AND index_name = 'IX_UserRequests_VehicleDispositionId');
SET @sql = IF(@index_exists = 0, 
              'CREATE INDEX IX_UserRequests_VehicleDispositionId ON UserRequests(VehicleDispositionId)', 
              'SELECT "Index IX_UserRequests_VehicleDispositionId already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @index_exists = (SELECT COUNT(*) FROM information_schema.statistics 
                     WHERE table_schema = DATABASE() 
                     AND table_name = 'UserRequests' 
                     AND index_name = 'IX_UserRequests_TransportToCareMethodId');
SET @sql = IF(@index_exists = 0, 
              'CREATE INDEX IX_UserRequests_TransportToCareMethodId ON UserRequests(TransportToCareMethodId)', 
              'SELECT "Index IX_UserRequests_TransportToCareMethodId already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @index_exists = (SELECT COUNT(*) FROM information_schema.statistics 
                     WHERE table_schema = DATABASE() 
                     AND table_name = 'UserRequests' 
                     AND index_name = 'IX_UserRequests_MedicalAttentionTypeId');
SET @sql = IF(@index_exists = 0, 
              'CREATE INDEX IX_UserRequests_MedicalAttentionTypeId ON UserRequests(MedicalAttentionTypeId)', 
              'SELECT "Index IX_UserRequests_MedicalAttentionTypeId already exists"');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Foreign keys (will fail if they already exist - that's okay)
ALTER TABLE UserRequests
  ADD CONSTRAINT FK_UserRequests_ResidenceState
    FOREIGN KEY (ResidenceStateCode) REFERENCES States(Code)
    ON DELETE SET NULL
    ON UPDATE CASCADE,

  ADD CONSTRAINT FK_UserRequests_AccidentState
    FOREIGN KEY (AccidentStateCode) REFERENCES States(Code)
    ON DELETE SET NULL
    ON UPDATE CASCADE,

  ADD CONSTRAINT FK_UserRequests_AccidentParticipantRole
    FOREIGN KEY (AccidentParticipantRoleId) REFERENCES AccidentParticipantRole(Id)
    ON DELETE SET NULL
    ON UPDATE CASCADE,

  ADD CONSTRAINT FK_UserRequests_VehicleDisposition
    FOREIGN KEY (VehicleDispositionId) REFERENCES VehicleDisposition(Id)
    ON DELETE SET NULL
    ON UPDATE CASCADE,

  ADD CONSTRAINT FK_UserRequests_TransportToCareMethod
    FOREIGN KEY (TransportToCareMethodId) REFERENCES TransportToCareMethod(Id)
    ON DELETE SET NULL
    ON UPDATE CASCADE,

  ADD CONSTRAINT FK_UserRequests_MedicalAttentionType
    FOREIGN KEY (MedicalAttentionTypeId) REFERENCES MedicalAttentionType(Id)
    ON DELETE SET NULL
    ON UPDATE CASCADE;

