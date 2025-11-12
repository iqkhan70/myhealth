using System.Text;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Services.ResponseHandlers;

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

        public EnhancedContextResponseService(
            ResponseHandlerFactory handlerFactory,
            ContextExtractor contextExtractor,
            QuestionExtractor questionExtractor,
            ILogger<EnhancedContextResponseService> logger)
        {
            _handlerFactory = handlerFactory;
            _contextExtractor = contextExtractor;
            _questionExtractor = questionExtractor;
            _logger = logger;
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

                // Get appropriate handler
                var handler = await _handlerFactory.GetHandlerAsync(userQuestion, context);
                
                if (handler == null)
                {
                    _logger.LogWarning("No handler found, using default fallback");
                    var fallback = await GetDefaultFallbackAsync(context);
                    return fallback;
                }

                // Handle the question
                var response = await handler.HandleAsync(userQuestion, context);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enhanced context response");
                return "I understand you're asking about the patient. Based on the available information, I can see their recent activity and medical content. How can I help you further with their care?";
            }
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

