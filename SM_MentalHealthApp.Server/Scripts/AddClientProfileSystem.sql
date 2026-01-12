-- =============================================
-- Migration Script: Add Client Profile System for Agentic AI
-- Description: Creates tables for client profiling, interaction patterns, and learning
-- Tables: ClientProfiles, ClientInteractionPatterns, ClientKeywordReactions
-- =============================================

-- Create ClientProfiles table
CREATE TABLE IF NOT EXISTS `ClientProfiles` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientId` INT NOT NULL,
    `CommunicationStyle` VARCHAR(50) NULL DEFAULT 'Balanced',
    `InformationTolerance` DECIMAL(3,2) NOT NULL DEFAULT 0.5 COMMENT '0-1 scale: how much information client can handle',
    `EmotionalSensitivity` DECIMAL(3,2) NOT NULL DEFAULT 0.5 COMMENT '0-1 scale: how client reacts to issues',
    `PreferredTone` VARCHAR(50) NULL DEFAULT 'Supportive' COMMENT 'Supportive, Professional, Casual, Technical',
    `TotalInteractions` INT NOT NULL DEFAULT 0,
    `SuccessfulResolutions` INT NOT NULL DEFAULT 0,
    `AverageResponseTime` INT NULL COMMENT 'Average time in seconds for client to respond',
    `LastUpdated` DATETIME(6) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_ClientProfiles_ClientId` (`ClientId`),
    CONSTRAINT `FK_ClientProfiles_Users_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ClientProfiles_LastUpdated` (`LastUpdated`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Create ClientInteractionPatterns table
CREATE TABLE IF NOT EXISTS `ClientInteractionPatterns` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientId` INT NOT NULL,
    `PatternType` VARCHAR(50) NOT NULL COMMENT 'UrgencyResponse, InfoPreference, CommunicationStyle, etc.',
    `PatternData` JSON NULL COMMENT 'Flexible JSON storage for pattern-specific data',
    `Confidence` DECIMAL(3,2) NOT NULL DEFAULT 0.5 COMMENT '0-1 scale: confidence in this pattern',
    `OccurrenceCount` INT NOT NULL DEFAULT 1 COMMENT 'How many times this pattern was observed',
    `LastObserved` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClientInteractionPatterns_Users_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    INDEX `IX_ClientInteractionPatterns_ClientId_PatternType` (`ClientId`, `PatternType`),
    INDEX `IX_ClientInteractionPatterns_LastObserved` (`LastObserved`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Create ClientKeywordReactions table
CREATE TABLE IF NOT EXISTS `ClientKeywordReactions` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientId` INT NOT NULL,
    `Keyword` VARCHAR(100) NOT NULL,
    `ReactionScore` INT NOT NULL DEFAULT 0 COMMENT 'Positive reactions increase, negative decrease',
    `OccurrenceCount` INT NOT NULL DEFAULT 1,
    `LastSeen` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClientKeywordReactions_Users_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    UNIQUE KEY `IX_ClientKeywordReactions_ClientId_Keyword` (`ClientId`, `Keyword`),
    INDEX `IX_ClientKeywordReactions_ReactionScore` (`ReactionScore`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Create ClientServicePreferences table (tracks which service types client prefers)
CREATE TABLE IF NOT EXISTS `ClientServicePreferences` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientId` INT NOT NULL,
    `ServiceType` VARCHAR(100) NOT NULL COMMENT 'Plumbing, Car Repair, Lawn Care, etc.',
    `PreferenceScore` DECIMAL(3,2) NOT NULL DEFAULT 0.5 COMMENT '0-1 scale: how much client prefers this service type',
    `RequestCount` INT NOT NULL DEFAULT 0 COMMENT 'Number of requests for this service type',
    `SuccessRate` DECIMAL(3,2) NULL COMMENT '0-1 scale: success rate for this service type',
    `LastRequestDate` DATETIME(6) NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClientServicePreferences_Users_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    UNIQUE KEY `IX_ClientServicePreferences_ClientId_ServiceType` (`ClientId`, `ServiceType`),
    INDEX `IX_ClientServicePreferences_PreferenceScore` (`PreferenceScore`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Create ClientInteractionHistory table (stores detailed interaction data for learning)
CREATE TABLE IF NOT EXISTS `ClientInteractionHistory` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientId` INT NOT NULL,
    `ServiceRequestId` INT NULL,
    `InteractionType` VARCHAR(50) NOT NULL COMMENT 'Message, Response, Action, etc.',
    `ClientMessage` TEXT NULL,
    `AgentResponse` TEXT NULL,
    `Sentiment` VARCHAR(50) NULL COMMENT 'Positive, Negative, Neutral, etc.',
    `Urgency` VARCHAR(50) NULL COMMENT 'Low, Medium, High, Critical',
    `InformationLevel` VARCHAR(50) NULL COMMENT 'Minimal, Moderate, Detailed',
    `ClientReaction` VARCHAR(50) NULL COMMENT 'Satisfied, Frustrated, Confused, etc.',
    `ResponseTime` INT NULL COMMENT 'Time in seconds for client to respond',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClientInteractionHistory_Users_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ClientInteractionHistory_ServiceRequests_ServiceRequestId` FOREIGN KEY (`ServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL,
    INDEX `IX_ClientInteractionHistory_ClientId_CreatedAt` (`ClientId`, `CreatedAt`),
    INDEX `IX_ClientInteractionHistory_ServiceRequestId` (`ServiceRequestId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Add comments for documentation
ALTER TABLE `ClientProfiles` COMMENT = 'Stores per-client communication and interaction preferences for agentic AI';
ALTER TABLE `ClientInteractionPatterns` COMMENT = 'Stores learned patterns about how clients interact';
ALTER TABLE `ClientKeywordReactions` COMMENT = 'Tracks how clients react to specific keywords';
ALTER TABLE `ClientServicePreferences` COMMENT = 'Tracks client preferences for different service types';
ALTER TABLE `ClientInteractionHistory` COMMENT = 'Detailed history of all client interactions for learning';

