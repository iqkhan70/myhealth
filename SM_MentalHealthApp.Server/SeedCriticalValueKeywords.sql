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

