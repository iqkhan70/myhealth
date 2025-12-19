-- US States (for dropdown + display)
CREATE TABLE IF NOT EXISTS States (
  Code CHAR(2) NOT NULL PRIMARY KEY,
  Name VARCHAR(50) NOT NULL UNIQUE
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Driver / Passenger / Pedestrian
CREATE TABLE IF NOT EXISTS AccidentParticipantRole (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Code VARCHAR(30) NOT NULL UNIQUE,
  Label VARCHAR(50) NOT NULL
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Towed vs Drove away
CREATE TABLE IF NOT EXISTS VehicleDisposition (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Code VARCHAR(30) NOT NULL UNIQUE,
  Label VARCHAR(50) NOT NULL
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Ambulance vs went on own (or none)
CREATE TABLE IF NOT EXISTS TransportToCareMethod (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Code VARCHAR(30) NOT NULL UNIQUE,
  Label VARCHAR(80) NOT NULL
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Where did they seek care
CREATE TABLE IF NOT EXISTS MedicalAttentionType (
  Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  Code VARCHAR(30) NOT NULL UNIQUE,
  Label VARCHAR(80) NOT NULL
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- Seeding lookup tables
-- Roles
INSERT INTO AccidentParticipantRole (Code, Label) VALUES
('DRIVER', 'Driver'),
('PASSENGER', 'Passenger'),
('PEDESTRIAN', 'Pedestrian')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

-- Vehicle disposition
INSERT INTO VehicleDisposition (Code, Label) VALUES
('TOWED', 'Vehicle was towed'),
('DROVE_AWAY', 'Able to drive away')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

-- Transport method
INSERT INTO TransportToCareMethod (Code, Label) VALUES
('AMBULANCE', 'Taken by ambulance'),
('WENT_ON_OWN', 'Went on my own'),
('NOT_APPLICABLE', 'Not applicable / did not go')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

-- Medical attention type
INSERT INTO MedicalAttentionType (Code, Label) VALUES
('EMERGENCY_ROOM', 'Emergency Room'),
('URGENT_CARE', 'Urgent Care'),
('DOCTORS_OFFICE', "Doctor's Office"),
('NOT_YET', 'Not yet')
ON DUPLICATE KEY UPDATE Label = VALUES(Label);

-- Seed US States
INSERT INTO States (Code, Name) VALUES
('AL','Alabama'),('AK','Alaska'),('AZ','Arizona'),('AR','Arkansas'),('CA','California'),
('CO','Colorado'),('CT','Connecticut'),('DE','Delaware'),('DC','District of Columbia'),
('FL','Florida'),('GA','Georgia'),('HI','Hawaii'),('ID','Idaho'),('IL','Illinois'),
('IN','Indiana'),('IA','Iowa'),('KS','Kansas'),('KY','Kentucky'),('LA','Louisiana'),
('ME','Maine'),('MD','Maryland'),('MA','Massachusetts'),('MI','Michigan'),('MN','Minnesota'),
('MS','Mississippi'),('MO','Missouri'),('MT','Montana'),('NE','Nebraska'),('NV','Nevada'),
('NH','New Hampshire'),('NJ','New Jersey'),('NM','New Mexico'),('NY','New York'),
('NC','North Carolina'),('ND','North Dakota'),('OH','Ohio'),('OK','Oklahoma'),('OR','Oregon'),
('PA','Pennsylvania'),('RI','Rhode Island'),('SC','South Carolina'),('SD','South Dakota'),
('TN','Tennessee'),('TX','Texas'),('UT','Utah'),('VT','Vermont'),('VA','Virginia'),
('WA','Washington'),('WV','West Virginia'),('WI','Wisconsin'),('WY','Wyoming')
ON DUPLICATE KEY UPDATE Name = VALUES(Name);

-- Add columns to Users
ALTER TABLE Users
  -- If you want "State" on the user profile (residence)
  ADD COLUMN ResidenceStateCode CHAR(2) NULL AFTER LastName,

  -- If you want state tied to the accident (often different than residence)
  ADD COLUMN AccidentStateCode CHAR(2) NULL AFTER AccidentAddress,

  -- Dropdown-backed fields
  ADD COLUMN AccidentParticipantRoleId INT NULL AFTER AccidentDate,
  ADD COLUMN VehicleDispositionId INT NULL AFTER VehicleDetails,
  ADD COLUMN TransportToCareMethodId INT NULL AFTER DateReported,
  ADD COLUMN MedicalAttentionTypeId INT NULL AFTER TransportToCareMethodId,

  -- Yes/No fields (use TINYINT(1) for MySQL boolean style)
  ADD COLUMN PoliceInvolvement TINYINT(1) NULL AFTER PoliceCaseNumber,
  ADD COLUMN LostConsciousness TINYINT(1) NULL AFTER AccidentDetails,
  ADD COLUMN NeuroSymptoms TINYINT(1) NULL AFTER LostConsciousness,
  ADD COLUMN MusculoskeletalSymptoms TINYINT(1) NULL AFTER NeuroSymptoms,
  ADD COLUMN PsychologicalSymptoms TINYINT(1) NULL AFTER MusculoskeletalSymptoms,

  -- Notes for case manager (brief notation)
  ADD COLUMN SymptomsNotes VARCHAR(2000) NULL AFTER PsychologicalSymptoms,

  -- Insurance / attorney
  ADD COLUMN InsuranceContacted TINYINT(1) NULL AFTER LawyersInformation,
  ADD COLUMN RepresentedByAttorney TINYINT(1) NULL AFTER InsuranceContacted;


-- Foreign keys + helpful indexes
-- Indexes (fast joins + filtering)
CREATE INDEX IX_Users_ResidenceStateCode ON Users(ResidenceStateCode);
CREATE INDEX IX_Users_AccidentStateCode ON Users(AccidentStateCode);
CREATE INDEX IX_Users_AccidentParticipantRoleId ON Users(AccidentParticipantRoleId);
CREATE INDEX IX_Users_VehicleDispositionId ON Users(VehicleDispositionId);
CREATE INDEX IX_Users_TransportToCareMethodId ON Users(TransportToCareMethodId);
CREATE INDEX IX_Users_MedicalAttentionTypeId ON Users(MedicalAttentionTypeId);

-- Foreign keys (enforce valid dropdown values)
ALTER TABLE Users
  ADD CONSTRAINT FK_Users_ResidenceState
    FOREIGN KEY (ResidenceStateCode) REFERENCES States(Code),

  ADD CONSTRAINT FK_Users_AccidentState
    FOREIGN KEY (AccidentStateCode) REFERENCES States(Code),

  ADD CONSTRAINT FK_Users_AccidentParticipantRole
    FOREIGN KEY (AccidentParticipantRoleId) REFERENCES AccidentParticipantRole(Id),

  ADD CONSTRAINT FK_Users_VehicleDisposition
    FOREIGN KEY (VehicleDispositionId) REFERENCES VehicleDisposition(Id),

  ADD CONSTRAINT FK_Users_TransportToCareMethod
    FOREIGN KEY (TransportToCareMethodId) REFERENCES TransportToCareMethod(Id),

  ADD CONSTRAINT FK_Users_MedicalAttentionType
    FOREIGN KEY (MedicalAttentionTypeId) REFERENCES MedicalAttentionType(Id);


