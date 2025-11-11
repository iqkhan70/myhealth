-- Migration SQL for AddCriticalValuePatternTables
-- Generated from EF Core Migration

CREATE TABLE IF NOT EXISTS `CriticalValueCategories` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(255) NOT NULL,
    `Description` varchar(500) NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    PRIMARY KEY (`Id`)
) CHARACTER SET utf8mb4;

CREATE TABLE IF NOT EXISTS `CriticalValuePatterns` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CategoryId` int NOT NULL,
    `Pattern` varchar(500) NOT NULL,
    `Description` varchar(500) NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CriticalValuePatterns_CriticalValueCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `CriticalValueCategories` (`Id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

-- Create indexes (MySQL doesn't support IF NOT EXISTS for indexes, so check manually if needed)
CREATE INDEX `IX_CriticalValueCategories_Name` ON `CriticalValueCategories` (`Name`);
CREATE INDEX `IX_CriticalValueCategories_IsActive` ON `CriticalValueCategories` (`IsActive`);
CREATE INDEX `IX_CriticalValuePatterns_CategoryId` ON `CriticalValuePatterns` (`CategoryId`);
CREATE INDEX `IX_CriticalValuePatterns_IsActive` ON `CriticalValuePatterns` (`IsActive`);

