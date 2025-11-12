-- This script marks the AddKnowledgeBaseTables migration as applied
-- Run this if the tables were partially created but the migration wasn't recorded
-- Execute this in your MySQL database

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251112000000_AddKnowledgeBaseTables', '9.0.9')
ON DUPLICATE KEY UPDATE MigrationId = MigrationId;

-- Verify the tables exist and have the correct structure
SELECT 'KnowledgeBaseCategories table check:' AS Status;
SELECT COUNT(*) AS TableExists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'KnowledgeBaseCategories';

SELECT 'KnowledgeBaseEntries table check:' AS Status;
SELECT COUNT(*) AS TableExists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'KnowledgeBaseEntries';

