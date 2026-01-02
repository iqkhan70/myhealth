-- Add SME Role to the database
-- This script adds the SME role (RoleId = 6) to the Roles table

INSERT INTO Roles (Id, Name, Description, IsActive, CreatedAt) VALUES
(6, 'SME', 'Subject Matter Experts who can create content for Service Requests they are assigned to', 1, NOW())
ON DUPLICATE KEY UPDATE 
    Description = VALUES(Description),
    IsActive = VALUES(IsActive),
    Name = VALUES(Name);

-- Verify the role was added
SELECT 'SME role added successfully!' as status;
SELECT * FROM Roles WHERE Id = 6;

