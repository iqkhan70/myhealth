using System.Text;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Default handler for general overview questions or when no specific handler matches
    /// </summary>
    public class OverviewResponseHandler : BaseResponseHandler
    {
        public OverviewResponseHandler(IAIResponseTemplateService templateService, ILogger<OverviewResponseHandler> logger)
            : base(templateService, logger)
        {
        }

        public override Task<bool> CanHandleAsync(string question, ResponseContext context)
        {
            // This is the default handler - always returns true
            return Task.FromResult(true);
        }

        public override async Task<string> HandleAsync(string question, ResponseContext context)
        {
            var response = new StringBuilder();

            // Add overview header
            await AppendTemplateAsync(response, "section_patient_overview",
                fallbackKey: "fallback_patient_overview",
                hardcodedFallback: "**Patient Medical Overview:**");

            // Handle based on context - prioritize concerns over normal values
            // IMPORTANT: Check concerns BEFORE normal values to avoid false "STABLE" status
            if (context.HasCriticalValues)
            {
                await HandleCriticalOverview(response, context);
            }
            else if (context.HasAnyConcerns)
            {
                // HasAnyConcerns includes: HasAbnormalValues OR hasHighConcern OR hasDistress
                // This catches clinical notes with "serious symptoms", "anxiety", "high blood pressure", etc.
                await AppendTemplateAsync(response, "concerns_detected",
                    hardcodedFallback: "⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations.");
            }
            else if (context.HasNormalValues && !context.HasAnyConcerns)
            {
                // Only show stable if we have normal values AND no concerns
                // This prevents showing "STABLE" when there are concerns that weren't detected
                await AppendTemplateAsync(response, "stable_status",
                    hardcodedFallback: "✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
            }
            else
            {
                await AppendTemplateAsync(response, "status_review",
                    hardcodedFallback: "📊 **Current Status:** Patient appears stable with no immediate concerns.");
            }

            // Add activity section
            if (context.HasJournalEntries)
            {
                response.AppendLine();
                await AppendTemplateAsync(response, "section_recent_activity",
                    fallbackKey: "fallback_recent_activity",
                    hardcodedFallback: "**Recent Patient Activity:**");
                
                var journalSection = string.Join("\n", context.JournalEntries.Take(3));
                response.AppendLine(journalSection);
            }

            return response.ToString().Trim();
        }

        private async Task HandleCriticalOverview(StringBuilder response, ResponseContext context)
        {
            var criticalValuesText = ExtractCriticalValues(context.FullText);
            var criticalAlertText = criticalValuesText ?? "- Critical medical values detected - review test results for details";

            var template = await GetTemplateAsync("critical_alert",
                new Dictionary<string, string> { { "CRITICAL_VALUES", criticalAlertText } },
                "fallback_critical_alert_header",
                "🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");

            if (!string.IsNullOrEmpty(template))
            {
                response.AppendLine(template);
            }
            else
            {
                response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                response.AppendLine(criticalAlertText);
            }
        }

        private string ExtractCriticalValues(string text)
        {
            var criticalStart = text.IndexOf("Critical Values Found:");
            if (criticalStart >= 0)
            {
                var sectionEnd = text.IndexOf("\n\n", criticalStart);
                if (sectionEnd < 0) sectionEnd = text.Length;
                var section = text.Substring(criticalStart, sectionEnd - criticalStart);
                var lines = section.Split('\n')
                    .Where(l => l.Contains("🚨") && l.Trim().Length > 0)
                    .Select(l => $"- {l.Trim()}")
                    .ToList();
                return string.Join("\n", lines);
            }
            return string.Empty;
        }
    }
}

