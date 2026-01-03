-- ============================================================================
-- Diagnostic Script: Check Billing Data Requirements
-- ============================================================================
-- Run this script to identify what data might be missing for invoice generation
-- ============================================================================

-- Step 1: Check if SMEs have BillingAccountId
SELECT 
    'SMEs without BillingAccountId' AS CheckType,
    COUNT(*) AS Count,
    GROUP_CONCAT(DISTINCT CONCAT(u.Id, ':', u.FirstName, ' ', u.LastName) SEPARATOR ', ') AS Details
FROM Users u
WHERE u.RoleId IN (2, 5, 6) -- Doctor, Attorney, SME
  AND u.BillingAccountId IS NULL
  AND u.IsActive = 1;

-- Step 2: Check Service Requests without PrimaryExpertiseId or expertise tags
SELECT 
    'Service Requests without PrimaryExpertiseId or expertise' AS CheckType,
    sr.Id AS ServiceRequestId,
    sr.Title,
    sr.PrimaryExpertiseId,
    (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) AS ExpertiseCount,
    CASE 
        WHEN sr.PrimaryExpertiseId IS NULL AND (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) = 0 
            THEN 'Missing: No expertise tags and no PrimaryExpertiseId'
        WHEN sr.PrimaryExpertiseId IS NULL AND (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) > 1 
            THEN 'Missing: Multiple expertise tags but no PrimaryExpertiseId'
        ELSE 'OK'
    END AS Status
FROM ServiceRequests sr
WHERE sr.IsActive = 1
  AND (
    sr.PrimaryExpertiseId IS NULL AND (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) = 0
    OR
    (sr.PrimaryExpertiseId IS NULL AND (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) > 1)
  );

-- Step 3: Check Assignments that are ready to bill but missing requirements
SELECT 
    'Assignments ready but missing requirements' AS CheckType,
    a.Id AS AssignmentId,
    a.ServiceRequestId,
    sr.Title AS ServiceRequestTitle,
    a.SmeUserId,
    CONCAT(u.FirstName, ' ', u.LastName) AS SmeName,
    a.Status,
    a.BillingStatus,
    a.IsBillable,
    u.BillingAccountId,
    CASE 
        WHEN u.BillingAccountId IS NULL THEN 'Missing: SME has no BillingAccountId'
        WHEN sr.PrimaryExpertiseId IS NULL AND (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) = 0 
            THEN 'Missing: SR has no expertise/PrimaryExpertiseId'
        WHEN sr.PrimaryExpertiseId IS NULL AND (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) > 1 
            THEN 'Missing: SR has multiple expertise but no PrimaryExpertiseId'
        WHEN a.Status NOT IN ('Accepted', 'InProgress', 'Completed') THEN 'Wrong: Assignment status not billable'
        WHEN a.IsBillable = 0 THEN 'Wrong: Assignment not marked as billable'
        ELSE 'OK'
    END AS Issue
FROM ServiceRequestAssignments a
INNER JOIN ServiceRequests sr ON a.ServiceRequestId = sr.Id
INNER JOIN Users u ON a.SmeUserId = u.Id
WHERE a.IsActive = 1
  AND a.BillingStatus = 'Ready'
  AND a.InvoiceId IS NULL
  AND (
    u.BillingAccountId IS NULL
    OR sr.PrimaryExpertiseId IS NULL AND (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) = 0
    OR (sr.PrimaryExpertiseId IS NULL AND (SELECT COUNT(*) FROM ServiceRequestExpertise sre WHERE sre.ServiceRequestId = sr.Id) > 1)
    OR a.Status NOT IN ('Accepted', 'InProgress', 'Completed')
    OR a.IsBillable = 0
  );

-- Step 4: Check existing charges
SELECT 
    'Existing charges summary' AS CheckType,
    COUNT(*) AS TotalCharges,
    COUNT(CASE WHEN Status = 'Ready' AND InvoiceId IS NULL THEN 1 END) AS ReadyCharges,
    COUNT(CASE WHEN InvoiceId IS NOT NULL THEN 1 END) AS InvoicedCharges,
    COUNT(CASE WHEN Status = 'Paid' THEN 1 END) AS PaidCharges
FROM ServiceRequestCharges;

-- Step 5: Check charges by BillingAccountId
SELECT 
    'Charges by BillingAccountId' AS CheckType,
    BillingAccountId,
    COUNT(*) AS TotalCharges,
    COUNT(CASE WHEN Status = 'Ready' AND InvoiceId IS NULL THEN 1 END) AS ReadyCharges
FROM ServiceRequestCharges
GROUP BY BillingAccountId
ORDER BY BillingAccountId;

-- Step 6: Check if BillingAccounts exist for all companies
SELECT 
    'Companies without BillingAccounts' AS CheckType,
    c.Id AS CompanyId,
    c.Name AS CompanyName
FROM Companies c
WHERE c.IsActive = 1
  AND NOT EXISTS (
    SELECT 1 FROM BillingAccounts ba 
    WHERE ba.CompanyId = c.Id AND ba.IsActive = 1
  );

-- Step 7: Check if BillingAccounts exist for individual SMEs
SELECT 
    'Individual SMEs without BillingAccounts' AS CheckType,
    COUNT(*) AS Count,
    GROUP_CONCAT(DISTINCT CONCAT(u.Id, ':', u.FirstName, ' ', u.LastName) SEPARATOR ', ') AS Details
FROM Users u
WHERE u.RoleId IN (2, 5, 6) -- Doctor, Attorney, SME
  AND u.CompanyId IS NULL
  AND u.IsActive = 1
  AND NOT EXISTS (
    SELECT 1 FROM BillingAccounts ba 
    WHERE ba.UserId = u.Id AND ba.IsActive = 1
  );

