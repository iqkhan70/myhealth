-- =====================================================
-- Seed Knowledge Base Data
-- This script populates the knowledge base with initial categories and entries
-- Run this after completing the migration
-- =====================================================

-- Step 1: Insert Knowledge Base Categories
INSERT INTO KnowledgeBaseCategories (Name, Description, DisplayOrder, IsActive, CreatedAt)
VALUES
    ('General Health', 'General health and wellness information', 1, TRUE, NOW()),
    ('Mental Health', 'Mental health and wellness topics', 2, TRUE, NOW()),
    ('Medications', 'Information about medications and prescriptions', 3, TRUE, NOW()),
    ('Symptoms', 'Common symptoms and their meanings', 4, TRUE, NOW()),
    ('Emergency', 'Emergency and urgent care information', 5, TRUE, NOW()),
    ('Appointments', 'Information about appointments and scheduling', 6, TRUE, NOW()),
    ('General Questions', 'General questions and answers', 7, TRUE, NOW())
ON DUPLICATE KEY UPDATE Name = Name;

-- Step 2: Insert Knowledge Base Entries
-- Note: Replace CategoryId values with actual IDs from the categories above
-- You may need to adjust these based on the actual IDs inserted

-- General Health Entries
INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'General Health' LIMIT 1) AS CategoryId,
    'Blood Pressure Information' AS Title,
    'Normal blood pressure is typically around 120/80 mmHg. High blood pressure (hypertension) is 140/90 or higher. Low blood pressure (hypotension) is below 90/60. If you experience symptoms like dizziness, fainting, or severe headaches, please contact your healthcare provider immediately.' AS Content,
    'blood pressure,hypertension,hypotension,high bp,low bp' AS Keywords,
    5 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'Blood Pressure Information');

INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'General Health' LIMIT 1) AS CategoryId,
    'Heart Rate Information' AS Title,
    'A normal resting heart rate for adults is typically between 60-100 beats per minute (bpm). Factors like age, fitness level, and medications can affect heart rate. If you experience a heart rate consistently above 100 bpm at rest or below 60 bpm with symptoms like dizziness, please consult your healthcare provider.' AS Content,
    'heart rate,pulse,bpm,heartbeat,tachycardia,bradycardia' AS Keywords,
    5 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'Heart Rate Information');

INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'General Health' LIMIT 1) AS CategoryId,
    'Glucose (Blood Sugar) Information' AS Title,
    'Normal blood glucose (blood sugar) levels vary depending on when you last ate:
- **Fasting (before eating)**: 70-100 mg/dL (3.9-5.6 mmol/L) is considered normal
- **After meals (2 hours)**: Less than 140 mg/dL (7.8 mmol/L) is considered normal
- **Random glucose**: 70-140 mg/dL (3.9-7.8 mmol/L) is typically normal

**High glucose (hyperglycemia)**: Fasting levels above 126 mg/dL (7.0 mmol/L) or random levels above 200 mg/dL (11.1 mmol/L) may indicate diabetes and require medical evaluation.

**Low glucose (hypoglycemia)**: Levels below 70 mg/dL (3.9 mmol/L) can cause symptoms like shakiness, sweating, confusion, and require immediate treatment.

If you have concerns about your blood glucose levels, please consult with your healthcare provider for proper evaluation and management.' AS Content,
    'glucose,blood sugar,blood glucose,normal glucose,glucose levels,normal values of glucose,blood sugar levels,normal blood sugar' AS Keywords,
    6 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'Glucose (Blood Sugar) Information');

-- Mental Health Entries
INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'Mental Health' LIMIT 1) AS CategoryId,
    'Anxiety Information' AS Title,
    'Anxiety is a normal response to stress, but when it becomes excessive or persistent, it may indicate an anxiety disorder. Common symptoms include excessive worry, restlessness, difficulty concentrating, and physical symptoms like rapid heartbeat or sweating. If anxiety is interfering with your daily life, please speak with your healthcare provider or mental health professional.' AS Content,
    'anxiety,anxious,worry,panic,stress,anxiety disorder' AS Keywords,
    8 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'Anxiety Information');

INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'Mental Health' LIMIT 1) AS CategoryId,
    'Depression Information' AS Title,
    'Depression is a common mental health condition characterized by persistent feelings of sadness, loss of interest in activities, changes in sleep or appetite, and difficulty concentrating. If you are experiencing symptoms of depression that last for more than two weeks, please reach out to your healthcare provider or a mental health professional. Remember, help is available and treatment can be effective.' AS Content,
    'depression,depressed,sadness,feeling down,mood,mental health' AS Keywords,
    8 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'Depression Information');

INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'Mental Health' LIMIT 1) AS CategoryId,
    'Sleep and Mental Health' AS Title,
    'Sleep plays a crucial role in mental health. Most adults need 7-9 hours of sleep per night. Poor sleep can worsen anxiety and depression, while good sleep hygiene can improve mental well-being. Tips for better sleep include maintaining a regular sleep schedule, creating a relaxing bedtime routine, avoiding screens before bed, and keeping your bedroom cool and dark.' AS Content,
    'sleep,insomnia,sleeping,rest,sleep schedule,sleep hygiene' AS Keywords,
    6 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'Sleep and Mental Health');

-- Symptoms Entries
INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'Symptoms' LIMIT 1) AS CategoryId,
    'Headache Information' AS Title,
    'Headaches can have many causes including stress, dehydration, lack of sleep, or underlying medical conditions. Most headaches are not serious, but you should seek immediate medical attention if you experience a sudden severe headache, headache with fever or stiff neck, or headache after a head injury. For mild headaches, rest, hydration, and over-the-counter pain relievers may help.' AS Content,
    'headache,head pain,migraine,head hurts' AS Keywords,
    4 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'Headache Information');

INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'Symptoms' LIMIT 1) AS CategoryId,
    'Chest Pain Information' AS Title,
    'Chest pain can be caused by various conditions, some serious. If you experience sudden, severe chest pain, especially with shortness of breath, sweating, or pain radiating to your arm or jaw, call emergency services immediately as this could indicate a heart attack. For less severe chest pain, it is still important to consult with your healthcare provider to determine the cause.' AS Content,
    'chest pain,chest discomfort,heart pain' AS Keywords,
    9 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'Chest Pain Information');

-- Emergency Entries
INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'Emergency' LIMIT 1) AS CategoryId,
    'When to Call Emergency Services' AS Title,
    'Call emergency services (911) immediately if you or someone else experiences: severe chest pain or pressure, difficulty breathing, severe allergic reaction, signs of stroke (sudden weakness, confusion, trouble speaking), severe injury, or thoughts of self-harm. For mental health emergencies, you can also call the National Suicide Prevention Lifeline at 988.' AS Content,
    'emergency,911,urgent,emergency services,call 911' AS Keywords,
    10 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'When to Call Emergency Services');

-- Appointments Entries
INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'Appointments' LIMIT 1) AS CategoryId,
    'How to Schedule an Appointment' AS Title,
    'You can schedule an appointment through this application by navigating to the Appointments page. Select your preferred doctor, date, and time. You will receive a confirmation and reminder notifications. If you need to reschedule or cancel, you can do so through the same page. For urgent matters, please contact your doctor''s office directly.' AS Content,
    'appointment,schedule,booking,appointment booking,make appointment' AS Keywords,
    3 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'How to Schedule an Appointment');

-- General Questions Entries
INSERT INTO KnowledgeBaseEntries (CategoryId, Title, Content, Keywords, Priority, UseAsDirectResponse, IsActive, CreatedAt)
SELECT 
    (SELECT Id FROM KnowledgeBaseCategories WHERE Name = 'General Questions' LIMIT 1) AS CategoryId,
    'About This Application' AS Title,
    'This is a mental health application designed to help you track your health, communicate with your healthcare providers, and access health information. You can use the journal feature to record your thoughts and moods, upload medical documents, schedule appointments, and chat with our AI assistant for health-related questions. For specific medical advice, always consult with your healthcare provider.' AS Content,
    'about,help,what is this,application,app,how to use' AS Keywords,
    2 AS Priority,
    TRUE AS UseAsDirectResponse,
    TRUE AS IsActive,
    NOW() AS CreatedAt
WHERE NOT EXISTS (SELECT 1 FROM KnowledgeBaseEntries WHERE Title = 'About This Application');

-- Step 3: Verify the seeded data
SELECT 'Knowledge Base Categories:' AS Status;
SELECT Id, Name, Description, DisplayOrder, IsActive 
FROM KnowledgeBaseCategories 
ORDER BY DisplayOrder;

SELECT 'Knowledge Base Entries Count:' AS Status;
SELECT 
    c.Name AS CategoryName,
    COUNT(e.Id) AS EntryCount
FROM KnowledgeBaseCategories c
LEFT JOIN KnowledgeBaseEntries e ON c.Id = e.CategoryId AND e.IsActive = TRUE
WHERE c.IsActive = TRUE
GROUP BY c.Id, c.Name
ORDER BY c.DisplayOrder;

SELECT 'Sample Entries:' AS Status;
SELECT 
    e.Id,
    c.Name AS Category,
    e.Title,
    e.Priority,
    e.Keywords
FROM KnowledgeBaseEntries e
INNER JOIN KnowledgeBaseCategories c ON e.CategoryId = c.Id
WHERE e.IsActive = TRUE
ORDER BY e.Priority DESC, e.Title
LIMIT 10;

