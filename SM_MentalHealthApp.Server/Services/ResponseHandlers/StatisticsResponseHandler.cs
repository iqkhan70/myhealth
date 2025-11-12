using System.Text;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Handles statistics-related questions (e.g., "show me stats", "what's the data?")
    /// </summary>
    public class StatisticsResponseHandler : BaseResponseHandler
    {
        public StatisticsResponseHandler(IAIResponseTemplateService templateService, ILogger<StatisticsResponseHandler> logger)
            : base(templateService, logger)
        {
        }

        public override Task<bool> CanHandleAsync(string question, ResponseContext context)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Task.FromResult(false);

            var lower = question.ToLower();
            return Task.FromResult(
                lower.Contains("stats") || 
                lower.Contains("statistics") || 
                lower.Contains("data") || 
                lower.Contains("snapshot") || 
                lower.Contains("results")
            );
        }

        public override async Task<string> HandleAsync(string question, ResponseContext context)
        {
            var response = new StringBuilder();

            await AppendTemplateAsync(response, "section_patient_medical_statistics",
                hardcodedFallback: "**Patient Medical Statistics:**");

            if (context.HasMedicalData)
            {
                await AppendTemplateAsync(response, "section_latest_medical_data",
                    hardcodedFallback: "📊 **Latest Medical Data:**");

                var criticalValuesText = ExtractCriticalValues(context.FullText);
                if (!string.IsNullOrEmpty(criticalValuesText))
                {
                    response.AppendLine(criticalValuesText);
                }
                else
                {
                    await AppendTemplateAsync(response, "medical_data_available",
                        hardcodedFallback: "Medical data available - review test results for specific values");
                }
            }
            else
            {
                await AppendTemplateAsync(response, "no_medical_data_statistics",
                    hardcodedFallback: "📊 **No recent medical data available for statistical analysis.**");
            }

            if (context.HasJournalEntries)
            {
                response.AppendLine();
                await AppendTemplateAsync(response, "section_mood_statistics",
                    hardcodedFallback: "**Mood Statistics:**");
                
                await AppendTemplateAsync(response, "mood_statistics_content",
                    hardcodedFallback: "- Recent entries show mixed mood patterns\n- Patient actively tracking health status");
            }

            return response.ToString().Trim();
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

