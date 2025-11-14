-- Update AI Model Configurations to use Qwen3 models
-- This script updates the existing model configurations to match your installed Ollama models
-- Models available: qwen3:4b, qwen3:8b, tinyllama:latest

-- Update Primary model configuration to use qwen3:8b (better for structured output)
UPDATE `AIModelConfigs`
SET 
    `ModelName` = 'Qwen3-8b-NoteGenerator',
    `ApiEndpoint` = 'qwen3:8b',
    `UpdatedAt` = NOW()
WHERE `ModelName` = 'Qwen2.5-NoteGenerator' AND `Context` = 'ClinicalNote';

-- If the above didn't match, try to find any Primary model for ClinicalNote context
UPDATE `AIModelConfigs`
SET 
    `ModelName` = 'Qwen3-8b-NoteGenerator',
    `ApiEndpoint` = 'qwen3:8b',
    `UpdatedAt` = NOW()
WHERE `ModelType` = 'Primary' AND `Context` = 'ClinicalNote' AND `Provider` = 'Ollama';

-- Update Secondary model configuration to use qwen3:4b (faster for analysis)
UPDATE `AIModelConfigs`
SET 
    `ModelName` = 'Qwen3-4b-Analyzer',
    `ApiEndpoint` = 'qwen3:4b',
    `UpdatedAt` = NOW()
WHERE `ModelName` = 'DeepSeek-Analyzer' AND `Context` = 'ClinicalNote';

-- If the above didn't match, try to find any Secondary model for ClinicalNote context
UPDATE `AIModelConfigs`
SET 
    `ModelName` = 'Qwen3-4b-Analyzer',
    `ApiEndpoint` = 'qwen3:4b',
    `UpdatedAt` = NOW()
WHERE `ModelType` = 'Secondary' AND `Context` = 'ClinicalNote' AND `Provider` = 'Ollama';

-- Verify the updates
SELECT 
    `Id`,
    `ModelName`,
    `ModelType`,
    `Provider`,
    `ApiEndpoint`,
    `Context`,
    `IsActive`
FROM `AIModelConfigs`
WHERE `Context` = 'ClinicalNote' AND `Provider` = 'Ollama'
ORDER BY `ModelType`, `DisplayOrder`;

-- Update the chain name to reflect the new models
UPDATE `AIModelChains`
SET 
    `ChainName` = 'Qwen3-8b-Qwen3-4b Chain',
    `Description` = 'Chained AI workflow: Primary model (Qwen3-8b) generates structured clinical note draft from patient encounter, then Secondary model (Qwen3-4b) analyzes the note to identify missed considerations and follow-up actions.',
    `UpdatedAt` = NOW()
WHERE `Context` = 'ClinicalNote';

-- Verify the chain configuration
SELECT 
    c.`ChainName`,
    c.`Context`,
    p.`ModelName` AS PrimaryModel,
    p.`ApiEndpoint` AS PrimaryEndpoint,
    s.`ModelName` AS SecondaryModel,
    s.`ApiEndpoint` AS SecondaryEndpoint,
    c.`IsActive`
FROM `AIModelChains` c
INNER JOIN `AIModelConfigs` p ON c.`PrimaryModelId` = p.`Id`
INNER JOIN `AIModelConfigs` s ON c.`SecondaryModelId` = s.`Id`
WHERE c.`Context` = 'ClinicalNote';

