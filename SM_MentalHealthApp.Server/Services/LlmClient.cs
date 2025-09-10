using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO; // Added for File operations

namespace SM_LLMServer.Services
{
    public enum AiProvider
    {
        OpenAI,
        Ollama,
        CustomKnowledge,
        HuggingFace
    }

    public class LlmRequest
    {
        public string Model { get; set; }
        public string Instructions { get; set; }
        public string Prompt { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public string PreviousResponseId { get; set; }
        public AiProvider Provider { get; set; } = AiProvider.OpenAI;
    }

    public class LlmResponse
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Provider { get; set; }
    }

    public class LlmClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private readonly string _openAiBaseUrl = "https://api.openai.com/v1";
        private readonly string _ollamaBaseUrl;
        private readonly string _customKnowledgePath;
        private readonly string _customKnowledgeContent;
        private readonly string _huggingFaceToken;
        private readonly string _huggingFaceBaseUrl = "https://api-inference.huggingface.co";

        public LlmClient(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found in configuration");
            _ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            _customKnowledgePath = configuration["CustomKnowledge:FilePath"] ?? "llm/prompts/WonderWorld.md";
            _huggingFaceToken = configuration["HuggingFace:Token"] ?? throw new InvalidOperationException("Hugging Face token not found in configuration");
            
            // Load custom knowledge content
            _customKnowledgeContent = LoadCustomKnowledge();
            
            // Set up headers
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiApiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SM-LLM-Server");
        }

        private string LoadCustomKnowledge()
        {
            try
            {
                if (File.Exists(_customKnowledgePath))
                {
                    return File.ReadAllText(_customKnowledgePath);
                }
                return "WonderWorld theme park information not available.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load custom knowledge from {_customKnowledgePath}: {ex.Message}");
                return "WonderWorld theme park information not available.";
            }
        }

        public async Task<LlmResponse> GenerateTextAsync(LlmRequest request)
        {
            try
            {
                switch (request.Provider)
                {
                    case AiProvider.OpenAI:
                        return await GenerateWithOpenAI(request);
                    case AiProvider.Ollama:
                        return await GenerateWithOllama(request);
                    case AiProvider.CustomKnowledge:
                        return await GenerateWithCustomKnowledge(request);
                    case AiProvider.HuggingFace:
                        return await GenerateWithHuggingFace(request);
                    default:
                        throw new ArgumentException($"Unknown AI provider: {request.Provider}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI Generation Error: {ex.Message}");
                return new LlmResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = $"I'm having trouble connecting to my AI service right now. Please try again later. (Error: {ex.Message})",
                    Provider = request.Provider.ToString()
                };
            }
        }

        private async Task<LlmResponse> GenerateWithOpenAI(LlmRequest request)
        {
            var openAiRequest = new
            {
                model = request.Model ?? "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = request.Instructions ?? "You are a helpful AI assistant." },
                    new { role = "user", content = request.Prompt }
                },
                temperature = request.Temperature,
                max_tokens = request.MaxTokens
            };

            var json = JsonSerializer.Serialize(openAiRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_openAiBaseUrl}/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var openAiResponse = JsonSerializer.Deserialize<OpenAiApiResponse>(responseJson);

            if (openAiResponse?.Choices?.Length > 0 && 
                !string.IsNullOrEmpty(openAiResponse.Choices[0].Message?.Content))
            {
                return new LlmResponse
                {
                    Id = openAiResponse.Id ?? Guid.NewGuid().ToString(),
                    Text = openAiResponse.Choices[0].Message.Content,
                    Provider = "OpenAI"
                };
            }

            throw new Exception($"Unexpected OpenAI response structure: {responseJson}");
        }

        private async Task<LlmResponse> GenerateWithOllama(LlmRequest request)
        {
            var ollamaRequest = new
            {
                model = request.Model ?? "llama3.2",
                messages = new[]
                {
                    new { role = "system", content = request.Instructions ?? "You are a helpful AI assistant." },
                    new { role = "user", content = request.Prompt }
                },
                stream = false,
                options = new
                {
                    temperature = request.Temperature,
                    num_predict = request.MaxTokens
                }
            };

            var json = JsonSerializer.Serialize(ollamaRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/chat", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<OllamaApiResponse>(responseJson);

            if (ollamaResponse?.Message?.Content != null)
            {
                return new LlmResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = ollamaResponse.Message.Content,
                    Provider = "Ollama"
                };
            }

            throw new Exception($"Unexpected Ollama response structure: {responseJson}");
        }

        private async Task<LlmResponse> GenerateWithHuggingFace(LlmRequest request)
        {
            // Create a new HTTP client for Hugging Face (different auth)
            using var hfClient = new HttpClient();
            hfClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _huggingFaceToken);
            hfClient.DefaultRequestHeaders.Add("User-Agent", "SM-LLM-Server");

            var hfRequest = new
            {
                inputs = $"{request.Instructions ?? "You are a helpful AI assistant."}\n\nUser: {request.Prompt}\nAssistant:",
                parameters = new
                {
                    max_new_tokens = request.MaxTokens,
                    temperature = request.Temperature,
                    do_sample = true,
                    return_full_text = false
                }
            };

            var json = JsonSerializer.Serialize(hfRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Use the model specified in the request, or default to a good text generation model
            var model = request.Model ?? "microsoft/DialoGPT-medium";
            var response = await hfClient.PostAsync($"{_huggingFaceBaseUrl}/models/{model}", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var hfResponse = JsonSerializer.Deserialize<HuggingFaceApiResponse>(responseJson);

            if (hfResponse?.GeneratedText != null)
            {
                return new LlmResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = hfResponse.GeneratedText.Trim(),
                    Provider = "HuggingFace"
                };
            }

            throw new Exception($"Unexpected Hugging Face response structure: {responseJson}");
        }

        private async Task<LlmResponse> GenerateWithCustomKnowledge(LlmRequest request)
        {
            // Create a context-aware prompt using the custom knowledge
            var enhancedPrompt = $@"
Context from WonderWorld Knowledge Base:
{_customKnowledgeContent}

User Question: {request.Prompt}

Please provide a helpful answer based on the WonderWorld information above. If the question is not related to WonderWorld, provide a general helpful response.";

            // Use OpenAI as the backend for processing the enhanced prompt
            var customRequest = new LlmRequest
            {
                Model = request.Model,
                Instructions = "You are a WonderWorld theme park expert. Use the provided knowledge base to answer questions accurately and helpfully.",
                Prompt = enhancedPrompt,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Provider = AiProvider.OpenAI
            };

            var response = await GenerateWithOpenAI(customRequest);
            response.Provider = "CustomKnowledge";
            return response;
        }
    }

    // OpenAI API response models
    public class OpenAiApiResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("object")]
        public string Object { get; set; }
        
        [JsonPropertyName("created")]
        public long Created { get; set; }
        
        [JsonPropertyName("model")]
        public string Model { get; set; }
        
        [JsonPropertyName("choices")]
        public Choice[] Choices { get; set; }
        
        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
        
        [JsonPropertyName("message")]
        public Message Message { get; set; }
        
        [JsonPropertyName("logprobs")]
        public object Logprobs { get; set; }
        
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        [JsonPropertyName("refusal")]
        public object Refusal { get; set; }
        
        [JsonPropertyName("annotations")]
        public object[] Annotations { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    // Ollama API response models
    public class OllamaApiResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
        
        [JsonPropertyName("message")]
        public OllamaMessage Message { get; set; }
        
        [JsonPropertyName("done")]
        public bool Done { get; set; }
        
        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }
        
        [JsonPropertyName("load_duration")]
        public long LoadDuration { get; set; }
        
        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }
        
        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }
        
        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }
        
        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }
    }

    public class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    // Hugging Face API response models
    public class HuggingFaceApiResponse
    {
        [JsonPropertyName("generated_text")]
        public string GeneratedText { get; set; }
    }
}
