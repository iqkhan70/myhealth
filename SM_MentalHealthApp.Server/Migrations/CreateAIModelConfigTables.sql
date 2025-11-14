-- Migration SQL for AI Model Configuration Tables
-- This table stores configuration for different AI models (BioMistral, Meditron, etc.)

CREATE TABLE IF NOT EXISTS `AIModelConfigs` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ModelName` varchar(100) NOT NULL,
    `ModelType` varchar(50) NOT NULL, -- 'Primary', 'Secondary', 'Chained'
    `Provider` varchar(50) NOT NULL, -- 'HuggingFace', 'OpenAI', 'Ollama', etc.
    `ApiEndpoint` varchar(500) NOT NULL,
    `ApiKeyConfigKey` varchar(100) NULL, -- Configuration key for API key (e.g., 'HuggingFace:ApiKey')
    `SystemPrompt` text NULL, -- System prompt/instructions for the model
    `Context` varchar(50) NOT NULL DEFAULT 'ClinicalNote', -- 'ClinicalNote', 'HealthCheck', 'Chat', etc.
    `DisplayOrder` int NOT NULL DEFAULT 0,
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    `UpdatedAt` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UK_AIModelConfigs_ModelName_Context` (`ModelName`, `Context`)
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `AIModelChains` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ChainName` varchar(100) NOT NULL,
    `Context` varchar(50) NOT NULL DEFAULT 'ClinicalNote',
    `Description` varchar(500) NULL,
    `PrimaryModelId` int NOT NULL, -- First model in chain (e.g., BioMistral)
    `SecondaryModelId` int NOT NULL, -- Second model in chain (e.g., Meditron)
    `ChainOrder` int NOT NULL DEFAULT 1, -- Order of execution
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    `UpdatedAt` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AIModelChains_PrimaryModel` FOREIGN KEY (`PrimaryModelId`) REFERENCES `AIModelConfigs` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_AIModelChains_SecondaryModel` FOREIGN KEY (`SecondaryModelId`) REFERENCES `AIModelConfigs` (`Id`) ON DELETE RESTRICT,
    UNIQUE KEY `UK_AIModelChains_ChainName_Context` (`ChainName`, `Context`)
) CHARACTER SET utf8mb4;

-- Create indexes
CREATE INDEX `IX_AIModelConfigs_ModelType` ON `AIModelConfigs` (`ModelType`);
CREATE INDEX `IX_AIModelConfigs_Context` ON `AIModelConfigs` (`Context`);
CREATE INDEX `IX_AIModelConfigs_IsActive` ON `AIModelConfigs` (`IsActive`);
CREATE INDEX `IX_AIModelConfigs_Context_IsActive_DisplayOrder` ON `AIModelConfigs` (`Context`, `IsActive`, `DisplayOrder`);

CREATE INDEX `IX_AIModelChains_Context` ON `AIModelChains` (`Context`);
CREATE INDEX `IX_AIModelChains_IsActive` ON `AIModelChains` (`IsActive`);
CREATE INDEX `IX_AIModelChains_PrimaryModelId` ON `AIModelChains` (`PrimaryModelId`);
CREATE INDEX `IX_AIModelChains_SecondaryModelId` ON `AIModelChains` (`SecondaryModelId`);

