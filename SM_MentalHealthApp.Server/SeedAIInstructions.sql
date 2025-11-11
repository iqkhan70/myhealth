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
(10, 2, 'Clinical notes take precedence over other data when they indicate concerns', NULL, 5, 1, NOW()),
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
(16, 3, 'Review journal entries and mood patterns', NULL, 1, 1, NOW()),
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

