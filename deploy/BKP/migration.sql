CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `AccidentParticipantRole` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `Label` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_AccidentParticipantRole` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `AIInstructionCategories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Context` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'HealthCheck',
        `DisplayOrder` int NOT NULL DEFAULT 0,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_AIInstructionCategories` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `AIModelConfigs` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ModelName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `ModelType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Provider` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `ApiEndpoint` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `ApiKeyConfigKey` varchar(100) CHARACTER SET utf8mb4 NULL,
        `SystemPrompt` longtext CHARACTER SET utf8mb4 NULL,
        `Context` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'ClinicalNote',
        `DisplayOrder` int NOT NULL DEFAULT 0,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_AIModelConfigs` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `ContentTypes` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(200) CHARACTER SET utf8mb4 NULL,
        `Icon` varchar(20) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `SortOrder` int NOT NULL DEFAULT 0,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        CONSTRAINT `PK_ContentTypes` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `CriticalValueCategories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        CONSTRAINT `PK_CriticalValueCategories` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `GenericQuestionPatterns` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Pattern` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Priority` int NOT NULL DEFAULT 0,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_GenericQuestionPatterns` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `KnowledgeBaseCategories` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `DisplayOrder` int NOT NULL DEFAULT 0,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_KnowledgeBaseCategories` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `MedicalAttentionType` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `Label` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_MedicalAttentionType` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `MedicalThresholds` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ParameterName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Unit` varchar(50) CHARACTER SET utf8mb4 NULL,
        `SeverityLevel` varchar(50) CHARACTER SET utf8mb4 NULL,
        `MinValue` double NULL,
        `MaxValue` double NULL,
        `ComparisonOperator` varchar(20) CHARACTER SET utf8mb4 NULL,
        `ThresholdValue` double NULL,
        `SecondaryParameterName` varchar(100) CHARACTER SET utf8mb4 NULL,
        `SecondaryThresholdValue` double NULL,
        `SecondaryComparisonOperator` varchar(20) CHARACTER SET utf8mb4 NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Priority` int NOT NULL DEFAULT 0,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_MedicalThresholds` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `Roles` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Roles` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `SectionMarkers` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Marker` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Category` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Priority` int NOT NULL DEFAULT 0,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_SectionMarkers` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `States` (
        `Code` varchar(2) CHARACTER SET utf8mb4 NOT NULL,
        `Name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_States` PRIMARY KEY (`Code`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `SymptomOngoingStatus` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `Label` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_SymptomOngoingStatus` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `TransportToCareMethod` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `Label` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_TransportToCareMethod` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `VehicleDisposition` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `Code` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `Label` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_VehicleDisposition` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `AIInstructions` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CategoryId` int NOT NULL,
        `Content` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NULL,
        `DisplayOrder` int NOT NULL DEFAULT 0,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_AIInstructions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AIInstructions_AIInstructionCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `AIInstructionCategories` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `AIModelChains` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ChainName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Context` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'ClinicalNote',
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `PrimaryModelId` int NOT NULL,
        `SecondaryModelId` int NOT NULL,
        `ChainOrder` int NOT NULL DEFAULT 1,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_AIModelChains` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AIModelChains_AIModelConfigs_PrimaryModelId` FOREIGN KEY (`PrimaryModelId`) REFERENCES `AIModelConfigs` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_AIModelChains_AIModelConfigs_SecondaryModelId` FOREIGN KEY (`SecondaryModelId`) REFERENCES `AIModelConfigs` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `CriticalValueKeywords` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CategoryId` int NOT NULL,
        `Keyword` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        CONSTRAINT `PK_CriticalValueKeywords` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CriticalValueKeywords_CriticalValueCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `CriticalValueCategories` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `CriticalValuePatterns` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CategoryId` int NOT NULL,
        `Pattern` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        CONSTRAINT `PK_CriticalValuePatterns` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CriticalValuePatterns_CriticalValueCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `CriticalValueCategories` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `Users` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `FirstName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `LastName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
        `DateOfBirthEncrypted` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Gender` varchar(20) CHARACTER SET utf8mb4 NULL,
        `MobilePhoneEncrypted` varchar(500) CHARACTER SET utf8mb4 NULL,
        `RoleId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `LastLoginAt` datetime(6) NULL,
        `IsActive` tinyint(1) NOT NULL,
        `IsFirstLogin` tinyint(1) NOT NULL,
        `MustChangePassword` tinyint(1) NOT NULL,
        `Specialization` varchar(100) CHARACTER SET utf8mb4 NULL,
        `LicenseNumber` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Age` int NULL,
        `Race` varchar(100) CHARACTER SET utf8mb4 NULL,
        `AccidentAddress` text CHARACTER SET utf8mb4 NULL,
        `AccidentDate` datetime(6) NULL,
        `VehicleDetails` text CHARACTER SET utf8mb4 NULL,
        `DateReported` datetime(6) NULL,
        `PoliceCaseNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `AccidentDetails` text CHARACTER SET utf8mb4 NULL,
        `RoadConditions` varchar(200) CHARACTER SET utf8mb4 NULL,
        `DoctorsInformation` text CHARACTER SET utf8mb4 NULL,
        `LawyersInformation` text CHARACTER SET utf8mb4 NULL,
        `AdditionalNotes` text CHARACTER SET utf8mb4 NULL,
        `SymptomOngoingStatusId` int NULL,
        `SymptomsHeadaches` tinyint(1) NULL,
        `SymptomsDizziness` tinyint(1) NULL,
        `SymptomsNeckPain` tinyint(1) NULL,
        `SymptomsBackPain` tinyint(1) NULL,
        `SymptomsJointPain` tinyint(1) NULL,
        `SymptomsNumbnessTingling` tinyint(1) NULL,
        `WentToEmergencyRoom` tinyint(1) NULL,
        `ERHospitalName` varchar(255) CHARACTER SET utf8mb4 NULL,
        `ERVisitDate` datetime(6) NULL,
        `TreatingInjurySpecialist` tinyint(1) NULL,
        `InjurySpecialistDetails` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `InsuranceAdjusterContacted` tinyint(1) NULL,
        `ProvidedRecordedStatement` tinyint(1) NULL,
        `ReceivedSettlementOffer` tinyint(1) NULL,
        `SettlementOfferAmount` decimal(65,30) NULL,
        `ClaimInsuranceCompany` varchar(255) CHARACTER SET utf8mb4 NULL,
        `SignedDocumentsRelatedToAccident` tinyint(1) NULL,
        `SignedDocumentsNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `AttorneyName` varchar(255) CHARACTER SET utf8mb4 NULL,
        `AttorneyFirm` varchar(255) CHARACTER SET utf8mb4 NULL,
        `VehicleCurrentLocation` varchar(500) CHARACTER SET utf8mb4 NULL,
        `InsuranceEstimateCompleted` tinyint(1) NULL,
        `EstimatedRepairAmount` decimal(65,30) NULL,
        `VehicleTotalLoss` tinyint(1) NULL,
        `MissedWork` tinyint(1) NULL,
        `MissedWorkDays` int NULL,
        `WorkingWithRestrictions` tinyint(1) NULL,
        `WorkRestrictionDetails` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `DailyActivitiesAffected` tinyint(1) NULL,
        `DailyActivitiesNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `ResidenceStateCode` varchar(2) CHARACTER SET utf8mb4 NULL,
        `AccidentStateCode` varchar(2) CHARACTER SET utf8mb4 NULL,
        `AccidentParticipantRoleId` int NULL,
        `VehicleDispositionId` int NULL,
        `TransportToCareMethodId` int NULL,
        `MedicalAttentionTypeId` int NULL,
        `PoliceInvolvement` tinyint(1) NULL,
        `LostConsciousness` tinyint(1) NULL,
        `NeuroSymptoms` tinyint(1) NULL,
        `MusculoskeletalSymptoms` tinyint(1) NULL,
        `PsychologicalSymptoms` tinyint(1) NULL,
        `SymptomsNotes` text CHARACTER SET utf8mb4 NULL,
        `InsuranceContacted` tinyint(1) NULL,
        `RepresentedByAttorney` tinyint(1) NULL,
        CONSTRAINT `PK_Users` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Users_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `AIResponseTemplates` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `TemplateKey` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `TemplateName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Content` TEXT CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Priority` int NOT NULL DEFAULT 0,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        `CreatedByUserId` int NULL,
        `UpdatedByUserId` int NULL,
        CONSTRAINT `PK_AIResponseTemplates` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AIResponseTemplates_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_AIResponseTemplates_Users_UpdatedByUserId` FOREIGN KEY (`UpdatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `Appointments` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `DoctorId` int NOT NULL,
        `PatientId` int NOT NULL,
        `AppointmentDateTime` datetime(6) NOT NULL,
        `Duration` time(6) NOT NULL,
        `AppointmentType` int NOT NULL,
        `Status` int NOT NULL,
        `Reason` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Notes` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `CreatedByUserId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `DayBeforeReminderSent` tinyint(1) NOT NULL DEFAULT FALSE,
        `DayOfReminderSent` tinyint(1) NOT NULL DEFAULT FALSE,
        `TimeZoneId` longtext CHARACTER SET utf8mb4 NOT NULL,
        `IsBusinessHours` tinyint(1) NOT NULL,
        CONSTRAINT `PK_Appointments` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Appointments_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Appointments_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Appointments_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `ChatSessions` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `SessionId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `PatientId` int NULL,
        `Summary` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `LastActivityAt` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `PrivacyLevel` longtext CHARACTER SET utf8mb4 NOT NULL,
        `MessageCount` int NOT NULL,
        `IsIgnoredByDoctor` tinyint(1) NOT NULL,
        `IgnoredByDoctorId` int NULL,
        `IgnoredAt` datetime(6) NULL,
        CONSTRAINT `PK_ChatSessions` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ChatSessions_Users_IgnoredByDoctorId` FOREIGN KEY (`IgnoredByDoctorId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_ChatSessions_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_ChatSessions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `ClinicalNotes` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `PatientId` int NOT NULL,
        `DoctorId` int NOT NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Content` TEXT CHARACTER SET utf8mb4 NOT NULL,
        `NoteType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Priority` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `IsConfidential` tinyint(1) NOT NULL DEFAULT FALSE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `IsArchived` tinyint(1) NOT NULL,
        `Tags` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IsIgnoredByDoctor` tinyint(1) NOT NULL DEFAULT FALSE,
        `IgnoredByDoctorId` int NULL,
        `IgnoredAt` datetime(6) NULL,
        CONSTRAINT `PK_ClinicalNotes` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ClinicalNotes_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_ClinicalNotes_Users_IgnoredByDoctorId` FOREIGN KEY (`IgnoredByDoctorId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_ClinicalNotes_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `Contents` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ContentGuid` char(36) COLLATE ascii_general_ci NOT NULL,
        `PatientId` int NOT NULL,
        `AddedByUserId` int NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `FileName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `OriginalFileName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `MimeType` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `FileSizeBytes` bigint NOT NULL,
        `S3Bucket` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `S3Key` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `ContentTypeModelId` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `LastAccessedAt` datetime(6) NULL,
        `IsActive` tinyint(1) NOT NULL,
        `IsIgnoredByDoctor` tinyint(1) NOT NULL,
        `IgnoredByDoctorId` int NULL,
        `IgnoredAt` datetime(6) NULL,
        CONSTRAINT `PK_Contents` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Contents_ContentTypes_ContentTypeModelId` FOREIGN KEY (`ContentTypeModelId`) REFERENCES `ContentTypes` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Contents_Users_AddedByUserId` FOREIGN KEY (`AddedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Contents_Users_IgnoredByDoctorId` FOREIGN KEY (`IgnoredByDoctorId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_Contents_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `DoctorAvailabilities` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `DoctorId` int NOT NULL,
        `Date` datetime(6) NOT NULL,
        `IsOutOfOffice` tinyint(1) NOT NULL DEFAULT FALSE,
        `Reason` varchar(500) CHARACTER SET utf8mb4 NULL,
        `StartTime` time(6) NULL,
        `EndTime` time(6) NULL,
        `TimeZoneId` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        CONSTRAINT `PK_DoctorAvailabilities` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_DoctorAvailabilities_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `EmergencyIncidents` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `PatientId` int NOT NULL,
        `DoctorId` int NULL,
        `EmergencyType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Severity` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Message` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `Timestamp` datetime(6) NOT NULL,
        `DeviceId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `DeviceToken` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `IsAcknowledged` tinyint(1) NOT NULL,
        `AcknowledgedAt` datetime(6) NULL,
        `DoctorResponse` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `ActionTaken` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `Resolution` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `ResolvedAt` datetime(6) NULL,
        `VitalSignsJson` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `LocationJson` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `IpAddress` varchar(50) CHARACTER SET utf8mb4 NULL,
        `UserAgent` varchar(500) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_EmergencyIncidents` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_EmergencyIncidents_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_EmergencyIncidents_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `JournalEntries` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` int NOT NULL,
        `EnteredByUserId` int NULL,
        `Text` longtext CHARACTER SET utf8mb4 NOT NULL,
        `AIResponse` longtext CHARACTER SET utf8mb4 NULL,
        `Mood` varchar(50) CHARACTER SET utf8mb4 NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `IsIgnoredByDoctor` tinyint(1) NOT NULL,
        `IgnoredByDoctorId` int NULL,
        `IgnoredAt` datetime(6) NULL,
        CONSTRAINT `PK_JournalEntries` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_JournalEntries_Users_EnteredByUserId` FOREIGN KEY (`EnteredByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_JournalEntries_Users_IgnoredByDoctorId` FOREIGN KEY (`IgnoredByDoctorId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_JournalEntries_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `KnowledgeBaseEntries` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `CategoryId` int NOT NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Content` TEXT CHARACTER SET utf8mb4 NOT NULL,
        `Keywords` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `Priority` int NOT NULL DEFAULT 0,
        `UseAsDirectResponse` tinyint(1) NOT NULL DEFAULT TRUE,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` datetime(6) NULL,
        `CreatedByUserId` int NULL,
        `UpdatedByUserId` int NULL,
        CONSTRAINT `PK_KnowledgeBaseEntries` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_KnowledgeBaseEntries_KnowledgeBaseCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `KnowledgeBaseCategories` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_KnowledgeBaseEntries_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_KnowledgeBaseEntries_Users_UpdatedByUserId` FOREIGN KEY (`UpdatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `RegisteredDevices` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `PatientId` int NOT NULL,
        `DeviceId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `DeviceName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `DeviceType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `DeviceModel` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `OperatingSystem` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `DeviceToken` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `PublicKey` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `ExpiresAt` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `LastUsedAt` datetime(6) NULL,
        `LastKnownLocation` varchar(500) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_RegisteredDevices` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_RegisteredDevices_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `SmsMessages` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SenderId` int NOT NULL,
        `ReceiverId` int NOT NULL,
        `Message` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `SentAt` datetime(6) NOT NULL,
        `IsRead` tinyint(1) NOT NULL DEFAULT FALSE,
        `ReadAt` datetime(6) NULL,
        CONSTRAINT `PK_SmsMessages` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SmsMessages_Users_ReceiverId` FOREIGN KEY (`ReceiverId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_SmsMessages_Users_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `UserAssignments` (
        `AssignerId` int NOT NULL,
        `AssigneeId` int NOT NULL,
        `AssignedAt` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        CONSTRAINT `PK_UserAssignments` PRIMARY KEY (`AssignerId`, `AssigneeId`),
        CONSTRAINT `FK_UserAssignments_Users_AssigneeId` FOREIGN KEY (`AssigneeId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_UserAssignments_Users_AssignerId` FOREIGN KEY (`AssignerId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `UserRequests` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `FirstName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `LastName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `DateOfBirthEncrypted` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Gender` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `MobilePhoneEncrypted` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Reason` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `Status` int NOT NULL,
        `RequestedAt` datetime(6) NOT NULL,
        `ReviewedByUserId` int NULL,
        `ReviewedAt` datetime(6) NULL,
        `Notes` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `Age` int NULL,
        `Race` varchar(100) CHARACTER SET utf8mb4 NULL,
        `AccidentAddress` text CHARACTER SET utf8mb4 NULL,
        `AccidentDate` datetime(6) NULL,
        `VehicleDetails` text CHARACTER SET utf8mb4 NULL,
        `DateReported` datetime(6) NULL,
        `PoliceCaseNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `AccidentDetails` text CHARACTER SET utf8mb4 NULL,
        `RoadConditions` varchar(200) CHARACTER SET utf8mb4 NULL,
        `DoctorsInformation` text CHARACTER SET utf8mb4 NULL,
        `LawyersInformation` text CHARACTER SET utf8mb4 NULL,
        `AdditionalNotes` text CHARACTER SET utf8mb4 NULL,
        `SymptomOngoingStatusId` int NULL,
        `SymptomsHeadaches` tinyint(1) NULL,
        `SymptomsDizziness` tinyint(1) NULL,
        `SymptomsNeckPain` tinyint(1) NULL,
        `SymptomsBackPain` tinyint(1) NULL,
        `SymptomsJointPain` tinyint(1) NULL,
        `SymptomsNumbnessTingling` tinyint(1) NULL,
        `WentToEmergencyRoom` tinyint(1) NULL,
        `ERHospitalName` varchar(255) CHARACTER SET utf8mb4 NULL,
        `ERVisitDate` datetime(6) NULL,
        `TreatingInjurySpecialist` tinyint(1) NULL,
        `InjurySpecialistDetails` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `InsuranceAdjusterContacted` tinyint(1) NULL,
        `ProvidedRecordedStatement` tinyint(1) NULL,
        `ReceivedSettlementOffer` tinyint(1) NULL,
        `SettlementOfferAmount` decimal(65,30) NULL,
        `ClaimInsuranceCompany` varchar(255) CHARACTER SET utf8mb4 NULL,
        `SignedDocumentsRelatedToAccident` tinyint(1) NULL,
        `SignedDocumentsNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `AttorneyName` varchar(255) CHARACTER SET utf8mb4 NULL,
        `AttorneyFirm` varchar(255) CHARACTER SET utf8mb4 NULL,
        `VehicleCurrentLocation` varchar(500) CHARACTER SET utf8mb4 NULL,
        `InsuranceEstimateCompleted` tinyint(1) NULL,
        `EstimatedRepairAmount` decimal(65,30) NULL,
        `VehicleTotalLoss` tinyint(1) NULL,
        `MissedWork` tinyint(1) NULL,
        `MissedWorkDays` int NULL,
        `WorkingWithRestrictions` tinyint(1) NULL,
        `WorkRestrictionDetails` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `DailyActivitiesAffected` tinyint(1) NULL,
        `DailyActivitiesNotes` varchar(1000) CHARACTER SET utf8mb4 NULL,
        `ResidenceStateCode` varchar(2) CHARACTER SET utf8mb4 NULL,
        `AccidentStateCode` varchar(2) CHARACTER SET utf8mb4 NULL,
        `AccidentParticipantRoleId` int NULL,
        `VehicleDispositionId` int NULL,
        `TransportToCareMethodId` int NULL,
        `MedicalAttentionTypeId` int NULL,
        `PoliceInvolvement` tinyint(1) NULL,
        `LostConsciousness` tinyint(1) NULL,
        `NeuroSymptoms` tinyint(1) NULL,
        `MusculoskeletalSymptoms` tinyint(1) NULL,
        `PsychologicalSymptoms` tinyint(1) NULL,
        `SymptomsNotes` varchar(2000) CHARACTER SET utf8mb4 NULL,
        `InsuranceContacted` tinyint(1) NULL,
        `RepresentedByAttorney` tinyint(1) NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `UpdatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_UserRequests` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_UserRequests_Users_ReviewedByUserId` FOREIGN KEY (`ReviewedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `ChatMessages` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `SessionId` int NOT NULL,
        `Role` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Content` TEXT CHARACTER SET utf8mb4 NOT NULL,
        `Timestamp` datetime(6) NOT NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `IsMedicalData` tinyint(1) NOT NULL,
        `MessageType` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Metadata` varchar(1000) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_ChatMessages` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ChatMessages_ChatSessions_SessionId` FOREIGN KEY (`SessionId`) REFERENCES `ChatSessions` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `ContentAlerts` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ContentId` int NOT NULL,
        `PatientId` int NOT NULL,
        `AlertType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `Severity` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        `IsRead` tinyint(1) NOT NULL,
        `IsResolved` tinyint(1) NOT NULL,
        CONSTRAINT `PK_ContentAlerts` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ContentAlerts_Contents_ContentId` FOREIGN KEY (`ContentId`) REFERENCES `Contents` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_ContentAlerts_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE TABLE `ContentAnalyses` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `ContentId` int NOT NULL,
        `ContentTypeName` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `ExtractedText` TEXT CHARACTER SET utf8mb4 NOT NULL,
        `AnalysisResults` JSON NOT NULL,
        `Alerts` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ProcessedAt` datetime(6) NOT NULL,
        `ProcessingStatus` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `ErrorMessage` varchar(1000) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `PK_ContentAnalyses` PRIMARY KEY (`Id`),
        CONSTRAINT `FK_ContentAnalyses_Contents_ContentId` FOREIGN KEY (`ContentId`) REFERENCES `Contents` (`Id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_AccidentParticipantRole_Code` ON `AccidentParticipantRole` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIInstructionCategories_Context` ON `AIInstructionCategories` (`Context`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIInstructionCategories_Context_IsActive_DisplayOrder` ON `AIInstructionCategories` (`Context`, `IsActive`, `DisplayOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIInstructionCategories_DisplayOrder` ON `AIInstructionCategories` (`DisplayOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIInstructionCategories_IsActive` ON `AIInstructionCategories` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIInstructions_CategoryId` ON `AIInstructions` (`CategoryId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIInstructions_CategoryId_IsActive_DisplayOrder` ON `AIInstructions` (`CategoryId`, `IsActive`, `DisplayOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIInstructions_DisplayOrder` ON `AIInstructions` (`DisplayOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIInstructions_IsActive` ON `AIInstructions` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_AIModelChains_ChainName_Context` ON `AIModelChains` (`ChainName`, `Context`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIModelChains_Context` ON `AIModelChains` (`Context`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIModelChains_IsActive` ON `AIModelChains` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIModelChains_PrimaryModelId` ON `AIModelChains` (`PrimaryModelId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIModelChains_SecondaryModelId` ON `AIModelChains` (`SecondaryModelId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIModelConfigs_Context` ON `AIModelConfigs` (`Context`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIModelConfigs_Context_IsActive_DisplayOrder` ON `AIModelConfigs` (`Context`, `IsActive`, `DisplayOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIModelConfigs_IsActive` ON `AIModelConfigs` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_AIModelConfigs_ModelName_Context` ON `AIModelConfigs` (`ModelName`, `Context`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIModelConfigs_ModelType` ON `AIModelConfigs` (`ModelType`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIResponseTemplates_CreatedByUserId` ON `AIResponseTemplates` (`CreatedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIResponseTemplates_IsActive` ON `AIResponseTemplates` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIResponseTemplates_IsActive_Priority` ON `AIResponseTemplates` (`IsActive`, `Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIResponseTemplates_Priority` ON `AIResponseTemplates` (`Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_AIResponseTemplates_TemplateKey` ON `AIResponseTemplates` (`TemplateKey`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_AIResponseTemplates_UpdatedByUserId` ON `AIResponseTemplates` (`UpdatedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Appointments_AppointmentDateTime` ON `Appointments` (`AppointmentDateTime`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Appointments_AppointmentType` ON `Appointments` (`AppointmentType`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Appointments_CreatedByUserId` ON `Appointments` (`CreatedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Appointments_DoctorId` ON `Appointments` (`DoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Appointments_DoctorId_AppointmentDateTime` ON `Appointments` (`DoctorId`, `AppointmentDateTime`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Appointments_IsActive` ON `Appointments` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Appointments_PatientId` ON `Appointments` (`PatientId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Appointments_Status` ON `Appointments` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ChatMessages_SessionId` ON `ChatMessages` (`SessionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ChatSessions_IgnoredByDoctorId` ON `ChatSessions` (`IgnoredByDoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ChatSessions_PatientId` ON `ChatSessions` (`PatientId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_ChatSessions_SessionId` ON `ChatSessions` (`SessionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ChatSessions_UserId` ON `ChatSessions` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ClinicalNotes_CreatedAt` ON `ClinicalNotes` (`CreatedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ClinicalNotes_DoctorId` ON `ClinicalNotes` (`DoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ClinicalNotes_IgnoredByDoctorId` ON `ClinicalNotes` (`IgnoredByDoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ClinicalNotes_IsActive` ON `ClinicalNotes` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ClinicalNotes_IsIgnoredByDoctor` ON `ClinicalNotes` (`IsIgnoredByDoctor`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ClinicalNotes_NoteType` ON `ClinicalNotes` (`NoteType`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ClinicalNotes_PatientId` ON `ClinicalNotes` (`PatientId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ClinicalNotes_Priority` ON `ClinicalNotes` (`Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ContentAlerts_ContentId` ON `ContentAlerts` (`ContentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ContentAlerts_PatientId` ON `ContentAlerts` (`PatientId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_ContentAnalyses_ContentId` ON `ContentAnalyses` (`ContentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Contents_AddedByUserId` ON `Contents` (`AddedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_Contents_ContentGuid` ON `Contents` (`ContentGuid`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Contents_ContentTypeModelId` ON `Contents` (`ContentTypeModelId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Contents_IgnoredByDoctorId` ON `Contents` (`IgnoredByDoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Contents_PatientId` ON `Contents` (`PatientId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ContentTypes_IsActive` ON `ContentTypes` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_ContentTypes_Name` ON `ContentTypes` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_ContentTypes_SortOrder` ON `ContentTypes` (`SortOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_CriticalValueCategories_IsActive` ON `CriticalValueCategories` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_CriticalValueCategories_Name` ON `CriticalValueCategories` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_CriticalValueKeywords_CategoryId` ON `CriticalValueKeywords` (`CategoryId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_CriticalValueKeywords_IsActive` ON `CriticalValueKeywords` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_CriticalValueKeywords_Keyword` ON `CriticalValueKeywords` (`Keyword`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_CriticalValuePatterns_CategoryId` ON `CriticalValuePatterns` (`CategoryId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_CriticalValuePatterns_IsActive` ON `CriticalValuePatterns` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_DoctorAvailabilities_Date` ON `DoctorAvailabilities` (`Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_DoctorAvailabilities_DoctorId` ON `DoctorAvailabilities` (`DoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_DoctorAvailabilities_DoctorId_Date` ON `DoctorAvailabilities` (`DoctorId`, `Date`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_DoctorAvailabilities_IsOutOfOffice` ON `DoctorAvailabilities` (`IsOutOfOffice`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_EmergencyIncidents_DeviceToken` ON `EmergencyIncidents` (`DeviceToken`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_EmergencyIncidents_DoctorId` ON `EmergencyIncidents` (`DoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_EmergencyIncidents_PatientId` ON `EmergencyIncidents` (`PatientId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_EmergencyIncidents_Timestamp` ON `EmergencyIncidents` (`Timestamp`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_GenericQuestionPatterns_IsActive` ON `GenericQuestionPatterns` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_GenericQuestionPatterns_IsActive_Priority` ON `GenericQuestionPatterns` (`IsActive`, `Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_GenericQuestionPatterns_Priority` ON `GenericQuestionPatterns` (`Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_JournalEntries_EnteredByUserId` ON `JournalEntries` (`EnteredByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_JournalEntries_IgnoredByDoctorId` ON `JournalEntries` (`IgnoredByDoctorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_JournalEntries_UserId` ON `JournalEntries` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseCategories_DisplayOrder` ON `KnowledgeBaseCategories` (`DisplayOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseCategories_IsActive` ON `KnowledgeBaseCategories` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseCategories_IsActive_DisplayOrder` ON `KnowledgeBaseCategories` (`IsActive`, `DisplayOrder`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseCategories_Name` ON `KnowledgeBaseCategories` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseEntries_CategoryId` ON `KnowledgeBaseEntries` (`CategoryId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseEntries_CategoryId_IsActive_Priority` ON `KnowledgeBaseEntries` (`CategoryId`, `IsActive`, `Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseEntries_CreatedByUserId` ON `KnowledgeBaseEntries` (`CreatedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseEntries_IsActive` ON `KnowledgeBaseEntries` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseEntries_Priority` ON `KnowledgeBaseEntries` (`Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_KnowledgeBaseEntries_UpdatedByUserId` ON `KnowledgeBaseEntries` (`UpdatedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_MedicalAttentionType_Code` ON `MedicalAttentionType` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_MedicalThresholds_IsActive` ON `MedicalThresholds` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_MedicalThresholds_IsActive_Priority` ON `MedicalThresholds` (`IsActive`, `Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_MedicalThresholds_ParameterName` ON `MedicalThresholds` (`ParameterName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_MedicalThresholds_ParameterName_IsActive` ON `MedicalThresholds` (`ParameterName`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_MedicalThresholds_Priority` ON `MedicalThresholds` (`Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_MedicalThresholds_SeverityLevel` ON `MedicalThresholds` (`SeverityLevel`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_RegisteredDevices_DeviceId` ON `RegisteredDevices` (`DeviceId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_RegisteredDevices_DeviceToken` ON `RegisteredDevices` (`DeviceToken`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_RegisteredDevices_PatientId` ON `RegisteredDevices` (`PatientId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_Roles_Name` ON `Roles` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_SectionMarkers_Category` ON `SectionMarkers` (`Category`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_SectionMarkers_IsActive` ON `SectionMarkers` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_SectionMarkers_IsActive_Priority` ON `SectionMarkers` (`IsActive`, `Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_SectionMarkers_Marker` ON `SectionMarkers` (`Marker`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_SectionMarkers_Priority` ON `SectionMarkers` (`Priority`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_SmsMessages_ReceiverId_IsRead` ON `SmsMessages` (`ReceiverId`, `IsRead`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_SmsMessages_SenderId_ReceiverId_SentAt` ON `SmsMessages` (`SenderId`, `ReceiverId`, `SentAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_States_Name` ON `States` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_SymptomOngoingStatus_Code` ON `SymptomOngoingStatus` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_TransportToCareMethod_Code` ON `TransportToCareMethod` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_UserAssignments_AssigneeId` ON `UserAssignments` (`AssigneeId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_UserAssignments_AssigneeId_IsActive` ON `UserAssignments` (`AssigneeId`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_UserAssignments_AssignerId` ON `UserAssignments` (`AssignerId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_UserAssignments_AssignerId_IsActive` ON `UserAssignments` (`AssignerId`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_UserAssignments_IsActive` ON `UserAssignments` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_UserRequests_Email` ON `UserRequests` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_UserRequests_ReviewedByUserId` ON `UserRequests` (`ReviewedByUserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_AccidentParticipantRoleId` ON `Users` (`AccidentParticipantRoleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_AccidentStateCode` ON `Users` (`AccidentStateCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_FirstName` ON `Users` (`FirstName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_IsActive` ON `Users` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_LastName` ON `Users` (`LastName`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_MedicalAttentionTypeId` ON `Users` (`MedicalAttentionTypeId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_ResidenceStateCode` ON `Users` (`ResidenceStateCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_RoleId` ON `Users` (`RoleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_RoleId_IsActive` ON `Users` (`RoleId`, `IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_TransportToCareMethodId` ON `Users` (`TransportToCareMethodId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE INDEX `IX_Users_VehicleDispositionId` ON `Users` (`VehicleDispositionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    CREATE UNIQUE INDEX `IX_VehicleDisposition_Code` ON `VehicleDisposition` (`Code`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251219144202_InitialCleanBaseline') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251219144202_InitialCleanBaseline', '9.0.9');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

