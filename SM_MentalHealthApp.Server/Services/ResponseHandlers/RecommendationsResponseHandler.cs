using System.Text;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Handles recommendations-related questions (e.g., "what should I do?", "suggestions?")
    /// </summary>
    public class RecommendationsResponseHandler : BaseResponseHandler
    {
        public RecommendationsResponseHandler(IAIResponseTemplateService templateService, ILogger<RecommendationsResponseHandler> logger)
            : base(templateService, logger)
        {
        }

        public override Task<bool> CanHandleAsync(string question, ResponseContext context)
        {
            if (string.IsNullOrWhiteSpace(question))
                return Task.FromResult(false);

            var lower = question.ToLower();
            return Task.FromResult(
                lower.Contains("suggestions") || 
                lower.Contains("recommendations") || 
                lower.Contains("approach") || 
                lower.Contains("what should") || 
                lower.Contains("where should")
            );
        }

        public override async Task<string> HandleAsync(string question, ResponseContext context)
        {
            var response = new StringBuilder();

            await AppendTemplateAsync(response, "section_clinical_recommendations",
                fallbackKey: "fallback_recommendations",
                hardcodedFallback: "**Clinical Recommendations:**");

            if (context.HasCriticalValues)
            {
                await AppendTemplateAsync(response, "recommendations_critical_detailed",
                    hardcodedFallback: "🚨 **IMMEDIATE ACTIONS REQUIRED:**\n1. **Emergency Medical Care**: Contact emergency services immediately\n2. **Hospital Admission**: Patient requires immediate hospitalization\n3. **Specialist Consultation**: Refer to appropriate specialist\n4. **Continuous Monitoring**: Vital signs every 15 minutes\n5. **Immediate Intervention**: Consider immediate medical intervention based on critical values");
            }
            else if (context.HasAnyConcerns)
            {
                await AppendTemplateAsync(response, "recommendations_abnormal_detailed",
                    hardcodedFallback: "⚠️ **MEDICAL MANAGEMENT NEEDED:**\n1. **Primary Care Follow-up**: Schedule appointment within 24-48 hours\n2. **Laboratory Monitoring**: Repeat blood work in 1-2 weeks\n3. **Lifestyle Modifications**: Dietary changes and exercise recommendations\n4. **Medication Review**: Assess current medications and interactions");
            }
            else
            {
                await AppendTemplateAsync(response, "recommendations_general",
                    hardcodedFallback: "📋 **General Recommendations:**\n1. **Regular Monitoring**: Schedule routine follow-up appointments\n2. **Lifestyle Modifications**: Dietary changes and exercise recommendations\n3. **Medication Review**: Assess current medications and interactions");
            }

            return response.ToString().Trim();
        }
    }
}

