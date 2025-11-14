-- Seed data for AI Model Configurations
-- This script populates the AIModelConfigs and AIModelChains tables with Ollama model configurations
-- NOTE: Update the model names (ApiEndpoint) to match your installed Ollama models
-- Recommended models for medical use:
--   Primary (Note Generation): qwen2.5:7b, qwen2.5:14b, gemma2:9b, or deepseek-r1:7b
--   Secondary (Analysis): deepseek-r1:7b, deepseek-r1:32b, qwen2.5:14b, or gemma2:9b

-- Insert Primary model configuration for structured note generation
-- Using Qwen2.5:7b as default (good for structured output and medical tasks)
-- Change 'qwen2.5:7b' to match your installed model (e.g., 'qwen2.5:14b', 'gemma2:9b', 'deepseek-r1:7b')
INSERT INTO `AIModelConfigs` (`ModelName`, `ModelType`, `Provider`, `ApiEndpoint`, `ApiKeyConfigKey`, `SystemPrompt`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`)
VALUES (
    'Qwen2.5-NoteGenerator',
    'Primary',
    'Ollama',
    'qwen2.5:7b',  -- CHANGE THIS to your model name (e.g., 'qwen2.5:14b', 'gemma2:9b', 'deepseek-r1:7b')
    NULL,  -- Ollama doesn't need API key
    'You are a specialized medical AI assistant. Your task is to generate structured clinical note drafts from patient encounter data. Analyze the provided patient information and create a well-organized, professional clinical note that includes: Chief Complaint, History of Present Illness, Review of Systems, Assessment, and Plan. Ensure the note is clear, concise, and follows standard medical documentation practices.',
    'ClinicalNote',
    1,
    TRUE,
    NOW()
) ON DUPLICATE KEY UPDATE `UpdatedAt` = NOW();

-- Insert Secondary model configuration for missed considerations analysis
-- Using DeepSeek-R1:7b as default (excellent for reasoning and analysis)
-- Change 'deepseek-r1:7b' to match your installed model (e.g., 'deepseek-r1:32b', 'qwen2.5:14b', 'gemma2:9b')
INSERT INTO `AIModelConfigs` (`ModelName`, `ModelType`, `Provider`, `ApiEndpoint`, `ApiKeyConfigKey`, `SystemPrompt`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`)
VALUES (
    'DeepSeek-Analyzer',
    'Secondary',
    'Ollama',
    'deepseek-r1:7b',  -- CHANGE THIS to your model name (e.g., 'deepseek-r1:32b', 'qwen2.5:14b', 'gemma2:9b')
    NULL,  -- Ollama doesn't need API key
    'You are a medical AI assistant specialized in clinical decision support. Your task is to analyze clinical notes and patient encounters to identify: 1) Possible missed considerations or diagnoses that should be evaluated, 2) Recommended follow-up actions or tests, 3) Potential drug interactions or contraindications, 4) Red flags or warning signs that require attention. Provide a structured list of considerations and recommendations based on evidence-based medicine principles.',
    'ClinicalNote',
    2,
    TRUE,
    NOW()
) ON DUPLICATE KEY UPDATE `UpdatedAt` = NOW();

-- Create the chained AI configuration (Primary -> Secondary)
-- First, get the IDs of the models we just inserted (or existing ones)
SET @primary_model_id = (SELECT `Id` FROM `AIModelConfigs` WHERE `ModelName` = 'Qwen2.5-NoteGenerator' AND `Context` = 'ClinicalNote' LIMIT 1);
SET @secondary_model_id = (SELECT `Id` FROM `AIModelConfigs` WHERE `ModelName` = 'DeepSeek-Analyzer' AND `Context` = 'ClinicalNote' LIMIT 1);

-- Insert the chain configuration
INSERT INTO `AIModelChains` (`ChainName`, `Context`, `Description`, `PrimaryModelId`, `SecondaryModelId`, `ChainOrder`, `IsActive`, `CreatedAt`)
VALUES (
    'Qwen-DeepSeek Chain',
    'ClinicalNote',
    'Chained AI workflow: Primary model (Qwen2.5) generates structured clinical note draft from patient encounter, then Secondary model (DeepSeek-R1) analyzes the note to identify missed considerations and follow-up actions.',
    @primary_model_id,
    @secondary_model_id,
    1,
    TRUE,
    NOW()
) ON DUPLICATE KEY UPDATE `UpdatedAt` = NOW();

-- NOTE: Detailed AI Instructions are in SeedChainedAIInstructions.sql
-- Run that script after this one for comprehensive instructions

