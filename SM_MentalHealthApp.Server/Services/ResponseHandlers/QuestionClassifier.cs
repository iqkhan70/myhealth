namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Classifies questions into different types for routing to appropriate handlers
    /// </summary>
    public class QuestionClassifier
    {
        public QuestionType Classify(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return QuestionType.Overview;

            var lower = question.ToLower();

            if (lower.Contains("status") || lower.Contains("how is") || lower.Contains("doing") || lower.Contains("condition"))
                return QuestionType.Status;

            if (lower.Contains("stats") || lower.Contains("statistics") || lower.Contains("data") || 
                lower.Contains("snapshot") || lower.Contains("results"))
                return QuestionType.Statistics;

            if (lower.Contains("suggestions") || lower.Contains("recommendations") || 
                lower.Contains("approach") || lower.Contains("what should") || lower.Contains("where should"))
                return QuestionType.Recommendations;

            if (lower.Contains("areas of concern") || lower.Contains("concerns"))
                return QuestionType.Concerns;

            return QuestionType.Overview;
        }
    }

    public enum QuestionType
    {
        Status,
        Statistics,
        Recommendations,
        Concerns,
        Overview
    }
}

