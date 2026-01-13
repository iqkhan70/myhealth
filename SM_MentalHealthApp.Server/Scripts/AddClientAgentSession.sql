-- =============================================
-- Migration Script: Add Client Agent Session for SR-First Agentic AI
-- Description: Creates table to track client agent conversation state and SR context
-- Tables: ClientAgentSessions
-- =============================================

-- Create ClientAgentSessions table
CREATE TABLE IF NOT EXISTS `ClientAgentSessions` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientId` INT NOT NULL,
    `CurrentServiceRequestId` INT NULL COMMENT 'Active SR context for this conversation',
    `State` VARCHAR(50) NOT NULL DEFAULT 'NoActiveSRContext' COMMENT 'NoActiveSRContext, SelectingExistingSR, CreatingNewSR, InSRContext',
    `PendingCreatedServiceRequestId` INT NULL COMMENT 'SR ID that was just created, waiting for confirmation',
    `LastUpdatedUtc` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_ClientAgentSessions_ClientId` (`ClientId`),
    CONSTRAINT `FK_ClientAgentSessions_Users_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ClientAgentSessions_SR_Current` FOREIGN KEY (`CurrentServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_ClientAgentSessions_SR_Pending` FOREIGN KEY (`PendingCreatedServiceRequestId`) REFERENCES `ServiceRequests` (`Id`) ON DELETE SET NULL,
    INDEX `IX_ClientAgentSessions_State` (`State`),
    INDEX `IX_ClientAgentSessions_LastUpdatedUtc` (`LastUpdatedUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Add comments for documentation
ALTER TABLE `ClientAgentSessions` COMMENT = 'Tracks client agent conversation state and active Service Request context for SR-first agentic AI';

