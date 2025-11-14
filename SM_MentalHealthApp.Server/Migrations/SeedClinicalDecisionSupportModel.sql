-- Seed data for Clinical Decision Support AI Model Configuration
-- This script adds a model configuration for the Clinical Decision Support feature
-- NOTE: Update the model name (ApiEndpoint) to match your installed Ollama model

-- Insert model configuration for Clinical Decision Support
-- Using tinyllama as default (lightweight, fast)
-- Change 'tinyllama' to match your installed model (e.g., 'qwen3:4b', 'qwen3:8b', 'gemma2:9b')
INSERT INTO `AIModelConfigs` (`ModelName`, `ModelType`, `Provider`, `ApiEndpoint`, `ApiKeyConfigKey`, `SystemPrompt`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`)
VALUES (
    'TinyLlama-ClinicalDecisionSupport',
    'Primary',
    'Ollama',
    'tinyllama',  -- CHANGE THIS to your model name (e.g., 'qwen3:4b', 'qwen3:8b', 'gemma2:9b')
    NULL,  -- Ollama doesn't need API key
    'You are a medical AI assistant specialized in clinical decision support. Your task is to provide evidence-based clinical recommendations, follow-up steps, insurance requirements, and clinical protocols for patient diagnoses. Be concise, accurate, and follow medical best practices.',
    'ClinicalDecisionSupport',
    1,
    TRUE,
    NOW()
) ON DUPLICATE KEY UPDATE `UpdatedAt` = NOW();

-- Verify the insert
SELECT 
    `Id`,
    `ModelName`,
    `ModelType`,
    `Provider`,
    `ApiEndpoint`,
    `Context`,
    `IsActive`
FROM `AIModelConfigs`
WHERE `Context` = 'ClinicalDecisionSupport'
ORDER BY `DisplayOrder`;

