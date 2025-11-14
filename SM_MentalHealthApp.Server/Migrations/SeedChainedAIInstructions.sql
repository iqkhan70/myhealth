-- SQL Script to seed AI Instructions for Chained AI (Primary + Secondary models)
-- This script adds detailed instructions for Clinical Note generation and analysis
-- Works with any Ollama models configured in AIModelConfigs (e.g., Qwen2.5, DeepSeek-R1)

-- First, ensure we have the categories
INSERT INTO AIInstructionCategories (Name, Description, Context, DisplayOrder, IsActive, CreatedAt)
VALUES 
    ('Clinical Note Generation', 'Instructions for primary model to generate structured clinical notes from patient encounters', 'ClinicalNote', 1, TRUE, NOW()),
    ('Clinical Analysis', 'Instructions for secondary model to analyze encounters and identify missed considerations', 'ClinicalNote', 2, TRUE, NOW())
ON DUPLICATE KEY UPDATE
    Description = VALUES(Description),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();

-- Get category IDs (assuming they exist or were just created)
SET @clinicalNoteCategoryId = (SELECT Id FROM AIInstructionCategories WHERE Name = 'Clinical Note Generation' AND Context = 'ClinicalNote' LIMIT 1);
SET @analysisCategoryId = (SELECT Id FROM AIInstructionCategories WHERE Name = 'Clinical Analysis' AND Context = 'ClinicalNote' LIMIT 1);

-- Insert instructions for Primary Model (Clinical Note Generation)
INSERT INTO AIInstructions (CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt)
VALUES 
    (
        @clinicalNoteCategoryId,
        'You are a medical AI assistant specialized in generating structured clinical notes. Your task is to create a well-organized, professional clinical note from patient encounter data. The note should follow standard medical documentation format with clear sections.',
        'Primary Model Role Definition',
        1,
        TRUE,
        NOW()
    ),
    (
        @clinicalNoteCategoryId,
        'Structure your clinical note with the following sections:\n1. Chief Complaint (CC): Patient\'s primary reason for visit\n2. History of Present Illness (HPI): Detailed narrative of current symptoms\n3. Review of Systems (ROS): Relevant system reviews\n4. Assessment: Clinical impression and differential diagnoses\n5. Plan: Treatment plan, medications, follow-up instructions',
        'Note Structure Requirements',
        2,
        TRUE,
        NOW()
    ),
    (
        @clinicalNoteCategoryId,
        'Use medical terminology appropriately. Be concise but comprehensive. Include relevant clinical details, vital signs if available, and any pertinent medical history. Maintain professional tone suitable for medical documentation.',
        'Writing Guidelines',
        3,
        TRUE,
        NOW()
    ),
    (
        @clinicalNoteCategoryId,
        'If patient context is provided, incorporate relevant past medical history, medications, and previous encounters into your note. Ensure continuity of care is reflected in the documentation.',
        'Context Integration',
        4,
        TRUE,
        NOW()
    )
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    Title = VALUES(Title),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();

-- Insert instructions for Secondary Model (Analysis)
INSERT INTO AIInstructions (CategoryId, Content, Title, DisplayOrder, IsActive, CreatedAt)
VALUES 
    (
        @analysisCategoryId,
        'You are a medical AI assistant specialized in clinical decision support. Your task is to analyze patient encounters and structured notes to identify potential missed considerations, overlooked diagnoses, or important follow-up actions.',
        'Secondary Model Role Definition',
        1,
        TRUE,
        NOW()
    ),
    (
        @analysisCategoryId,
        'Analyze the encounter data and structured note carefully. Look for:\n- Potential diagnoses that may have been missed\n- Important symptoms or findings that need further investigation\n- Drug interactions or contraindications\n- Necessary follow-up tests or referrals\n- Red flags or warning signs that require immediate attention',
        'Analysis Focus Areas',
        2,
        TRUE,
        NOW()
    ),
    (
        @analysisCategoryId,
        'Format your response with two clear sections:\n\n**Missed Considerations:**\nList any potential issues, diagnoses, or considerations that may have been overlooked. Be specific and evidence-based.\n\n**Follow-up Actions:**\nProvide a numbered or bulleted list of recommended follow-up actions, tests, referrals, or monitoring that should be considered.',
        'Response Format',
        3,
        TRUE,
        NOW()
    ),
    (
        @analysisCategoryId,
        'Be thorough but prioritize. Focus on clinically significant findings. If patient context is provided, consider the full medical history when identifying potential issues. Always prioritize patient safety and evidence-based recommendations.',
        'Analysis Guidelines',
        4,
        TRUE,
        NOW()
    ),
    (
        @analysisCategoryId,
        'Compare the original encounter data with the structured note. Identify any discrepancies, missing information, or areas where additional detail or investigation might be warranted.',
        'Comparison Analysis',
        5,
        TRUE,
        NOW()
    )
ON DUPLICATE KEY UPDATE
    Content = VALUES(Content),
    Title = VALUES(Title),
    DisplayOrder = VALUES(DisplayOrder),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();

-- Verify the inserts
SELECT 
    c.Name AS CategoryName,
    c.Context,
    i.Title,
    i.DisplayOrder,
    i.IsActive,
    LEFT(i.Content, 100) AS ContentPreview
FROM AIInstructions i
INNER JOIN AIInstructionCategories c ON i.CategoryId = c.Id
WHERE c.Context = 'ClinicalNote'
ORDER BY c.Context, i.DisplayOrder;

