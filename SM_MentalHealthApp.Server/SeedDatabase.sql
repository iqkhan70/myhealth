-- Comprehensive Database Seeding Script
-- This script seeds all essential tables with initial data

-- Step 1: Seed Roles table
INSERT INTO Roles (Id, Name, Description, IsActive, CreatedAt) VALUES
(1, 'Patient', 'Regular patients who use the app for self-care and journaling', 1, NOW()),
(2, 'Doctor', 'Medical professionals who provide care and consultations', 1, NOW()),
(3, 'Admin', 'System administrators who manage users and system settings', 1, NOW())
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
INSERT INTO Users (Id, FirstName, LastName, Email, PasswordHash, DateOfBirth, Gender, MobilePhone, RoleId, CreatedAt, LastLoginAt, IsActive, IsFirstLogin, MustChangePassword, Specialization, LicenseNumber) VALUES
(1, 'Admin', 'User', 'admin@mentalhealth.com', '$2a$11$demo123hash', '1980-01-01', 'Other', '+1234567890', 3, NOW(), NULL, 1, 0, 0, NULL, NULL),
(2, 'Dr. Sarah', 'Johnson', 'dr.sarah@mentalhealth.com', '$2a$11$demo123hash', '1975-05-15', 'Female', '+1234567891', 2, NOW(), NULL, 1, 0, 0, 'Psychiatry', 'MD123456'),
(3, 'John', 'Doe', 'john@doe.com', '$2a$11$demo123hash', '1990-08-20', 'Male', '+1234567892', 1, NOW(), NULL, 1, 1, 1, NULL, NULL)
ON DUPLICATE KEY UPDATE 
    FirstName = VALUES(FirstName),
    LastName = VALUES(LastName),
    PasswordHash = VALUES(PasswordHash),
    DateOfBirth = VALUES(DateOfBirth),
    Gender = VALUES(Gender),
    MobilePhone = VALUES(MobilePhone),
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
INSERT INTO JournalEntries (UserId, EnteredByUserId, Text, AIResponse, Mood, CreatedAt) VALUES
(3, 3, 'Feeling anxious about work today. Had a difficult meeting with my manager.', 'It sounds like you had a challenging day at work. Anxiety about work situations is very common. Consider taking some deep breaths and maybe talking to someone you trust about your concerns.', 'Anxious', NOW() - INTERVAL 1 DAY),
(3, 3, 'Much better day today! Went for a walk in the park and felt more relaxed.', 'That\'s wonderful to hear! Physical activity and time in nature can be very therapeutic. Keep up the great work with self-care!', 'Happy', NOW() - INTERVAL 2 DAY),
(3, 2, 'Patient reported improved sleep patterns this week. Discussed stress management techniques.', 'Great progress on sleep patterns! Stress management is crucial for mental health. Continue monitoring and provide ongoing support.', 'Neutral', NOW() - INTERVAL 3 DAY)
ON DUPLICATE KEY UPDATE 
    Text = VALUES(Text),
    AIResponse = VALUES(AIResponse),
    Mood = VALUES(Mood);

-- Step 6: Seed some sample Chat Sessions
INSERT INTO ChatSessions (SessionId, UserId, PatientId, CreatedAt, LastActivityAt, IsActive, MessageCount, PrivacyLevel, Summary) VALUES
('session_001', 2, 3, NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 HOUR, 1, 5, 'Private', 'Discussion about anxiety management and coping strategies'),
('session_002', 3, 3, NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 HOUR, 1, 3, 'Private', 'Self-reflection on daily mood and activities')
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
INSERT INTO Contents (ContentGuid, PatientId, AddedByUserId, Title, Description, FileName, OriginalFileName, ContentType, FileSizeBytes, S3Bucket, S3Key, ContentTypeModelId, CreatedAt, LastAccessedAt, IsActive) VALUES
(UUID(), 3, 3, 'My Daily Mood Journal', 'A document tracking my daily emotional state', 'mood_journal.pdf', 'mood_journal.pdf', 'application/pdf', 1024000, 'mentalhealth-content', 'content/mood_journal.pdf', 1, NOW() - INTERVAL 1 DAY, NULL, 1),
(UUID(), 3, 2, 'Therapy Session Notes', 'Notes from today\'s therapy session', 'therapy_notes.pdf', 'therapy_notes.pdf', 'application/pdf', 512000, 'mentalhealth-content', 'content/therapy_notes.pdf', 1, NOW() - INTERVAL 2 DAY, NULL, 1),
(UUID(), 3, 3, 'Relaxation Exercise Video', 'A video demonstrating breathing exercises', 'breathing_exercise.mp4', 'breathing_exercise.mp4', 'video/mp4', 15728640, 'mentalhealth-content', 'content/breathing_exercise.mp4', 3, NOW() - INTERVAL 3 DAY, NULL, 1)
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
