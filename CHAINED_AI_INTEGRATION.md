# Chained AI Integration (BioMistral + Meditron)

## Overview

This integration chains two AI models together for clinical note generation:

1. **BioMistral** - Generates structured clinical note drafts from patient encounters
2. **Meditron** - Analyzes the note to identify missed considerations and follow-up actions

## Database Setup

### Step 1: Run the Migration Script

Execute the SQL script to create the necessary tables:

```sql
-- File: SM_MentalHealthApp.Server/Migrations/CreateAIModelConfigTables.sql
```

This creates:

- `AIModelConfigs` - Stores configuration for individual AI models
- `AIModelChains` - Stores chain configurations (which models to use together)

### Step 2: Seed the Data

Execute the seed script to populate the tables with BioMistral and Meditron configurations:

```sql
-- File: SM_MentalHealthApp.Server/Migrations/SeedAIModelConfigData.sql
```

This script:

- Inserts BioMistral model configuration
- Inserts Meditron model configuration
- Creates the chain configuration linking them together
- Adds AI instructions for the chained workflow

## Configuration

### Ollama Configuration

Ensure your `appsettings.json` has the Ollama base URL (defaults to localhost):

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434"
  }
}
```

**No API keys needed** - Ollama runs locally and doesn't require authentication.

Make sure Ollama is running:

```bash
ollama serve
```

### Model Configuration

The seed script uses Ollama models (running locally). Update the model names in the seed script to match your installed Ollama models:

**Recommended Models for Medical Use:**

**Primary Model (Note Generation):**

- `qwen2.5:7b` or `qwen2.5:14b` - Excellent for structured output and medical tasks
- `gemma2:9b` - Good general purpose with medical knowledge
- `deepseek-r1:7b` - Strong reasoning capabilities

**Secondary Model (Analysis):**

- `deepseek-r1:7b` or `deepseek-r1:32b` - Excellent for reasoning and analysis
- `qwen2.5:14b` - Good for comprehensive analysis
- `gemma2:9b` - Reliable for follow-up recommendations

**To find your installed models:**

```bash
ollama list
```

**To install recommended models:**

```bash
# For note generation (Primary)
ollama pull qwen2.5:7b
# or
ollama pull gemma2:9b

# For analysis (Secondary)
ollama pull deepseek-r1:7b
# or
ollama pull deepseek-r1:32b
```

**Note:** Update the `ApiEndpoint` field in the seed script SQL to match your installed model names (e.g., `qwen2.5:7b`, `deepseek-r1:7b`).

## Usage

### Service Interface

The `IChainedAIService` provides:

```csharp
Task<ChainedAIResult> GenerateStructuredNoteAndAnalysisAsync(
    string encounterData,
    int patientId,
    string context = "ClinicalNote"
);
```

### Result Structure

```csharp
public class ChainedAIResult
{
    public string PrimaryModelOutput { get; set; }      // BioMistral output (structured note)
    public string SecondaryModelOutput { get; set; }    // Meditron output (missed considerations)
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string PrimaryModelName { get; set; }
    public string SecondaryModelName { get; set; }
}
```

## How It Works

1. **Service retrieves chain configuration** from database based on context (default: "ClinicalNote")
2. **Primary model (BioMistral)** generates structured note from:
   - Patient encounter data
   - Patient context (from journal entries, clinical notes, content, etc.)
   - AI instructions from database
   - Model's system prompt from database
3. **Secondary model (Meditron)** analyzes:
   - Original encounter data
   - Generated structured note
   - Patient context
   - AI instructions from database
   - Model's system prompt from database
4. **Returns combined result** with both outputs

## Database-Driven Configuration

All configuration is stored in the database:

### AIModelConfigs Table

- Model name, type, provider
- API endpoint
- System prompt
- API key configuration key
- Context (ClinicalNote, HealthCheck, Chat, etc.)

### AIModelChains Table

- Links primary and secondary models
- Defines execution order
- Context-specific chains

### AIInstructions Table

- Instructions for each context
- Organized by categories
- Can be updated without code changes

## Customization

### Adding New Models

1. Insert into `AIModelConfigs` table
2. Create chain in `AIModelChains` if needed
3. Add AI instructions in `AIInstructions` table

### Modifying Prompts

- Update `SystemPrompt` in `AIModelConfigs` table
- Update instructions in `AIInstructions` table
- No code changes needed

### Changing Model Endpoints

- Update `ApiEndpoint` in `AIModelConfigs` table
- No code changes needed

## Integration Points

The service can be integrated into:

- Clinical notes creation/editing
- Patient encounter workflows
- Health check analysis
- Any context where structured notes and analysis are needed

## Quick Start

1. **Check your installed Ollama models:**

   ```bash
   ollama list
   ```

2. **Install recommended models (if not already installed):**

   ```bash
   # For note generation
   ollama pull qwen2.5:7b
   # or
   ollama pull gemma2:9b

   # For analysis
   ollama pull deepseek-r1:7b
   # or
   ollama pull deepseek-r1:32b
   ```

3. **Update the seed script** (`SeedAIModelConfigData.sql`):

   - Change `'qwen2.5:7b'` to match your installed primary model
   - Change `'deepseek-r1:7b'` to match your installed secondary model

4. **Run the SQL scripts:**

   - `CreateAIModelConfigTables.sql` (creates tables)
   - `SeedAIModelConfigData.sql` (populates with your models)

5. **Verify Ollama is running:**

   ```bash
   ollama serve
   ```

6. **Test the service** - The chained AI is now ready to use!

## Model Recommendations

Based on your existing models (qwen, gemma, deepseek with labels like 3:1b, 3:4b):

**Best for Medical Note Generation:**

- `qwen2.5:7b` or `qwen2.5:14b` - Excellent structured output
- `gemma2:9b` - Good balance of quality and speed
- `deepseek-r1:7b` - Strong reasoning, good for complex cases

**Best for Medical Analysis:**

- `deepseek-r1:7b` or `deepseek-r1:32b` - Best reasoning and analysis
- `qwen2.5:14b` - Comprehensive analysis
- `gemma2:9b` - Reliable recommendations

**Note:** If you have models with different labels (like `3:1b`, `3:4b`), use those exact names in the seed script. The format is typically `model-name:tag` (e.g., `qwen2.5:3b`, `deepseek-r1:7b`).
