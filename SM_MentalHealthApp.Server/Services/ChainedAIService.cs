using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IChainedAIService
    {
        Task<ChainedAIResult> GenerateStructuredNoteAndAnalysisAsync(string encounterData, int patientId, string context = "ClinicalNote");
    }

    public class ChainedAIService : IChainedAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ChainedAIService> _logger;
        private readonly IAIInstructionService _instructionService;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly JournalDbContext _context;

        public ChainedAIService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<ChainedAIService> logger,
            IAIInstructionService instructionService,
            IContentAnalysisService contentAnalysisService,
            JournalDbContext context)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _instructionService = instructionService;
            _contentAnalysisService = contentAnalysisService;
            _context = context;

            // Set a longer timeout for the injected HttpClient (5 minutes) for AI model calls
            // This is especially important for Ollama and other local models that can take 60-120+ seconds
            //_httpClient.Timeout = TimeSpan.FromMinutes(5);
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;

            // Set up authentication for HuggingFace API
            var apiKey = _config["HuggingFace:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        public async Task<ChainedAIResult> GenerateStructuredNoteAndAnalysisAsync(string encounterData, int patientId, string context = "ClinicalNote")
        {
            var result = new ChainedAIResult();

            try
            {
                // Get the active chain configuration from database
                var chain = await _context.Set<AIModelChain>()
                    .Include(c => c.PrimaryModel)
                    .Include(c => c.SecondaryModel)
                    .Where(c => c.Context == context && c.IsActive)
                    .OrderBy(c => c.ChainOrder)
                    .FirstOrDefaultAsync();

                if (chain == null || chain.PrimaryModel == null || chain.SecondaryModel == null)
                {
                    _logger.LogError("No active AI model chain found for context: {Context}. Please run the SQL seed scripts.", context);
                    throw new InvalidOperationException($"No active AI model chain found for context: {context}. Please ensure you have run the SQL migration and seed scripts.");
                }

                result.PrimaryModelName = chain.PrimaryModel.ModelName;
                result.SecondaryModelName = chain.SecondaryModel.ModelName;

                _logger.LogInformation("=== CHAINED AI WORKFLOW STARTED ===");
                _logger.LogInformation("Primary Model: {ModelName} ({Provider}) - Endpoint: {Endpoint}",
                    chain.PrimaryModel.ModelName, chain.PrimaryModel.Provider, chain.PrimaryModel.ApiEndpoint);
                _logger.LogInformation("Secondary Model: {ModelName} ({Provider}) - Endpoint: {Endpoint}",
                    chain.SecondaryModel.ModelName, chain.SecondaryModel.Provider, chain.SecondaryModel.ApiEndpoint);

                // Step 1: Generate structured note using primary model
                _logger.LogInformation("Step 1: Generating structured note with {ModelName}...", chain.PrimaryModel.ModelName);
                var structuredNote = await GenerateStructuredNoteAsync(encounterData, patientId, chain.PrimaryModel);
                result.PrimaryModelOutput = structuredNote;
                _logger.LogInformation("Step 1 Complete: Generated {Length} characters of structured note", structuredNote.Length);

                // Step 2: Analyze with secondary model using the structured note + original encounter
                _logger.LogInformation("Step 2: Analyzing with {ModelName}...", chain.SecondaryModel.ModelName);
                var analysis = await AnalyzeWithSecondaryModelAsync(encounterData, structuredNote, patientId, chain.SecondaryModel);
                result.SecondaryModelOutput = analysis;
                _logger.LogInformation("Step 2 Complete: Generated {Length} characters of analysis", analysis.Length);
                _logger.LogInformation("=== CHAINED AI WORKFLOW COMPLETED ===");

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chained AI processing for patient {PatientId}", patientId);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<string> GenerateStructuredNoteAsync(string encounterData, int patientId, AIModelConfig modelConfig)
        {
            try
            {
                // Get instructions from database for the context
                var instructionText = await _instructionService.BuildInstructionsAsync(modelConfig.Context);

                // Build patient context if available
                var patientContext = await _contentAnalysisService.BuildEnhancedContextAsync(patientId, "");

                // Build prompt using model's system prompt and instructions
                var systemPrompt = modelConfig.SystemPrompt ?? "";
                var prompt = BuildModelPrompt(encounterData, patientContext, instructionText, systemPrompt);

                // Get API key from config (only needed for non-Ollama providers)
                string? apiKey = null;
                if (!modelConfig.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
                {
                    apiKey = !string.IsNullOrEmpty(modelConfig.ApiKeyConfigKey)
                        ? _config[modelConfig.ApiKeyConfigKey]
                        : _config["HuggingFace:ApiKey"];

                    if (string.IsNullOrEmpty(apiKey))
                    {
                        throw new InvalidOperationException($"API key not found for model: {modelConfig.ModelName}");
                    }
                }

                // For Ollama, ApiEndpoint contains the model name (e.g., "qwen2.5:7b")
                // For other providers, ApiEndpoint is the full URL
                var endpoint = modelConfig.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
                    ? modelConfig.ApiEndpoint  // Model name for Ollama
                    : modelConfig.ApiEndpoint;  // Full URL for others

                // Call model via API
                var response = await CallModelAsync(endpoint, prompt, apiKey ?? "", modelConfig.Provider);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating structured note with {ModelName}", modelConfig.ModelName);
                throw;
            }
        }

        private async Task<string> AnalyzeWithSecondaryModelAsync(
            string encounterData, string structuredNote, int patientId, AIModelConfig modelConfig)
        {
            try
            {
                // Get instructions from database for the context
                var instructionText = await _instructionService.BuildInstructionsAsync(modelConfig.Context);

                // Build patient context
                var patientContext = await _contentAnalysisService.BuildEnhancedContextAsync(patientId, "");

                // Build prompt using model's system prompt and instructions
                var systemPrompt = modelConfig.SystemPrompt ?? "";
                var prompt = BuildSecondaryModelPrompt(encounterData, structuredNote, patientContext, instructionText, systemPrompt);

                // Get API key from config using the model's ApiKeyConfigKey
                var apiKey = !string.IsNullOrEmpty(modelConfig.ApiKeyConfigKey)
                    ? _config[modelConfig.ApiKeyConfigKey]
                    : _config["HuggingFace:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException($"API key not found for model: {modelConfig.ModelName}");
                }

                // Call model via API
                var response = await CallModelAsync(modelConfig.ApiEndpoint, prompt, apiKey, modelConfig.Provider);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing with {ModelName}", modelConfig.ModelName);
                throw;
            }
        }

        private string BuildModelPrompt(string encounterData, string patientContext, string instructions, string systemPrompt)
        {
            var prompt = new StringBuilder();

            // Add system prompt if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                prompt.AppendLine(systemPrompt);
                prompt.AppendLine();
            }

            // Add instructions from database
            if (!string.IsNullOrEmpty(instructions))
            {
                prompt.AppendLine(instructions);
                prompt.AppendLine();
            }

            // Add patient context if available
            if (!string.IsNullOrEmpty(patientContext))
            {
                prompt.AppendLine("=== PATIENT CONTEXT ===");
                prompt.AppendLine(patientContext);
                prompt.AppendLine();
            }

            // Add encounter data
            prompt.AppendLine("=== PATIENT ENCOUNTER DATA ===");
            prompt.AppendLine(encounterData);
            prompt.AppendLine();

            prompt.AppendLine("Please generate a structured clinical note draft based on the above encounter. " +
                            "Include sections for: Chief Complaint, History of Present Illness, Assessment, and Plan.");

            return prompt.ToString();
        }

        private string BuildSecondaryModelPrompt(string encounterData, string structuredNote, string patientContext, string instructions, string systemPrompt)
        {
            var prompt = new StringBuilder();

            // Add system prompt if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                prompt.AppendLine(systemPrompt);
                prompt.AppendLine();
            }

            // Add instructions from database
            if (!string.IsNullOrEmpty(instructions))
            {
                prompt.AppendLine(instructions);
                prompt.AppendLine();
            }

            // Add patient context if available
            if (!string.IsNullOrEmpty(patientContext))
            {
                prompt.AppendLine("=== PATIENT CONTEXT ===");
                prompt.AppendLine(patientContext);
                prompt.AppendLine();
            }

            // Add original encounter data
            prompt.AppendLine("=== ORIGINAL ENCOUNTER DATA ===");
            prompt.AppendLine(encounterData);
            prompt.AppendLine();

            // Add generated structured note
            prompt.AppendLine("=== GENERATED STRUCTURED NOTE ===");
            prompt.AppendLine(structuredNote);
            prompt.AppendLine();

            prompt.AppendLine("Please analyze the encounter and structured note above. " +
                            "Identify any missed considerations, potential diagnoses, or follow-up actions that should be addressed. " +
                            "Format your response with clear sections for 'Missed Considerations' and 'Follow-up Actions'.");

            return prompt.ToString();
        }

        private async Task<string> CallModelAsync(string modelUrl, string prompt, string apiKey, string provider)
        {
            _logger.LogInformation("Calling {Provider} model: {Endpoint}", provider, modelUrl);

            // Handle Ollama provider differently
            if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                return await CallOllamaModelAsync(modelUrl, prompt);
            }

            // Handle HuggingFace and other providers
            using var client = new HttpClient();

            // Only set auth header if API key is provided (Ollama doesn't need it)
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            var requestBody = new
            {
                inputs = prompt,
                parameters = new
                {
                    max_new_tokens = 1024,
                    temperature = 0.7,
                    return_full_text = false
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(modelUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("{Provider} API error: {StatusCode} - {Error}", provider, response.StatusCode, errorContent);
                throw new Exception($"{provider} API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Handle different response formats
            if (responseData.ValueKind == JsonValueKind.Array && responseData.GetArrayLength() > 0)
            {
                var firstItem = responseData[0];
                if (firstItem.TryGetProperty("generated_text", out var generatedText))
                {
                    return generatedText.GetString() ?? string.Empty;
                }
            }

            // Fallback: try to extract text directly
            if (responseData.TryGetProperty("generated_text", out var directText))
            {
                return directText.GetString() ?? string.Empty;
            }

            return responseContent;
        }

        // private async Task<string> CallOllamaModelAsync(string modelName, string prompt)
        // {
        //     // Extract base URL from config or use default
        //     var ollamaBaseUrl = _config["Ollama:BaseUrl"] ?? "http://localhost:11434";

        //     _logger.LogInformation("Calling Ollama API: {BaseUrl}/api/generate with model: {ModelName}", ollamaBaseUrl, modelName);
        //     _logger.LogDebug("Prompt preview (first 200 chars): {PromptPreview}", prompt.Length > 200 ? prompt.Substring(0, 200) + "..." : prompt);

        //     // For Ollama, the modelUrl parameter is actually the model name (e.g., "qwen2.5:7b")
        //     // The endpoint is always /api/generate
        //     var requestBody = new
        //     {
        //         model = modelName,
        //         prompt = prompt,
        //         stream = false,
        //         options = new
        //         {
        //             temperature = 0.7,
        //             num_predict = 2048
        //         }
        //     };

        //     var json = JsonSerializer.Serialize(requestBody);
        //     var content = new StringContent(json, Encoding.UTF8, "application/json");

        //     using var client = new HttpClient();
        //     // Set a longer timeout for Ollama requests (5 minutes) as they can take 60-120+ seconds for large models
        //     client.Timeout = TimeSpan.FromMinutes(5);

        //     var startTime = DateTime.UtcNow;
        //     var response = await client.PostAsync($"{ollamaBaseUrl}/api/generate", content);
        //     var duration = (DateTime.UtcNow - startTime).TotalSeconds;

        //     if (!response.IsSuccessStatusCode)
        //     {
        //         var errorContent = await response.Content.ReadAsStringAsync();
        //         _logger.LogError("Ollama API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
        //         throw new Exception($"Ollama API error: {response.StatusCode} - {errorContent}");
        //     }

        //     var responseContent = await response.Content.ReadAsStringAsync();
        //     var ollamaResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        //     // Ollama returns { "response": "..." }
        //     if (ollamaResponse.TryGetProperty("response", out var responseText))
        //     {
        //         var result = responseText.GetString() ?? string.Empty;
        //         _logger.LogInformation("Ollama response received for model {ModelName}: {Length} characters in {Duration:F2}s",
        //             modelName, result.Length, duration);
        //         _logger.LogDebug("Response preview (first 200 chars): {ResponsePreview}",
        //             result.Length > 200 ? result.Substring(0, 200) + "..." : result);
        //         return result;
        //     }

        //     _logger.LogError("Unexpected Ollama response structure: {Response}", responseContent);
        //     throw new Exception($"Unexpected Ollama response structure: {responseContent}");
        // }

        private async Task<string> CallOllamaModelAsync(string modelName, string prompt)
        {
            // Base URL comes from config, fallback to localhost
            var ollamaBaseUrl = _config["Ollama:BaseUrl"] ?? "http://127.0.0.1:11434";

            _logger.LogInformation("Calling Ollama API: {BaseUrl}/api/generate with model: {ModelName}", ollamaBaseUrl, modelName);
            _logger.LogDebug("Prompt preview (first 200 chars): {PromptPreview}",
                prompt.Length > 200 ? prompt.Substring(0, 200) + "..." : prompt);

            // Use streaming = true so we can consume output progressively
            var requestBody = new
            {
                model = modelName,
                prompt = prompt,
                stream = true,
                options = new
                {
                    temperature = 0.7,
                    // IMPORTANT: Reduced for faster response on 1vCPU/2GB droplet
                    // 256 tokens is enough for structured notes and much faster
                    num_predict = 256
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{ollamaBaseUrl}/api/generate")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Use our own timeout window (e.g., 15 minutes) instead of HttpClient.Timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));

            var startTime = DateTime.UtcNow;

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogError("Ollama API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"Ollama API error: {response.StatusCode} - {errorContent}");
            }

            // Stream the response line by line
            await using var responseStream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var reader = new StreamReader(responseStream);

            var sb = new StringBuilder();
            string? line;
            int chunkCount = 0;
            var lastChunkTime = DateTime.UtcNow;

            // Read with timeout protection - if no data for 30 seconds, assume done
            while (!cts.Token.IsCancellationRequested)
            {
                // Check for timeout (no data for 30 seconds)
                if ((DateTime.UtcNow - lastChunkTime).TotalSeconds > 30)
                {
                    _logger.LogWarning("Ollama stream timeout - no data for 30 seconds, assuming complete");
                    break;
                }

                try
                {
                    // Use ReadLineAsync with cancellation token support
                    line = await reader.ReadLineAsync().WaitAsync(cts.Token);
                    if (line == null)
                        break;

                    lastChunkTime = DateTime.UtcNow;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Ollama stream cancelled");
                    break;
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Ollama stream read timeout");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var chunk = JsonSerializer.Deserialize<JsonElement>(line);
                    if (chunk.ValueKind != JsonValueKind.Object)
                        continue;

                    if (chunk.TryGetProperty("response", out var respPart))
                    {
                        var piece = respPart.GetString();
                        if (!string.IsNullOrEmpty(piece))
                        {
                            sb.Append(piece);
                            chunkCount++;
                        }
                    }

                    if (chunk.TryGetProperty("done", out var doneProp) && doneProp.GetBoolean())
                    {
                        break;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug(ex, "Failed to parse Ollama chunk: {Line}", line);
                }
            }

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            var result = sb.ToString();

            _logger.LogInformation(
                "Ollama response received for model {ModelName}: {Length} characters in {Duration:F2}s across {ChunkCount} chunks",
                modelName, result.Length, duration, chunkCount);

            _logger.LogDebug("Response preview (first 200 chars): {ResponsePreview}",
                result.Length > 200 ? result.Substring(0, 200) + "..." : result);

            return result;
        }

    }
}

