-- Seed Critical Value Patterns for AI Health Check
-- This script seeds the CriticalValueCategories and CriticalValuePatterns tables
-- with initial patterns for detecting critical medical values

-- Step 1: Seed Critical Value Categories
INSERT INTO CriticalValueCategories (Id, Name, Description, IsActive, CreatedAt) VALUES
(1, 'Hemoglobin', 'Critical hemoglobin values indicating severe anemia', 1, NOW()),
(2, 'Blood Pressure', 'Critical blood pressure values indicating hypertensive crisis', 1, NOW()),
(3, 'Triglycerides', 'Critical triglyceride values indicating severe hypertriglyceridemia', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 2: Seed Critical Value Patterns for Hemoglobin
-- Patterns to detect critically low hemoglobin values (severe anemia)
INSERT INTO CriticalValuePatterns (Id, CategoryId, Pattern, Description, IsActive, CreatedAt) VALUES
(1, 1, 'Hemoglobin[:\s]+(6\.0(?![0-9])|6\s(?![0-9])|6\s*g/dL|6\s*g/|<7\.0|≤6\.0)', 'Matches hemoglobin values of 6.0 or below (severe anemia)', 1, NOW()),
(2, 1, 'Hemoglobin[:\s]+(7\.0(?![0-9])|7\s(?![0-9])|7\s*g/dL|7\s*g/)', 'Matches hemoglobin values of 7.0 (critical threshold)', 1, NOW()),
(3, 1, 'Hb[:\s]+(6\.0(?![0-9])|6\s(?![0-9])|6\s*g/dL|6\s*g/|<7\.0|≤6\.0)', 'Matches Hb abbreviation for critically low values', 1, NOW()),
(4, 1, 'Hgb[:\s]+(6\.0(?![0-9])|6\s(?![0-9])|6\s*g/dL|6\s*g/|<7\.0|≤6\.0)', 'Matches Hgb abbreviation for critically low values', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Pattern = VALUES(Pattern),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 3: Seed Critical Value Patterns for Blood Pressure
-- Patterns to detect critically high blood pressure values (hypertensive crisis)
INSERT INTO CriticalValuePatterns (Id, CategoryId, Pattern, Description, IsActive, CreatedAt) VALUES
(5, 2, 'Blood\s*Pressure[:\s]+(19[0-9]|18[0-9]|180/11[0-9]|≥180)', 'Matches systolic BP of 180+ or diastolic 110+ (hypertensive crisis)', 1, NOW()),
(6, 2, 'BP[:\s]+(19[0-9]|18[0-9]|180/11[0-9]|≥180)', 'Matches BP abbreviation for hypertensive crisis values', 1, NOW()),
(7, 2, 'Blood\s*Pressure[:\s]+(2[0-9][0-9]|≥200)', 'Matches extremely high systolic BP (200+)', 1, NOW()),
(8, 2, 'Systolic[:\s]+(19[0-9]|18[0-9]|≥180)', 'Matches high systolic pressure specifically', 1, NOW()),
(9, 2, 'Diastolic[:\s]+(11[0-9]|12[0-9]|≥110)', 'Matches high diastolic pressure specifically', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Pattern = VALUES(Pattern),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 4: Seed Critical Value Patterns for Triglycerides
-- Patterns to detect critically high triglyceride values (severe hypertriglyceridemia)
INSERT INTO CriticalValuePatterns (Id, CategoryId, Pattern, Description, IsActive, CreatedAt) VALUES
(10, 3, 'Triglycerides[:\s]+(64[0-9]|6[5-9][0-9]|7[0-9][0-9]|≥500)', 'Matches triglyceride values of 500+ mg/dL (severe hypertriglyceridemia)', 1, NOW()),
(11, 3, 'Trig[:\s]+(64[0-9]|6[5-9][0-9]|7[0-9][0-9]|≥500)', 'Matches Trig abbreviation for critical values', 1, NOW()),
(12, 3, 'TG[:\s]+(64[0-9]|6[5-9][0-9]|7[0-9][0-9]|≥500)', 'Matches TG abbreviation for critical values', 1, NOW()),
(13, 3, 'Triglycerides[:\s]+(5[0-9][0-9]|≥500)', 'Matches triglyceride values of 500-999 mg/dL', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Pattern = VALUES(Pattern),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

