-- Data Recovery Script for ContentTypes Migration
-- This script helps recover data that may have been lost during the migration

-- Step 1: Check if the migration has already been applied
-- If ContentTypes table exists, we need to check if data was preserved

-- First, let's see what's in the Contents table currently
SELECT 'Current Contents table structure:' as info;
DESCRIBE Contents;

-- Check if ContentTypeModelId column exists and has data
SELECT 'Checking ContentTypeModelId column:' as info;
SELECT COUNT(*) as total_records, 
       COUNT(ContentTypeModelId) as records_with_content_type_id,
       MIN(ContentTypeModelId) as min_content_type_id,
       MAX(ContentTypeModelId) as max_content_type_id
FROM Contents;

-- Check if Type column still exists
SELECT 'Checking if Type column still exists:' as info;
SELECT COUNT(*) as records_with_type_column
FROM information_schema.columns 
WHERE table_name = 'Contents' 
AND column_name = 'Type' 
AND table_schema = DATABASE();

-- Step 2: If Type column still exists, preserve the data
-- This will only run if the Type column exists
SET @type_column_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_name = 'Contents' 
    AND column_name = 'Type' 
    AND table_schema = DATABASE()
);

-- If Type column exists, show the data distribution
SELECT 'Type column data distribution:' as info;
SELECT Type, COUNT(*) as count 
FROM Contents 
WHERE @type_column_exists > 0
GROUP BY Type 
ORDER BY Type;

-- Step 3: Check ContentTypes table
SELECT 'ContentTypes table data:' as info;
SELECT * FROM ContentTypes ORDER BY Id;

-- Step 4: If we need to recover data, here's the recovery process
-- This assumes the Type column still exists and ContentTypeModelId is empty or wrong

-- First, ensure ContentTypes table has the right data
INSERT IGNORE INTO ContentTypes (Id, Name, Description, Icon, IsActive, SortOrder, CreatedAt) VALUES
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

-- Step 5: If Type column exists, copy the data to ContentTypeModelId
-- This will only execute if both columns exist
UPDATE Contents 
SET ContentTypeModelId = Type 
WHERE @type_column_exists > 0 
AND Type IN (1, 2, 3, 4, 5)
AND (ContentTypeModelId IS NULL OR ContentTypeModelId = 0);

-- Step 6: Verify the recovery
SELECT 'After recovery - ContentTypeModelId distribution:' as info;
SELECT ContentTypeModelId, COUNT(*) as count 
FROM Contents 
GROUP BY ContentTypeModelId 
ORDER BY ContentTypeModelId;

-- Step 7: Show any records that couldn't be recovered
SELECT 'Records that couldn\'t be recovered (invalid Type values):' as info;
SELECT Id, Type, ContentTypeModelId, Title 
FROM Contents 
WHERE @type_column_exists > 0 
AND Type NOT IN (1, 2, 3, 4, 5);

-- Final verification
SELECT 'Final verification - all records should have valid ContentTypeModelId:' as info;
SELECT 
    COUNT(*) as total_records,
    COUNT(CASE WHEN ContentTypeModelId IN (1,2,3,4,5) THEN 1 END) as valid_content_type_ids,
    COUNT(CASE WHEN ContentTypeModelId NOT IN (1,2,3,4,5) OR ContentTypeModelId IS NULL THEN 1 END) as invalid_content_type_ids
FROM Contents;
