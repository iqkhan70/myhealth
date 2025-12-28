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
            // Method 1: Look for "=== USER QUESTION ===" section (most reliable)
            var questionStart = text.IndexOf("=== USER QUESTION ===", StringComparison.OrdinalIgnoreCase);
            if (questionStart >= 0)
            {
                var sectionStart = questionStart + 21; // Length of "=== USER QUESTION ==="

                // Find the end - look for next section marker (===) or end of text
                var nextSection = text.IndexOf("===", sectionStart);
                var newlineAfter = text.IndexOf("\n", sectionStart);

                int questionEnd;
                if (nextSection > sectionStart && (newlineAfter < 0 || nextSection < newlineAfter))
                {
                    questionEnd = nextSection;
                }
                else if (newlineAfter > sectionStart)
                {
                    questionEnd = newlineAfter;
                }
                else
                {
                    questionEnd = text.Length;
                }

                if (questionEnd > sectionStart)
                {
                    var question = text.Substring(sectionStart, questionEnd - sectionStart).Trim();

                    // Filter out instruction-like text (contains "INSTRUCTIONS", numbered lists, etc.)
                    if (!string.IsNullOrWhiteSpace(question) &&
                        !question.StartsWith("INSTRUCTIONS", StringComparison.OrdinalIgnoreCase) &&
                        !question.StartsWith("RESPONSE GUIDELINES", StringComparison.OrdinalIgnoreCase) &&
                        !question.StartsWith("1.", StringComparison.OrdinalIgnoreCase) &&
                        !question.StartsWith("2.", StringComparison.OrdinalIgnoreCase) &&
                        !question.StartsWith("3.", StringComparison.OrdinalIgnoreCase) &&
                        !question.Contains("If the question is NOT related") &&
                        question.Length < 500) // Reasonable question length
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

            // Method 2: Look for "User question:" pattern (case-insensitive, for other prompt formats)
            var userQuestionPattern = "user question:";
            var userQuestionIndex = text.IndexOf(userQuestionPattern, StringComparison.OrdinalIgnoreCase);
            if (userQuestionIndex >= 0)
            {
                var startIndex = userQuestionIndex + userQuestionPattern.Length;
                // Find the end - look for next line or section
                var endIndex = text.IndexOf("\n", startIndex);
                if (endIndex < 0) endIndex = text.IndexOf("===", startIndex);
                if (endIndex < 0) endIndex = text.Length;

                if (endIndex > startIndex)
                {
                    var question = text.Substring(startIndex, endIndex - startIndex).Trim();

                    // Filter out instruction-like text
                    if (!string.IsNullOrWhiteSpace(question) &&
                        !question.StartsWith("INSTRUCTIONS", StringComparison.OrdinalIgnoreCase) &&
                        !question.StartsWith("RESPONSE GUIDELINES", StringComparison.OrdinalIgnoreCase) &&
                        !question.StartsWith("1.", StringComparison.OrdinalIgnoreCase) &&
                        !question.StartsWith("2.", StringComparison.OrdinalIgnoreCase) &&
                        !question.StartsWith("3.", StringComparison.OrdinalIgnoreCase) &&
                        !question.Contains("If the question is NOT related") &&
                        question.Length < 500)
                    {
                        _logger.LogInformation("Extracted from 'user question:' pattern: '{Question}'", question);
                        return question;
                    }
                }
            }

            // Method 3: Look for question patterns in text (skip generic knowledge questions and instructions)
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip instruction sections
                if (trimmedLine.StartsWith("===") &&
                    (trimmedLine.Contains("INSTRUCTIONS", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("GUIDELINES", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (await _genericQuestionPatternService.IsGenericKnowledgeQuestionAsync(trimmedLine))
                {
                    continue;
                }

                // Skip lines that look like instructions
                if (trimmedLine.StartsWith("1.") || trimmedLine.StartsWith("2.") || trimmedLine.StartsWith("3.") ||
                    trimmedLine.Contains("If the question is NOT related"))
                {
                    continue;
                }

                if ((trimmedLine.Contains("how is") || trimmedLine.Contains("status") ||
                     trimmedLine.Contains("suggestions") || trimmedLine.Contains("snapshot") ||
                     trimmedLine.Contains("results") || trimmedLine.Contains("stats") ||
                     trimmedLine.Contains("who is") || trimmedLine.Contains("what is") ||
                     trimmedLine.Contains("who are") || trimmedLine.Contains("what are")) &&
                    (trimmedLine.Contains("?") || trimmedLine.StartsWith("how") ||
                     trimmedLine.StartsWith("what") || trimmedLine.StartsWith("where") ||
                     trimmedLine.StartsWith("who")))
                {
                    if (trimmedLine.Length < 200) // Reasonable question length
                    {
                        _logger.LogInformation("Extracted from question pattern: '{Question}'", trimmedLine);
                        return trimmedLine;
                    }
                }
            }

            // Method 4: Look for last question-like line (fallback)
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var trimmedLine = lines[i].Trim();

                // Skip instruction sections
                if (trimmedLine.StartsWith("===") &&
                    (trimmedLine.Contains("INSTRUCTIONS", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("GUIDELINES", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (await _genericQuestionPatternService.IsGenericKnowledgeQuestionAsync(trimmedLine))
                {
                    continue;
                }

                // Skip lines that look like instructions
                if (trimmedLine.StartsWith("1.") || trimmedLine.StartsWith("2.") || trimmedLine.StartsWith("3.") ||
                    trimmedLine.Contains("If the question is NOT related"))
                {
                    continue;
                }

                if (trimmedLine.Contains("?") && trimmedLine.Length > 3 && trimmedLine.Length < 200 &&
                    !trimmedLine.StartsWith("**") && !trimmedLine.StartsWith("📊") &&
                    !trimmedLine.StartsWith("🚨") && !trimmedLine.Contains("No uploaded") &&
                    !trimmedLine.StartsWith("INSTRUCTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Extracted from last question-like line: '{Question}'", trimmedLine);
                    return trimmedLine;
                }
            }

            return string.Empty;
        }
    }
}

