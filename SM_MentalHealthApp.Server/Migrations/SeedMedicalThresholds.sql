-- Seed Medical Thresholds
-- This script seeds the MedicalThresholds table with initial threshold values
-- for detecting critical medical conditions

-- Blood Pressure Thresholds
INSERT INTO MedicalThresholds (ParameterName, Unit, SeverityLevel, ThresholdValue, ComparisonOperator, SecondaryParameterName, SecondaryThresholdValue, SecondaryComparisonOperator, Description, Priority, IsActive, CreatedAt) VALUES
('Blood Pressure', 'mmHg', 'Critical', 180, '>=', 'Blood Pressure Diastolic', 110, '>=', 'Hypertensive crisis - immediate medical intervention required', 10, TRUE, NOW()),
('Blood Pressure', 'mmHg', 'High', 140, '>=', 'Blood Pressure Diastolic', 90, '>=', 'High blood pressure - requires immediate attention', 8, TRUE, NOW()),

-- Hemoglobin Thresholds
('Hemoglobin', 'g/dL', 'Critical', 7.0, '<', NULL, NULL, NULL, 'Severe anemia - blood transfusion may be required', 10, TRUE, NOW()),
('Hemoglobin', 'g/dL', 'Low', 10.0, '<', NULL, NULL, NULL, 'Moderate anemia - requires monitoring', 8, TRUE, NOW()),

-- Triglycerides Thresholds
('Triglycerides', 'mg/dL', 'Critical', 500, '>=', NULL, NULL, NULL, 'Extremely high - risk of pancreatitis', 10, TRUE, NOW()),
('Triglycerides', 'mg/dL', 'High', 200, '>=', NULL, NULL, NULL, 'High - requires dietary intervention', 8, TRUE, NOW())

ON DUPLICATE KEY UPDATE
    Description = VALUES(Description),
    Priority = VALUES(Priority),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();

