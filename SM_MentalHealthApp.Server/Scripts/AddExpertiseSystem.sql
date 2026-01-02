-- Add Expertise System for SMEs and Service Requests
-- This allows coordinators to quickly find SMEs with matching expertise for Service Requests

-- Step 1: Create Expertise lookup table
CREATE TABLE IF NOT EXISTS Expertise (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    UpdatedAt DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
    INDEX IX_Expertise_Name (Name),
    INDEX IX_Expertise_IsActive (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Step 2: Create SmeExpertise junction table (SME ↔ Expertise many-to-many)
CREATE TABLE IF NOT EXISTS SmeExpertise (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    SmeUserId INT NOT NULL,
    ExpertiseId INT NOT NULL,
    IsPrimary TINYINT(1) NOT NULL DEFAULT 0,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    INDEX IX_SmeExpertise_SmeUserId (SmeUserId),
    INDEX IX_SmeExpertise_ExpertiseId (ExpertiseId),
    INDEX IX_SmeExpertise_IsActive (IsActive),
    UNIQUE KEY UQ_SmeExpertise_Sme_Expertise (SmeUserId, ExpertiseId),
    CONSTRAINT FK_SmeExpertise_User FOREIGN KEY (SmeUserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SmeExpertise_Expertise FOREIGN KEY (ExpertiseId) REFERENCES Expertise(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Step 3: Create ServiceRequestExpertise junction table (ServiceRequest ↔ Expertise many-to-many)
CREATE TABLE IF NOT EXISTS ServiceRequestExpertise (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    ServiceRequestId INT NOT NULL,
    ExpertiseId INT NOT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    INDEX IX_ServiceRequestExpertise_ServiceRequestId (ServiceRequestId),
    INDEX IX_ServiceRequestExpertise_ExpertiseId (ExpertiseId),
    UNIQUE KEY UQ_ServiceRequestExpertise_SR_Expertise (ServiceRequestId, ExpertiseId),
    CONSTRAINT FK_ServiceRequestExpertise_ServiceRequest FOREIGN KEY (ServiceRequestId) REFERENCES ServiceRequests(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ServiceRequestExpertise_Expertise FOREIGN KEY (ExpertiseId) REFERENCES Expertise(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Step 4: Insert some default expertise categories
INSERT INTO Expertise (Id, Name, Description, IsActive, CreatedAt) VALUES
(1, 'General', 'General service requests', 1, NOW()),
(2, 'Medical', 'Medical and healthcare services', 1, NOW()),
(3, 'Legal', 'Legal services and consultation', 1, NOW()),
(4, 'Therapy', 'Therapy and counseling services', 1, NOW()),
(5, 'Consultation', 'General consultation services', 1, NOW()),
(6, 'Follow-up', 'Follow-up services', 1, NOW()),
(7, 'Emergency', 'Emergency services', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Name = VALUES(Name),
    Description = VALUES(Description),
    IsActive = VALUES(IsActive);

