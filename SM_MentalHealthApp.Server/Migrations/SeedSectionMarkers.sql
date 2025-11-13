-- Seed Section Markers
-- This script seeds the SectionMarkers table with initial markers
-- for identifying different sections within input text

INSERT INTO SectionMarkers (Marker, Description, Category, Priority, IsActive, CreatedAt) VALUES
-- Major section markers (high priority)
('=== RECENT JOURNAL ENTRIES ===', 'Marks the start of recent journal entries section', 'Patient Data', 100, TRUE, NOW()),
('=== MEDICAL DATA SUMMARY ===', 'Marks the start of medical data summary section', 'Patient Data', 100, TRUE, NOW()),
('=== CURRENT MEDICAL STATUS ===', 'Marks the start of current medical status section', 'Patient Data', 100, TRUE, NOW()),
('=== HISTORICAL MEDICAL CONCERNS ===', 'Marks the start of historical medical concerns section', 'Patient Data', 100, TRUE, NOW()),
('=== HEALTH TREND ANALYSIS ===', 'Marks the start of health trend analysis section', 'Patient Data', 100, TRUE, NOW()),
('=== RECENT CLINICAL NOTES ===', 'Marks the start of recent clinical notes section', 'Patient Data', 100, TRUE, NOW()),
('=== RECENT CHAT HISTORY ===', 'Marks the start of recent chat history section', 'Patient Data', 100, TRUE, NOW()),
('=== RECENT EMERGENCY INCIDENTS ===', 'Marks the start of recent emergency incidents section', 'Emergency', 100, TRUE, NOW()),
('=== USER QUESTION ===', 'Marks the start of user question section', 'Instructions', 100, TRUE, NOW()),
('=== PROGRESSION ANALYSIS ===', 'Marks the start of progression analysis section', 'Patient Data', 100, TRUE, NOW()),
('=== INSTRUCTIONS FOR AI HEALTH CHECK ANALYSIS ===', 'Marks the start of AI health check instructions', 'Instructions', 100, TRUE, NOW()),

-- Secondary section markers (medium priority)
('Recent Patient Activity:', 'Alternative marker for recent patient activity', 'Patient Data', 50, TRUE, NOW()),
('Current Test Results', 'Alternative marker for current test results', 'Patient Data', 50, TRUE, NOW()),
('Latest Update:', 'Alternative marker for latest update', 'Patient Data', 50, TRUE, NOW()),
('Doctor asks:', 'Marks doctor questions in chat', 'Patient Data', 50, TRUE, NOW()),
('Patient asks:', 'Marks patient questions in chat', 'Patient Data', 50, TRUE, NOW()),

-- Medical resource markers
('**Medical Resource Information', 'Marks medical resource information section', 'Resources', 50, TRUE, NOW()),
('**Medical Facilities Search', 'Marks medical facilities search section', 'Resources', 50, TRUE, NOW()),

-- Emergency and data detection markers (lower priority, used for pattern matching)
('Fall', 'Detects fall incidents in emergency data', 'Emergency', 30, TRUE, NOW()),
('Session:', 'Marks session information', 'Patient Data', 30, TRUE, NOW()),
('Summary:', 'Marks summary sections', 'Patient Data', 30, TRUE, NOW()),
('Clinical Notes', 'Alternative marker for clinical notes', 'Patient Data', 30, TRUE, NOW()),
('Journal Entries', 'Alternative marker for journal entries', 'Patient Data', 30, TRUE, NOW()),
('Chat History', 'Alternative marker for chat history', 'Patient Data', 30, TRUE, NOW()),

-- Mood and activity patterns
('MOOD PATTERNS (Last 30 days):', 'Marks mood patterns section', 'Patient Data', 40, TRUE, NOW()),
('RECENT JOURNAL ENTRIES (Last 14 days):', 'Alternative marker for recent journal entries', 'Patient Data', 40, TRUE, NOW()),

-- AI Health Check marker
('AI Health Check for Patient', 'Default prompt for AI health check', 'Instructions', 20, TRUE, NOW())

ON DUPLICATE KEY UPDATE
    Description = VALUES(Description),
    Category = VALUES(Category),
    Priority = VALUES(Priority),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();

