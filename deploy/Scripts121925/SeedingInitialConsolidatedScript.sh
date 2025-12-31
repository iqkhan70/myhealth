

-- Seed AI Instruction Categories and Instructions for Health Check context
-- This makes AI instructions data-driven and easily maintainable

-- Insert AI Instruction Categories
INSERT INTO AIInstructionCategories (Id, Name, Description, Context, DisplayOrder, IsActive, CreatedAt) VALUES
(1, 'CRITICAL PRIORITY', 'Instructions for handling critical medical values and emergencies', 'HealthCheck', 1, 1, NOW()),
(2, 'Patient Medical Overview', 'Instructions for analyzing patient medical data and status', 'HealthCheck', 2, 1, NOW()),
(3, 'Recent Patient Activity', 'Instructions for reviewing patient activity and history', 'HealthCheck', 3, 1, NOW()),
(4, 'Emergency Incidents', 'Instructions for handling emergency incidents', 'HealthCheck', 4, 1, NOW()),
(5, 'Clinical Assessment', 'Instructions for providing clinical assessment', 'HealthCheck', 5, 1, NOW()),
(6, 'Recommendations', 'Instructions for providing medical recommendations', 'HealthCheck', 6, 1, NOW())
ON DUPLICATE KEY UPDATE
    Description = VALUES(Description),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);

-- Insert AI Instructions for CRITICAL PRIORITY category
INSERT INTO AIInstructions (Id, CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt) VALUES
(1, 1, 'If you see ''🚨 CRITICAL MEDICAL VALUES DETECTED'' or any critical values in the medical data, you MUST:', NULL, 1, 1, NOW()),
(2, 1, 'Start your response by highlighting these critical values IMMEDIATELY', NULL, 2, 1, NOW()),
(3, 1, 'State that the patient''s status is CRITICAL or CONCERNING, NOT stable', NULL, 3, 1, NOW()),
(4, 1, 'Emphasize that immediate medical attention is required', NULL, 4, 1, NOW()),
(5, 1, 'Do NOT say the patient is stable if critical values are present', NULL, 5, 1, NOW())
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);

-- Insert AI Instructions for Patient Medical Overview category
INSERT INTO AIInstructions (Id, CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt) VALUES
(6, 2, 'FIRST: Check if there are any 🚨 CRITICAL VALUES in the medical data above', NULL, 1, 1, NOW()),
(7, 2, 'SECOND: Review RECENT CLINICAL NOTES section above - these are written by doctors and contain critical medical observations', NULL, 2, 1, NOW()),
(8, 2, 'If clinical notes mention serious symptoms, concerns, monitoring needs, or health risks, you MUST reflect this in your assessment', NULL, 3, 1, NOW()),
(9, 2, 'Do NOT say the patient is ''stable'' if clinical notes indicate serious symptoms or require monitoring', NULL, 4, 1, NOW()),
(10, 2, 'Service notes take precedence over other data when they indicate concerns', NULL, 5, 1, NOW()),
(11, 2, 'If critical values exist, state: ''🚨 CRITICAL STATUS: Patient has critical medical values requiring immediate attention''', NULL, 6, 1, NOW()),
(12, 2, 'If no critical values but abnormal values exist, state: ''⚠️ CONCERNING STATUS: Patient has abnormal values requiring monitoring''', NULL, 7, 1, NOW()),
(13, 2, 'Only state ''STABLE'' if ALL values are normal, no clinical notes indicate concerns, and no concerning patterns are detected', NULL, 8, 1, NOW()),
(14, 2, 'Summarize key medical findings from test results, vital signs, medical data, AND clinical notes', NULL, 9, 1, NOW()),
(15, 2, 'Reference specific values from the medical data (e.g., if hemoglobin is below 7 g/dL, it indicates severe anemia)', NULL, 10, 1, NOW())
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);

-- Insert AI Instructions for Recent Patient Activity category
INSERT INTO AIInstructions (Id, CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt) VALUES
(16, 3, 'Review journal entries and journalentry patterns', NULL, 1, 1, NOW()),
(17, 3, 'Analyze chat history for concerning conversations or medical data', NULL, 2, 1, NOW()),
(18, 3, 'Review clinical notes for doctor observations and assessments', NULL, 3, 1, NOW())
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);

-- Insert AI Instructions for Emergency Incidents category
INSERT INTO AIInstructions (Id, CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt) VALUES
(19, 4, 'If emergency incidents exist, start with: ''🚨 CRITICAL EMERGENCY ALERT: [number] unacknowledged emergency incidents detected''', NULL, 1, 1, NOW()),
(20, 4, 'List each emergency with severity (all are unacknowledged and require immediate attention)', NULL, 2, 1, NOW()),
(21, 4, 'Note: Only unacknowledged emergencies are included in this analysis', NULL, 3, 1, NOW())
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);

-- Insert AI Instructions for Clinical Assessment category
INSERT INTO AIInstructions (Id, CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt) VALUES
(22, 5, 'Provide a professional assessment of the patient''s overall health status', NULL, 1, 1, NOW()),
(23, 5, 'If critical values are present, state clearly that the patient requires IMMEDIATE medical attention', NULL, 2, 1, NOW()),
(24, 5, 'Identify any trends (improving, stable, deteriorating)', NULL, 3, 1, NOW()),
(25, 5, 'Highlight areas requiring attention or follow-up', NULL, 4, 1, NOW())
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);

-- Insert AI Instructions for Recommendations category
INSERT INTO AIInstructions (Id, CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt) VALUES
(26, 6, 'If critical values are found, recommend IMMEDIATE medical evaluation and emergency department visit if necessary', NULL, 1, 1, NOW()),
(27, 6, 'Suggest any immediate actions if critical issues are found', NULL, 2, 1, NOW()),
(28, 6, 'Recommend follow-up care or monitoring if needed', NULL, 3, 1, NOW())
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);

-- Final instruction (appended after all categories)
INSERT INTO AIInstructionCategories (Id, Name, Description, Context, DisplayOrder, IsActive, CreatedAt) VALUES
(7, 'IMPORTANT', 'Final important instructions', 'HealthCheck', 99, 1, NOW())
ON DUPLICATE KEY UPDATE
    Description = VALUES(Description),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);

INSERT INTO AIInstructions (Id, CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt) VALUES
(29, 7, 'Be specific and reference actual data from the patient''s records. If critical values are present in the medical data, you MUST indicate the patient is NOT stable. Only state ''stable'' if ALL medical values are normal. Keep the response comprehensive but concise (300-400 words).', NULL, 1, 1, NOW())
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive);



-- Seed ContentTypes table with initial data
INSERT INTO ContentTypes (Name, Description, Icon, IsActive, SortOrder, CreatedAt) VALUES
('Document', 'General document files (PDF, DOC, TXT, etc.)', '📄', 1, 1, NOW()),
('Image', 'Image files (JPG, PNG, GIF, etc.)', '🖼️', 1, 2, NOW()),
('Video', 'Video files (MP4, AVI, MOV, etc.)', '🎥', 1, 3, NOW()),
('Audio', 'Audio files (MP3, WAV, FLAC, etc.)', '🎵', 1, 4, NOW()),
('Other', 'Other file types', '📁', 1, 5, NOW())
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    Icon = VALUES(Icon),
    IsActive = VALUES(IsActive),
    SortOrder = VALUES(SortOrder);


-- Seed Critical Value Keywords for AI Health Check
-- This script seeds the CriticalValueKeywords table with all hardcoded keywords
-- that were previously in the code

-- Step 1: Create additional categories if needed (Abnormal, Normal, Mental Health)
INSERT INTO CriticalValueCategories (Id, Name, Description, IsActive, CreatedAt) VALUES
(4, 'Critical', 'Keywords indicating critical medical status', 1, NOW()),
(5, 'Abnormal', 'Keywords indicating abnormal medical values', 1, NOW()),
(6, 'Normal', 'Keywords indicating normal medical values', 1, NOW()),
(7, 'High Concern', 'Mental health keywords indicating high concern or crisis', 1, NOW()),
(8, 'Distress', 'Mental health keywords indicating distress or negative emotions', 1, NOW()),
(9, 'Positive', 'Mental health keywords indicating positive emotions or well-being', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 2: Seed Critical Value Keywords
-- Critical status keywords
INSERT INTO CriticalValueKeywords (Id, CategoryId, Keyword, Description, IsActive, CreatedAt) VALUES
(1, 4, '🚨 CRITICAL MEDICAL VALUES DETECTED', 'Matches critical medical values detected message', 1, NOW()),
(2, 4, 'CRITICAL VALUES DETECTED IN LATEST RESULTS', 'Matches critical values detected in latest results', 1, NOW()),
(3, 4, 'STATUS: CRITICAL', 'Matches critical status indicator', 1, NOW()),
(4, 4, 'Critical Values:', 'Matches critical values label', 1, NOW()),
(5, 4, 'CRITICAL MEDICAL ALERT', 'Matches critical medical alert message', 1, NOW()),
(6, 4, '🚨 **CRITICAL VALUES DETECTED IN LATEST RESULTS:**', 'Matches formatted critical values detected message', 1, NOW()),
(7, 4, '⚠️ **STATUS: CRITICAL', 'Matches formatted critical status with warning emoji', 1, NOW()),
(8, 4, '🚨 CRITICAL: Severe Anemia', 'Matches severe anemia critical alert', 1, NOW()),
(9, 4, '🚨 CRITICAL: Extremely High Triglycerides', 'Matches extremely high triglycerides critical alert', 1, NOW()),
(10, 4, '🚨 CRITICAL: Hypertensive Crisis', 'Matches hypertensive crisis critical alert', 1, NOW()),
(11, 4, '**CRITICAL MEDICAL VALUES DETECTED**', 'Matches bold critical medical values detected', 1, NOW()),
(12, 4, '**CRITICAL VALUES DETECTED IN LATEST RESULTS:**', 'Matches bold critical values detected message', 1, NOW()),
(13, 4, 'CRITICAL VALUES DETECTED IN EXTRACTED TEXT:', 'Matches critical values in extracted text', 1, NOW()),
(14, 4, 'STATUS: CRITICAL - IMMEDIATE MEDICAL ATTENTION REQUIRED', 'Matches critical status requiring immediate attention', 1, NOW()),
(15, 4, 'CRITICAL MEDICAL VALUES DETECTED:', 'Matches critical medical values detected with colon', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Keyword = VALUES(Keyword),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 3: Seed Abnormal Value Keywords
INSERT INTO CriticalValueKeywords (Id, CategoryId, Keyword, Description, IsActive, CreatedAt) VALUES
(16, 5, 'ABNORMAL VALUES DETECTED IN LATEST RESULTS', 'Matches abnormal values detected in latest results', 1, NOW()),
(17, 5, 'STATUS: CONCERNING', 'Matches concerning status indicator', 1, NOW()),
(18, 5, 'Abnormal Values:', 'Matches abnormal values label', 1, NOW()),
(19, 5, 'ABNORMAL', 'Matches abnormal keyword in alerts', 1, NOW()),
(20, 5, '⚠️', 'Matches warning emoji indicating abnormal values', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Keyword = VALUES(Keyword),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 4: Seed Normal Value Keywords
INSERT INTO CriticalValueKeywords (Id, CategoryId, Keyword, Description, IsActive, CreatedAt) VALUES
(21, 6, 'NORMAL VALUES IN LATEST RESULTS', 'Matches normal values in latest results', 1, NOW()),
(22, 6, 'STATUS: STABLE', 'Matches stable status indicator', 1, NOW()),
(23, 6, 'Normal Values:', 'Matches normal values label', 1, NOW()),
(24, 6, 'IMPROVEMENT NOTED', 'Matches improvement noted message', 1, NOW()),
(25, 6, 'NORMAL', 'Matches normal keyword in alerts', 1, NOW()),
(26, 6, '✅', 'Matches checkmark emoji indicating normal values', 1, NOW()),
(27, 6, 'STATUS: STABLE - All values within normal range', 'Matches stable status with normal range message', 1, NOW()),
(28, 6, '**NORMAL VALUES IN LATEST RESULTS:**', 'Matches formatted normal values message', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Keyword = VALUES(Keyword),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 5: Seed High Concern Mental Health Keywords
INSERT INTO CriticalValueKeywords (Id, CategoryId, Keyword, Description, IsActive, CreatedAt) VALUES
(29, 7, 'really bad', 'Matches really bad feeling - high concern', 1, NOW()),
(30, 7, 'terrible', 'Matches terrible feeling - high concern', 1, NOW()),
(31, 7, 'awful', 'Matches awful feeling - high concern', 1, NOW()),
(32, 7, 'horrible', 'Matches horrible feeling - high concern', 1, NOW()),
(33, 7, 'worst', 'Matches worst feeling - high concern', 1, NOW()),
(34, 7, 'can\'t take it', 'Matches inability to handle situation - high concern', 1, NOW()),
(35, 7, 'can\'t handle', 'Matches inability to handle situation - high concern', 1, NOW()),
(36, 7, 'suicidal', 'Matches suicidal ideation - high concern', 1, NOW()),
(37, 7, 'suicide', 'Matches suicide keyword - high concern', 1, NOW()),
(38, 7, 'kill myself', 'Matches self-harm intent - high concern', 1, NOW()),
(39, 7, 'want to die', 'Matches suicidal ideation - high concern', 1, NOW()),
(40, 7, 'end it all', 'Matches suicidal ideation - high concern', 1, NOW()),
(41, 7, 'not worth living', 'Matches hopelessness with suicidal ideation - high concern', 1, NOW()),
(42, 7, 'no point living', 'Matches hopelessness with suicidal ideation - high concern', 1, NOW()),
(43, 7, 'hopeless', 'Matches hopeless feeling - high concern', 1, NOW()),
(44, 7, 'desperate', 'Matches desperate feeling - high concern', 1, NOW()),
(45, 7, 'crisis', 'Matches crisis keyword - high concern', 1, NOW()),
(46, 7, 'emergency', 'Matches emergency keyword - high concern', 1, NOW()),
(47, 7, 'urgent', 'Matches urgent keyword - high concern', 1, NOW()),
(48, 7, 'help me', 'Matches cry for help - high concern', 1, NOW()),
(49, 7, 'can\'t cope', 'Matches inability to cope - high concern', 1, NOW()),
(50, 7, 'breaking down', 'Matches mental breakdown - high concern', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Keyword = VALUES(Keyword),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 6: Seed Distress Mental Health Keywords
INSERT INTO CriticalValueKeywords (Id, CategoryId, Keyword, Description, IsActive, CreatedAt) VALUES
(51, 8, 'bad', 'Matches bad feeling keyword', 1, NOW()),
(52, 8, 'not well', 'Matches not well keyword', 1, NOW()),
(53, 8, 'struggling', 'Matches struggling keyword', 1, NOW()),
(54, 8, 'suffering', 'Matches suffering keyword', 1, NOW()),
(55, 8, 'pain', 'Matches pain keyword', 1, NOW()),
(56, 8, 'hurt', 'Matches hurt keyword', 1, NOW()),
(57, 8, 'broken', 'Matches broken keyword', 1, NOW()),
(58, 8, 'lost', 'Matches lost keyword', 1, NOW()),
(59, 8, 'confused', 'Matches confused keyword', 1, NOW()),
(60, 8, 'overwhelmed', 'Matches overwhelmed keyword', 1, NOW()),
(61, 8, 'stressed', 'Matches stressed keyword', 1, NOW()),
(62, 8, 'anxious', 'Matches anxious keyword', 1, NOW()),
(63, 8, 'worried', 'Matches worried keyword', 1, NOW()),
(64, 8, 'scared', 'Matches scared keyword', 1, NOW()),
(65, 8, 'frightened', 'Matches frightened keyword', 1, NOW()),
(66, 8, 'depressed', 'Matches depressed keyword', 1, NOW()),
(67, 8, 'sad', 'Matches sad keyword', 1, NOW()),
(68, 8, 'down', 'Matches down keyword', 1, NOW()),
(69, 8, 'low', 'Matches low keyword', 1, NOW()),
(70, 8, 'empty', 'Matches empty keyword', 1, NOW()),
(71, 8, 'numb', 'Matches numb keyword', 1, NOW()),
(72, 8, 'alone', 'Matches alone keyword', 1, NOW()),
(73, 8, 'isolated', 'Matches isolated keyword', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Keyword = VALUES(Keyword),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 7: Seed Positive Mental Health Keywords
INSERT INTO CriticalValueKeywords (Id, CategoryId, Keyword, Description, IsActive, CreatedAt) VALUES
(74, 9, 'good', 'Matches good feeling keyword', 1, NOW()),
(75, 9, 'great', 'Matches great feeling keyword', 1, NOW()),
(76, 9, 'wonderful', 'Matches wonderful feeling keyword', 1, NOW()),
(77, 9, 'amazing', 'Matches amazing feeling keyword', 1, NOW()),
(78, 9, 'fantastic', 'Matches fantastic feeling keyword', 1, NOW()),
(79, 9, 'excellent', 'Matches excellent feeling keyword', 1, NOW()),
(80, 9, 'happy', 'Matches happy keyword', 1, NOW()),
(81, 9, 'joyful', 'Matches joyful keyword', 1, NOW()),
(82, 9, 'grateful', 'Matches grateful keyword', 1, NOW()),
(83, 9, 'blessed', 'Matches blessed keyword', 1, NOW()),
(84, 9, 'lucky', 'Matches lucky keyword', 1, NOW()),
(85, 9, 'proud', 'Matches proud keyword', 1, NOW()),
(86, 9, 'accomplished', 'Matches accomplished keyword', 1, NOW()),
(87, 9, 'confident', 'Matches confident keyword', 1, NOW()),
(88, 9, 'hopeful', 'Matches hopeful keyword', 1, NOW()),
(89, 9, 'better', 'Matches better keyword', 1, NOW()),
(90, 9, 'improving', 'Matches improving keyword', 1, NOW()),
(91, 9, 'progress', 'Matches progress keyword', 1, NOW()),
(92, 9, 'breakthrough', 'Matches breakthrough keyword', 1, NOW()),
(93, 9, 'success', 'Matches success keyword', 1, NOW()),
(94, 9, 'achievement', 'Matches achievement keyword', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Keyword = VALUES(Keyword),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);


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


-- Comprehensive Database Seeding Script
-- This script seeds all essential tables with initial data

-- Step 1: Seed Roles table
INSERT INTO Roles (Id, Name, Description, IsActive, CreatedAt) VALUES
(1, 'Patient', 'Regular patients who use the app for self-care and journaling', 1, NOW()),
(2, 'Doctor', 'Medical professionals who provide care and consultations', 1, NOW()),
(3, 'Admin', 'System administrators who manage users and system settings', 1, NOW()),
(4, 'Coordinator', 'Coordinators who manage appointments and patient-doctor assignments', 1, NOW()),
(5, 'Attorney', 'Attorneys who have read access to patient information and can communicate with patients', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 2: Seed ContentTypes table (if not already seeded)
INSERT INTO ContentTypes (Id, Name, Description, Icon, IsActive, SortOrder, CreatedAt) VALUES
(1, 'Document', 'General document files (PDF, DOC, TXT, etc.)', '📄', 1, 1, NOW()),
(2, 'Image', 'Image files (JPG, PNG, GIF, etc.)', '🖼️', 1, 2, NOW()),
(3, 'Video', 'Video files (MP4, AVI, MOV, etc.)', '🎥', 1, 3, NOW()),
(4, 'Audio', 'Audio files (MP3, WAV, FLAC, etc.)', '🎵', 1, 4, NOW()),
(5, 'Other', 'Other file types', '📁', 1, 5, NOW())
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    Icon = VALUES(Icon),
    IsActive = VALUES(IsActive),
    SortOrder = VALUES(SortOrder);

-- Step 3: Seed Users table
-- Note: Using demo123 as password hash (you should use proper password hashing in production)
INSERT INTO Users (Id, FirstName, LastName, Email, PasswordHash, DateOfBirthEncrypted, Gender, MobilePhoneEncrypted, RoleId, CreatedAt, LastLoginAt, IsActive, IsFirstLogin, MustChangePassword, Specialization, LicenseNumber) VALUES
(1, 'Admin', 'User', 'admin@mentalhealth.com', 'tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==', '', 'Other', '', 3, NOW(), NULL, 1, 0, 0, NULL, NULL),
(2, 'Dr. Sarah', 'Johnson', 'dr.sarah@mentalhealth.com', 'tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==', '', 'Female', '', 2, NOW(), NULL, 1, 0, 0, 'Psychiatry', 'MD123456'),
(3, 'John', 'Doe', 'john@doe.com', 'tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==', '', 'Male', '', 1, NOW(), NULL, 1, 1, 1, NULL, NULL)
ON DUPLICATE KEY UPDATE 
    FirstName = VALUES(FirstName),
    LastName = VALUES(LastName),
    PasswordHash = VALUES(PasswordHash),
    DateOfBirthEncrypted = VALUES(DateOfBirthEncrypted),
    Gender = VALUES(Gender),
    MobilePhoneEncrypted = VALUES(MobilePhoneEncrypted),
    RoleId = VALUES(RoleId),
    IsActive = VALUES(IsActive),
    Specialization = VALUES(Specialization),
    LicenseNumber = VALUES(LicenseNumber);

-- Step 4: Create User Assignments (Doctor-Patient relationships)
INSERT INTO UserAssignments (AssignerId, AssigneeId, AssignedAt, IsActive) VALUES
(1, 2, NOW(), 1),  -- Admin assigns Doctor
(2, 3, NOW(), 1)   -- Doctor assigned to Patient
ON DUPLICATE KEY UPDATE 
    AssignedAt = VALUES(AssignedAt),
    IsActive = VALUES(IsActive);

-- Step 5: Seed Critical Value Categories and Patterns
-- Note: Run SeedCriticalValuePatterns.sql and SeedCriticalValueKeywords.sql after running this script

-- Step 6: Seed AI Instruction Categories and Instructions
-- Note: Run SeedAIInstructions.sql after running this script to make AI instructions data-driven

-- Step 5: Seed some sample Journal Entries
INSERT INTO JournalEntries (UserId, EnteredByUserId, Text, AIResponse, Mood, CreatedAt,IsIgnoredByDoctor) VALUES
(3, 3, 'Feeling anxious about work today. Had a difficult meeting with my manager.', 'It sounds like you had a challenging day at work. Anxiety about work situations is very common. Consider taking some deep breaths and maybe talking to someone you trust about your concerns.', 'Anxious', NOW() - INTERVAL 1 DAY, 0),
(3, 3, 'Much better day today! Went for a walk in the park and felt more relaxed.', 'That\'s wonderful to hear! Physical activity and time in nature can be very therapeutic. Keep up the great work with self-care!', 'Happy', NOW() - INTERVAL 2 DAY, 0),
(3, 2, 'Patient reported improved sleep patterns this week. Discussed stress management techniques.', 'Great progress on sleep patterns! Stress management is crucial for mental health. Continue monitoring and provide ongoing support.', 'Neutral', NOW() - INTERVAL 3 DAY, 0)
ON DUPLICATE KEY UPDATE 
    Text = VALUES(Text),
    AIResponse = VALUES(AIResponse),
    Mood = VALUES(Mood);

-- Step 6: Seed some sample Chat Sessions
INSERT INTO ChatSessions (SessionId, UserId, PatientId, CreatedAt, LastActivityAt, IsActive, MessageCount, PrivacyLevel, Summary, IsIgnoredByDoctor) VALUES
('session_001', 2, 3, NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 HOUR, 1, 5, 'Private', 'Discussion about anxiety management and coping strategies', 0),
('session_002', 3, 3, NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 HOUR, 1, 3, 'Private', 'Self-reflection on daily mood and activities', 0)
ON DUPLICATE KEY UPDATE 
    LastActivityAt = VALUES(LastActivityAt),
    IsActive = VALUES(IsActive),
    MessageCount = VALUES(MessageCount),
    Summary = VALUES(Summary);

-- Step 7: Seed some sample Chat Messages
INSERT INTO ChatMessages (SessionId, Role, MessageType, Content, IsMedicalData, Metadata, Timestamp) VALUES
(1, 'User', 'Text', 'I\'ve been feeling more anxious lately. What can I do?', 0, '{"mood": "anxious"}', NOW() - INTERVAL 1 HOUR),
(1, 'Assistant', 'Text', 'I understand you\'re feeling anxious. Here are some techniques that might help: deep breathing, progressive muscle relaxation, and grounding exercises. Would you like me to guide you through one of these?', 0, '{"response_type": "therapeutic"}', NOW() - INTERVAL 1 HOUR),
(1, 'User', 'Text', 'Yes, please guide me through deep breathing.', 0, '{"request": "breathing_exercise"}', NOW() - INTERVAL 50 MINUTE),
(1, 'Assistant', 'Text', 'Great! Let\'s start with 4-7-8 breathing: Inhale for 4 counts, hold for 7, exhale for 8. Ready to begin?', 0, '{"exercise": "478_breathing"}', NOW() - INTERVAL 50 MINUTE),
(1, 'User', 'Text', 'That helped a lot, thank you!', 0, '{"feedback": "positive"}', NOW() - INTERVAL 45 MINUTE)
ON DUPLICATE KEY UPDATE 
    Content = VALUES(Content),
    IsMedicalData = VALUES(IsMedicalData),
    Metadata = VALUES(Metadata);

-- Step 8: Seed some sample Content Items (if you want to test content functionality)
INSERT INTO Contents (ContentGuid, PatientId, AddedByUserId, Title, Description, FileName, OriginalFileName, FileSizeBytes, S3Bucket, S3Key, ContentTypeModelId, CreatedAt, LastAccessedAt, IsActive,MimeType,isIgnoredByDoctor) VALUES
(UUID(), 3, 3, 'My Daily Mood Journal', 'A document tracking my daily emotional state', 'mood_journal.pdf', 'mood_journal.pdf', 1024000, 'mentalhealth-content', 'content/mood_journal.pdf', 1, NOW() - INTERVAL 1 DAY, NULL, 1, 'application/pdf', 0),
(UUID(), 3, 2, 'Therapy Session Notes', 'Notes from today\'s therapy session', 'therapy_notes.pdf', 'therapy_notes.pdf', 512000, 'mentalhealth-content', 'content/therapy_notes.pdf', 1, NOW() - INTERVAL 2 DAY, NULL, 1, 'application/pdf', 0),
(UUID(), 3, 3, 'Relaxation Exercise Video', 'A video demonstrating breathing exercises', 'breathing_exercise.mp4', 'breathing_exercise.mp4', 15728640, 'mentalhealth-content', 'content/breathing_exercise.mp4', 3, NOW() - INTERVAL 3 DAY, NULL, 1, 'video/mp4', 0)
ON DUPLICATE KEY UPDATE 
    Title = VALUES(Title),
    Description = VALUES(Description),
    LastAccessedAt = VALUES(LastAccessedAt),
    IsActive = VALUES(IsActive);

-- Step 9: Seed some sample Emergency Incidents (for testing emergency system)
INSERT INTO EmergencyIncidents (PatientId, DoctorId, DeviceId, DeviceToken, EmergencyType, Message, Severity, LocationJson, VitalSignsJson, IpAddress, UserAgent, Timestamp, IsAcknowledged, AcknowledgedAt, DoctorResponse, ActionTaken, Resolution, ResolvedAt) VALUES
(3, 2, 'device_001', 'token_001', 'PanicAttack', 'Patient experiencing severe panic attack, needs immediate assistance', 'High', '{"lat": 37.7749, "lng": -122.4194, "address": "San Francisco, CA"}', '{"heart_rate": 120, "blood_pressure": "140/90"}', '192.168.1.100', 'MentalHealthApp/1.0', NOW() - INTERVAL 1 HOUR, 1, NOW() - INTERVAL 50 MINUTE, 'I\'m on my way. Please try the breathing exercises we discussed.', 'Dispatched emergency response team', 'Patient stabilized with breathing exercises', NOW() - INTERVAL 30 MINUTE)
ON DUPLICATE KEY UPDATE 
    IsAcknowledged = VALUES(IsAcknowledged),
    AcknowledgedAt = VALUES(AcknowledgedAt),
    DoctorResponse = VALUES(DoctorResponse),
    ActionTaken = VALUES(ActionTaken),
    Resolution = VALUES(Resolution),
    ResolvedAt = VALUES(ResolvedAt);

-- Step 10: Seed some sample SMS Messages
INSERT INTO SmsMessages (SenderId, ReceiverId, Message, SentAt, IsRead, ReadAt) VALUES
(2, 3, 'Hi John, I wanted to check in on how you\'re feeling today. Please let me know if you need to talk.', NOW() - INTERVAL 2 HOUR, 1, NOW() - INTERVAL 1 HOUR),
(3, 2, 'Thank you Dr. Sarah. I\'m feeling better today after our session yesterday.', NOW() - INTERVAL 1 HOUR, 0, NULL),
(1, 2, 'System notification: New patient assignment - John Doe has been assigned to you.', NOW() - INTERVAL 1 DAY, 1, NOW() - INTERVAL 23 HOUR)
ON DUPLICATE KEY UPDATE 
    Message = VALUES(Message),
    IsRead = VALUES(IsRead),
    ReadAt = VALUES(ReadAt);

-- Verification queries
SELECT 'Seeding completed successfully!' as status;
SELECT 'Roles count:' as info, COUNT(*) as count FROM Roles;
SELECT 'Users count:' as info, COUNT(*) as count FROM Users;
SELECT 'ContentTypes count:' as info, COUNT(*) as count FROM ContentTypes;
SELECT 'Journal Entries count:' as info, COUNT(*) as count FROM JournalEntries;
SELECT 'Chat Sessions count:' as info, COUNT(*) as count FROM ChatSessions;
SELECT 'Chat Messages count:' as info, COUNT(*) as count FROM ChatMessages;
SELECT 'Contents count:' as info, COUNT(*) as count FROM Contents;
SELECT 'Emergency Incidents count:' as info, COUNT(*) as count FROM EmergencyIncidents;
SELECT 'SMS Messages count:' as info, COUNT(*) as count FROM SmsMessages;


-- Comprehensive Database Seeding Script
-- This script seeds all essential tables with initial data

-- Step 1: Seed Roles table
INSERT INTO Roles (Id, Name, Description, IsActive, CreatedAt) VALUES
(1, 'Patient', 'Regular patients who use the app for self-care and journaling', 1, NOW()),
(2, 'Doctor', 'Medical professionals who provide care and consultations', 1, NOW()),
(3, 'Admin', 'System administrators who manage users and system settings', 1, NOW()),
(4, 'Coordinator', 'Coordinators who manage appointments and patient-doctor assignments', 1, NOW()),
(5, 'Attorney', 'Attorneys who have read access to patient information and can communicate with patients', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

-- Step 2: Seed ContentTypes table (if not already seeded)
INSERT INTO ContentTypes (Id, Name, Description, Icon, IsActive, SortOrder, CreatedAt) VALUES
(1, 'Document', 'General document files (PDF, DOC, TXT, etc.)', '📄', 1, 1, NOW()),
(2, 'Image', 'Image files (JPG, PNG, GIF, etc.)', '🖼️', 1, 2, NOW()),
(3, 'Video', 'Video files (MP4, AVI, MOV, etc.)', '🎥', 1, 3, NOW()),
(4, 'Audio', 'Audio files (MP3, WAV, FLAC, etc.)', '🎵', 1, 4, NOW()),
(5, 'Other', 'Other file types', '📁', 1, 5, NOW())
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    Icon = VALUES(Icon),
    IsActive = VALUES(IsActive),
    SortOrder = VALUES(SortOrder);

-- Step 3: Seed Users table
-- Note: Using demo123 as password hash (you should use proper password hashing in production)
INSERT INTO Users (Id, FirstName, LastName, Email, PasswordHash, DateOfBirthEncrypted, Gender, MobilePhoneEncrypted, RoleId, CreatedAt, LastLoginAt, IsActive, IsFirstLogin, MustChangePassword, Specialization, LicenseNumber) VALUES
(1, 'Admin', 'User', 'admin@mentalhealth.com', 'tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==', '', 'Other', '', 3, NOW(), NULL, 1, 0, 0, NULL, NULL),
(2, 'Dr. Sarah', 'Johnson', 'dr.sarah@mentalhealth.com', 'tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==', '', 'Female', '', 2, NOW(), NULL, 1, 0, 0, 'Psychiatry', 'MD123456'),
(3, 'John', 'Doe', 'john@doe.com', 'tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==', '', 'Male', '', 1, NOW(), NULL, 1, 1, 1, NULL, NULL)
ON DUPLICATE KEY UPDATE 
    FirstName = VALUES(FirstName),
    LastName = VALUES(LastName),
    PasswordHash = VALUES(PasswordHash),
    DateOfBirthEncrypted = VALUES(DateOfBirthEncrypted),
    Gender = VALUES(Gender),
    MobilePhoneEncrypted = VALUES(MobilePhoneEncrypted),
    RoleId = VALUES(RoleId),
    IsActive = VALUES(IsActive),
    Specialization = VALUES(Specialization),
    LicenseNumber = VALUES(LicenseNumber);

-- Step 4: Create User Assignments (Doctor-Patient relationships)
INSERT INTO UserAssignments (AssignerId, AssigneeId, AssignedAt, IsActive) VALUES
(1, 2, NOW(), 1),  -- Admin assigns Doctor
(2, 3, NOW(), 1)   -- Doctor assigned to Patient
ON DUPLICATE KEY UPDATE 
    AssignedAt = VALUES(AssignedAt),
    IsActive = VALUES(IsActive);

-- Step 5: Seed Critical Value Categories and Patterns
-- Note: Run SeedCriticalValuePatterns.sql and SeedCriticalValueKeywords.sql after running this script

-- Step 6: Seed AI Instruction Categories and Instructions
-- Note: Run SeedAIInstructions.sql after running this script to make AI instructions data-driven

-- Step 5: Seed some sample Journal Entries
INSERT INTO JournalEntries (UserId, EnteredByUserId, Text, AIResponse, Mood, CreatedAt,isIgnoredByDoctor) VALUES
(3, 3, 'Feeling anxious about work today. Had a difficult meeting with my manager.', 'It sounds like you had a challenging day at work. Anxiety about work situations is very common. Consider taking some deep breaths and maybe talking to someone you trust about your concerns.', 'Anxious', NOW() - INTERVAL 1 DAY, 0),
(3, 3, 'Much better day today! Went for a walk in the park and felt more relaxed.', 'That\'s wonderful to hear! Physical activity and time in nature can be very therapeutic. Keep up the great work with self-care!', 'Happy', NOW() - INTERVAL 2 DAY, 0),
(3, 2, 'Patient reported improved sleep patterns this week. Discussed stress management techniques.', 'Great progress on sleep patterns! Stress management is crucial for mental health. Continue monitoring and provide ongoing support.', 'Neutral', NOW() - INTERVAL 3 DAY, 0)
ON DUPLICATE KEY UPDATE 
    Text = VALUES(Text),
    AIResponse = VALUES(AIResponse),
    Mood = VALUES(Mood);

-- Step 6: Seed some sample Chat Sessions
INSERT INTO ChatSessions (SessionId, UserId, PatientId, CreatedAt, LastActivityAt, IsActive, MessageCount, PrivacyLevel, Summary, IsIgnoredByDoctor) VALUES
('session_001', 2, 3, NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 HOUR, 1, 5, 'Private', 'Discussion about anxiety management and coping strategies', 0),
('session_002', 3, 3, NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 HOUR, 1, 3, 'Private', 'Self-reflection on daily mood and activities', 0)
ON DUPLICATE KEY UPDATE 
    LastActivityAt = VALUES(LastActivityAt),
    IsActive = VALUES(IsActive),
    MessageCount = VALUES(MessageCount),
    Summary = VALUES(Summary);

-- Step 7: Seed some sample Chat Messages
INSERT INTO ChatMessages (SessionId, Role, MessageType, Content, IsMedicalData, Metadata, Timestamp) VALUES
(1, 'User', 'Text', 'I\'ve been feeling more anxious lately. What can I do?', 0, '{"mood": "anxious"}', NOW() - INTERVAL 1 HOUR),
(1, 'Assistant', 'Text', 'I understand you\'re feeling anxious. Here are some techniques that might help: deep breathing, progressive muscle relaxation, and grounding exercises. Would you like me to guide you through one of these?', 0, '{"response_type": "therapeutic"}', NOW() - INTERVAL 1 HOUR),
(1, 'User', 'Text', 'Yes, please guide me through deep breathing.', 0, '{"request": "breathing_exercise"}', NOW() - INTERVAL 50 MINUTE),
(1, 'Assistant', 'Text', 'Great! Let\'s start with 4-7-8 breathing: Inhale for 4 counts, hold for 7, exhale for 8. Ready to begin?', 0, '{"exercise": "478_breathing"}', NOW() - INTERVAL 50 MINUTE),
(1, 'User', 'Text', 'That helped a lot, thank you!', 0, '{"feedback": "positive"}', NOW() - INTERVAL 45 MINUTE)
ON DUPLICATE KEY UPDATE 
    Content = VALUES(Content),
    IsMedicalData = VALUES(IsMedicalData),
    Metadata = VALUES(Metadata);

-- Step 8: Seed some sample Content Items (if you want to test content functionality)
INSERT INTO Contents (ContentGuid, PatientId, AddedByUserId, Title, Description, FileName, OriginalFileName, FileSizeBytes, S3Bucket, S3Key, ContentTypeModelId, CreatedAt, LastAccessedAt, IsActive,MimeType,isIgnoredByDoctor) VALUES
(UUID(), 3, 3, 'My Daily Mood Journal', 'A document tracking my daily emotional state', 'mood_journal.pdf', 'mood_journal.pdf', 1024000, 'mentalhealth-content', 'content/mood_journal.pdf', 1, NOW() - INTERVAL 1 DAY, NULL, 1, 'application/pdf', 0),
(UUID(), 3, 2, 'Therapy Session Notes', 'Notes from today\'s therapy session', 'therapy_notes.pdf', 'therapy_notes.pdf', 512000, 'mentalhealth-content', 'content/therapy_notes.pdf', 1, NOW() - INTERVAL 2 DAY, NULL, 1, 'application/pdf', 0),
(UUID(), 3, 3, 'Relaxation Exercise Video', 'A video demonstrating breathing exercises', 'breathing_exercise.mp4', 'breathing_exercise.mp4', 15728640, 'mentalhealth-content', 'content/breathing_exercise.mp4', 3, NOW() - INTERVAL 3 DAY, NULL, 1, 'video/mp4', 0)
ON DUPLICATE KEY UPDATE 
    Title = VALUES(Title),
    Description = VALUES(Description),
    LastAccessedAt = VALUES(LastAccessedAt),
    IsActive = VALUES(IsActive);

-- Step 9: Seed some sample Emergency Incidents (for testing emergency system)
INSERT INTO EmergencyIncidents (PatientId, DoctorId, DeviceId, DeviceToken, EmergencyType, Message, Severity, LocationJson, VitalSignsJson, IpAddress, UserAgent, Timestamp, IsAcknowledged, AcknowledgedAt, DoctorResponse, ActionTaken, Resolution, ResolvedAt) VALUES
(3, 2, 'device_001', 'token_001', 'PanicAttack', 'Patient experiencing severe panic attack, needs immediate assistance', 'High', '{"lat": 37.7749, "lng": -122.4194, "address": "San Francisco, CA"}', '{"heart_rate": 120, "blood_pressure": "140/90"}', '192.168.1.100', 'MentalHealthApp/1.0', NOW() - INTERVAL 1 HOUR, 1, NOW() - INTERVAL 50 MINUTE, 'I\'m on my way. Please try the breathing exercises we discussed.', 'Dispatched emergency response team', 'Patient stabilized with breathing exercises', NOW() - INTERVAL 30 MINUTE)
ON DUPLICATE KEY UPDATE 
    IsAcknowledged = VALUES(IsAcknowledged),
    AcknowledgedAt = VALUES(AcknowledgedAt),
    DoctorResponse = VALUES(DoctorResponse),
    ActionTaken = VALUES(ActionTaken),
    Resolution = VALUES(Resolution),
    ResolvedAt = VALUES(ResolvedAt);

-- Step 10: Seed some sample SMS Messages
INSERT INTO SmsMessages (SenderId, ReceiverId, Message, SentAt, IsRead, ReadAt) VALUES
(2, 3, 'Hi John, I wanted to check in on how you\'re feeling today. Please let me know if you need to talk.', NOW() - INTERVAL 2 HOUR, 1, NOW() - INTERVAL 1 HOUR),
(3, 2, 'Thank you Dr. Sarah. I\'m feeling better today after our session yesterday.', NOW() - INTERVAL 1 HOUR, 0, NULL),
(1, 2, 'System notification: New patient assignment - John Doe has been assigned to you.', NOW() - INTERVAL 1 DAY, 1, NOW() - INTERVAL 23 HOUR)
ON DUPLICATE KEY UPDATE 
    Message = VALUES(Message),
    IsRead = VALUES(IsRead),
    ReadAt = VALUES(ReadAt);

-- Verification queries
SELECT 'Seeding completed successfully!' as status;
SELECT 'Roles count:' as info, COUNT(*) as count FROM Roles;
SELECT 'Users count:' as info, COUNT(*) as count FROM Users;
SELECT 'ContentTypes count:' as info, COUNT(*) as count FROM ContentTypes;
SELECT 'Journal Entries count:' as info, COUNT(*) as count FROM JournalEntries;
SELECT 'Chat Sessions count:' as info, COUNT(*) as count FROM ChatSessions;
SELECT 'Chat Messages count:' as info, COUNT(*) as count FROM ChatMessages;
SELECT 'Contents count:' as info, COUNT(*) as count FROM Contents;
SELECT 'Emergency Incidents count:' as info, COUNT(*) as count FROM EmergencyIncidents;
SELECT 'SMS Messages count:' as info, COUNT(*) as count FROM SmsMessages;


------------------------------------------------

INSERT INTO SymptomOngoingStatus (Code, Label) VALUES
('ONGOING', 'Ongoing'),
('IMPROVING', 'Improving'),
('RESOLVED', 'Resolved'),
('WORSENING', 'Worsening'),
('UNKNOWN', 'Unknown')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);



-- Seeding lookup tables
-- Roles
INSERT INTO AccidentParticipantRole (Code, Label) VALUES
('DRIVER', 'Driver'),
('PASSENGER', 'Passenger'),
('PEDESTRIAN', 'Pedestrian')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

-- Vehicle disposition
INSERT INTO VehicleDisposition (Code, Label) VALUES
('TOWED', 'Vehicle was towed'),
('DROVE_AWAY', 'Able to drive away')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

-- Transport method
INSERT INTO TransportToCareMethod (Code, Label) VALUES
('AMBULANCE', 'Taken by ambulance'),
('WENT_ON_OWN', 'Went on my own'),
('NOT_APPLICABLE', 'Not applicable / did not go')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

-- Medical attention type
INSERT INTO MedicalAttentionType (Code, Label) VALUES
('EMERGENCY_ROOM', 'Emergency Room'),
('URGENT_CARE', 'Urgent Care'),
('DOCTORS_OFFICE', "Doctor's Office"),
('NOT_YET', 'Not yet')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

-- Seed US States
INSERT INTO States (Code, Name) VALUES
('AL','Alabama'),('AK','Alaska'),('AZ','Arizona'),('AR','Arkansas'),('CA','California'),
('CO','Colorado'),('CT','Connecticut'),('DE','Delaware'),('DC','District of Columbia'),
('FL','Florida'),('GA','Georgia'),('HI','Hawaii'),('ID','Idaho'),('IL','Illinois'),
('IN','Indiana'),('IA','Iowa'),('KS','Kansas'),('KY','Kentucky'),('LA','Louisiana'),
('ME','Maine'),('MD','Maryland'),('MA','Massachusetts'),('MI','Michigan'),('MN','Minnesota'),
('MS','Mississippi'),('MO','Missouri'),('MT','Montana'),('NE','Nebraska'),('NV','Nevada'),
('NH','New Hampshire'),('NJ','New Jersey'),('NM','New Mexico'),('NY','New York'),
('NC','North Carolina'),('ND','North Dakota'),('OH','Ohio'),('OK','Oklahoma'),('OR','Oregon'),
('PA','Pennsylvania'),('RI','Rhode Island'),('SC','South Carolina'),('SD','South Dakota'),
('TN','Tennessee'),('TX','Texas'),('UT','Utah'),('VT','Vermont'),('VA','Virginia'),
('WA','Washington'),('WV','West Virginia'),('WI','Wisconsin'),('WY','Wyoming')
ON DUPLICATE KEY UPDATE Name = VALUES(Name);

-- ============================================================================
-- Seed AI Model Configurations and Chains
-- ============================================================================
-- This seeds the AI model configurations for Ollama and sets up the chained AI workflow

-- Insert AI Model Configs for Ollama (Primary and Secondary models)
-- Using Qwen3 models: qwen2.5:8b-instruct for primary, qwen2.5:4b-instruct for secondary
-- If these models don't work, you can update to use tinyllama as fallback
INSERT INTO AIModelConfigs (Id, ModelName, ModelType, Provider, ApiEndpoint, ApiKeyConfigKey, SystemPrompt, Context, DisplayOrder, IsActive, CreatedAt, UpdatedAt) VALUES
(1, 'Qwen3-8b', 'Primary', 'Ollama', 'tinyllama', NULL, 'You are a specialized medical AI assistant. Your task is to generate structured clinical note drafts from patient encounter data. Analyze the provided patient information and create a well-organized, professional clinical note that includes: Chief Complaint, History of Present Illness, Review of Systems, Assessment, and Plan. Ensure the note is clear, concise, and follows standard medical documentation practices.', 'ClinicalNote', 1, 1, NOW(), NULL),
(2, 'Qwen3-4b', 'Secondary', 'Ollama', 'tinyllama', NULL, 'You are a medical AI assistant specialized in clinical decision support. Your task is to analyze clinical notes and patient encounters to identify: 1) Possible missed considerations or diagnoses that should be evaluated, 2) Recommended follow-up actions or tests, 3) Potential drug interactions or contraindications, 4) Red flags or warning signs that require attention. Provide a structured list of considerations and recommendations based on evidence-based medicine principles.', 'ClinicalNote', 2, 1, NOW(), NULL)
ON DUPLICATE KEY UPDATE
    ModelType = VALUES(ModelType),
    Provider = VALUES(Provider),
    ApiEndpoint = VALUES(ApiEndpoint),
    SystemPrompt = VALUES(SystemPrompt),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();

-- Insert AI Model Chain for ClinicalNote context (Qwen3-8b-Qwen3-4b Chain)
INSERT INTO AIModelChains (Id, ChainName, Context, Description, PrimaryModelId, SecondaryModelId, ChainOrder, IsActive, CreatedAt, UpdatedAt) VALUES
(1, 'Qwen3-8b-Qwen3-4b Chain', 'ClinicalNote', 'Chained AI workflow: Primary model (Qwen3-8b) generates structured clinical note draft from patient encounter, then Secondary model (Qwen3-4b) analyzes the note to identify missed considerations and follow-up actions.', 1, 2, 1, 1, NOW(), NULL)
ON DUPLICATE KEY UPDATE
    ChainName = VALUES(ChainName),
    Description = VALUES(Description),
    PrimaryModelId = VALUES(PrimaryModelId),
    SecondaryModelId = VALUES(SecondaryModelId),
    ChainOrder = VALUES(ChainOrder),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();