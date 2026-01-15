-- =============================================
-- Migration Script: Add Metadata Field to ClientAgentSession
-- Description: Adds JSON metadata field to store structured confirmation data and conversation context
-- This enables expert system optimization: detect confirmations, avoid re-asking, optimize token costs
-- =============================================

USE `customerhealthdb`;

-- Check if Metadata column exists before adding it
SET @col_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'ClientAgentSessions' 
    AND COLUMN_NAME = 'Metadata');

-- Add Metadata column to store structured JSON data (only if it doesn't exist)
-- This will store confirmations like: {"appointmentConfirmed": true, "timeWindow": "2026-01-16 10:00-12:00"}
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE `ClientAgentSessions` 
     ADD COLUMN `Metadata` JSON NULL COMMENT ''Structured JSON metadata for confirmations and conversation context. Example: {"appointmentConfirmed": true, "timeWindow": "2026-01-16 10:00-12:00", "lastConfirmedAt": "2026-01-15T10:30:00Z"}'' AFTER `PendingCreatedServiceRequestId`;',
    'SELECT "Metadata column already exists in ClientAgentSessions" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Drop any existing problematic index on Metadata if it exists
-- (Entity Framework might have tried to create one that uses unsupported CAST)
SET @idx_exists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'ClientAgentSessions' 
    AND INDEX_NAME = 'IX_ClientAgentSessions_Metadata');

SET @sql = IF(@idx_exists > 0,
    'ALTER TABLE `ClientAgentSessions` DROP INDEX `IX_ClientAgentSessions_Metadata`;',
    'SELECT "No problematic index found on Metadata column" AS message;');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Note: We skip creating an index on the JSON Metadata column because:
-- 1. MySQL doesn't support casting JSON to array for functional indexes in this way
-- 2. JSON queries can still be efficient using JSON_EXTRACT() without an index
-- 3. The Metadata column will typically be small and queried infrequently
-- If needed in the future, we can create a generated column with an index instead

-- Update table comment for documentation
ALTER TABLE `ClientAgentSessions` COMMENT = 'Tracks client agent conversation state and active Service Request context for SR-first agentic AI. Metadata field stores structured confirmations to enable expert system optimization.';

