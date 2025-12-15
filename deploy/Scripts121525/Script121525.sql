ALTER TABLE Users
  MODIFY COLUMN VehicleDetails TEXT NULL,
  MODIFY COLUMN AccidentDetails TEXT NULL,
  MODIFY COLUMN DoctorsInformation TEXT NULL,
  MODIFY COLUMN LawyersInformation TEXT NULL,
  MODIFY COLUMN AdditionalNotes TEXT NULL,
  MODIFY COLUMN SymptomsNotes TEXT NULL
  -- MODIFY COLUMN InjurySpecialistDetails TEXT NULL,
  -- MODIFY COLUMN SignedDocumentsNotes TEXT NULL,
  -- MODIFY COLUMN WorkRestrictionDetails TEXT NULL,
  -- MODIFY COLUMN DailyActivitiesNotes TEXT NULL
  ;
  
ALTER TABLE Users
  /* 1) Detailed Injury Assessment */
  ADD COLUMN SymptomsOngoingStatus VARCHAR(20) NULL
    COMMENT 'ONGOING | IMPROVING | RESOLVED | WORSENING | UNKNOWN' AFTER SymptomsNotes,
  ADD COLUMN SymptomsHeadaches TINYINT(1) NULL AFTER SymptomsOngoingStatus,
  ADD COLUMN SymptomsDizziness TINYINT(1) NULL AFTER SymptomsHeadaches,
  ADD COLUMN SymptomsNeckPain TINYINT(1) NULL AFTER SymptomsDizziness,
  ADD COLUMN SymptomsBackPain TINYINT(1) NULL AFTER SymptomsNeckPain,
  ADD COLUMN SymptomsJointPain TINYINT(1) NULL AFTER SymptomsBackPain,
  ADD COLUMN SymptomsNumbnessTingling TINYINT(1) NULL AFTER SymptomsJointPain,

  /* 2) Medical Treatment Details */
  ADD COLUMN WentToEmergencyRoom TINYINT(1) NULL AFTER MedicalAttentionTypeId,
  ADD COLUMN ERHospitalName VARCHAR(255) NULL AFTER WentToEmergencyRoom,
  ADD COLUMN ERVisitDate DATETIME NULL AFTER ERHospitalName,
  ADD COLUMN TreatingInjurySpecialist TINYINT(1) NULL
    COMMENT 'Treating with any injury specialist (ER/UrgentCare/Chiro/PrimaryCare/etc.)' AFTER ERVisitDate,
  ADD COLUMN InjurySpecialistDetails VARCHAR(1000) NULL AFTER TreatingInjurySpecialist,

  /* 3) Insurance & Claims Handling */
  ADD COLUMN InsuranceAdjusterContacted TINYINT(1) NULL AFTER InsuranceContacted,
  ADD COLUMN ProvidedRecordedStatement TINYINT(1) NULL AFTER InsuranceAdjusterContacted,
  ADD COLUMN ReceivedSettlementOffer TINYINT(1) NULL AFTER ProvidedRecordedStatement,
  ADD COLUMN SettlementOfferAmount DECIMAL(12,2) NULL AFTER ReceivedSettlementOffer,
  ADD COLUMN ClaimInsuranceCompany VARCHAR(255) NULL AFTER SettlementOfferAmount,

  /* 4) Legal Representation */
  ADD COLUMN SignedDocumentsRelatedToAccident TINYINT(1) NULL AFTER RepresentedByAttorney,
  ADD COLUMN SignedDocumentsNotes VARCHAR(1000) NULL AFTER SignedDocumentsRelatedToAccident,
  ADD COLUMN AttorneyName VARCHAR(255) NULL AFTER SignedDocumentsNotes,
  ADD COLUMN AttorneyFirm VARCHAR(255) NULL AFTER AttorneyName,

  /* 5) Vehicle & Property Damage */
  ADD COLUMN VehicleCurrentLocation VARCHAR(500) NULL AFTER VehicleDispositionId,
  ADD COLUMN InsuranceEstimateCompleted TINYINT(1) NULL AFTER VehicleCurrentLocation,
  ADD COLUMN EstimatedRepairAmount DECIMAL(12,2) NULL AFTER InsuranceEstimateCompleted,
  ADD COLUMN VehicleTotalLoss TINYINT(1) NULL AFTER EstimatedRepairAmount,

  /* 6) Work & Life Impact */
  ADD COLUMN MissedWork TINYINT(1) NULL AFTER VehicleTotalLoss,
  ADD COLUMN MissedWorkDays INT NULL AFTER MissedWork,
  ADD COLUMN WorkingWithRestrictions TINYINT(1) NULL AFTER MissedWorkDays,
  ADD COLUMN WorkRestrictionDetails VARCHAR(1000) NULL AFTER WorkingWithRestrictions,
  ADD COLUMN DailyActivitiesAffected TINYINT(1) NULL AFTER WorkRestrictionDetails,
  ADD COLUMN DailyActivitiesNotes VARCHAR(1000) NULL AFTER DailyActivitiesAffected;


ALTER TABLE Users
  MODIFY COLUMN VehicleDetails TEXT NULL,
  MODIFY COLUMN AccidentDetails TEXT NULL,
  MODIFY COLUMN DoctorsInformation TEXT NULL,
  MODIFY COLUMN LawyersInformation TEXT NULL,
  MODIFY COLUMN AdditionalNotes TEXT NULL,
  MODIFY COLUMN SymptomsNotes TEXT NULL,
   MODIFY COLUMN InjurySpecialistDetails TEXT NULL,
   MODIFY COLUMN SignedDocumentsNotes TEXT NULL,
   MODIFY COLUMN WorkRestrictionDetails TEXT NULL,
   MODIFY COLUMN DailyActivitiesNotes TEXT NULL
  ;


CREATE TABLE IF NOT EXISTS SymptomOngoingStatus (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Code VARCHAR(30) NOT NULL UNIQUE,
  Label VARCHAR(80) NOT NULL
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO SymptomOngoingStatus (Code, Label) VALUES
('ONGOING', 'Ongoing'),
('IMPROVING', 'Improving'),
('RESOLVED', 'Resolved'),
('WORSENING', 'Worsening'),
('UNKNOWN', 'Unknown')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

ALTER TABLE Users
  ADD COLUMN SymptomOngoingStatusId INT NULL AFTER SymptomsNotes;

CREATE INDEX IX_Users_SymptomOngoingStatusId ON Users(SymptomOngoingStatusId);

ALTER TABLE Users
  ADD CONSTRAINT FK_Users_SymptomOngoingStatus
    FOREIGN KEY (SymptomOngoingStatusId) REFERENCES SymptomOngoingStatus(Id);


-- =========UserRequests Table=========
ALTER TABLE UserRequests
  MODIFY COLUMN VehicleDetails TEXT NULL,
  MODIFY COLUMN AccidentDetails TEXT NULL,
  MODIFY COLUMN DoctorsInformation TEXT NULL,
  MODIFY COLUMN LawyersInformation TEXT NULL,
  MODIFY COLUMN AdditionalNotes TEXT NULL,
  MODIFY COLUMN SymptomsNotes TEXT NULL
  -- MODIFY COLUMN InjurySpecialistDetails TEXT NULL,
  -- MODIFY COLUMN SignedDocumentsNotes TEXT NULL,
  -- MODIFY COLUMN WorkRestrictionDetails TEXT NULL,
  -- MODIFY COLUMN DailyActivitiesNotes TEXT NULL
  ;
  
ALTER TABLE UserRequests
  /* 1) Detailed Injury Assessment */
  ADD COLUMN SymptomsOngoingStatus VARCHAR(20) NULL
    COMMENT 'ONGOING | IMPROVING | RESOLVED | WORSENING | UNKNOWN' AFTER SymptomsNotes,
  ADD COLUMN SymptomsHeadaches TINYINT(1) NULL AFTER SymptomsOngoingStatus,
  ADD COLUMN SymptomsDizziness TINYINT(1) NULL AFTER SymptomsHeadaches,
  ADD COLUMN SymptomsNeckPain TINYINT(1) NULL AFTER SymptomsDizziness,
  ADD COLUMN SymptomsBackPain TINYINT(1) NULL AFTER SymptomsNeckPain,
  ADD COLUMN SymptomsJointPain TINYINT(1) NULL AFTER SymptomsBackPain,
  ADD COLUMN SymptomsNumbnessTingling TINYINT(1) NULL AFTER SymptomsJointPain,

  /* 2) Medical Treatment Details */
  ADD COLUMN WentToEmergencyRoom TINYINT(1) NULL AFTER MedicalAttentionTypeId,
  ADD COLUMN ERHospitalName VARCHAR(255) NULL AFTER WentToEmergencyRoom,
  ADD COLUMN ERVisitDate DATETIME NULL AFTER ERHospitalName,
  ADD COLUMN TreatingInjurySpecialist TINYINT(1) NULL
    COMMENT 'Treating with any injury specialist (ER/UrgentCare/Chiro/PrimaryCare/etc.)' AFTER ERVisitDate,
  ADD COLUMN InjurySpecialistDetails VARCHAR(1000) NULL AFTER TreatingInjurySpecialist,

  /* 3) Insurance & Claims Handling */
  ADD COLUMN InsuranceAdjusterContacted TINYINT(1) NULL AFTER InsuranceContacted,
  ADD COLUMN ProvidedRecordedStatement TINYINT(1) NULL AFTER InsuranceAdjusterContacted,
  ADD COLUMN ReceivedSettlementOffer TINYINT(1) NULL AFTER ProvidedRecordedStatement,
  ADD COLUMN SettlementOfferAmount DECIMAL(12,2) NULL AFTER ReceivedSettlementOffer,
  ADD COLUMN ClaimInsuranceCompany VARCHAR(255) NULL AFTER SettlementOfferAmount,

  /* 4) Legal Representation */
  ADD COLUMN SignedDocumentsRelatedToAccident TINYINT(1) NULL AFTER RepresentedByAttorney,
  ADD COLUMN SignedDocumentsNotes VARCHAR(1000) NULL AFTER SignedDocumentsRelatedToAccident,
  ADD COLUMN AttorneyName VARCHAR(255) NULL AFTER SignedDocumentsNotes,
  ADD COLUMN AttorneyFirm VARCHAR(255) NULL AFTER AttorneyName,

  /* 5) Vehicle & Property Damage */
  ADD COLUMN VehicleCurrentLocation VARCHAR(500) NULL AFTER VehicleDispositionId,
  ADD COLUMN InsuranceEstimateCompleted TINYINT(1) NULL AFTER VehicleCurrentLocation,
  ADD COLUMN EstimatedRepairAmount DECIMAL(12,2) NULL AFTER InsuranceEstimateCompleted,
  ADD COLUMN VehicleTotalLoss TINYINT(1) NULL AFTER EstimatedRepairAmount,

  /* 6) Work & Life Impact */
  ADD COLUMN MissedWork TINYINT(1) NULL AFTER VehicleTotalLoss,
  ADD COLUMN MissedWorkDays INT NULL AFTER MissedWork,
  ADD COLUMN WorkingWithRestrictions TINYINT(1) NULL AFTER MissedWorkDays,
  ADD COLUMN WorkRestrictionDetails VARCHAR(1000) NULL AFTER WorkingWithRestrictions,
  ADD COLUMN DailyActivitiesAffected TINYINT(1) NULL AFTER WorkRestrictionDetails,
  ADD COLUMN DailyActivitiesNotes VARCHAR(1000) NULL AFTER DailyActivitiesAffected;


ALTER TABLE UserRequests
  MODIFY COLUMN VehicleDetails TEXT NULL,
  MODIFY COLUMN AccidentDetails TEXT NULL,
  MODIFY COLUMN DoctorsInformation TEXT NULL,
  MODIFY COLUMN LawyersInformation TEXT NULL,
  MODIFY COLUMN AdditionalNotes TEXT NULL,
  MODIFY COLUMN SymptomsNotes TEXT NULL,
   MODIFY COLUMN InjurySpecialistDetails TEXT NULL,
   MODIFY COLUMN SignedDocumentsNotes TEXT NULL,
   MODIFY COLUMN WorkRestrictionDetails TEXT NULL,
   MODIFY COLUMN DailyActivitiesNotes TEXT NULL
  ;


CREATE TABLE IF NOT EXISTS SymptomOngoingStatus (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Code VARCHAR(30) NOT NULL UNIQUE,
  Label VARCHAR(80) NOT NULL
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT INTO SymptomOngoingStatus (Code, Label) VALUES
('ONGOING', 'Ongoing'),
('IMPROVING', 'Improving'),
('RESOLVED', 'Resolved'),
('WORSENING', 'Worsening'),
('UNKNOWN', 'Unknown')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

ALTER TABLE UserRequests
  ADD COLUMN SymptomOngoingStatusId INT NULL AFTER SymptomsNotes;

CREATE INDEX IX_UserRequests_SymptomOngoingStatusId ON UserRequests(SymptomOngoingStatusId);

ALTER TABLE UserRequests
  ADD CONSTRAINT FK_UserRequests_SymptomOngoingStatus
    FOREIGN KEY (SymptomOngoingStatusId) REFERENCES SymptomOngoingStatus(Id);


