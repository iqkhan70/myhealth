-- Seed Generic Question Patterns
-- This script seeds the GenericQuestionPatterns table with initial patterns
-- for detecting generic knowledge questions (not patient-specific concerns)

-- Insert generic question patterns
INSERT INTO GenericQuestionPatterns (Pattern, Description, Priority, IsActive, CreatedAt) VALUES
('what are normal', 'Matches questions asking about normal values', 10, TRUE, NOW()),
('what are the normal', 'Matches questions asking about normal values (with "the")', 10, TRUE, NOW()),
('what is normal', 'Matches questions asking what is normal', 10, TRUE, NOW()),
('what are critical', 'Matches questions asking about critical values', 10, TRUE, NOW()),
('what are serious', 'Matches questions asking about serious values', 10, TRUE, NOW()),
('what is a normal', 'Matches questions asking what is a normal value', 10, TRUE, NOW()),
('what are typical', 'Matches questions asking about typical values', 10, TRUE, NOW()),
('what is typical', 'Matches questions asking what is typical', 10, TRUE, NOW()),
('normal values of', 'Matches questions about normal values of something', 10, TRUE, NOW()),
('normal range of', 'Matches questions about normal range of something', 10, TRUE, NOW()),
('normal levels of', 'Matches questions about normal levels of something', 10, TRUE, NOW()),
('what does', 'Matches questions asking what something does', 5, TRUE, NOW()),
('how does', 'Matches questions asking how something works', 5, TRUE, NOW()),
('explain', 'Matches questions asking to explain something', 5, TRUE, NOW()),
('tell me about', 'Matches questions asking to tell about something', 5, TRUE, NOW()),
('what is', 'Matches questions asking what something is', 5, TRUE, NOW()),
('what are', 'Matches questions asking what things are', 5, TRUE, NOW())
ON DUPLICATE KEY UPDATE
    Description = VALUES(Description),
    Priority = VALUES(Priority),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();

