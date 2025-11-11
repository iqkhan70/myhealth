-- Add clinical note concern keywords to the database
-- These keywords help detect concerning content in clinical notes
-- All keywords are database-driven - no hardcoded patterns in code

-- Get category IDs
SET @high_concern_id = (SELECT Id FROM CriticalValueCategories WHERE Name = 'High Concern' LIMIT 1);
SET @distress_id = (SELECT Id FROM CriticalValueCategories WHERE Name = 'Distress' LIMIT 1);

-- Add clinical note concern keywords to High Concern category
INSERT INTO CriticalValueKeywords (Id, CategoryId, Keyword, Description, IsActive, CreatedAt) VALUES
(95, @high_concern_id, 'serious symptoms', 'Matches serious symptoms mentioned in clinical notes', 1, NOW()),
(96, @high_concern_id, 'serious symptom', 'Matches serious symptom (singular) mentioned in clinical notes', 1, NOW()),
(97, @high_concern_id, 'serious concern', 'Matches serious concern mentioned in clinical notes', 1, NOW()),
(98, @high_concern_id, 'serious condition', 'Matches serious condition mentioned in clinical notes', 1, NOW()),
(99, @high_concern_id, 'high blood pressure', 'Matches high blood pressure mentioned in clinical notes', 1, NOW()),
(100, @high_concern_id, 'elevated blood pressure', 'Matches elevated blood pressure mentioned in clinical notes', 1, NOW()),
(101, @high_concern_id, 'hypertension', 'Matches hypertension mentioned in clinical notes', 1, NOW()),
(102, @high_concern_id, 'heart problem', 'Matches heart problem (singular) mentioned in clinical notes', 1, NOW()),
(103, @high_concern_id, 'heart problems', 'Matches heart problems (plural) mentioned in clinical notes', 1, NOW()),
(104, @high_concern_id, 'cardiac', 'Matches cardiac issues mentioned in clinical notes', 1, NOW()),
(105, @high_concern_id, 'cardiovascular', 'Matches cardiovascular issues mentioned in clinical notes', 1, NOW()),
(106, @high_concern_id, 'risk of', 'Matches risk indicators in clinical notes', 1, NOW()),
(107, @high_concern_id, 'requires monitoring', 'Matches monitoring requirements in clinical notes', 1, NOW()),
(108, @high_concern_id, 'needs monitoring', 'Matches monitoring needs in clinical notes', 1, NOW()),
(109, @high_concern_id, 'must monitor', 'Matches mandatory monitoring in clinical notes', 1, NOW()),
(110, @high_concern_id, 'more test', 'Matches need for additional tests in clinical notes', 1, NOW()),
(111, @high_concern_id, 'additional test', 'Matches additional test requirements in clinical notes', 1, NOW()),
(112, @high_concern_id, 'further test', 'Matches further test requirements in clinical notes', 1, NOW()),
(113, @high_concern_id, 'further evaluation', 'Matches further evaluation needs in clinical notes', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Keyword = VALUES(Keyword),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Add anxiety-related keywords to Distress category (if not already there)
INSERT INTO CriticalValueKeywords (Id, CategoryId, Keyword, Description, IsActive, CreatedAt) VALUES
(114, @distress_id, 'anxiety', 'Matches anxiety mentioned in clinical notes or patient data', 1, NOW()),
(115, @distress_id, 'anxious', 'Matches anxious state mentioned in clinical notes or patient data', 1, NOW()),
(116, @distress_id, 'panic', 'Matches panic mentioned in clinical notes or patient data', 1, NOW()),
(117, @distress_id, 'worry', 'Matches worry mentioned in clinical notes or patient data', 1, NOW()),
(118, @distress_id, 'concerned', 'Matches concerned state mentioned in clinical notes or patient data', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Keyword = VALUES(Keyword),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

