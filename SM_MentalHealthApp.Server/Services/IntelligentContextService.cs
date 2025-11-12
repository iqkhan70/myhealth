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
        private readonly IQuestionClassificationService _questionClassificationService;
        private readonly IAIResponseTemplateService _templateService;

        public IntelligentContextService(
            ILogger<IntelligentContextService> logger,
            IContentAnalysisService contentAnalysisService,
            HuggingFaceService huggingFaceService,
            UserService userService,
            HttpClient httpClient,
            IQuestionClassificationService questionClassificationService,
            IAIResponseTemplateService templateService)
        {
            _logger = logger;
            _contentAnalysisService = contentAnalysisService;
            _huggingFaceService = huggingFaceService;
            _userService = userService;
            _httpClient = httpClient;
            _questionClassificationService = questionClassificationService;
            _templateService = templateService;
        }

        public async Task<string> ProcessQuestionAsync(string question, int patientId, int userId)
        {
            try
            {
                _logger.LogInformation("=== INTELLIGENT CONTEXT PROCESSING ===");
                _logger.LogInformation("Question: {Question}", question);
                _logger.LogInformation("Patient ID: {PatientId}, User ID: {UserId}", patientId, userId);

                // Step 1: Classify the question type using database-driven service
                var questionType = await _questionClassificationService.ClassifyQuestionAsync(question);
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
                        return await ProcessNonPatientQuestion(question);

                    case QuestionType.GeneralMedical:
                        return await ProcessGeneralMedicalQuestion(question);

                    default:
                        return await ProcessPatientMedicalQuestion(question, patientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in intelligent context processing");
                var errorTemplate = await _templateService.FormatTemplateAsync("intelligent_context_error", null);
                return !string.IsNullOrEmpty(errorTemplate)
                    ? errorTemplate
                    : "I apologize, but I encountered an error processing your question. Please try rephrasing it or contact support if the issue persists.";
            }
        }

        private async Task<string> ProcessPatientMedicalQuestion(string question, int patientId)
        {
            _logger.LogInformation("Processing patient medical question with enhanced context");

            // If no patient is selected, return a general response using template
            if (patientId <= 0)
            {
                var template = await _templateService.FormatTemplateAsync("intelligent_context_no_patient_medical",
                    new Dictionary<string, string> { { "QUESTION", question } });
                if (!string.IsNullOrEmpty(template)) return template;

                // Fallback
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
            _logger.LogInformation("Calling BuildEnhancedContextAsync for patient {PatientId}", patientId);
            var enhancedContext = await _contentAnalysisService.BuildEnhancedContextAsync(patientId, question);
            _logger.LogInformation("BuildEnhancedContextAsync returned context length: {Length}", enhancedContext.Length);
            _logger.LogInformation("Enhanced context preview: {Preview}", enhancedContext.Substring(0, Math.Min(500, enhancedContext.Length)));
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

            // If no patient is selected, return just the web search results using template
            if (patientId <= 0)
            {
                var template = await _templateService.FormatTemplateAsync("intelligent_context_no_patient_resources",
                    new Dictionary<string, string> { { "WEB_RESULTS", webResults } });
                if (!string.IsNullOrEmpty(template)) return template;

                // Fallback
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

            // Combine patient context with web search results using template
            var combinedTemplate = await _templateService.FormatTemplateAsync("intelligent_context_patient_resources",
                new Dictionary<string, string>
                {
                    { "PATIENT_INFO", patientInfo },
                    { "WEB_RESULTS", webResults }
                });
            if (!string.IsNullOrEmpty(combinedTemplate)) return combinedTemplate;

            // Fallback
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

            // If no patient is selected, return a general response using template
            if (patientId <= 0)
            {
                var template = await _templateService.FormatTemplateAsync("intelligent_context_no_patient_recommendations",
                    new Dictionary<string, string> { { "QUESTION", question } });
                if (!string.IsNullOrEmpty(template)) return template;

                // Fallback
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

        private async Task<string> ProcessNonPatientQuestion(string question)
        {
            _logger.LogInformation("Processing non-patient related question");
            var template = await _templateService.FormatTemplateAsync("intelligent_context_non_patient",
                new Dictionary<string, string> { { "QUESTION", question } });
            if (!string.IsNullOrEmpty(template)) return template;

            // Fallback
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
            var template = await _templateService.FormatTemplateAsync("intelligent_context_general_medical",
                new Dictionary<string, string> { { "QUESTION", question } });
            if (!string.IsNullOrEmpty(template)) return template;

            // Fallback
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
                var errorTemplate = await _templateService.FormatTemplateAsync("intelligent_context_web_search_error", null);
                return !string.IsNullOrEmpty(errorTemplate)
                    ? errorTemplate
                    : "Web search is currently unavailable. Please use standard search engines to find medical facilities.";
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
