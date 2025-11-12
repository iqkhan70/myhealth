using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Extracts user questions from various text formats
    /// </summary>
    public class QuestionExtractor
    {
        private readonly IGenericQuestionPatternService _genericQuestionPatternService;
        private readonly ILogger<QuestionExtractor> _logger;

        public QuestionExtractor(
            IGenericQuestionPatternService genericQuestionPatternService,
            ILogger<QuestionExtractor> logger)
        {
            _genericQuestionPatternService = genericQuestionPatternService;
            _logger = logger;
        }

        public async Task<string> ExtractUserQuestionAsync(string text, bool isAiHealthCheck)
        {
            // Method 1: Look for "=== USER QUESTION ===" section
            var questionStart = text.IndexOf("=== USER QUESTION ===");
            if (questionStart >= 0)
            {
                var questionEnd = text.IndexOf("\n", questionStart + 21);
                if (questionEnd < 0) questionEnd = text.IndexOf("===", questionStart + 21);
                if (questionEnd < 0) questionEnd = text.Length;

                if (questionEnd > questionStart)
                {
                    var question = text.Substring(questionStart + 21, questionEnd - questionStart - 21).Trim();
                    if (!string.IsNullOrWhiteSpace(question))
                    {
                        _logger.LogInformation("Extracted from USER QUESTION section: '{Question}'", question);
                        return question;
                    }
                }
            }

            // For AI Health Check, don't look elsewhere
            if (isAiHealthCheck)
            {
                return "AI Health Check for Patient";
            }

            // Method 2: Look for question patterns in text (skip generic knowledge questions)
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (await _genericQuestionPatternService.IsGenericKnowledgeQuestionAsync(trimmedLine))
                {
                    continue;
                }

                if ((trimmedLine.Contains("how is") || trimmedLine.Contains("status") || 
                     trimmedLine.Contains("suggestions") || trimmedLine.Contains("snapshot") ||
                     trimmedLine.Contains("results") || trimmedLine.Contains("stats")) &&
                    (trimmedLine.Contains("?") || trimmedLine.StartsWith("how") || 
                     trimmedLine.StartsWith("what") || trimmedLine.StartsWith("where")))
                {
                    return trimmedLine;
                }
            }

            // Method 3: Look for last question-like line
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var trimmedLine = lines[i].Trim();
                if (await _genericQuestionPatternService.IsGenericKnowledgeQuestionAsync(trimmedLine))
                {
                    continue;
                }

                if (trimmedLine.Contains("?") && trimmedLine.Length > 5 && trimmedLine.Length < 100 &&
                    !trimmedLine.StartsWith("**") && !trimmedLine.StartsWith("📊") && 
                    !trimmedLine.StartsWith("🚨") && !trimmedLine.Contains("No uploaded"))
                {
                    return trimmedLine;
                }
            }

            return string.Empty;
        }
    }
}

