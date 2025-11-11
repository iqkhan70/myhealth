-- Migration SQL for AddCriticalValueKeywordsTable
-- Generated from EF Core Migration

CREATE TABLE IF NOT EXISTS `CriticalValueKeywords` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CategoryId` int NOT NULL,
    `Keyword` varchar(500) NOT NULL,
    `Description` varchar(500) NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CriticalValueKeywords_CriticalValueCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `CriticalValueCategories` (`Id`) ON DELETE RESTRICT
) CHARACTER SET utf8mb4;

-- Create indexes (MySQL doesn't support IF NOT EXISTS for indexes, so check manually if needed)
CREATE INDEX `IX_CriticalValueKeywords_CategoryId` ON `CriticalValueKeywords` (`CategoryId`);
CREATE INDEX `IX_CriticalValueKeywords_IsActive` ON `CriticalValueKeywords` (`IsActive`);
CREATE INDEX `IX_CriticalValueKeywords_Keyword` ON `CriticalValueKeywords` (`Keyword`);

