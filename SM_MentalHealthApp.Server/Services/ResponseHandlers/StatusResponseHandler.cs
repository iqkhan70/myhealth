using System.Text;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Handles status-related questions (e.g., "how is the patient?", "what's their status?")
    /// </summary>
    public class StatusResponseHandler : BaseResponseHandler
    {
        public StatusResponseHandler(IAIResponseTemplateService templateService, ILogger<StatusResponseHandler> logger)
            : base(templateService, logger)
        {
        }

        public override Task<bool> CanHandleAsync(string question, ResponseContext context)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Task.FromResult(false);

            var lower = question.ToLower();
            return Task.FromResult(
                lower.Contains("status") || 
                lower.Contains("how is") || 
                lower.Contains("doing") || 
                lower.Contains("condition")
            );
        }

        public override async Task<string> HandleAsync(string question, ResponseContext context)
        {
            var response = new StringBuilder();

            // Add header
            await AppendTemplateAsync(response, "section_patient_status_assessment", 
                fallbackKey: "fallback_clinical_assessment", 
                hardcodedFallback: "**Patient Status Assessment:**");

            // Handle based on context
            if (context.HasCriticalValues)
            {
                await HandleCriticalStatus(response, context);
            }
            else if (context.HasAnyConcerns)
            {
                await AppendTemplateAsync(response, "concerns_detected", 
                    hardcodedFallback: "⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.");
            }
            else if (context.HasNormalValues)
            {
                await AppendTemplateAsync(response, "stable_status", 
                    hardcodedFallback: "✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
            }
            else if (context.HasMedicalData)
            {
                await AppendTemplateAsync(response, "medical_data_warning", 
                    hardcodedFallback: "⚠️ **WARNING:** Medical content was found, but critical values may not have been properly detected.");
            }
            else
            {
                await AppendTemplateAsync(response, "status_review", 
                    hardcodedFallback: "📊 **Current Status:** Patient appears stable with no immediate concerns.");
            }

            // Add journal entries if available
            if (context.HasJournalEntries)
            {
                await AppendJournalSection(response, context);
            }

            return response.ToString().Trim();
        }

        private async Task HandleCriticalStatus(StringBuilder response, ResponseContext context)
        {
            var criticalValuesText = ExtractCriticalValuesFromContext(context.FullText);
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
                response.AppendLine();
                response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                response.AppendLine("- These values indicate a medical emergency");
                response.AppendLine("- Contact emergency services if symptoms worsen");
                response.AppendLine("- Patient needs immediate medical evaluation");
            }
        }

        private async Task AppendJournalSection(StringBuilder response, ResponseContext context)
        {
            var journalSection = string.Join("\n", context.JournalEntries.Take(3));
            await AppendTemplateAsync(response, "recent_patient_activity",
                new Dictionary<string, string> { { "JOURNAL_ENTRIES", journalSection } },
                "section_recent_activity",
                "**Recent Patient Activity:**");
        }

        private string ExtractCriticalValuesFromContext(string text)
        {
            // Simplified extraction - can be enhanced
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

