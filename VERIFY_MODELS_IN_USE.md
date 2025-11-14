# How to Verify Which Models Are Being Used

## Method 1: Check Application Logs (Recommended)

When you run the application and use the chained AI service, you'll see detailed logs showing exactly which models are being used.

### What to Look For:

When the chained AI workflow runs, you'll see logs like this:

```
=== CHAINED AI WORKFLOW STARTED ===
Primary Model: Qwen2.5-NoteGenerator (Ollama) - Endpoint: qwen2.5:7b
Secondary Model: DeepSeek-Analyzer (Ollama) - Endpoint: deepseek-r1:7b
Step 1: Generating structured note with Qwen2.5-NoteGenerator...
Calling Ollama model: qwen2.5:7b
Calling Ollama API: http://localhost:11434/api/generate with model: qwen2.5:7b
Ollama response received for model qwen2.5:7b: 1234 characters in 2.45s
Step 1 Complete: Generated 1234 characters of structured note
Step 2: Analyzing with DeepSeek-Analyzer...
Calling Ollama model: deepseek-r1:7b
Calling Ollama API: http://localhost:11434/api/generate with model: deepseek-r1:7b
Ollama response received for model deepseek-r1:7b: 567 characters in 1.89s
Step 2 Complete: Generated 567 characters of analysis
=== CHAINED AI WORKFLOW COMPLETED ===
```

### Where to See Logs:

1. **Console Output** (if running from terminal):
   ```bash
   dotnet run
   ```
   Logs will appear in the console.

2. **Log Files** (check your `appsettings.json` for log file location):
   - Usually in `logs/` directory
   - Or configured in `Logging:LogLevel` settings

3. **Visual Studio Output Window**:
   - View → Output → Show output from: "Debug"

## Method 2: Check Database Directly

Query the database to see what's configured:

```sql
-- Check active model configurations
SELECT 
    ModelName,
    ModelType,
    Provider,
    ApiEndpoint,
    Context,
    IsActive
FROM AIModelConfigs
WHERE IsActive = TRUE
ORDER BY Context, DisplayOrder;

-- Check active chains
SELECT 
    c.ChainName,
    c.Context,
    p.ModelName AS PrimaryModel,
    p.Provider AS PrimaryProvider,
    p.ApiEndpoint AS PrimaryEndpoint,
    s.ModelName AS SecondaryModel,
    s.Provider AS SecondaryProvider,
    s.ApiEndpoint AS SecondaryEndpoint,
    c.IsActive
FROM AIModelChains c
INNER JOIN AIModelConfigs p ON c.PrimaryModelId = p.Id
INNER JOIN AIModelConfigs s ON c.SecondaryModelId = s.Id
WHERE c.IsActive = TRUE;
```

## Method 3: Add a Diagnostic Endpoint (Optional)

You can add a simple endpoint to check the current configuration. Here's an example you can add to `ChainedAIController.cs`:

```csharp
[HttpGet("current-config")]
public async Task<ActionResult> GetCurrentConfig()
{
    try
    {
        var chain = await _context.Set<AIModelChain>()
            .Include(c => c.PrimaryModel)
            .Include(c => c.SecondaryModel)
            .Where(c => c.Context == "ClinicalNote" && c.IsActive)
            .FirstOrDefaultAsync();

        if (chain == null)
        {
            return NotFound("No active chain configuration found");
        }

        return Ok(new
        {
            ChainName = chain.ChainName,
            PrimaryModel = new
            {
                Name = chain.PrimaryModel.ModelName,
                Provider = chain.PrimaryModel.Provider,
                Endpoint = chain.PrimaryModel.ApiEndpoint
            },
            SecondaryModel = new
            {
                Name = chain.SecondaryModel.ModelName,
                Provider = chain.SecondaryModel.Provider,
                Endpoint = chain.SecondaryModel.ApiEndpoint
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting current config");
        return StatusCode(500, "Internal server error");
    }
}
```

Then call: `GET /api/chainedai/current-config`

## Method 4: Check Ollama Directly

You can also verify Ollama is receiving requests:

1. **Check Ollama logs** (if running with verbose logging):
   ```bash
   ollama serve --verbose
   ```

2. **Monitor Ollama API**:
   - Ollama logs all API requests
   - You'll see requests to `/api/generate` with your model names

3. **Test Ollama directly**:
   ```bash
   curl http://localhost:11434/api/generate -d '{
     "model": "qwen2.5:7b",
     "prompt": "test",
     "stream": false
   }'
   ```

## What the Logs Tell You:

- **Provider**: Shows "Ollama" (not "HuggingFace")
- **Endpoint**: Shows your model name (e.g., "qwen2.5:7b") not a URL
- **API URL**: Shows "http://localhost:11434/api/generate" (Ollama's endpoint)
- **Response Time**: Shows how long each model took to respond
- **Response Length**: Shows how much text was generated

## Troubleshooting:

If you see errors like:
- `"No active AI model chain found"` → Run the SQL seed scripts
- `"Ollama API error: 404"` → Model name doesn't match your installed models
- `"Connection refused"` → Ollama is not running (`ollama serve`)

## Quick Verification Checklist:

- [ ] SQL scripts have been run
- [ ] Model names in database match your installed Ollama models
- [ ] Ollama is running (`ollama serve`)
- [ ] Logs show "Provider: Ollama" (not HuggingFace)
- [ ] Logs show your model names (e.g., "qwen2.5:7b")
- [ ] API calls go to `localhost:11434` (not HuggingFace URLs)

