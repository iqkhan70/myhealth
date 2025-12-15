ALTER TABLE Users
  /* 6) Work & Life Impact */
  DROP COLUMN DailyActivitiesNotes,
  DROP COLUMN DailyActivitiesAffected,
  DROP COLUMN WorkRestrictionDetails,
  DROP COLUMN WorkingWithRestrictions,
  DROP COLUMN MissedWorkDays,
  DROP COLUMN MissedWork,

  /* 5) Vehicle & Property Damage */
  DROP COLUMN VehicleTotalLoss,
  DROP COLUMN EstimatedRepairAmount,
  DROP COLUMN InsuranceEstimateCompleted,
  DROP COLUMN VehicleCurrentLocation,

  /* 4) Legal Representation */
  DROP COLUMN AttorneyFirm,
  DROP COLUMN AttorneyName,
  DROP COLUMN SignedDocumentsNotes,
  DROP COLUMN SignedDocumentsRelatedToAccident,

  /* 3) Insurance & Claims Handling */
  DROP COLUMN ClaimInsuranceCompany,
  DROP COLUMN SettlementOfferAmount,
  DROP COLUMN ReceivedSettlementOffer,
  DROP COLUMN ProvidedRecordedStatement,
  DROP COLUMN InsuranceAdjusterContacted,

  /* 2) Medical Treatment Details */
  DROP COLUMN InjurySpecialistDetails,
  DROP COLUMN TreatingInjurySpecialist,
  DROP COLUMN ERVisitDate,
  DROP COLUMN ERHospitalName,
  DROP COLUMN WentToEmergencyRoom,

  /* 1) Detailed Injury Assessment */
  DROP COLUMN SymptomsNumbnessTingling,
  DROP COLUMN SymptomsJointPain,
  DROP COLUMN SymptomsBackPain,
  DROP COLUMN SymptomsNeckPain,
  DROP COLUMN SymptomsDizziness,
  DROP COLUMN SymptomsHeadaches,
  DROP COLUMN SymptomsOngoingStatus;



-- 1) Drop FK + index
ALTER TABLE Users
  DROP FOREIGN KEY FK_Users_SymptomOngoingStatus;

DROP INDEX IX_Users_SymptomOngoingStatusId ON Users;

-- 2) Drop the FK column
ALTER TABLE Users
  DROP COLUMN SymptomOngoingStatusId;

-- 3) Drop the lookup table
DROP TABLE IF EXISTS SymptomOngoingStatus;



-- =========UserRequests Table=========
ALTER TABLE UserRequests
  /* 6) Work & Life Impact */
  DROP COLUMN DailyActivitiesNotes,
  DROP COLUMN DailyActivitiesAffected,
  DROP COLUMN WorkRestrictionDetails,
  DROP COLUMN WorkingWithRestrictions,
  DROP COLUMN MissedWorkDays,
  DROP COLUMN MissedWork,

  /* 5) Vehicle & Property Damage */
  DROP COLUMN VehicleTotalLoss,
  DROP COLUMN EstimatedRepairAmount,
  DROP COLUMN InsuranceEstimateCompleted,
  DROP COLUMN VehicleCurrentLocation,

  /* 4) Legal Representation */
  DROP COLUMN AttorneyFirm,
  DROP COLUMN AttorneyName,
  DROP COLUMN SignedDocumentsNotes,
  DROP COLUMN SignedDocumentsRelatedToAccident,

  /* 3) Insurance & Claims Handling */
  DROP COLUMN ClaimInsuranceCompany,
  DROP COLUMN SettlementOfferAmount,
  DROP COLUMN ReceivedSettlementOffer,
  DROP COLUMN ProvidedRecordedStatement,
  DROP COLUMN InsuranceAdjusterContacted,

  /* 2) Medical Treatment Details */
  DROP COLUMN InjurySpecialistDetails,
  DROP COLUMN TreatingInjurySpecialist,
  DROP COLUMN ERVisitDate,
  DROP COLUMN ERHospitalName,
  DROP COLUMN WentToEmergencyRoom,

  /* 1) Detailed Injury Assessment */
  DROP COLUMN SymptomsNumbnessTingling,
  DROP COLUMN SymptomsJointPain,
  DROP COLUMN SymptomsBackPain,
  DROP COLUMN SymptomsNeckPain,
  DROP COLUMN SymptomsDizziness,
  DROP COLUMN SymptomsHeadaches,
  DROP COLUMN SymptomsOngoingStatus;



-- 1) Drop FK + index
ALTER TABLE UserRequests
  DROP FOREIGN KEY FK_UserRequests_SymptomOngoingStatus;

DROP INDEX IX_UserRequests_SymptomOngoingStatusId ON UserRequests;

-- 2) Drop the FK column
ALTER TABLE UserRequests
  DROP COLUMN SymptomOngoingStatusId;

-- 3) Drop the lookup table
DROP TABLE IF EXISTS SymptomOngoingStatus;


