using System.Text;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Base class for response handlers with common functionality
    /// </summary>
    public abstract class BaseResponseHandler : IResponseHandler
    {
        protected readonly IAIResponseTemplateService _templateService;
        protected readonly ILogger _logger;

        protected BaseResponseHandler(IAIResponseTemplateService templateService, ILogger logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        public abstract Task<bool> CanHandleAsync(string question, ResponseContext context);
        public abstract Task<string> HandleAsync(string question, ResponseContext context);

        /// <summary>
        /// Helper method to get template with fallback
        /// </summary>
        protected async Task<string> GetTemplateAsync(string templateKey, Dictionary<string, string>? parameters = null, string? fallbackKey = null, string? hardcodedFallback = null)
        {
            var template = await _templateService.FormatTemplateAsync(templateKey, parameters);
            if (!string.IsNullOrEmpty(template)) return template;

            if (!string.IsNullOrEmpty(fallbackKey))
            {
                var fallback = await _templateService.FormatTemplateAsync(fallbackKey, parameters);
                if (!string.IsNullOrEmpty(fallback)) return fallback;
            }

            return hardcodedFallback ?? string.Empty;
        }

        /// <summary>
        /// Helper method to append template to response
        /// </summary>
        protected async Task AppendTemplateAsync(StringBuilder response, string templateKey, Dictionary<string, string>? parameters = null, string? fallbackKey = null, string? hardcodedFallback = null)
        {
            var template = await GetTemplateAsync(templateKey, parameters, fallbackKey, hardcodedFallback);
            if (!string.IsNullOrEmpty(template))
            {
                response.AppendLine(template);
            }
        }
    }
}

