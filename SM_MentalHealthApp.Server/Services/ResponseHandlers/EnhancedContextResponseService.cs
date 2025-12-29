using System.Text;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Services.ResponseHandlers;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    /// <summary>
    /// Service to handle enhanced context responses using the handler pattern
    /// This replaces the massive ProcessEnhancedContextResponseAsync method
    /// </summary>
    public class EnhancedContextResponseService
    {
        private readonly ResponseHandlerFactory _handlerFactory;
        private readonly ContextExtractor _contextExtractor;
        private readonly QuestionExtractor _questionExtractor;
        private readonly ILogger<EnhancedContextResponseService> _logger;
        private readonly LlmClient _llmClient;

        public EnhancedContextResponseService(
            ResponseHandlerFactory handlerFactory,
            ContextExtractor contextExtractor,
            QuestionExtractor questionExtractor,
            ILogger<EnhancedContextResponseService> logger,
            LlmClient llmClient)
        {
            _handlerFactory = handlerFactory;
            _contextExtractor = contextExtractor;
            _questionExtractor = questionExtractor;
            _logger = logger;
            _llmClient = llmClient;
        }

        public async Task<string> ProcessAsync(string text)
        {
            try
            {
                _logger.LogInformation("Processing enhanced context response");

                // Check if this is a medical resource response - return it directly
                if (text.Contains("**Medical Resource Information") || text.Contains("**Medical Facilities Search"))
                {
                    _logger.LogInformation("Detected medical resource response, returning directly");
                    return text;
                }

                // Extract context
                var context = await _contextExtractor.ExtractContextAsync(text);

                // Extract user question
                var userQuestion = await _questionExtractor.ExtractUserQuestionAsync(text, context.IsAiHealthCheck);

                if (string.IsNullOrEmpty(userQuestion) && context.IsAiHealthCheck)
                {
                    userQuestion = "AI Health Check for Patient";
                }

                _logger.LogInformation("Extracted user question: '{UserQuestion}'", userQuestion);

                // For specific questions, always use OpenAI for intelligent responses
                // This ensures questions are actually answered, not just template responses
                if (!string.IsNullOrWhiteSpace(userQuestion) &&
                    userQuestion != "AI Health Check for Patient" &&
                    userQuestion.Length < 200) // Only for reasonable length questions
                {
                    _logger.LogInformation("Using OpenAI to intelligently answer question: {Question}", userQuestion);
                    var intelligentResponse = await GetIntelligentFallbackAsync(userQuestion, text, context);

                    // Only use handler response if OpenAI fails or returns empty
                    if (string.IsNullOrWhiteSpace(intelligentResponse) ||
                        intelligentResponse.Contains("I apologize, but I'm having trouble"))
                    {
                        _logger.LogWarning("OpenAI response was empty or error, falling back to handler");
                        var handler = await _handlerFactory.GetHandlerAsync(userQuestion, context);
                        if (handler != null)
                        {
                            return await handler.HandleAsync(userQuestion, context);
                        }
                    }
                    else
                    {
                        return intelligentResponse;
                    }
                }

                // Get appropriate handler for AI Health Check or when no specific question
                var defaultHandler = await _handlerFactory.GetHandlerAsync(userQuestion, context);

                if (defaultHandler == null)
                {
                    _logger.LogWarning("No handler found, using OpenAI to answer question intelligently");
                    var fallback = await GetIntelligentFallbackAsync(userQuestion, text, context);
                    return fallback;
                }

                // Handle the question with handler
                var handlerResponse = await defaultHandler.HandleAsync(userQuestion, context);
                return handlerResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enhanced context response");
                return "I understand you're asking about the patient. Based on the available information, I can see their recent activity and medical content. How can I help you further with their care?";
            }
        }

        private async Task<string> GetIntelligentFallbackAsync(string userQuestion, string fullContext, ResponseContext context)
        {
            // If we have a specific question, use OpenAI to answer it intelligently
            if (!string.IsNullOrWhiteSpace(userQuestion) && userQuestion != "AI Health Check for Patient")
            {
                try
                {
                    _logger.LogInformation("Using OpenAI to answer question: {Question}", userQuestion);

                    // Build a prompt that includes the patient context and asks to answer the specific question
                    var prompt = $@"Based on the following patient medical data, please answer the user's question: ""{userQuestion}""

CLIENT MEDICAL DATA:
{fullContext}

INSTRUCTIONS:
- If the question is about the patient's health/medical status, use the medical data above to provide a relevant answer.
- If the question is NOT related to medical data (e.g., asking about a person, place, or general topic), respond appropriately by saying you don't have that information in the patient's medical records, but you can help with health-related questions.
- Be conversational and helpful - directly answer the question asked.
- Only provide a general medical overview if the question explicitly asks for it (e.g., 'how am I doing?', 'what's my status?').";

                    var llmRequest = new LlmRequest
                    {
                        Model = "gpt-4o-mini",
                        Instructions = "You are a helpful clinical AI assistant. Answer the user's question directly and helpfully based on the patient data provided. If the question is not medical, politely redirect to health-related topics.",
                        Prompt = prompt,
                        Temperature = 0.7,
                        MaxTokens = 1000,
                        Provider = AiProvider.OpenAI
                    };

                    var llmResponse = await _llmClient.GenerateTextAsync(llmRequest);

                    if (!string.IsNullOrWhiteSpace(llmResponse?.Text))
                    {
                        _logger.LogInformation("OpenAI successfully answered question");
                        return llmResponse.Text.Trim();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error using OpenAI for intelligent fallback, using default");
                }
            }

            // Fallback to default medical overview if OpenAI fails or no specific question
            return await GetDefaultFallbackAsync(context);
        }

        private async Task<string> GetDefaultFallbackAsync(ResponseContext context)
        {
            var response = new StringBuilder();
            response.AppendLine("**Patient Medical Overview:**");

            if (context.HasCriticalValues)
            {
                response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
            }
            else if (context.HasAnyConcerns)
            {
                response.AppendLine("⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations.");
            }
            else
            {
                response.AppendLine("✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
            }

            return response.ToString().Trim();
        }
    }
}

