using System.Text;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Handles concerns-related questions (e.g., "what are the concerns?", "areas of concern")
    /// </summary>
    public class ConcernsResponseHandler : BaseResponseHandler
    {
        public ConcernsResponseHandler(IAIResponseTemplateService templateService, ILogger<ConcernsResponseHandler> logger)
            : base(templateService, logger)
        {
        }

        public override Task<bool> CanHandleAsync(string question, ResponseContext context)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Task.FromResult(false);

            var lower = question.ToLower();
            return Task.FromResult(
                lower.Contains("areas of concern") || 
                lower.Contains("concerns")
            );
        }

        public override async Task<string> HandleAsync(string question, ResponseContext context)
        {
            var response = new StringBuilder();

            await AppendTemplateAsync(response, "section_areas_of_concern",
                hardcodedFallback: "**Areas of Concern Analysis:**");

            if (context.CriticalAlerts.Any() || context.AbnormalValues.Any())
            {
                var concernsText = string.Join("\n", 
                    context.CriticalAlerts.Concat(context.AbnormalValues).Select(c => $"- {c}"));

                await AppendTemplateAsync(response, "concerns_detected",
                    new Dictionary<string, string> { { "CONCERNS_LIST", concernsText } },
                    hardcodedFallback: "🚨 **High Priority Concerns:**");
                
                response.AppendLine(concernsText);
            }
            else
            {
                await AppendTemplateAsync(response, "stable_status",
                    hardcodedFallback: "✅ No immediate concerns detected in the current data.");
            }

            return response.ToString().Trim();
        }
    }
}

