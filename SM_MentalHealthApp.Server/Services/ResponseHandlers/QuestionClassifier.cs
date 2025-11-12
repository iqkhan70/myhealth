using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Classifies questions into different types for routing to appropriate handlers
    /// Note: This uses a different QuestionType enum than IntelligentContextService
    /// This is for response handler routing, not for intelligent context processing
    /// </summary>
    public class QuestionClassifier
    {
        public HandlerQuestionType Classify(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return HandlerQuestionType.Overview;

            var lower = question.ToLower();

            if (lower.Contains("status") || lower.Contains("how is") || lower.Contains("doing") || lower.Contains("condition"))
                return HandlerQuestionType.Status;

            if (lower.Contains("stats") || lower.Contains("statistics") || lower.Contains("data") || 
                lower.Contains("snapshot") || lower.Contains("results"))
                return HandlerQuestionType.Statistics;

            if (lower.Contains("suggestions") || lower.Contains("recommendations") || 
                lower.Contains("approach") || lower.Contains("what should") || lower.Contains("where should"))
                return HandlerQuestionType.Recommendations;

            if (lower.Contains("areas of concern") || lower.Contains("concerns"))
                return HandlerQuestionType.Concerns;

            return HandlerQuestionType.Overview;
        }
    }

    /// <summary>
    /// Question types for response handler routing (different from IntelligentContextService.QuestionType)
    /// </summary>
    public enum HandlerQuestionType
    {
        Status,
        Statistics,
        Recommendations,
        Concerns,
        Overview
    }
}

