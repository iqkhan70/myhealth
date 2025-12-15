ALTER TABLE Users
  DROP FOREIGN KEY FK_Users_ResidenceState,
  DROP FOREIGN KEY FK_Users_AccidentState,
  DROP FOREIGN KEY FK_Users_AccidentParticipantRole,
  DROP FOREIGN KEY FK_Users_VehicleDisposition,
  DROP FOREIGN KEY FK_Users_TransportToCareMethod,
  DROP FOREIGN KEY FK_Users_MedicalAttentionType;


SELECT CONSTRAINT_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Users'
  AND REFERENCED_TABLE_NAME IS NOT NULL
  AND CONSTRAINT_NAME != 'FK_Users_Roles_RoleId'
  ;

--…and drop by the returned names. IMPORTANT.......

DROP INDEX IX_Users_ResidenceStateCode ON Users;
DROP INDEX IX_Users_AccidentStateCode ON Users;
DROP INDEX IX_Users_AccidentParticipantRoleId ON Users;
DROP INDEX IX_Users_VehicleDispositionId ON Users;
DROP INDEX IX_Users_TransportToCareMethodId ON Users;
DROP INDEX IX_Users_MedicalAttentionTypeId ON Users;


ALTER TABLE Users
  DROP COLUMN ResidenceStateCode,
  DROP COLUMN AccidentStateCode,
  DROP COLUMN AccidentParticipantRoleId,
  DROP COLUMN VehicleDispositionId,
  DROP COLUMN TransportToCareMethodId,
  DROP COLUMN MedicalAttentionTypeId,
  DROP COLUMN PoliceInvolvement,
  DROP COLUMN LostConsciousness,
  DROP COLUMN NeuroSymptoms,
  DROP COLUMN MusculoskeletalSymptoms,
  DROP COLUMN PsychologicalSymptoms,
  DROP COLUMN SymptomsNotes,
  DROP COLUMN InsuranceContacted,
  DROP COLUMN RepresentedByAttorney;


DROP TABLE IF EXISTS MedicalAttentionType;
DROP TABLE IF EXISTS TransportToCareMethod;
DROP TABLE IF EXISTS VehicleDisposition;
DROP TABLE IF EXISTS AccidentParticipantRole;
DROP TABLE IF EXISTS States;
