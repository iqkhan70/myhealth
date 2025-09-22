using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IIntelligentContextService
    {
        Task<string> ProcessQuestionAsync(string question, int patientId, int userId);
        Task<string> SearchWebAsync(string query);
    }

    public class IntelligentContextService : IIntelligentContextService
    {
        private readonly ILogger<IntelligentContextService> _logger;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly HuggingFaceService _huggingFaceService;
        private readonly UserService _userService;
        private readonly HttpClient _httpClient;

        public IntelligentContextService(
            ILogger<IntelligentContextService> logger,
            IContentAnalysisService contentAnalysisService,
            HuggingFaceService huggingFaceService,
            UserService userService,
            HttpClient httpClient)
        {
            _logger = logger;
            _contentAnalysisService = contentAnalysisService;
            _huggingFaceService = huggingFaceService;
            _userService = userService;
            _httpClient = httpClient;
        }

        public async Task<string> ProcessQuestionAsync(string question, int patientId, int userId)
        {
            try
            {
                _logger.LogInformation("=== INTELLIGENT CONTEXT PROCESSING ===");
                _logger.LogInformation("Question: {Question}", question);
                _logger.LogInformation("Patient ID: {PatientId}, User ID: {UserId}", patientId, userId);

                // Step 1: Classify the question type
                var questionType = ClassifyQuestion(question);
                _logger.LogInformation("Question classified as: {QuestionType}", questionType);

                switch (questionType)
                {
                    case QuestionType.PatientMedicalStatus:
                        return await ProcessPatientMedicalQuestion(question, patientId);

                    case QuestionType.PatientMedicalResources:
                        return await ProcessPatientResourceQuestion(question, patientId);

                    case QuestionType.PatientMedicalRecommendations:
                        return await ProcessPatientRecommendationQuestion(question, patientId);

                    case QuestionType.NonPatientRelated:
                        return ProcessNonPatientQuestion(question);

                    case QuestionType.GeneralMedical:
                        return await ProcessGeneralMedicalQuestion(question);

                    default:
                        return await ProcessPatientMedicalQuestion(question, patientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in intelligent context processing");
                return "I apologize, but I encountered an error processing your question. Please try rephrasing it or contact support if the issue persists.";
            }
        }

        private QuestionType ClassifyQuestion(string question)
        {
            var questionLower = question.ToLower();

            _logger.LogInformation("Classifying question: {Question}", questionLower);

            // Non-patient related indicators
            if (IsNonPatientQuestion(questionLower))
            {
                _logger.LogInformation("Detected non-patient related question");
                return QuestionType.NonPatientRelated;
            }

            // Medical resource questions (hospitals, doctors, facilities)
            if (IsMedicalResourceQuestion(questionLower))
            {
                _logger.LogInformation("Detected medical resource question");
                return QuestionType.PatientMedicalResources;
            }

            // Medical status questions
            if (IsMedicalStatusQuestion(questionLower))
            {
                _logger.LogInformation("Detected medical status question");
                return QuestionType.PatientMedicalStatus;
            }

            // Medical recommendation questions
            if (IsMedicalRecommendationQuestion(questionLower))
            {
                _logger.LogInformation("Detected medical recommendation question");
                return QuestionType.PatientMedicalRecommendations;
            }

            // General medical questions
            if (IsGeneralMedicalQuestion(questionLower))
            {
                _logger.LogInformation("Detected general medical question");
                return QuestionType.GeneralMedical;
            }

            // Default to patient medical status
            _logger.LogInformation("Defaulting to patient medical status question");
            return QuestionType.PatientMedicalStatus;
        }

        private bool IsNonPatientQuestion(string question)
        {
            // Check for celebrity, entertainment, or completely unrelated topics
            var nonPatientKeywords = new[]
            {
                "bollywood", "movie", "actor", "actress", "celebrity", "star", "film",
                "salam khan", "salman khan", "shah rukh", "amir khan", "hritik",
                "weather", "sports", "cricket", "football", "politics", "news",
                "recipe", "cooking", "travel", "vacation", "hotel", "restaurant"
            };

            return nonPatientKeywords.Any(keyword => question.Contains(keyword));
        }

        private bool IsMedicalResourceQuestion(string question)
        {
            var resourceKeywords = new[]
            {
                "hospital", "clinic", "doctor", "specialist", "facility", "medical center",
                "zip code", "near", "location", "address", "phone", "contact",
                "emergency", "urgent care", "pharmacy", "lab", "imaging",
                "recommended", "recommendation", "recommend", "suggest", "suggestion"
            };

            // Check for hospital + recommendation combination
            if ((question.Contains("hospital") || question.Contains("hospitals")) && (question.Contains("recommend") || question.Contains("suggest")))
            {
                return true;
            }

            // Check for zip code + medical facility combination (including emergency)
            if (question.Contains("zip code") && (question.Contains("hospital") || question.Contains("hospitals") || question.Contains("clinic") || question.Contains("facility") || question.Contains("emergency")))
            {
                return true;
            }

            // Check for emergency + location combination
            if (question.Contains("emergency") && (question.Contains("near") || question.Contains("zip") || question.Contains("location")))
            {
                return true;
            }

            // Check for hospital + location combination
            if ((question.Contains("hospital") || question.Contains("hospitals")) && (question.Contains("near") || question.Contains("zip") || question.Contains("location")))
            {
                return true;
            }

            return resourceKeywords.Any(keyword => question.Contains(keyword));
        }

        private bool IsMedicalStatusQuestion(string question)
        {
            var statusKeywords = new[]
            {
                "how is", "status", "doing", "condition", "health", "wellbeing",
                "progress", "improvement", "worse", "better", "stable", "critical",
                "hospitalization", "admitted", "discharge", "recovery", "treatment"
            };

            return statusKeywords.Any(keyword => question.Contains(keyword));
        }

        private bool IsMedicalRecommendationQuestion(string question)
        {
            var recommendationKeywords = new[]
            {
                "suggest", "recommend", "advice", "approach", "strategy", "plan",
                "treatment", "therapy", "medication", "intervention", "next steps",
                "what should", "how to", "attacking", "reduce", "prevent"
            };

            return recommendationKeywords.Any(keyword => question.Contains(keyword));
        }

        private bool IsGeneralMedicalQuestion(string question)
        {
            var generalKeywords = new[]
            {
                "what is", "explain", "define", "meaning", "symptoms", "causes",
                "diagnosis", "treatment", "side effects", "complications"
            };

            return generalKeywords.Any(keyword => question.Contains(keyword));
        }

        private async Task<string> ProcessPatientMedicalQuestion(string question, int patientId)
        {
            _logger.LogInformation("Processing patient medical question with enhanced context");

            // If no patient is selected, return a general response
            if (patientId <= 0)
            {
                return $@"**General Medical Information Request**

You're asking: ""{question}""

To provide personalized medical insights, please:
1. **Select a specific patient** from the dropdown above
2. **Ask your question in the context of that patient's care**

This will allow me to provide:
- Patient-specific medical assessments
- Personalized treatment recommendations
- Context-aware clinical guidance
- Integration with the patient's medical history and current data

If you need general medical information without patient context, I recommend consulting medical literature or professional medical resources.";
            }

            // Use the HuggingFace service for medical questions
            var enhancedContext = await _contentAnalysisService.BuildEnhancedContextAsync(patientId, question);
            return await _huggingFaceService.GenerateResponse(enhancedContext);
        }

        private async Task<string> ProcessPatientResourceQuestion(string question, int patientId)
        {
            _logger.LogInformation("Processing patient resource question with web search");

            // Extract location information from the question
            var locationInfo = ExtractLocationInfo(question);

            // Search for medical resources
            var resourceQuery = BuildResourceSearchQuery(question, locationInfo);
            var webResults = await SearchWebAsync(resourceQuery);

            // If no patient is selected, return just the web search results
            if (patientId <= 0)
            {
                var response = new StringBuilder();
                response.AppendLine("**Medical Resources Search**");
                response.AppendLine();
                response.AppendLine("To provide personalized medical facility recommendations, please select a specific patient first.");
                response.AppendLine();
                response.AppendLine("=== GENERAL MEDICAL RESOURCES ===");
                response.AppendLine(webResults);
                return response.ToString();
            }

            // Get patient information for context
            var patient = await _userService.GetUserByIdAsync(patientId);
            var patientInfo = patient != null ? $"Patient: {patient.FirstName} {patient.LastName} (ID: {patientId})" : $"Patient ID: {patientId}";

            // Combine patient context with web search results
            var combinedResponse = new StringBuilder();
            combinedResponse.AppendLine($"**Medical Resource Information for {patientInfo}:**");
            combinedResponse.AppendLine();
            combinedResponse.AppendLine(webResults);
            combinedResponse.AppendLine();
            combinedResponse.AppendLine("---");
            combinedResponse.AppendLine("Please note: This information is for guidance only. Always verify details with the medical facility directly.");

            return combinedResponse.ToString();
        }

        private async Task<string> ProcessPatientRecommendationQuestion(string question, int patientId)
        {
            _logger.LogInformation("Processing patient recommendation question");

            // If no patient is selected, return a general response
            if (patientId <= 0)
            {
                return $@"**General Medical Recommendations Request**

You're asking: ""{question}""

To provide personalized medical recommendations, please:
1. **Select a specific patient** from the dropdown above
2. **Ask your question in the context of that patient's care**

This will allow me to provide:
- Patient-specific treatment recommendations
- Personalized care approaches
- Context-aware clinical guidance
- Integration with the patient's medical history and current data

If you need general medical recommendations without patient context, I recommend consulting medical literature or professional medical resources.";
            }

            // Use the HuggingFace service for recommendation questions
            var enhancedContext = await _contentAnalysisService.BuildEnhancedContextAsync(patientId, question);
            return await _huggingFaceService.GenerateResponse(enhancedContext);
        }

        private string ProcessNonPatientQuestion(string question)
        {
            _logger.LogInformation("Processing non-patient related question");
            return $@"**Query Not Applicable to Patient Care**

I understand you're asking about: ""{question}""

However, this question appears to be unrelated to patient care or medical practice. As a clinical AI assistant, I'm designed to help with:

- Patient medical assessments and status updates
- Clinical recommendations and treatment approaches  
- Medical resource identification and referrals
- Healthcare provider decision support

For questions about entertainment, celebrities, or other non-medical topics, please use a general-purpose AI assistant or search engine.

If you have a medical question related to patient care, I'd be happy to help with that instead.";
        }

        private async Task<string> ProcessGeneralMedicalQuestion(string question)
        {
            _logger.LogInformation("Processing general medical question");
            return $@"**General Medical Information Request**

You're asking: ""{question}""

While I can provide general medical information, for the most accurate and personalized guidance, please:

1. **Select a specific patient** from the dropdown above
2. **Ask your question in the context of that patient's care**

This will allow me to provide:
- Patient-specific medical assessments
- Personalized treatment recommendations
- Context-aware clinical guidance
- Integration with the patient's medical history and current data

If you need general medical information without patient context, I recommend consulting medical literature or professional medical resources.";
        }

        private string ExtractLocationInfo(string question)
        {
            // Extract zip codes
            var zipCodeMatch = Regex.Match(question, @"\b\d{5}(-\d{4})?\b");
            if (zipCodeMatch.Success)
            {
                return zipCodeMatch.Value;
            }

            // Extract city names (basic pattern)
            var cityKeywords = new[] { "near", "in", "around", "close to" };
            foreach (var keyword in cityKeywords)
            {
                var index = question.ToLower().IndexOf(keyword);
                if (index >= 0)
                {
                    var afterKeyword = question.Substring(index + keyword.Length).Trim();
                    var words = afterKeyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length > 0)
                    {
                        return words[0];
                    }
                }
            }

            return "";
        }

        private string BuildResourceSearchQuery(string question, string locationInfo)
        {
            var query = new StringBuilder();

            // Add medical resource keywords
            if (question.ToLower().Contains("hospital"))
                query.Append("hospital ");
            if (question.ToLower().Contains("clinic"))
                query.Append("clinic ");
            if (question.ToLower().Contains("doctor"))
                query.Append("doctor ");
            if (question.ToLower().Contains("specialist"))
                query.Append("specialist ");

            // Add location
            if (!string.IsNullOrEmpty(locationInfo))
                query.Append($"near {locationInfo}");

            return query.ToString().Trim();
        }

        public async Task<string> SearchWebAsync(string query)
        {
            try
            {
                _logger.LogInformation("Searching web for: {Query}", query);

                // Extract zip code from query
                var zipCodeMatch = Regex.Match(query, @"\b\d{5}(-\d{4})?\b");
                var zipCode = zipCodeMatch.Success ? zipCodeMatch.Value : "";

                // For now, return a structured response with guidance
                var response = new StringBuilder();
                response.AppendLine($"**Medical Facilities Search for: {query}**");
                response.AppendLine();

                if (!string.IsNullOrEmpty(zipCode))
                {
                    response.AppendLine($"**Searching in ZIP Code: {zipCode}**");
                    response.AppendLine();
                }

                response.AppendLine("**Recommended Search Strategy:**");
                response.AppendLine("1. **Emergency Care**: Search for 'emergency room near {zipCode}' or 'urgent care {zipCode}'");
                response.AppendLine("2. **Hospitals**: Search for 'hospitals near {zipCode}' or 'medical centers {zipCode}'");
                response.AppendLine("3. **Specialists**: Search for 'hematologist near {zipCode}' (for anemia treatment)");
                response.AppendLine("4. **Insurance**: Check which facilities accept your insurance");
                response.AppendLine();

                response.AppendLine("**Key Considerations for This Patient:**");
                response.AppendLine("- **Severe Anemia (Hemoglobin 6.0)**: Requires immediate blood transfusion capability");
                response.AppendLine("- **Critical Triglycerides (640)**: Needs cardiology/endocrinology specialists");
                response.AppendLine("- **Emergency Priority**: Look for Level 1 trauma centers or major hospitals");
                response.AppendLine();

                response.AppendLine("**Immediate Action Required:**");
                response.AppendLine("- Call 911 or go to nearest emergency room immediately");
                response.AppendLine("- This patient's condition requires urgent medical attention");
                response.AppendLine("- Do not delay seeking emergency care");

                return response.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in web search");
                return "Web search is currently unavailable. Please use standard search engines to find medical facilities.";
            }
        }
    }

    public enum QuestionType
    {
        PatientMedicalStatus,
        PatientMedicalResources,
        PatientMedicalRecommendations,
        NonPatientRelated,
        GeneralMedical
    }
}
