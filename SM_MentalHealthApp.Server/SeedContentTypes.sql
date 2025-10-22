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
