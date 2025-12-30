-- ============================================================================
-- Phase 2: Create Default ServiceRequests and Backfill Data
-- ============================================================================
-- This script creates a default "General" ServiceRequest for each client
-- that has an existing assignment, and backfills all existing content to
-- point to that default ServiceRequest.
--
-- IMPORTANT: Run Phase 1 (AddServiceRequestTables_Phase1.sql) first!
-- 
-- This migration preserves existing behavior by creating a default SR
-- for each client-SME pair, effectively making the current system work
-- as "SR0" (the default ServiceRequest).
-- ============================================================================

USE customerhealthdb;

-- Temporarily disable safe update mode for this migration
SET SQL_SAFE_UPDATES = 0;

-- ============================================================================
-- Step 1: Create default ServiceRequests for each client with assignments
-- ============================================================================
-- For each unique client (AssigneeId) that has active assignments,
-- create a default "General" ServiceRequest and assign it to the first
-- active SME (AssignerId) for that client.

INSERT INTO `ServiceRequests` (`ClientId`, `Title`, `Type`, `Status`, `CreatedAt`, `IsActive`, `Description`)
SELECT DISTINCT
    ua.`AssigneeId` AS `ClientId`,
    'General' AS `Title`,
    'General' AS `Type`,
    'Active' AS `Status`,
    NOW() AS `CreatedAt`,
    1 AS `IsActive`,
    'Default service request created during migration. Contains all existing content for this client.' AS `Description`
FROM `UserAssignments` ua
WHERE ua.`IsActive` = 1
    AND ua.`AssigneeId` IN (SELECT `Id` FROM `Users` WHERE `RoleId` = 1 AND `IsActive` = 1) -- Only patients/clients
    AND NOT EXISTS (
        SELECT 1 FROM `ServiceRequests` sr 
        WHERE sr.`ClientId` = ua.`AssigneeId` 
        AND sr.`Title` = 'General' 
        AND sr.`IsActive` = 1
    );

-- ============================================================================
-- Step 2: Assign SMEs to the default ServiceRequests
-- ============================================================================
-- For each default ServiceRequest, assign the first active SME for that client.
-- If a client has multiple SMEs, we'll assign the first one (oldest assignment).

INSERT INTO `ServiceRequestAssignments` (`ServiceRequestId`, `SmeUserId`, `AssignedAt`, `IsActive`)
SELECT 
    sr.`Id` AS `ServiceRequestId`,
    ua.`AssignerId` AS `SmeUserId`,
    ua.`AssignedAt` AS `AssignedAt`,
    1 AS `IsActive`
FROM `ServiceRequests` sr
INNER JOIN `UserAssignments` ua ON ua.`AssigneeId` = sr.`ClientId`
WHERE sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND ua.`IsActive` = 1
    AND NOT EXISTS (
        SELECT 1 FROM `ServiceRequestAssignments` sra
        WHERE sra.`ServiceRequestId` = sr.`Id`
        AND sra.`SmeUserId` = ua.`AssignerId`
        AND sra.`IsActive` = 1
    )
ORDER BY ua.`AssignedAt` ASC; -- Assign the first (oldest) SME

-- ============================================================================
-- Step 3: Backfill ClinicalNotes to default ServiceRequests
-- ============================================================================
UPDATE `ClinicalNotes` cn, `ServiceRequests` sr
SET cn.`ServiceRequestId` = sr.`Id`
WHERE cn.`PatientId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND cn.`ServiceRequestId` IS NULL
    AND cn.`IsActive` = 1
    AND cn.`Id` > 0; -- Explicit key reference for safe update mode

-- ============================================================================
-- Step 4: Backfill Contents (ContentItem) to default ServiceRequests
-- ============================================================================
UPDATE `Contents` c, `ServiceRequests` sr
SET c.`ServiceRequestId` = sr.`Id`
WHERE c.`PatientId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND c.`ServiceRequestId` IS NULL
    AND c.`IsActive` = 1
    AND c.`Id` > 0; -- Explicit key reference for safe update mode

-- ============================================================================
-- Step 5: Backfill JournalEntries to default ServiceRequests
-- ============================================================================
UPDATE `JournalEntries` je, `ServiceRequests` sr
SET je.`ServiceRequestId` = sr.`Id`
WHERE je.`UserId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND je.`ServiceRequestId` IS NULL
    AND je.`IsActive` = 1
    AND je.`Id` > 0; -- Explicit key reference for safe update mode

-- ============================================================================
-- Step 6: Backfill ChatSessions to default ServiceRequests
-- ============================================================================
UPDATE `ChatSessions` cs, `ServiceRequests` sr
SET cs.`ServiceRequestId` = sr.`Id`
WHERE (cs.`PatientId` = sr.`ClientId` OR (cs.`PatientId` IS NULL AND cs.`UserId` = sr.`ClientId`))
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND cs.`ServiceRequestId` IS NULL
    AND cs.`IsActive` = 1
    AND (cs.`PatientId` IS NOT NULL OR cs.`UserId` IN (SELECT `Id` FROM `Users` WHERE `RoleId` = 1))
    AND cs.`Id` > 0; -- Explicit key reference for safe update mode

-- ============================================================================
-- Step 7: Backfill Appointments to default ServiceRequests
-- ============================================================================
UPDATE `Appointments` a, `ServiceRequests` sr
SET a.`ServiceRequestId` = sr.`Id`
WHERE a.`PatientId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND a.`ServiceRequestId` IS NULL
    AND a.`IsActive` = 1
    AND a.`Id` > 0; -- Explicit key reference for safe update mode

-- ============================================================================
-- Step 8: Backfill ContentAlerts to default ServiceRequests
-- ============================================================================
UPDATE `ContentAlerts` ca, `ServiceRequests` sr
SET ca.`ServiceRequestId` = sr.`Id`
WHERE ca.`PatientId` = sr.`ClientId`
    AND sr.`Title` = 'General' 
    AND sr.`IsActive` = 1
    AND ca.`ServiceRequestId` IS NULL
    AND ca.`Id` > 0; -- Explicit key reference for safe update mode

-- ============================================================================
-- Verification: Check migration results
-- ============================================================================

-- Count of ServiceRequests created
SELECT 
    COUNT(*) AS TotalServiceRequests,
    COUNT(DISTINCT `ClientId`) AS UniqueClients
FROM `ServiceRequests`
WHERE `Title` = 'General' AND `IsActive` = 1;

-- Count of ServiceRequestAssignments created
SELECT 
    COUNT(*) AS TotalAssignments,
    COUNT(DISTINCT `ServiceRequestId`) AS ServiceRequestsWithAssignments,
    COUNT(DISTINCT `SmeUserId`) AS UniqueSMEs
FROM `ServiceRequestAssignments`
WHERE `IsActive` = 1;

-- Count of content items backfilled
SELECT 
    'ClinicalNotes' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `ClinicalNotes`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'Contents' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `Contents`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'JournalEntries' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `JournalEntries`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'ChatSessions' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `ChatSessions`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'Appointments' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `Appointments`
WHERE `IsActive` = 1
UNION ALL
SELECT 
    'ContentAlerts' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(`ServiceRequestId`) AS RowsWithServiceRequestId
FROM `ContentAlerts`;

-- ============================================================================
-- Phase 2 Complete
-- ============================================================================
-- All existing data has been backfilled to default ServiceRequests.
-- The application now operates with ServiceRequest-based access control.
-- 
-- Next step: Update application code to filter queries by ServiceRequestId
-- and enforce access control through ServiceRequest assignments.
-- ============================================================================

-- Re-enable safe update mode
SET SQL_SAFE_UPDATES = 1;

