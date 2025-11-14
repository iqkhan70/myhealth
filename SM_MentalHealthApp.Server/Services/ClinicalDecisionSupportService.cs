using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Server.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IClinicalDecisionSupportService
    {
        Task<ClinicalRecommendation> GetRecommendationsAsync(string diagnosis, int patientId, int doctorId);
        Task<List<FollowUpStep>> GetFollowUpStepsAsync(string diagnosis, string severity);
        Task<List<InsuranceRequirement>> GetInsuranceRequirementsAsync(string diagnosis);
        Task<ClinicalProtocol> GetClinicalProtocolAsync(string diagnosis);
    }

    public class ClinicalDecisionSupportService : IClinicalDecisionSupportService
    {
        private readonly ILogger<ClinicalDecisionSupportService> _logger;
        private readonly JournalDbContext _context;
        private readonly LlmClient _llmClient;
        private readonly IContentAnalysisService _contentAnalysisService;

        public ClinicalDecisionSupportService(
            ILogger<ClinicalDecisionSupportService> logger,
            JournalDbContext context,
            LlmClient llmClient,
            IContentAnalysisService contentAnalysisService)
        {
            _logger = logger;
            _context = context;
            _llmClient = llmClient;
            _contentAnalysisService = contentAnalysisService;
        }

        public async Task<ClinicalRecommendation> GetRecommendationsAsync(string diagnosis, int patientId, int doctorId)
        {
            try
            {
                _logger.LogInformation("Getting clinical recommendations for diagnosis: {Diagnosis}, Patient: {PatientId}", diagnosis, patientId);

                // Get patient context
                var patient = await _context.Users.FirstOrDefaultAsync(u => u.Id == patientId);
                var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == doctorId);

                if (patient == null || doctor == null)
                {
                    throw new ArgumentException("Patient or doctor not found");
                }

                // Get recent patient data for context (excluding ignored entries)
                var recentJournalEntries = await _context.JournalEntries
                    .Where(j => j.UserId == patientId && !j.IsIgnoredByDoctor)
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                var recentMoodEntries = await _context.JournalEntries
                    .Where(j => j.UserId == patientId && !j.IsIgnoredByDoctor && !string.IsNullOrEmpty(j.Mood))
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Get content analysis for medical data (excluding ignored content)
                var contentAnalyses = await _contentAnalysisService.GetContentAnalysisForPatientAsync(patientId);

                // Build context for AI (includes journal entries and content analysis)
                var patientContext = await BuildPatientContextAsync(patient, recentJournalEntries, recentMoodEntries, contentAnalyses);

                // Get AI recommendations
                var recommendations = await GetAIRecommendations(diagnosis, patientContext);

                // Get standardized protocols
                var protocols = await GetClinicalProtocolAsync(diagnosis);

                // Get follow-up steps
                var followUpSteps = await GetFollowUpStepsAsync(diagnosis, recommendations.Severity);

                // Get insurance requirements
                var insuranceRequirements = await GetInsuranceRequirementsAsync(diagnosis);

                return new ClinicalRecommendation
                {
                    Diagnosis = diagnosis,
                    PatientId = patientId,
                    DoctorId = doctorId,
                    GeneratedAt = DateTime.UtcNow,
                    Severity = recommendations.Severity,
                    ImmediateActions = recommendations.ImmediateActions,
                    FollowUpSteps = followUpSteps,
                    ClinicalProtocol = protocols,
                    InsuranceRequirements = insuranceRequirements,
                    PatientSpecificNotes = recommendations.PatientSpecificNotes,
                    RiskFactors = recommendations.RiskFactors,
                    Contraindications = recommendations.Contraindications,
                    AlternativeTreatments = recommendations.AlternativeTreatments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical recommendations for diagnosis: {Diagnosis}", diagnosis);
                throw;
            }
        }

        public async Task<List<FollowUpStep>> GetFollowUpStepsAsync(string diagnosis, string severity)
        {
            try
            {
                var prompt = $@"
As a clinical decision support system, provide evidence-based follow-up steps for a patient diagnosed with: {diagnosis}
Severity Level: {severity}

Please provide a structured list of follow-up steps including:
1. Immediate follow-up (within 24-48 hours)
2. Short-term follow-up (within 1-2 weeks)
3. Medium-term follow-up (within 1-3 months)
4. Long-term follow-up (within 3-6 months)

For each step, include:
- Specific action required
- Timeline
- Responsible party (doctor, patient, specialist)
- Documentation requirements
- Insurance considerations

Format as JSON with the following structure:
{{
  ""followUpSteps"": [
    {{
      ""step"": ""string"",
      ""timeline"": ""string"",
      ""responsibleParty"": ""string"",
      ""documentationRequired"": ""string"",
      ""insuranceConsiderations"": ""string"",
      ""priority"": ""High|Medium|Low""
    }}
  ]
}}";

                var modelName = "tinyllama:latest";
                var request = new LlmRequest
                {
                    Prompt = prompt,
                    Provider = AiProvider.Ollama,
                    Model = modelName,
                    Temperature = 0.3,
                    MaxTokens = 1000
                };
                _logger.LogInformation("Calling Ollama model: {ModelName} for follow-up steps (diagnosis: {Diagnosis})", modelName, diagnosis);
                var response = await _llmClient.GenerateTextAsync(request);
                var responseText = response.Text;
                _logger.LogInformation("Ollama response from model {ModelName} ({Provider}): {Length} characters", modelName, response.Provider, responseText?.Length ?? 0);

                // Clean the response and extract structured data
                var cleanText = CleanOllamaResponse(responseText);
                var steps = ParseFollowUpStepsFromText(cleanText);

                return steps.Any() ? steps : GetDefaultFollowUpSteps(diagnosis, severity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting follow-up steps for diagnosis: {Diagnosis}", diagnosis);
                return GetDefaultFollowUpSteps(diagnosis, severity);
            }
        }

        public async Task<List<InsuranceRequirement>> GetInsuranceRequirementsAsync(string diagnosis)
        {
            try
            {
                var prompt = $@"
As a clinical decision support system, provide insurance requirements and considerations for treating a patient with: {diagnosis}

Include:
1. Pre-authorization requirements
2. Documentation needed for insurance claims
3. Treatment limitations or restrictions
4. Alternative treatments if primary treatment is not covered
5. Coding requirements (ICD-10, CPT codes)

Format as JSON:
{{
  ""insuranceRequirements"": [
    {{
      ""requirement"": ""string"",
      ""description"": ""string"",
      ""priority"": ""High|Medium|Low"",
      ""category"": ""PreAuth|Documentation|Coding|Limitations""
    }}
  ]
}}";

                var modelName = "tinyllama:latest";
                var request = new LlmRequest
                {
                    Prompt = prompt,
                    Provider = AiProvider.Ollama,
                    Model = modelName,
                    Temperature = 0.3,
                    MaxTokens = 1000
                };
                _logger.LogInformation("Calling Ollama model: {ModelName} for insurance requirements (diagnosis: {Diagnosis})", modelName, diagnosis);
                var response = await _llmClient.GenerateTextAsync(request);
                var responseText = response.Text;
                _logger.LogInformation("Ollama response from model {ModelName} ({Provider}): {Length} characters", modelName, response.Provider, responseText?.Length ?? 0);

                // Clean the response and extract structured data
                var cleanText = CleanOllamaResponse(responseText);
                var requirements = ParseInsuranceRequirementsFromText(cleanText);

                return requirements.Any() ? requirements : GetDefaultInsuranceRequirements(diagnosis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting insurance requirements for diagnosis: {Diagnosis}", diagnosis);
                return GetDefaultInsuranceRequirements(diagnosis);
            }
        }

        public async Task<ClinicalProtocol> GetClinicalProtocolAsync(string diagnosis)
        {
            try
            {
                var prompt = $@"
As a clinical decision support system, provide a standardized clinical protocol for treating: {diagnosis}

Include:
1. Diagnostic criteria confirmation
2. Treatment guidelines (evidence-based)
3. Monitoring requirements
4. Safety considerations
5. Referral criteria
6. Emergency protocols

Format as JSON:
{{
  ""protocol"": {{
    ""diagnosticCriteria"": [""string""],
    ""treatmentGuidelines"": [""string""],
    ""monitoringRequirements"": [""string""],
    ""safetyConsiderations"": [""string""],
    ""referralCriteria"": [""string""],
    ""emergencyProtocols"": [""string""]
  }}
}}";

                var modelName = "tinyllama:latest";
                var request = new LlmRequest
                {
                    Prompt = prompt,
                    Provider = AiProvider.Ollama,
                    Model = modelName,
                    Temperature = 0.3,
                    MaxTokens = 1000
                };
                _logger.LogInformation("Calling Ollama model: {ModelName} for clinical protocol (diagnosis: {Diagnosis})", modelName, diagnosis);
                var response = await _llmClient.GenerateTextAsync(request);
                var responseText = response.Text;
                _logger.LogInformation("Ollama response from model {ModelName} ({Provider}): {Length} characters", modelName, response.Provider, responseText?.Length ?? 0);

                // Clean the response and extract structured data
                var cleanText = CleanOllamaResponse(responseText);
                var protocol = ParseClinicalProtocolFromText(cleanText);

                return protocol ?? GetDefaultClinicalProtocol(diagnosis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical protocol for diagnosis: {Diagnosis}", diagnosis);
                return GetDefaultClinicalProtocol(diagnosis);
            }
        }

        private async Task<AIRecommendationResponse> GetAIRecommendations(string diagnosis, string patientContext)
        {
            // Craft a simple prompt for tinyllama - just ask for recommendations
            var prompt = $@"Generate clinical recommendations for a patient with diagnosis: {diagnosis}

Patient Information:
{patientContext}

Provide detailed clinical recommendations including:
1. Immediate actions to take
2. Patient-specific considerations
3. Risk factors to monitor
4. Contraindications to be aware of
5. Alternative treatment options

Format your response as a clear, structured clinical recommendation.";

            // Use Ollama with tinyllama
            _logger.LogInformation("Getting AI recommendations for diagnosis: {Diagnosis}", diagnosis);

            var modelName = "tinyllama";
            var request = new LlmRequest
            {
                Prompt = prompt,
                Provider = AiProvider.Ollama,
                Model = modelName,
                Temperature = 0.3,
                MaxTokens = 1000
            };

            _logger.LogInformation("Calling Ollama model: {ModelName} for diagnosis: {Diagnosis}", modelName, diagnosis);
            var response = await _llmClient.GenerateTextAsync(request);
            var responseText = response.Text;

            _logger.LogInformation("Ollama response from model {ModelName} ({Provider}): {Response}", modelName, response.Provider, responseText);

            // Parse the raw text into structured response
            try
            {
                // Extract the AI-generated text (remove "User:" and "Assistant:" prefixes if present)
                var cleanText = CleanOllamaResponse(responseText);

                var severity = DetermineSeverity(diagnosis);

                // Parse the text into structured format
                return new AIRecommendationResponse
                {
                    Severity = severity,
                    ImmediateActions = ExtractListFromText(cleanText, "immediate", "action", "recommendation"),
                    PatientSpecificNotes = ExtractListFromText(cleanText, "patient", "consideration", "note"),
                    RiskFactors = ExtractListFromText(cleanText, "risk", "monitor", "factor"),
                    Contraindications = ExtractListFromText(cleanText, "contraindication", "avoid", "warning"),
                    AlternativeTreatments = ExtractListFromText(cleanText, "alternative", "option", "treatment")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI response, using fallback: {Response}", responseText.Substring(0, Math.Min(200, responseText.Length)));

                // Fallback to rule-based
                var severity = DetermineSeverity(diagnosis);
                return new AIRecommendationResponse
                {
                    Severity = severity,
                    ImmediateActions = GetImmediateActions(diagnosis, severity),
                    PatientSpecificNotes = GetPatientSpecificNotes(patientContext),
                    RiskFactors = GetRiskFactors(diagnosis),
                    Contraindications = GetContraindications(diagnosis),
                    AlternativeTreatments = GetAlternativeTreatments(diagnosis)
                };
            }
        }

        private async Task<string> BuildPatientContextAsync(User patient, List<JournalEntry> journalEntries, List<JournalEntry> moodEntries, List<SM_MentalHealthApp.Shared.ContentAnalysis> contentAnalyses)
        {
            var context = $@"
Patient: {patient.FirstName} {patient.LastName}
Age: {CalculateAge(patient.DateOfBirth)}
Gender: {patient.Gender}
Medical History: Not specified

Recent Journal Entries:
{string.Join("\n", journalEntries.Take(5).Select(j => $"- {j.CreatedAt:yyyy-MM-dd}: {j.Text?.Substring(0, Math.Min(100, j.Text.Length))}..."))}

Recent Mood Entries:
{string.Join("\n", moodEntries.Take(3).Select(m => $"- {m.CreatedAt:yyyy-MM-dd}: {m.Mood}"))}
";

            // Add content analysis (medical test results) if available
            if (contentAnalyses != null && contentAnalyses.Any())
            {
                context += "\n=== MEDICAL TEST RESULTS / CONTENT ANALYSIS ===\n";

                // Get the most recent analysis with critical values
                var analysesWithCritical = contentAnalyses
                    .Where(a => a.AnalysisResults != null && a.AnalysisResults.ContainsKey("CriticalValues"))
                    .OrderByDescending(a => a.ProcessedAt)
                    .ToList();

                if (analysesWithCritical.Any())
                {
                    var mostRecent = analysesWithCritical.First();
                    context += $"Latest Analysis: {mostRecent.ProcessedAt:yyyy-MM-dd HH:mm}\n";

                    if (!string.IsNullOrEmpty(mostRecent.ExtractedText))
                    {
                        context += $"Test Results: {mostRecent.ExtractedText.Substring(0, Math.Min(300, mostRecent.ExtractedText.Length))}...\n";
                    }

                    // Add critical values if available
                    if (mostRecent.AnalysisResults.ContainsKey("CriticalValues"))
                    {
                        var criticalValues = mostRecent.AnalysisResults["CriticalValues"];
                        if (criticalValues != null)
                        {
                            context += $"Critical Values Detected: {criticalValues}\n";
                        }
                    }
                }
                else
                {
                    // Show latest analysis even if no critical values
                    var latest = contentAnalyses.OrderByDescending(a => a.ProcessedAt).First();
                    context += $"Latest Analysis: {latest.ProcessedAt:yyyy-MM-dd HH:mm}\n";
                    if (!string.IsNullOrEmpty(latest.ExtractedText))
                    {
                        context += $"Test Results: {latest.ExtractedText.Substring(0, Math.Min(300, latest.ExtractedText.Length))}...\n";
                    }
                }
            }

            return context;
        }

        private string DetermineSeverity(string diagnosis)
        {
            var lowerDiagnosis = diagnosis.ToLower();
            if (lowerDiagnosis.Contains("emergency") || lowerDiagnosis.Contains("critical") || lowerDiagnosis.Contains("suicide"))
                return "Critical";
            if (lowerDiagnosis.Contains("severe") || lowerDiagnosis.Contains("acute"))
                return "Severe";
            if (lowerDiagnosis.Contains("moderate") || lowerDiagnosis.Contains("chronic"))
                return "Moderate";
            return "Mild";
        }

        private List<string> GetImmediateActions(string diagnosis, string severity)
        {
            var actions = new List<string>();
            var lowerDiagnosis = diagnosis.ToLower();

            if (lowerDiagnosis.Contains("suicide") || lowerDiagnosis.Contains("self-harm"))
            {
                actions.Add("Immediate psychiatric evaluation required");
                actions.Add("Establish safety plan with patient");
                actions.Add("Consider hospitalization if risk is high");
            }
            else if (severity == "Critical" || severity == "Severe")
            {
                actions.Add("Immediate psychiatric assessment recommended");
                actions.Add("Evaluate need for medication adjustment");
                actions.Add("Schedule urgent follow-up appointment");
            }
            else
            {
                actions.Add("Review current treatment plan");
                actions.Add("Assess patient's support system");
                actions.Add("Schedule follow-up appointment within 1-2 weeks");
            }

            return actions;
        }

        private List<string> GetPatientSpecificNotes(string patientContext)
        {
            return new List<string>
            {
                "Consider patient's full clinical history in treatment decisions",
                "Review patient's current medications and dosages",
                "Assess patient's social support network",
                "Evaluate patient's adherence to current treatment plan"
            };
        }

        private List<string> GetRiskFactors(string diagnosis)
        {
            var lowerDiagnosis = diagnosis.ToLower();
            var riskFactors = new List<string>();

            if (lowerDiagnosis.Contains("depression") || lowerDiagnosis.Contains("anxiety"))
            {
                riskFactors.Add("Monitor for suicidal ideation");
                riskFactors.Add("Assess for substance use");
                riskFactors.Add("Evaluate sleep patterns and hygiene");
            }
            else if (lowerDiagnosis.Contains("bipolar"))
            {
                riskFactors.Add("Monitor for manic or hypomanic episodes");
                riskFactors.Add("Track mood cycling patterns");
                riskFactors.Add("Assess medication compliance");
            }
            else if (lowerDiagnosis.Contains("ptsd") || lowerDiagnosis.Contains("trauma"))
            {
                riskFactors.Add("Monitor for triggers and flashbacks");
                riskFactors.Add("Assess for dissociative episodes");
                riskFactors.Add("Evaluate hypervigilance and avoidance");
            }

            return riskFactors;
        }

        private List<string> GetContraindications(string diagnosis)
        {
            var lowerDiagnosis = diagnosis.ToLower();
            var contraindications = new List<string>();

            if (lowerDiagnosis.Contains("depression"))
            {
                contraindications.Add("Avoid prescribing contraindicated medications");
                contraindications.Add("Monitor for interactions with current medications");
                contraindications.Add("Consider patient's medical history before prescribing");
            }
            else if (lowerDiagnosis.Contains("bipolar"))
            {
                contraindications.Add("Avoid antidepressants without mood stabilizer");
                contraindications.Add("Monitor for medication-induced mood episodes");
                contraindications.Add("Assess for contraindicated medications");
            }

            return contraindications;
        }

        private List<string> GetAlternativeTreatments(string diagnosis)
        {
            var lowerDiagnosis = diagnosis.ToLower();
            var treatments = new List<string>();

            if (lowerDiagnosis.Contains("depression"))
            {
                treatments.Add("Cognitive Behavioral Therapy (CBT)");
                treatments.Add("Interpersonal Therapy (IPT)");
                treatments.Add("Consider medication if symptoms persist");
            }
            else if (lowerDiagnosis.Contains("anxiety"))
            {
                treatments.Add("Exposure Therapy");
                treatments.Add("Cognitive Restructuring");
                treatments.Add("Mindfulness-Based Stress Reduction (MBSR)");
            }
            else if (lowerDiagnosis.Contains("trauma") || lowerDiagnosis.Contains("ptsd"))
            {
                treatments.Add("EMDR (Eye Movement Desensitization and Reprocessing)");
                treatments.Add("Prolonged Exposure Therapy");
                treatments.Add("Trauma-Focused CBT");
            }

            return treatments;
        }

        private int CalculateAge(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return 0;
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Value.Year;
            if (dateOfBirth.Value.Date > today.AddYears(-age)) age--;
            return age;
        }

        private List<FollowUpStep> GetDefaultFollowUpSteps(string diagnosis, string severity)
        {
            return new List<FollowUpStep>
            {
                new FollowUpStep
                {
                    Step = "Schedule follow-up appointment",
                    Timeline = severity == "Severe" || severity == "Critical" ? "Within 24-48 hours" : "Within 1-2 weeks",
                    ResponsibleParty = "Doctor",
                    DocumentationRequired = "Progress notes, treatment response",
                    InsuranceConsiderations = "Verify coverage for follow-up visits",
                    Priority = severity == "Severe" || severity == "Critical" ? "High" : "Medium"
                },
                new FollowUpStep
                {
                    Step = "Monitor patient symptoms",
                    Timeline = "Daily for first week",
                    ResponsibleParty = "Patient",
                    DocumentationRequired = "Symptom diary",
                    InsuranceConsiderations = "Patient self-monitoring",
                    Priority = "High"
                }
            };
        }

        private List<InsuranceRequirement> GetDefaultInsuranceRequirements(string diagnosis)
        {
            return new List<InsuranceRequirement>
            {
                new InsuranceRequirement
                {
                    Requirement = "Document diagnosis with ICD-10 code",
                    Description = "Ensure proper coding for insurance billing",
                    Priority = "High",
                    Category = "Coding"
                },
                new InsuranceRequirement
                {
                    Requirement = "Treatment plan documentation",
                    Description = "Document evidence-based treatment approach",
                    Priority = "High",
                    Category = "Documentation"
                }
            };
        }

        private ClinicalProtocol GetDefaultClinicalProtocol(string diagnosis)
        {
            return new ClinicalProtocol
            {
                DiagnosticCriteria = new List<string> { "Confirm diagnosis based on clinical presentation" },
                TreatmentGuidelines = new List<string> { "Follow evidence-based treatment protocols" },
                MonitoringRequirements = new List<string> { "Regular follow-up appointments" },
                SafetyConsiderations = new List<string> { "Monitor for adverse effects" },
                ReferralCriteria = new List<string> { "Refer to specialist if needed" },
                EmergencyProtocols = new List<string> { "Emergency contact information provided" }
            };
        }

        /// <summary>
        /// Extract JSON from text response, handling cases where AI returns text + JSON
        /// </summary>
        private string ExtractJsonFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Look for the last occurrence of "Output JSON:" or just find JSON objects
            int outputJsonIndex = text.LastIndexOf("Output JSON:", StringComparison.OrdinalIgnoreCase);
            int jsonStart = -1;

            if (outputJsonIndex >= 0)
            {
                jsonStart = text.IndexOf("{", outputJsonIndex);
            }

            if (jsonStart < 0)
                jsonStart = text.IndexOf('{');

            if (jsonStart < 0)
                return text; // No JSON found

            // Find the matching closing brace, accounting for strings and escaped quotes
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            int lastBrace = jsonStart;

            for (int i = jsonStart; i < text.Length; i++)
            {
                char c = text[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"' && !escaped)
                {
                    inString = !inString;
                }

                if (!inString)
                {
                    if (c == '{')
                        depth++;
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            lastBrace = i;
                            break;
                        }
                    }
                }
            }

            if (lastBrace > jsonStart)
            {
                return text.Substring(jsonStart, lastBrace - jsonStart + 1);
            }

            return text;
        }

        /// <summary>
        /// Clean Ollama response by removing "User:" and "Assistant:" prefixes
        /// </summary>
        private string CleanOllamaResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Remove "User:" and "Assistant:" prefixes
            var lines = text.Split('\n');
            var cleanedLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("User:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Assistant:", StringComparison.OrdinalIgnoreCase))
                    continue;
                cleanedLines.Add(line);
            }

            return string.Join("\n", cleanedLines);
        }

        /// <summary>
        /// Extract a list of items from text by looking for bullet points or numbered lists
        /// </summary>
        private List<string> ExtractListFromText(string text, params string[] keywords)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return results;

            var lines = text.Split('\n');
            bool capturing = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Check if this line starts a relevant section
                if (keywords.Any(k => trimmed.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    capturing = true;
                    continue;
                }

                // Capture bullet points or numbered items
                if (capturing)
                {
                    if (trimmed.StartsWith("-") || trimmed.StartsWith("•") || trimmed.StartsWith("*") ||
                        char.IsDigit(trimmed[0]) && trimmed.Contains("."))
                    {
                        var content = trimmed.TrimStart('-', '•', '*', ' ', '\t');
                        content = content.Substring(content.IndexOf('.') + 1).Trim();
                        if (!string.IsNullOrWhiteSpace(content))
                            results.Add(content);
                    }
                    else if (trimmed.Contains(":") && !string.IsNullOrWhiteSpace(trimmed))
                    {
                        // Simple key-value extraction
                        var parts = trimmed.Split(':', 2);
                        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                            results.Add(parts[1].Trim());
                    }
                }
            }

            // If we found nothing, return a simple extracted snippet
            if (results.Count == 0 && !string.IsNullOrWhiteSpace(text))
            {
                // Extract first meaningful sentence
                var sentences = text.Split('.');
                foreach (var sentence in sentences)
                {
                    var trimmed = sentence.Trim();
                    if (keywords.Any(k => trimmed.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) && trimmed.Length > 20)
                    {
                        results.Add(trimmed);
                        if (results.Count >= 2)
                            break;
                    }
                }
            }

            // Fallback: if still nothing, return a generic message
            if (results.Count == 0)
            {
                results.Add($"AI-generated recommendations based on diagnosis and patient context");
            }

            return results;
        }

        /// <summary>
        /// Parse follow-up steps from Ollama text response
        /// </summary>
        private List<FollowUpStep> ParseFollowUpStepsFromText(string text)
        {
            var steps = new List<FollowUpStep>();
            if (string.IsNullOrWhiteSpace(text))
                return steps;

            var lines = text.Split('\n');
            string currentStep = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Look for numbered or bulleted items
                if ((trimmed.StartsWith("-") || char.IsDigit(trimmed[0])) && trimmed.Contains("."))
                {
                    var stepText = trimmed.Substring(trimmed.IndexOf('.') + 1).Trim();
                    if (!string.IsNullOrWhiteSpace(stepText) && stepText.Length > 10)
                    {
                        steps.Add(new FollowUpStep
                        {
                            Step = stepText,
                            Priority = "Medium",
                            Timeline = "1-2 weeks",
                            ResponsibleParty = "Doctor",
                            DocumentationRequired = "Follow-up notes",
                            InsuranceConsiderations = "Standard follow-up"
                        });
                    }
                }
            }

            return steps;
        }

        /// <summary>
        /// Parse insurance requirements from Ollama text response
        /// </summary>
        private List<InsuranceRequirement> ParseInsuranceRequirementsFromText(string text)
        {
            var requirements = new List<InsuranceRequirement>();
            if (string.IsNullOrWhiteSpace(text))
                return requirements;

            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Look for bulleted or numbered items
                if ((trimmed.StartsWith("-") || char.IsDigit(trimmed[0])) && trimmed.Length > 20)
                {
                    var reqText = trimmed.TrimStart('-', '•', '*', ' ', '\t');
                    if (reqText.Contains('.'))
                        reqText = reqText.Substring(reqText.IndexOf('.') + 1).Trim();

                    if (!string.IsNullOrWhiteSpace(reqText))
                    {
                        requirements.Add(new InsuranceRequirement
                        {
                            Requirement = reqText,
                            Description = reqText,
                            Priority = "Medium",
                            Category = "Documentation"
                        });
                    }
                }
            }

            return requirements;
        }

        /// <summary>
        /// Parse clinical protocol from Ollama text response
        /// </summary>
        private ClinicalProtocol ParseClinicalProtocolFromText(string text)
        {
            var protocol = new ClinicalProtocol();
            if (string.IsNullOrWhiteSpace(text))
                return protocol;

            var lines = text.Split('\n');

            protocol.DiagnosticCriteria = new List<string> { "Based on clinical assessment" };
            protocol.TreatmentGuidelines = ExtractListFromText(text, "treatment", "guideline", "management");
            protocol.MonitoringRequirements = ExtractListFromText(text, "monitor", "follow-up", "check");
            protocol.SafetyConsiderations = ExtractListFromText(text, "safety", "risk", "precaution");
            protocol.ReferralCriteria = ExtractListFromText(text, "referral", "specialist", "consult");
            protocol.EmergencyProtocols = ExtractListFromText(text, "emergency", "urgent", "immediate");

            return protocol;
        }
    }
}
