-- =====================================================
-- Complete AI Response Template Migration and Seed Script
-- Run this entire script in MySQL Workbench to:
-- 1. Create the AIResponseTemplates table
-- 2. Create all indexes
-- 3. Seed all response templates
-- 4. Mark migration as applied
-- 5. Verify everything
-- =====================================================

-- Step 1: Create the AIResponseTemplates table
CREATE TABLE IF NOT EXISTS `AIResponseTemplates` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `TemplateKey` varchar(100) NOT NULL,
    `TemplateName` varchar(200) NOT NULL,
    `Content` longtext NOT NULL,
    `Description` varchar(500) DEFAULT NULL,
    `Priority` int NOT NULL DEFAULT 0,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL DEFAULT (CURRENT_TIMESTAMP(6)),
    `UpdatedAt` datetime(6) DEFAULT NULL,
    `CreatedByUserId` int DEFAULT NULL,
    `UpdatedByUserId` int DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_AIResponseTemplates_TemplateKey` (`TemplateKey`),
    KEY `IX_AIResponseTemplates_Priority` (`Priority`),
    KEY `IX_AIResponseTemplates_IsActive` (`IsActive`),
    KEY `IX_AIResponseTemplates_IsActive_Priority` (`IsActive`, `Priority`),
    KEY `IX_AIResponseTemplates_CreatedByUserId` (`CreatedByUserId`),
    KEY `IX_AIResponseTemplates_UpdatedByUserId` (`UpdatedByUserId`),
    CONSTRAINT `FK_AIResponseTemplates_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_AIResponseTemplates_Users_UpdatedByUserId` FOREIGN KEY (`UpdatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Step 2: Mark the migration as applied in the migration history
-- Ensure the migrations history table exists
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Mark this migration as applied
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251113000000_AddAIResponseTemplateTable', '9.0.9')
ON DUPLICATE KEY UPDATE `ProductVersion` = VALUES(`ProductVersion`);

-- Step 3: Seed AI Response Templates
-- Template for critical medical alerts
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'critical_alert',
    'Critical Medical Alert',
    '🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.
{CRITICAL_VALUES}

**IMMEDIATE MEDICAL ATTENTION REQUIRED:**
- These values indicate a medical emergency
- Contact emergency services if symptoms worsen
- Patient needs immediate medical evaluation',
    'Template for critical medical alert responses',
    10,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for critical alert with deterioration
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'critical_alert_deterioration',
    'Critical Alert - Deterioration',
    '🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.
{CRITICAL_VALUES}

**IMMEDIATE MEDICAL ATTENTION REQUIRED:**
- These values indicate a medical emergency
- Contact emergency services if symptoms worsen
- Patient needs immediate medical evaluation',
    'Template for critical alert when deterioration is detected',
    10,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for medical concerns detected
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'concerns_detected',
    'Medical Concerns Detected',
    '⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.',
    'Template for when medical concerns are detected',
    8,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for stable status
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'stable_status',
    'Stable Status',
    '✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.',
    'Template for stable patient status',
    5,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for improvement noted
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'improvement_noted',
    'Improvement Noted',
    '✅ **IMPROVEMENT NOTED:** Previous results showed critical values, but current results show normal values.
This indicates positive progress, though continued monitoring is recommended.',
    'Template for when improvement is noted from previous critical values',
    7,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for status review
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'status_review',
    'Status Review',
    '📊 **Status Review:** Based on available data, the patient appears to be stable with no immediate concerns detected.',
    'Template for general status review',
    4,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for medical data warning
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'medical_data_warning',
    'Medical Data Warning',
    '⚠️ **WARNING:** Medical content was found, but critical values may not have been properly detected.
Please review the medical data manually to ensure no critical values are missed.

📊 **Status Review:** Based on available data, the patient appears to be stable with no immediate concerns detected.
However, please verify the medical content manually for accuracy.',
    'Template for warning when medical data exists but critical values may not be detected',
    6,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for recent patient activity
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'recent_patient_activity',
    'Recent Patient Activity',
    '**Recent Patient Activity:**
{JOURNAL_ENTRIES}

The patient has been actively engaging with their health tracking.',
    'Template for recent patient activity section',
    3,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for mood statistics
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'mood_statistics',
    'Mood Statistics',
    '**Mood Statistics:**
{JOURNAL_ENTRIES}

- Patient actively tracking health status',
    'Template for mood statistics section',
    3,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for critical recommendations
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'critical_recommendations',
    'Critical Recommendations',
    '🚨 **IMMEDIATE ACTIONS REQUIRED:**
1. **Emergency Medical Care**: Contact emergency services immediately
2. **Hospital Admission**: Patient requires immediate hospitalization
3. **Specialist Consultation**: Refer to appropriate specialist
4. **Continuous Monitoring**: Vital signs every 15 minutes
5. **Immediate Evaluation**: Patient needs immediate medical evaluation',
    'Template for critical medical recommendations',
    9,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- Template for general recommendations
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES (
    'general_recommendations',
    'General Recommendations',
    '📋 **General Recommendations:**
1. **Regular Monitoring**: Schedule routine follow-up appointments
2. **Lifestyle Modifications**: Dietary changes and exercise recommendations
3. **Medication Review**: Assess current medications and interactions',
    'Template for general medical recommendations',
    5,
    TRUE,
    NOW()
)
ON DUPLICATE KEY UPDATE TemplateName = TemplateName;

-- =====================================================
-- Step 4: Verification Queries
-- =====================================================

-- Verify migration was applied
SELECT '=== Migration Status ===' AS Status;
SELECT MigrationId, ProductVersion 
FROM __EFMigrationsHistory 
WHERE MigrationId = '20251113000000_AddAIResponseTemplateTable';

-- Verify table exists and show structure
SELECT '=== Table Verification ===' AS Status;
SELECT 
    'AIResponseTemplates' AS TableName,
    COUNT(*) AS TableExists 
FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'AIResponseTemplates';

-- Show table structure
SELECT '=== Table Structure ===' AS Info;
DESCRIBE AIResponseTemplates;

-- Verify all templates were seeded
SELECT '=== Seeded Templates ===' AS Status;
SELECT 
    Id,
    TemplateKey,
    TemplateName,
    Priority,
    IsActive,
    LENGTH(Content) AS ContentLength,
    CreatedAt
FROM AIResponseTemplates 
ORDER BY Priority DESC, TemplateName;

-- Count templates
SELECT '=== Template Count ===' AS Status;
SELECT 
    COUNT(*) AS TotalTemplates, 
    SUM(CASE WHEN IsActive = TRUE THEN 1 ELSE 0 END) AS ActiveTemplates,
    SUM(CASE WHEN IsActive = FALSE THEN 1 ELSE 0 END) AS InactiveTemplates
FROM AIResponseTemplates;

-- Show sample template content (first 200 chars)
SELECT '=== Sample Template Content ===' AS Status;
SELECT 
    TemplateKey,
    TemplateName,
    LEFT(Content, 200) AS ContentPreview
FROM AIResponseTemplates 
ORDER BY Priority DESC
LIMIT 3;

SELECT '=== Migration and Seeding Complete! ===' AS Status;

