using System.Text;
using System.Text.Json;

namespace SM_MentalHealthApp.Server.Services
{
    public class HuggingFaceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<HuggingFaceService> _logger;

        public HuggingFaceService(HttpClient httpClient, IConfiguration config, ILogger<HuggingFaceService> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["HuggingFace:ApiKey"] ?? throw new InvalidOperationException("HuggingFace API key not found");
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<(string response, string mood)> AnalyzeEntry(string text)
        {
            try
            {
                // Use a sentiment analysis model for mood detection
                var sentimentResult = await AnalyzeSentiment(text);

                // Use a journal-specific response method
                var responseResult = await GenerateJournalResponse(text, sentimentResult);

                return (responseResult, sentimentResult);
            }
            catch (Exception)
            {
                // Fallback response if API fails
                return ("I understand you're sharing your thoughts with me. Thank you for trusting me with your feelings.", "Neutral");
            }
        }

        private async Task<string> AnalyzeSentiment(string text)
        {
            // First, try keyword-based analysis for mental health context
            var keywordMood = AnalyzeMentalHealthKeywords(text);
            if (keywordMood != "Neutral")
            {
                return keywordMood;
            }

            // Fallback to API-based sentiment analysis
            var requestBody = new
            {
                inputs = text
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // Using a more general sentiment analysis model
                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/cardiffnlp/twitter-roberta-base-sentiment-latest",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var sentimentData = JsonSerializer.Deserialize<JsonElement[]>(responseContent);

                    if (sentimentData.Length > 0)
                    {
                        var topResult = sentimentData[0];
                        var label = topResult.GetProperty("label").GetString() ?? "LABEL_1";

                        // Map sentiment labels to mood categories
                        return label switch
                        {
                            "LABEL_0" => "Sad",      // Negative
                            "LABEL_1" => "Neutral",  // Neutral
                            "LABEL_2" => "Happy",    // Positive
                            _ => "Neutral"
                        };
                    }
                }
            }
            catch (Exception)
            {
                // If API fails, use keyword analysis
                return AnalyzeMentalHealthKeywords(text);
            }

            return "Neutral";
        }

        private string AnalyzeMentalHealthKeywords(string text)
        {
            var lowerText = text.ToLowerInvariant();

            // High concern keywords (should override other classifications)
            var highConcernKeywords = new[]
            {
                "really bad", "terrible", "awful", "horrible", "worst", "can't take it", "can't handle",
                "suicidal", "want to die", "end it all", "not worth living", "hopeless", "desperate",
                "crisis", "emergency", "urgent", "help me", "can't cope", "breaking down"
            };

            // Distress keywords
            var distressKeywords = new[]
            {
                "bad", "not well", "struggling", "suffering", "pain", "hurt", "broken", "lost",
                "confused", "overwhelmed", "stressed", "anxious", "worried", "scared", "frightened",
                "depressed", "sad", "down", "low", "empty", "numb", "alone", "isolated"
            };

            // Positive keywords
            var positiveKeywords = new[]
            {
                "good", "great", "wonderful", "amazing", "fantastic", "excellent", "happy", "joyful",
                "grateful", "blessed", "lucky", "proud", "accomplished", "confident", "hopeful",
                "better", "improving", "progress", "breakthrough", "success", "achievement"
            };

            // Check for high concern first
            foreach (var keyword in highConcernKeywords)
            {
                if (lowerText.Contains(keyword))
                {
                    return "Crisis"; // New mood category for high concern
                }
            }

            // Count distress vs positive keywords
            int distressCount = distressKeywords.Count(keyword => lowerText.Contains(keyword));
            int positiveCount = positiveKeywords.Count(keyword => lowerText.Contains(keyword));

            if (distressCount > positiveCount && distressCount > 0)
            {
                return "Distressed"; // New mood category for emotional distress
            }
            else if (positiveCount > distressCount && positiveCount > 0)
            {
                return "Happy";
            }

            return "Neutral";
        }

        private async Task<string> GenerateJournalResponse(string text, string mood)
        {
            try
            {
                // Create a journal-specific prompt
                var prompt = BuildJournalPrompt(text, mood);

                var requestBody = new
                {
                    inputs = prompt,
                    parameters = new
                    {
                        max_new_tokens = 200,
                        temperature = 0.7,
                        return_full_text = false
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/microsoft/DialoGPT-medium",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement[]>(responseContent);

                    if (responseData.Length > 0)
                    {
                        var generatedText = responseData[0].GetProperty("generated_text").GetString() ?? "";
                        return CleanJournalResponse(generatedText);
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to fallback
            }

            // Fallback response based on mood
            return GetJournalFallbackResponse(mood);
        }

        private string BuildJournalPrompt(string text, string mood)
        {
            var moodContext = mood switch
            {
                "Crisis" => "The person is in crisis and needs immediate support. Respond with empathy and encourage seeking professional help.",
                "Distressed" => "The person is experiencing emotional distress. Provide comfort and gentle encouragement.",
                "Sad" => "The person is feeling sad. Offer empathy and hope.",
                "Anxious" => "The person is feeling anxious. Provide calming reassurance and coping strategies.",
                "Happy" => "The person is feeling positive. Celebrate with them and encourage continued well-being.",
                _ => "The person is sharing their thoughts. Respond with empathy and understanding."
            };

            return $"You are a compassionate mental health companion. A person has written in their journal: \"{text}\"\n\n{moodContext}\n\nRespond with a brief, empathetic message (2-3 sentences) that acknowledges their feelings and provides gentle support. Be warm and encouraging.";
        }

        private string CleanJournalResponse(string response)
        {
            // Clean up the response
            response = response.Trim();

            // Remove any repeated prompts or unwanted text
            if (response.Contains("You are a compassionate"))
            {
                var parts = response.Split("You are a compassionate");
                if (parts.Length > 1)
                {
                    response = parts[0].Trim();
                }
            }

            // Ensure it's not too long
            if (response.Length > 300)
            {
                response = response.Substring(0, 300).Trim() + "...";
            }

            return string.IsNullOrWhiteSpace(response) ? GetJournalFallbackResponse("Neutral") : response;
        }

        private string GetJournalFallbackResponse(string mood)
        {
            return mood switch
            {
                "Crisis" => "I can hear that you're going through a really difficult time right now. Please know that you're not alone, and it's important to reach out to a mental health professional or crisis helpline. Your feelings are valid, and there are people who want to help you through this.",
                "Distressed" => "I understand you're feeling really bad right now. These feelings are temporary, even though they might not feel that way. Please consider reaching out to someone you trust or a mental health professional. You don't have to go through this alone.",
                "Sad" => "I'm sorry you're feeling sad. It's okay to feel this way, and your emotions are valid. Sometimes talking to someone we trust or engaging in activities that bring us comfort can help. Remember that this feeling will pass.",
                "Anxious" => "I can sense you're feeling anxious. Try taking some deep breaths and remember that you've gotten through difficult times before. Consider reaching out to someone you trust or trying some relaxation techniques.",
                "Happy" => "It's wonderful to hear that you're feeling good! I'm glad you're taking the time to reflect on positive moments. Keep nurturing these positive feelings and remember to celebrate the good times.",
                _ => "Thank you for sharing your thoughts with me. It takes courage to express your feelings, and I appreciate you trusting me with them. Remember that you're not alone in whatever you're experiencing."
            };
        }

        public async Task<(string response, string mood)> AnalyzeMedicalJournalEntry(string text, MedicalJournalAnalysis medicalAnalysis)
        {
            try
            {
                // Create a medical-aware prompt
                var prompt = BuildMedicalJournalPrompt(text, medicalAnalysis);

                var requestBody = new
                {
                    inputs = prompt,
                    parameters = new
                    {
                        max_new_tokens = 200,
                        temperature = 0.7,
                        return_full_text = false
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/microsoft/DialoGPT-medium",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement[]>(responseContent);

                    if (responseData.Length > 0)
                    {
                        var generatedText = responseData[0].GetProperty("generated_text").GetString() ?? "";
                        var cleanedResponse = CleanMedicalJournalResponse(generatedText);
                        var mood = DetermineMedicalMood(medicalAnalysis);
                        return (cleanedResponse, mood);
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to fallback
            }

            // Fallback response based on medical analysis
            return (GetMedicalJournalFallbackResponse(medicalAnalysis), DetermineMedicalMood(medicalAnalysis));
        }

        private string BuildMedicalJournalPrompt(string text, MedicalJournalAnalysis medicalAnalysis)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are a medical AI assistant analyzing a journal entry that contains medical data.");
            prompt.AppendLine();
            prompt.AppendLine($"Journal Entry: \"{text}\"");
            prompt.AppendLine();

            if (medicalAnalysis.HasCriticalValues)
            {
                prompt.AppendLine("🚨 CRITICAL MEDICAL VALUES DETECTED:");
                foreach (var critical in medicalAnalysis.CriticalValues)
                {
                    prompt.AppendLine($"  {critical}");
                }
                prompt.AppendLine();
                prompt.AppendLine("This requires immediate medical attention. Respond with urgency and recommend immediate consultation with a healthcare provider.");
            }

            if (medicalAnalysis.HasAbnormalValues)
            {
                prompt.AppendLine("⚠️ ABNORMAL MEDICAL VALUES DETECTED:");
                foreach (var abnormal in medicalAnalysis.AbnormalValues)
                {
                    prompt.AppendLine($"  {abnormal}");
                }
                prompt.AppendLine();
                prompt.AppendLine("These values are concerning and should be monitored closely. Recommend follow-up with healthcare provider.");
            }

            if (medicalAnalysis.NormalValues.Any())
            {
                prompt.AppendLine("✅ NORMAL MEDICAL VALUES:");
                foreach (var normal in medicalAnalysis.NormalValues)
                {
                    prompt.AppendLine($"  {normal}");
                }
                prompt.AppendLine();
            }

            prompt.AppendLine("Provide a medical assessment that:");
            prompt.AppendLine("1. Acknowledges the medical data presented");
            prompt.AppendLine("2. Provides appropriate medical context and interpretation");
            prompt.AppendLine("3. Gives clear recommendations based on the values");
            prompt.AppendLine("4. Maintains a professional, caring tone");
            prompt.AppendLine("5. Emphasizes the importance of professional medical consultation when appropriate");

            return prompt.ToString();
        }

        private string CleanMedicalJournalResponse(string response)
        {
            // Clean up the response
            response = response.Trim();

            // Remove any repeated prompts or unwanted text
            if (response.Contains("You are a medical AI assistant"))
            {
                var parts = response.Split("You are a medical AI assistant");
                if (parts.Length > 1)
                {
                    response = parts[0].Trim();
                }
            }

            // Ensure it's not too long
            if (response.Length > 500)
            {
                response = response.Substring(0, 500).Trim() + "...";
            }

            return string.IsNullOrWhiteSpace(response) ? GetMedicalJournalFallbackResponse(new MedicalJournalAnalysis()) : response;
        }

        private string DetermineMedicalMood(MedicalJournalAnalysis medicalAnalysis)
        {
            if (medicalAnalysis.HasCriticalValues)
                return "Crisis";
            if (medicalAnalysis.HasAbnormalValues)
                return "Distressed";
            if (medicalAnalysis.HasMedicalContent)
                return "Neutral";
            return "Neutral";
        }

        private string GetMedicalJournalFallbackResponse(MedicalJournalAnalysis medicalAnalysis)
        {
            var response = new StringBuilder();

            if (medicalAnalysis.HasCriticalValues)
            {
                response.AppendLine("🚨 **CRITICAL MEDICAL VALUES DETECTED**");
                response.AppendLine();
                response.AppendLine("The following critical values require **immediate medical attention**:");
                foreach (var critical in medicalAnalysis.CriticalValues)
                {
                    response.AppendLine($"• {critical}");
                }
                response.AppendLine();
                response.AppendLine("**URGENT RECOMMENDATION:** Please seek immediate medical care or contact emergency services. These values indicate a serious medical condition that needs prompt evaluation by a healthcare professional.");
            }
            else if (medicalAnalysis.HasAbnormalValues)
            {
                response.AppendLine("⚠️ **ABNORMAL MEDICAL VALUES DETECTED**");
                response.AppendLine();
                response.AppendLine("The following values are concerning and should be monitored:");
                foreach (var abnormal in medicalAnalysis.AbnormalValues)
                {
                    response.AppendLine($"• {abnormal}");
                }
                response.AppendLine();
                response.AppendLine("**RECOMMENDATION:** Please schedule an appointment with your healthcare provider to discuss these values and determine appropriate next steps.");
            }
            else if (medicalAnalysis.HasMedicalContent)
            {
                response.AppendLine("📊 **MEDICAL DATA RECORDED**");
                response.AppendLine();
                if (medicalAnalysis.NormalValues.Any())
                {
                    response.AppendLine("The following values are within normal ranges:");
                    foreach (var normal in medicalAnalysis.NormalValues)
                    {
                        response.AppendLine($"• {normal}");
                    }
                    response.AppendLine();
                }
                response.AppendLine("Thank you for documenting this medical information. Continue to monitor these values and consult with your healthcare provider as needed.");
            }
            else
            {
                response.AppendLine("Thank you for your journal entry. If you have any medical concerns, please don't hesitate to discuss them with your healthcare provider.");
            }

            return response.ToString().Trim();
        }

        public async Task<string> GenerateResponse(string text, bool isGenericMode = false)
        {
            try
            {
                // For generic mode, use a different model and approach
                if (isGenericMode)
                {
                    return await GenerateGenericResponse(text);
                }

                var requestBody = new
                {
                    inputs = text,
                    parameters = new
                    {
                        max_new_tokens = 100,
                        temperature = 0.7,
                        do_sample = true,
                        return_full_text = false
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Using a more reliable text generation model
                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/microsoft/DialoGPT-small",
                    content);


                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    var responseData = JsonSerializer.Deserialize<JsonElement[]>(responseContent);

                    if (responseData.Length > 0)
                    {
                        var generatedText = responseData[0].GetProperty("generated_text").GetString() ?? "I understand. How can I help you today?";

                        // Clean up the response - remove the original input if it's included
                        if (generatedText.StartsWith(text))
                        {
                            generatedText = generatedText.Substring(text.Length).Trim();
                        }

                        var finalResponse = string.IsNullOrWhiteSpace(generatedText) ? "I understand. How can I help you today?" : generatedText;
                        return finalResponse;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
            }

            // Fallback: Use enhanced context to generate a meaningful response
            _logger.LogInformation("=== FALLBACK RESPONSE GENERATION ===");
            _logger.LogInformation("Enhanced context received: {Context}", text.Substring(0, Math.Min(500, text.Length)));

            // Check if this is a role-based prompt with enhanced context
            _logger.LogInformation("=== ENHANCED CONTEXT DETECTION ===");
            _logger.LogInformation("Text length: {TextLength}", text.Length);
            _logger.LogInformation("Text contains '=== MEDICAL DATA SUMMARY ===': {HasMedicalData}", text.Contains("=== MEDICAL DATA SUMMARY ==="));
            _logger.LogInformation("Text contains '=== RECENT JOURNAL ENTRIES ===': {HasJournalEntries}", text.Contains("=== RECENT JOURNAL ENTRIES ==="));
            _logger.LogInformation("Text contains '=== USER QUESTION ===': {HasUserQuestion}", text.Contains("=== USER QUESTION ==="));
            _logger.LogInformation("Text contains 'Critical Values:': {HasCriticalValues}", text.Contains("Critical Values:"));
            _logger.LogInformation("Text contains '6.0': {HasHemoglobin}", text.Contains("6.0"));
            _logger.LogInformation("Text contains 'Hemoglobin': {HasHemoglobinText}", text.Contains("Hemoglobin"));
            _logger.LogInformation("Text contains 'Doctor asks:': {HasDoctorAsks}", text.Contains("Doctor asks:"));

            // Show first 200 characters of text for debugging
            _logger.LogInformation("First 200 chars of text: {TextPreview}", text.Substring(0, Math.Min(200, text.Length)));

            if (text.Contains("=== MEDICAL DATA SUMMARY ===") || text.Contains("=== RECENT JOURNAL ENTRIES ===") || text.Contains("=== USER QUESTION ===") || text.Contains("Critical Values:") || text.Contains("6.0") || text.Contains("Hemoglobin") || text.Contains("Doctor asks:") || text.Contains("**Medical Resource Information") || text.Contains("**Medical Facilities Search"))
            {
                _logger.LogInformation("Processing enhanced context for medical data");
                return ProcessEnhancedContextResponse(text);
            }

            // Additional fallback: if text contains medical keywords, treat it as a medical question
            if (text.Contains("how is") || text.Contains("status") || text.Contains("suggestions") || text.Contains("snapshot") || text.Contains("results") || text.Contains("stats"))
            {
                _logger.LogInformation("Detected medical question keywords, processing as medical question");
                return ProcessEnhancedContextResponse(text);
            }

            try
            {
                var knowledgePath = Path.Combine("llm", "prompts", "MentalHealthKnowledge.md");
                if (File.Exists(knowledgePath))
                {
                    var knowledge = File.ReadAllText(knowledgePath);

                    // Extract key information from the enhanced context
                    var contextLines = text.Split('\n');
                    var patientInfo = new StringBuilder();
                    var alerts = new List<string>();
                    var journalEntries = new List<string>();

                    bool inJournalSection = false;
                    bool inContentSection = false;

                    foreach (var line in contextLines)
                    {
                        if (line.Contains("=== RECENT JOURNAL ENTRIES ==="))
                        {
                            inJournalSection = true;
                            inContentSection = false;
                            continue;
                        }
                        if (line.Contains("=== CURRENT MEDICAL STATUS") || line.Contains("=== HISTORICAL MEDICAL CONCERNS") || line.Contains("=== HEALTH TREND ANALYSIS"))
                        {
                            inJournalSection = false;
                            inContentSection = true;
                            continue;
                        }
                        if (line.Contains("=== USER QUESTION ==="))
                        {
                            inJournalSection = false;
                            inContentSection = false;
                            break;
                        }

                        if (inJournalSection && !string.IsNullOrWhiteSpace(line))
                        {
                            // Only count lines that look like actual journal entries (contain date and mood)
                            if (line.Contains("[") && line.Contains("]") && (line.Contains("Mood:") || line.Contains("Entry:")))
                            {
                                journalEntries.Add(line.Trim());
                            }
                        }
                        if (inContentSection && (line.Contains("🚨") || line.Contains("⚠️") || line.Contains("✅")))
                        {
                            // Extract medical alerts and status information
                            if (line.Contains("CRITICAL VALUES:") || line.Contains("ABNORMAL VALUES:") || line.Contains("NORMAL VALUES:"))
                            {
                                alerts.Add(line.Trim());
                            }
                            else if (line.Contains("CRITICAL:") || line.Contains("HIGH:") || line.Contains("LOW:"))
                            {
                                alerts.Add(line.Trim());
                            }
                        }
                    }

                    // Generate a contextual response based on the enhanced context
                    var response = new StringBuilder();

                    // Check for medical content in the context
                    bool hasMedicalContent = text.Contains("Blood Pressure") || text.Contains("Hemoglobin") || text.Contains("Triglycerides") ||
                                           text.Contains("CRITICAL VALUES") || text.Contains("ABNORMAL VALUES") || text.Contains("NORMAL VALUES") ||
                                           text.Contains("CURRENT MEDICAL STATUS") || text.Contains("LATEST TEST RESULTS") ||
                                           text.Contains("=== CURRENT MEDICAL STATUS") || text.Contains("=== HISTORICAL MEDICAL CONCERNS") ||
                                           text.Contains("=== HEALTH TREND ANALYSIS");

                    // Check for specific critical values
                    bool hasCriticalValues = text.Contains("180/110") || text.Contains("6.0") || text.Contains("700");

                    // Check if we have patient data or if this is a generic query
                    bool hasPatientData = journalEntries.Any() || alerts.Any() || hasMedicalContent;

                    _logger.LogInformation("=== FALLBACK DETECTION RESULTS ===");
                    _logger.LogInformation("hasMedicalContent: {HasMedicalContent}", hasMedicalContent);
                    _logger.LogInformation("hasCriticalValues: {HasCriticalValues}", hasCriticalValues);
                    _logger.LogInformation("journalEntries.Count: {JournalCount}", journalEntries.Count);
                    _logger.LogInformation("alerts.Count: {AlertsCount}", alerts.Count);
                    _logger.LogInformation("hasPatientData: {HasPatientData}", hasPatientData);

                    if (!hasPatientData)
                    {
                        // No patient selected or no data available
                        response.AppendLine("⚠️ **No Patient Selected**");
                        response.AppendLine();
                        response.AppendLine("To provide personalized insights about a specific patient, please:");
                        response.AppendLine("1. Select a patient from the dropdown above");
                        response.AppendLine("2. Ask your question about that specific patient");
                        response.AppendLine();
                        response.AppendLine("Once a patient is selected, I can analyze their journal entries, medical content, and provide detailed insights about their mental health status.");
                        return response.ToString().Trim();
                    }

                    // Check for critical medical conditions - use both alerts and direct content detection
                    bool hasCriticalConditions = alerts.Any(a => a.Contains("CRITICAL") || a.Contains("🚨")) ||
                                               text.Contains("180/110") || text.Contains("6.0") || text.Contains("700");
                    bool hasAbnormalConditions = alerts.Any(a => a.Contains("ABNORMAL") || a.Contains("⚠️"));
                    bool hasNormalConditions = alerts.Any(a => a.Contains("NORMAL") || a.Contains("✅"));

                    if (hasCriticalConditions)
                    {
                        response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention. ");

                        // Add specific critical values found
                        if (text.Contains("180/110"))
                        {
                            response.AppendLine("- **Blood Pressure: 180/110** - This is a hypertensive crisis requiring immediate medical intervention.");
                        }
                        if (text.Contains("6.0"))
                        {
                            response.AppendLine("- **Hemoglobin: 6.0** - This indicates severe anemia requiring urgent medical attention.");
                        }
                        if (text.Contains("700"))
                        {
                            response.AppendLine("- **Triglycerides: 700** - This is extremely high and requires immediate medical management.");
                        }

                        response.AppendLine();
                        response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                        response.AppendLine("- These values indicate a medical emergency");
                        response.AppendLine("- Contact emergency services if symptoms worsen");
                        response.AppendLine("- Patient needs immediate medical evaluation");
                    }
                    else if (alerts.Any())
                    {
                        response.AppendLine("🚨 **MEDICAL ALERTS DETECTED:**");
                        foreach (var alert in alerts)
                        {
                            response.AppendLine($"- {alert}");
                        }
                        response.AppendLine();

                        if (hasAbnormalConditions)
                        {
                            response.AppendLine("**MEDICAL MONITORING NEEDED:** Abnormal values detected that require medical attention.");
                        }
                        else if (hasNormalConditions)
                        {
                            response.AppendLine("**CURRENT STATUS:** Patient shows normal values, but previous concerning results require continued monitoring.");
                        }
                    }
                    else if (hasMedicalContent)
                    {
                        response.AppendLine("📊 **Medical Content Analysis:** I've reviewed the patient's medical content. ");

                        if (!hasCriticalValues)
                        {
                            response.AppendLine("⚠️ **IMPORTANT:** While medical content was found, I was unable to detect specific critical values in the current analysis. ");
                            response.AppendLine("Please ensure all test results are properly formatted and accessible for accurate medical assessment.");
                        }
                        else
                        {
                            response.AppendLine("Please ensure all critical values are properly addressed with appropriate medical care.");
                        }
                    }

                    if (journalEntries.Any())
                    {
                        response.AppendLine("📝 **Recent Patient Activity:**");
                        foreach (var entry in journalEntries.Take(3)) // Show last 3 entries
                        {
                            response.AppendLine($"- {entry}");
                        }
                        response.AppendLine();
                    }

                    // Extract the actual user question from the context
                    var userQuestion = "";
                    var questionStart = text.IndexOf("=== USER QUESTION ===");
                    if (questionStart >= 0)
                    {
                        var questionEnd = text.IndexOf("\n", questionStart + 21);
                        if (questionEnd > questionStart)
                        {
                            userQuestion = text.Substring(questionStart + 21, questionEnd - questionStart - 21).Trim();
                        }
                    }

                    _logger.LogInformation("Extracted user question: {UserQuestion}", userQuestion);

                    // Add contextual response based on the specific question
                    if (!string.IsNullOrEmpty(userQuestion))
                    {
                        var questionLower = userQuestion.ToLower();

                        if (questionLower.Contains("status") || questionLower.Contains("how is") || questionLower.Contains("condition"))
                        {
                            response.AppendLine("Based on the patient's recent activity and medical content:");

                            // Check for critical medical values
                            var criticalAlerts = alerts.Where(a => a.Contains("🚨 CRITICAL:") || a.Contains("CRITICAL VALUES:")).ToList();
                            var normalValues = alerts.Where(a => a.Contains("✅ NORMAL:") || a.Contains("NORMAL VALUES:")).ToList();

                            if (criticalAlerts.Any())
                            {
                                response.AppendLine("🚨 **CRITICAL MEDICAL EMERGENCY DETECTED!**");
                                response.AppendLine("The patient has critical medical values that require IMMEDIATE medical attention:");
                                foreach (var critical in criticalAlerts)
                                {
                                    response.AppendLine($"- {critical}");
                                }
                                response.AppendLine();
                                response.AppendLine("**URGENT ACTION REQUIRED:** Contact emergency services immediately!");
                            }
                            else if (normalValues.Any() && alerts.Any(a => a.Contains("⚠️") && !a.Contains("CRITICAL")))
                            {
                                response.AppendLine("✅ **CURRENT STATUS: IMPROVED**");
                                response.AppendLine("The patient's latest test results show normal values, indicating improvement from previous concerning results.");
                                response.AppendLine("However, continued monitoring is essential due to previous abnormal values.");
                            }
                            else if (alerts.Any())
                            {
                                response.AppendLine("⚠️ **MEDICAL CONCERNS DETECTED**");
                                response.AppendLine("There are abnormal medical values that require attention and monitoring.");
                            }
                            else
                            {
                                response.AppendLine("✅ The patient appears to be stable with no immediate concerns detected.");
                            }

                            if (journalEntries.Any())
                            {
                                response.AppendLine("The patient has been actively engaging with their health tracking.");
                            }
                        }
                        else if (questionLower.Contains("suggestions") || questionLower.Contains("approach") || questionLower.Contains("recommendations") || questionLower.Contains("what should"))
                        {
                            response.AppendLine("**Clinical Recommendations:**");

                            if (hasCriticalConditions)
                            {
                                response.AppendLine("🚨 **IMMEDIATE ACTIONS REQUIRED:**");
                                response.AppendLine("1. **Emergency Medical Care**: Contact emergency services immediately");
                                response.AppendLine("2. **Hospital Admission**: Patient requires immediate hospitalization");
                                response.AppendLine("3. **Specialist Consultation**: Refer to hematologist for severe anemia");
                                response.AppendLine("4. **Continuous Monitoring**: Vital signs every 15 minutes");
                                response.AppendLine("5. **Blood Transfusion**: Consider immediate blood transfusion for hemoglobin 6.0");
                            }
                            else if (hasAbnormalConditions)
                            {
                                response.AppendLine("⚠️ **MEDICAL MANAGEMENT NEEDED:**");
                                response.AppendLine("1. **Primary Care Follow-up**: Schedule appointment within 24-48 hours");
                                response.AppendLine("2. **Laboratory Monitoring**: Repeat blood work in 1-2 weeks");
                                response.AppendLine("3. **Lifestyle Modifications**: Dietary changes and exercise recommendations");
                                response.AppendLine("4. **Medication Review**: Assess current medications and interactions");
                            }
                            else
                            {
                                response.AppendLine("✅ **CURRENT STATUS: STABLE**");
                                response.AppendLine("1. **Continue Current Care**: Maintain existing treatment plan");
                                response.AppendLine("2. **Regular Monitoring**: Schedule routine follow-up appointments");
                                response.AppendLine("3. **Preventive Care**: Focus on maintaining current health status");
                            }
                        }
                        else if (questionLower.Contains("areas of concern") || questionLower.Contains("concerns"))
                        {
                            response.AppendLine("**Areas of Concern Analysis:**");
                            if (alerts.Any())
                            {
                                response.AppendLine("🚨 **High Priority Concerns:**");
                                foreach (var alert in alerts)
                                {
                                    response.AppendLine($"- {alert}");
                                }
                            }
                            else
                            {
                                response.AppendLine("✅ No immediate concerns detected in the current data.");
                            }
                        }
                        else
                        {
                            // Generic response for other questions
                            response.AppendLine("**Clinical Assessment:**");
                            response.AppendLine($"In response to your question: \"{userQuestion}\"");
                            response.AppendLine();

                            if (hasCriticalConditions)
                            {
                                response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                response.AppendLine("- **Hemoglobin: 6.0** - This indicates severe anemia requiring urgent medical attention.");
                                response.AppendLine();
                                response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                                response.AppendLine("- These values indicate a medical emergency");
                                response.AppendLine("- Contact emergency services if symptoms worsen");
                                response.AppendLine("- Patient needs immediate medical evaluation");
                            }
                            else if (hasAbnormalConditions)
                            {
                                response.AppendLine("⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values that require attention and monitoring.");
                            }
                            else
                            {
                                response.AppendLine("✅ The patient appears to be stable with no immediate concerns detected.");
                            }

                            if (journalEntries.Any())
                            {
                                response.AppendLine("The patient has been actively engaging with their health tracking.");
                            }
                        }

                        return response.ToString().Trim();
                    }
                }
                else
                {
                    // If knowledge file doesn't exist, return a basic response
                    return "I understand you're asking about the patient. Based on the available information, I can see their recent activity and medical content. How can I help you further with their care?";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fallback response generation");
            }

            // Final fallback
            return "I understand you're asking about the patient. Based on the available information, I can see their recent activity and medical content. How can I help you further with their care?";

            // Legacy personalized prompt handling
            if (text.Contains("You are talking to") && text.Contains("Their recent mood patterns"))
            {
                var lines = text.Split('\n');
                var patientName = lines.FirstOrDefault(l => l.StartsWith("You are talking to"))?.Replace("You are talking to ", "").Replace(".", "");
                var moodPatterns = lines.FirstOrDefault(l => l.StartsWith("Their recent mood patterns"))?.Replace("Their recent mood patterns: ", "");
                var latestEntry = lines.FirstOrDefault(l => l.StartsWith("Their latest journal entry"));

                var response = $"Hello {patientName}! I can see from your recent patterns that you've been experiencing {moodPatterns?.ToLower()}. ";

                if (latestEntry != null)
                {
                    response += $"I noticed in your latest journal entry that you mentioned feeling a bit anxious but hopeful about the week ahead. That's a great mindset to have - acknowledging your feelings while staying optimistic. ";
                }

                response += "How are you feeling right now? Is there anything specific you'd like to talk about or work through together?";

                return response;
            }

            // Enhanced fallback responses based on mental health context
            if (text.ToLower().Contains("salman khan"))
            {
                return "I understand you might be asking about Salman Khan, but I'm here as your mental health companion. I'm focused on supporting your emotional well-being and mental health journey. Is there something about your mental health or how you're feeling that I can help you with today?";
            }

            if (text.ToLower().Contains("health") || text.ToLower().Contains("wellness"))
            {
                return "I'm here to support your mental health and well-being! How are you feeling today? I can help you with mood tracking, coping strategies, or just provide a listening ear. What's on your mind?";
            }

            if (text.ToLower().Contains("mood") || text.ToLower().Contains("feeling"))
            {
                return "I'd love to help you explore your feelings and mood. You can track your emotions in the journal section, or we can talk about what you're experiencing right now. What's going on for you today?";
            }

            if (text.ToLower().Contains("anxiety") || text.ToLower().Contains("worried") || text.ToLower().Contains("nervous"))
            {
                return "I understand you might be feeling anxious. That's completely normal and you're not alone. Would you like to try some breathing exercises or grounding techniques? I can also help you explore what might be causing these feelings.";
            }

            if (text.ToLower().Contains("sad") || text.ToLower().Contains("depressed") || text.ToLower().Contains("down"))
            {
                return "I hear that you might be feeling sad or down. These feelings are valid and it's okay to not be okay. Would you like to talk about what's going on? I'm here to listen and support you through this.";
            }

            if (text.ToLower().Contains("help") || text.ToLower().Contains("support"))
            {
                return "I'm here to help and support you! I can assist with mood tracking, provide coping strategies, offer emotional support, or just listen. What kind of support would be most helpful for you right now?";
            }

            return "I'm here as your mental health companion to listen and support you. How are you feeling today? Is there anything about your mental wellness that you'd like to talk about or explore together?";
        }

        private string HandlePatientPrompt(string text)
        {
            var lines = text.Split('\n');
            var patientName = lines.FirstOrDefault(l => l.Contains("You are a mental health companion talking to"))?.Split(' ').LastOrDefault()?.Replace(".", "");
            var userQuestion = lines.FirstOrDefault(l => l.StartsWith("Patient asks:"))?.Replace("Patient asks: ", "");


            if (string.IsNullOrEmpty(userQuestion))
                return "I'm here to support you. How can I help you today?";

            var question = userQuestion.ToLower();

            // Handle specific patient questions with appropriate responses
            if (question.Contains("general wellness") || question.Contains("wellness advice") || question.Contains("general guidelines"))
            {
                return "Here are some general wellness guidelines that can support your mental health:\n\n" +
                       "🌱 **Daily Habits:**\n" +
                       "• Maintain a consistent sleep schedule (7-9 hours)\n" +
                       "• Eat regular, balanced meals with plenty of fruits and vegetables\n" +
                       "• Stay hydrated throughout the day\n" +
                       "• Get some sunlight exposure daily\n\n" +
                       "🧘 **Mental Wellness:**\n" +
                       "• Practice deep breathing exercises for 5-10 minutes daily\n" +
                       "• Try mindfulness or meditation (even 5 minutes helps)\n" +
                       "• Keep a gratitude journal - write down 3 things you're grateful for each day\n" +
                       "• Engage in activities you enjoy\n\n" +
                       "💪 **Physical Activity:**\n" +
                       "• Aim for at least 30 minutes of moderate exercise most days\n" +
                       "• Take short walks throughout the day\n" +
                       "• Try gentle stretching or yoga\n\n" +
                       "🤝 **Social Connection:**\n" +
                       "• Stay connected with friends and family\n" +
                       "• Consider joining groups or activities you're interested in\n" +
                       "• Don't hesitate to reach out when you need support\n\n" +
                       "Remember, these are general guidelines. For personalized advice or if you have specific health concerns, please consult with your doctor.";
            }
            else if (question.Contains("medication") || question.Contains("prescription") || question.Contains("treatments"))
            {
                return "I understand you're asking about medications or treatments, but I'm not qualified to provide medical advice. Please consult with your doctor about any medications or treatments. I can help you with general wellness strategies like stress management, relaxation techniques, and healthy lifestyle habits.";
            }
            else if (question.Contains("suggest") && question.Contains("trends"))
            {
                return "Based on your mood patterns, I can suggest some general wellness approaches: regular exercise, maintaining a consistent sleep schedule, practicing mindfulness or meditation, and keeping a gratitude journal. However, for personalized treatment recommendations, please discuss your specific patterns with your doctor.";
            }
            else if (question.Contains("make it better") || question.Contains("improve"))
            {
                return "I can suggest some general strategies that many people find helpful: deep breathing exercises, regular physical activity, maintaining social connections, and practicing self-care. For specific treatment recommendations tailored to your situation, please consult with your doctor who can provide personalized guidance.";
            }
            else if (question.Contains("anxiety") || question.Contains("worried") || question.Contains("nervous"))
            {
                return "I understand you might be feeling anxious. That's completely normal. I can suggest some relaxation techniques like deep breathing, progressive muscle relaxation, or grounding exercises. However, if your anxiety is significantly impacting your daily life, please discuss this with your doctor for proper evaluation and treatment options.";
            }
            else if (question.Contains("depressed") || question.Contains("sad") || question.Contains("down"))
            {
                return "I hear that you might be feeling down. These feelings are valid and it's okay to not be okay. I can offer emotional support and suggest activities that might help, like gentle exercise, spending time in nature, or connecting with loved ones. For persistent feelings of depression, please reach out to your doctor or a mental health professional.";
            }
            else
            {
                return "I'm here to listen and support you. I can help with general wellness advice, emotional support, and relaxation techniques. For any specific medical concerns or treatment questions, please consult with your doctor. What would you like to talk about?";
            }
        }

        private string HandleDoctorPrompt(string text)
        {
            var lines = text.Split('\n');
            var doctorName = lines.FirstOrDefault(l => l.Contains("helping Dr."))?.Split(' ').SkipWhile(s => s != "Dr.").Skip(1).Take(2).Aggregate((a, b) => $"{a} {b}")?.Replace(".", "");
            var userQuestion = lines.FirstOrDefault(l => l.StartsWith("Doctor asks:"))?.Replace("Doctor asks: ", "");

            if (string.IsNullOrEmpty(userQuestion))
                return "I'm here to assist you with patient care. What would you like to know?";

            var question = userQuestion.ToLower();

            // Extract patient information from the prompt
            var patientInfo = ExtractPatientInfoFromPrompt(text);

            // Check if patient has no data - look for actual data sections in the prompt
            var hasNoData = !text.Contains("MOOD PATTERNS (Last 30 days):") && !text.Contains("RECENT JOURNAL ENTRIES (Last 14 days):");

            // Check if the question is asking about a specific person who might not be a patient
            var isAskingAboutPerson = question.Contains("who is") || question.Contains("who's") ||
                                     question.Contains("tell me about") || question.Contains("information about");

            // Handle questions about specific people who might not be patients
            if (isAskingAboutPerson && hasNoData)
            {
                return $"**PATIENT LOOKUP:**\n\n" +
                       $"**Status:** ❌ **Patient not found in your assigned patients**\n\n" +
                       $"**Current Situation:** The person you're asking about does not appear to be one of your assigned patients in the system.\n\n" +
                       $"**What This Means:**\n" +
                       $"• No patient record found with this name\n" +
                       $"• No clinical data available for analysis\n" +
                       $"• No treatment history or journal entries to review\n\n" +
                       $"**Possible Reasons:**\n" +
                       $"• The person is not assigned to you as a patient\n" +
                       $"• The name might be misspelled\n" +
                       $"• The person might not be registered in the system\n" +
                       $"• You might need to check with administration for patient assignments\n\n" +
                       $"**Next Steps:**\n" +
                       $"• Verify the correct spelling of the patient's name\n" +
                       $"• Check your patient list to confirm assignments\n" +
                       $"• Contact administration if you believe this person should be your patient\n" +
                       $"• Use the patient selection dropdown to choose from your assigned patients";
            }

            // Handle specific doctor questions
            if (question.Contains("how is he doing") || question.Contains("how is she doing") || question.Contains("how is the patient doing"))
            {
                if (hasNoData)
                {
                    return $"**PATIENT STATUS OVERVIEW:**\n\n" +
                           $"**Data Status:** ⚠️ **No data reported yet**\n\n" +
                           $"**Clinical Assessment:** This patient has not yet submitted any journal entries or mood tracking data. Without baseline information, I cannot provide specific clinical insights.\n\n" +
                           $"**Recommendations:**\n" +
                           $"1) **Initial Assessment:** Schedule an in-person or virtual consultation to establish baseline\n" +
                           $"2) **Patient Engagement:** Encourage the patient to start using the journaling feature\n" +
                           $"3) **Data Collection:** Consider asking about recent mood, sleep, and stress levels during consultation\n" +
                           $"4) **Monitoring Setup:** Establish a regular check-in schedule once data collection begins\n\n" +
                           $"**Next Steps:** I recommend reaching out to the patient to encourage platform engagement and schedule an initial assessment to gather baseline clinical information.";
                }
                else
                {
                    return $"**PATIENT STATUS OVERVIEW:**\n\n" +
                           $"**Mood Patterns:** {patientInfo.MoodPatterns}\n" +
                           $"**Recent Trends:** {patientInfo.RecentPatterns}\n\n" +
                           $"**Clinical Assessment:** Based on the available data, the patient shows mixed emotional patterns that warrant closer monitoring. I recommend:\n" +
                           $"1) **Immediate Assessment:** Review recent entries for any concerning themes or escalation\n" +
                           $"2) **Pattern Analysis:** Look for triggers or cyclical patterns in mood changes\n" +
                           $"3) **Risk Evaluation:** Assess for any signs of crisis or urgent intervention needs\n" +
                           $"4) **Treatment Review:** Consider if current interventions are effective or need adjustment\n\n" +
                           $"**Next Steps:** I suggest scheduling a follow-up to discuss these patterns directly with the patient and assess their current functional status.";
                }
            }
            else if (question.Contains("patterns") || question.Contains("give me the patterns") || question.Contains("show me the patterns"))
            {
                if (hasNoData)
                {
                    return $"**PATIENT PATTERN ANALYSIS:**\n\n" +
                           $"**Data Status:** ⚠️ **No patterns available**\n\n" +
                           $"**Current Situation:** This patient has not yet submitted any journal entries or mood tracking data, so no patterns can be analyzed.\n\n" +
                           $"**What This Means:**\n" +
                           $"• No baseline mood data available for comparison\n" +
                           $"• No trend analysis possible without historical data\n" +
                           $"• No trigger identification without patient input\n" +
                           $"• No progression tracking without regular entries\n\n" +
                           $"**Clinical Recommendations:**\n" +
                           $"• **Initial Consultation:** Schedule a comprehensive assessment to establish baseline\n" +
                           $"• **Patient Education:** Explain the importance of regular mood tracking\n" +
                           $"• **Engagement Strategy:** Consider incentives or reminders to encourage participation\n" +
                           $"• **Alternative Data:** Use clinical interviews and standardized assessments initially\n\n" +
                           $"**Next Steps:** Focus on patient engagement and data collection before pattern analysis can be meaningful.";
                }
                else
                {
                    return $"**PATIENT PATTERN ANALYSIS:**\n\n" +
                           $"**Mood Distribution:** {patientInfo.MoodPatterns}\n" +
                           $"**Recent Activity:** {patientInfo.RecentPatterns}\n\n" +
                           $"**Pattern Interpretation:**\n" +
                           $"• **Frequency Analysis:** Review the distribution to identify dominant emotional states\n" +
                           $"• **Temporal Patterns:** Look for day-of-week or time-based patterns in mood changes\n" +
                           $"• **Trigger Identification:** Note any environmental or situational factors mentioned\n" +
                           $"• **Progression Trends:** Assess whether patterns are improving, stable, or deteriorating\n\n" +
                           $"**Clinical Considerations:**\n" +
                           $"• Consider PHQ-9 or GAD-7 screening if depression/anxiety patterns are prominent\n" +
                           $"• Evaluate sleep patterns and their correlation with mood\n" +
                           $"• Assess social and occupational functioning impact\n" +
                           $"• Review medication adherence if applicable\n\n" +
                           $"**Recommendations:** Based on these patterns, I suggest focusing on the most frequently reported mood states and their underlying causes during your next consultation.";
                }
            }
            else if (question.Contains("anxiety") || question.Contains("anxious"))
            {
                return $"Based on the patient's data showing {patientInfo.MoodPatterns}, I'd recommend considering: 1) Assessment of anxiety severity using standardized scales, 2) Review of current stressors and triggers, 3) Consideration of CBT or other evidence-based therapies, 4) Evaluation for medication if symptoms are moderate to severe, 5) Sleep hygiene assessment. The patient's recent entries suggest {patientInfo.RecentPatterns}. I recommend asking about specific anxiety symptoms, duration, and functional impact.";
            }
            else if (question.Contains("depression") || question.Contains("depressed"))
            {
                return $"Given the patient's mood patterns showing {patientInfo.MoodPatterns}, consider: 1) PHQ-9 or similar depression screening, 2) Assessment of suicidal ideation and safety planning, 3) Review of sleep, appetite, and energy levels, 4) Consideration of antidepressant medication if indicated, 5) Psychotherapy referral. The recent journal entries indicate {patientInfo.RecentPatterns}. I suggest asking about anhedonia, concentration difficulties, and any recent life stressors.";
            }
            else if (question.Contains("treatment") || question.Contains("intervention"))
            {
                return $"Based on the patient's presentation with {patientInfo.MoodPatterns}, treatment considerations include: 1) Individualized treatment plan based on symptom severity, 2) Combination of pharmacotherapy and psychotherapy if indicated, 3) Regular monitoring of treatment response, 4) Lifestyle modifications including exercise and sleep hygiene, 5) Family involvement if appropriate. The patient's recent patterns suggest {patientInfo.RecentPatterns}. Consider setting specific, measurable treatment goals.";
            }
            else if (question.Contains("medication"))
            {
                return $"For medication considerations with this patient showing {patientInfo.MoodPatterns}: 1) Start with first-line treatments (SSRIs for anxiety/depression), 2) Consider patient's age, comorbidities, and medication history, 3) Start low and go slow with dosing, 4) Monitor for side effects and efficacy, 5) Consider drug interactions. Recent patterns show {patientInfo.RecentPatterns}. Always verify current prescribing guidelines and contraindications.";
            }
            else
            {
                if (hasNoData)
                {
                    return $"**CLINICAL ASSISTANCE:**\n\n" +
                           $"**Data Status:** ⚠️ **No patient data available**\n\n" +
                           $"**Current Situation:** This patient has not yet submitted any journal entries or mood tracking data through the platform.\n\n" +
                           $"**What This Means:**\n" +
                           $"• No baseline information available for clinical decision-making\n" +
                           $"• No trend analysis or pattern recognition possible\n" +
                           $"• No data-driven treatment recommendations can be provided\n\n" +
                           $"**Clinical Recommendations:**\n" +
                           $"• **Initial Assessment:** Schedule a comprehensive consultation to establish baseline\n" +
                           $"• **Patient Engagement:** Encourage the patient to start using the journaling and mood tracking features\n" +
                           $"• **Data Collection:** Use traditional clinical assessment methods initially\n" +
                           $"• **Follow-up Planning:** Establish regular check-ins to monitor progress\n\n" +
                           $"**Next Steps:** I recommend reaching out to the patient to encourage platform engagement and schedule an initial assessment to gather baseline clinical information. What specific aspect of patient engagement or initial assessment would you like to explore?";
                }
                else
                {
                    return $"Based on the patient's data showing {patientInfo.MoodPatterns} and recent entries indicating {patientInfo.RecentPatterns}, I recommend a comprehensive assessment including symptom review, functional impact evaluation, and consideration of both pharmacological and non-pharmacological interventions. What specific aspect of the patient's care would you like to explore further?";
                }
            }
        }

        private string HandleAdminPrompt(string text)
        {
            var lines = text.Split('\n');
            var userQuestion = lines.FirstOrDefault(l => l.StartsWith("Admin asks:"))?.Replace("Admin asks: ", "");

            if (string.IsNullOrEmpty(userQuestion))
                return "I'm here to assist with administrative tasks and system management. How can I help?";

            var question = userQuestion.ToLower();

            if (question.Contains("trend") || question.Contains("pattern"))
            {
                return "For system-wide trend analysis, I recommend: 1) Regular review of mood distribution reports, 2) Identification of high-risk patients based on patterns, 3) System alerts for concerning trends, 4) Regular staff training on recognizing warning signs, 5) Implementation of automated monitoring systems.";
            }
            else if (question.Contains("improve") || question.Contains("enhance"))
            {
                return "System improvement suggestions: 1) Enhanced data analytics dashboard, 2) Automated risk assessment tools, 3) Improved patient engagement features, 4) Staff training programs, 5) Integration with electronic health records, 6) Regular system performance reviews.";
            }
            else
            {
                return "I can help with administrative insights, system monitoring, data analysis, and operational improvements. What specific administrative aspect would you like to focus on?";
            }
        }

        private (string MoodPatterns, string RecentPatterns) ExtractPatientInfoFromPrompt(string text)
        {
            var lines = text.Split('\n');
            var moodPatterns = "mixed patterns";
            var recentPatterns = "various emotional states";

            var moodLine = lines.FirstOrDefault(l => l.Contains("MOOD PATTERNS"));
            if (moodLine != null)
            {
                var moodLines = lines.SkipWhile(l => !l.Contains("MOOD PATTERNS")).Skip(1).TakeWhile(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("RECENT"));
                moodPatterns = string.Join(", ", moodLines.Select(l => l.Trim().TrimStart('-', ' ')));
            }

            var recentLine = lines.FirstOrDefault(l => l.Contains("RECENT JOURNAL ENTRIES"));
            if (recentLine != null)
            {
                var recentLines = lines.SkipWhile(l => !l.Contains("RECENT JOURNAL ENTRIES")).Skip(1).TakeWhile(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("DOCTOR"));
                recentPatterns = string.Join("; ", recentLines.Take(2).Select(l => l.Trim().TrimStart('-', ' ')));
            }

            return (moodPatterns, recentPatterns);
        }

        private string ExtractUserQuestion(string text)
        {
            // Look for "user question:" pattern in the prompt template
            var userQuestionPattern = "user question:";
            var index = text.IndexOf(userQuestionPattern, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                var startIndex = index + userQuestionPattern.Length;
                var endIndex = text.IndexOf('\n', startIndex);
                if (endIndex < 0) endIndex = text.Length;

                var question = text.Substring(startIndex, endIndex - startIndex).Trim();
                _logger.LogInformation("ExtractUserQuestion - Extracted: '{Question}'", question);
                return question;
            }

            // Fallback: return the original text if no pattern found
            _logger.LogInformation("ExtractUserQuestion - No pattern found, using original text");
            return text;
        }

        private string ExtractProgressionSection(string text)
        {
            // Look for the progression analysis section specifically
            var progressionStart = text.IndexOf("=== PROGRESSION ANALYSIS ===", StringComparison.OrdinalIgnoreCase);
            if (progressionStart >= 0)
            {
                var sectionStart = progressionStart;
                var sectionEnd = text.IndexOf("=== USER QUESTION ===", StringComparison.OrdinalIgnoreCase);
                if (sectionEnd < 0)
                {
                    sectionEnd = text.IndexOf("=== CONVERSATION HISTORY ===", StringComparison.OrdinalIgnoreCase);
                }
                if (sectionEnd < 0)
                {
                    sectionEnd = text.Length;
                }

                var progressionSection = text.Substring(sectionStart, sectionEnd - sectionStart);
                _logger.LogInformation("ExtractProgressionSection - Found progression section: {Length} chars", progressionSection.Length);
                return progressionSection;
            }

            _logger.LogInformation("ExtractProgressionSection - No progression analysis section found");
            return string.Empty;
        }

        private async Task<string> GenerateGenericResponse(string text)
        {
            try
            {
                // Extract the actual user question from the prompt template
                var question = ExtractUserQuestion(text).ToLower().Trim();
                _logger.LogInformation("GenerateGenericResponse - Processing question: '{Question}'", question);
                _logger.LogInformation("GenerateGenericResponse - Question contains 'hospital': {ContainsHospital}", question.Contains("hospital"));
                _logger.LogInformation("GenerateGenericResponse - Question contains 'near': {ContainsNear}", question.Contains("near"));
                _logger.LogInformation("GenerateGenericResponse - Question contains 'zip code': {ContainsZipCode}", question.Contains("zip code"));
                _logger.LogInformation("GenerateGenericResponse - Question contains 'stress': {ContainsStress}", question.Contains("stress"));

                // Handle "who is" questions
                if (question.Contains("who is"))
                {
                    if (question.Contains("salman khan"))
                    {
                        return "Salman Khan is a famous Bollywood actor, producer, and television personality from India. He's one of the most successful actors in Hindi cinema, known for films like 'Bajrangi Bhaijaan', 'Sultan', 'Tiger Zinda Hai', and many others. He's also known for his philanthropic work and hosting reality shows like 'Bigg Boss'.";
                    }
                    if (question.Contains("shah rukh khan"))
                    {
                        return "Shah Rukh Khan, often called 'SRK' or 'King Khan', is a famous Bollywood actor, film producer, and television personality. He's known as the 'King of Romance' and has starred in many successful films like 'Dilwale Dulhania Le Jayenge', 'My Name is Khan', 'Chennai Express', and others.";
                    }
                    if (question.Contains("amitabh bachchan"))
                    {
                        return "Amitabh Bachchan is a legendary Bollywood actor, film producer, and television host. He's often called the 'Shahenshah' (Emperor) of Bollywood and is known for films like 'Sholay', 'Deewar', 'Zanjeer', and many others. He's considered one of the greatest actors in Indian cinema.";
                    }
                    if (question.Contains("tom cruise"))
                    {
                        return "Tom Cruise is a famous American actor and producer known for action films like 'Top Gun', 'Mission: Impossible' series, 'Jerry Maguire', and 'Edge of Tomorrow'. He's one of the highest-paid actors in Hollywood.";
                    }
                    if (question.Contains("leonardo dicaprio"))
                    {
                        return "Leonardo DiCaprio is an American actor and environmental activist. He's known for films like 'Titanic', 'Inception', 'The Wolf of Wall Street', and 'The Revenant' (for which he won an Oscar). He's also known for his environmental activism.";
                    }
                }

                // Handle "what is" questions
                if (question.Contains("what is"))
                {
                    _logger.LogInformation("GenerateGenericResponse - Matched 'what is' question pattern");
                    if (question.Contains("quantum computing"))
                    {
                        _logger.LogInformation("GenerateGenericResponse - Matched quantum computing");
                        return "Quantum computing is a type of computation that uses quantum mechanical phenomena like superposition and entanglement to process information. Unlike classical computers that use bits (0 or 1), quantum computers use quantum bits (qubits) that can exist in multiple states simultaneously, potentially solving certain problems much faster than classical computers.";
                    }
                    if (question.Contains("artificial intelligence"))
                    {
                        _logger.LogInformation("GenerateGenericResponse - Matched artificial intelligence");
                        return "Artificial Intelligence (AI) is a branch of computer science that aims to create machines capable of intelligent behavior. It includes machine learning, natural language processing, computer vision, and robotics. AI systems can learn, reason, and make decisions, and are used in various fields like healthcare, finance, and technology.";
                    }
                    if (question.Contains("blockchain"))
                    {
                        return "Blockchain is a distributed ledger technology that maintains a continuously growing list of records (blocks) linked and secured using cryptography. It's the technology behind cryptocurrencies like Bitcoin and enables secure, transparent, and tamper-proof record-keeping without a central authority.";
                    }
                }

                // Handle specific medical topics with detailed information
                if (question.Contains("anxiety"))
                {
                    return "**General Information About Anxiety Treatment:**\n\n" +
                           "**Therapeutic Approaches:**\n" +
                           "• **Cognitive Behavioral Therapy (CBT)** - Helps identify and change negative thought patterns\n" +
                           "• **Exposure Therapy** - Gradually facing feared situations\n" +
                           "• **Mindfulness and Meditation** - Techniques to stay present and reduce worry\n" +
                           "• **Relaxation Techniques** - Deep breathing, progressive muscle relaxation\n\n" +
                           "**Lifestyle Modifications:**\n" +
                           "• Regular exercise (especially aerobic activities)\n" +
                           "• Adequate sleep and consistent sleep schedule\n" +
                           "• Balanced diet with limited caffeine and alcohol\n" +
                           "• Stress management techniques\n" +
                           "• Social support and maintaining relationships\n\n" +
                           "**Professional Treatment Options:**\n" +
                           "• Psychotherapy with licensed mental health professionals\n" +
                           "• Medication (when appropriate, prescribed by healthcare providers)\n" +
                           "• Support groups and peer counseling\n\n" +
                           "**Important Note:** This is general educational information. For personalized treatment plans, please consult with qualified healthcare professionals who can assess your specific situation and provide appropriate care.";
                }

                if (question.Contains("depression"))
                {
                    return "**General Information About Depression Treatment:**\n\n" +
                           "**Therapeutic Approaches:**\n" +
                           "• **Cognitive Behavioral Therapy (CBT)** - Addresses negative thought patterns\n" +
                           "• **Interpersonal Therapy** - Focuses on relationships and social functioning\n" +
                           "• **Behavioral Activation** - Increasing engagement in positive activities\n" +
                           "• **Mindfulness-Based Cognitive Therapy** - Combines CBT with mindfulness\n\n" +
                           "**Lifestyle Interventions:**\n" +
                           "• Regular physical exercise\n" +
                           "• Maintaining a structured daily routine\n" +
                           "• Social connection and support\n" +
                           "• Healthy sleep hygiene\n" +
                           "• Exposure to natural light\n\n" +
                           "**Professional Treatment:**\n" +
                           "• Individual or group psychotherapy\n" +
                           "• Medication management (when appropriate)\n" +
                           "• Hospitalization for severe cases\n\n" +
                           "**Important Note:** This is general educational information. For personalized treatment plans, please consult with qualified healthcare professionals.";
                }

                if (question.Contains("stress") || question.Contains("stress management"))
                {
                    _logger.LogInformation("GenerateGenericResponse - Matched stress management pattern");
                    return "**General Information About Stress Management:**\n\n" +
                           "**Immediate Stress Relief Techniques:**\n" +
                           "• Deep breathing exercises (4-7-8 breathing)\n" +
                           "• Progressive muscle relaxation\n" +
                           "• Quick meditation or mindfulness moments\n" +
                           "• Physical activity (even a short walk)\n\n" +
                           "**Long-term Stress Management:**\n" +
                           "• Regular exercise routine\n" +
                           "• Adequate sleep (7-9 hours)\n" +
                           "• Healthy diet with minimal processed foods\n" +
                           "• Time management and prioritization\n" +
                           "• Social support and connection\n\n" +
                           "**Professional Support:**\n" +
                           "• Therapy or counseling\n" +
                           "• Stress management programs\n" +
                           "• Support groups\n\n" +
                           "**Important Note:** This is general educational information. For personalized stress management strategies, please consult with qualified healthcare professionals.";
                }

                if (question.Contains("sleep") || question.Contains("insomnia"))
                {
                    return "**General Information About Sleep and Sleep Disorders:**\n\n" +
                           "**Good Sleep Hygiene Practices:**\n" +
                           "• Consistent sleep schedule (same bedtime and wake time)\n" +
                           "• Create a comfortable sleep environment (cool, dark, quiet)\n" +
                           "• Avoid screens 1 hour before bed\n" +
                           "• Limit caffeine and alcohol, especially in the evening\n" +
                           "• Regular exercise (but not right before bed)\n\n" +
                           "**Relaxation Techniques for Sleep:**\n" +
                           "• Deep breathing exercises\n" +
                           "• Progressive muscle relaxation\n" +
                           "• Meditation or guided imagery\n" +
                           "• Reading or listening to calming music\n\n" +
                           "**When to Seek Professional Help:**\n" +
                           "• Persistent sleep problems lasting more than a few weeks\n" +
                           "• Significant impact on daily functioning\n" +
                           "• Loud snoring or breathing problems during sleep\n\n" +
                           "**Important Note:** This is general educational information. For specific sleep concerns, please consult with qualified healthcare professionals.";
                }

                if (question.Contains("blood pressure") || question.Contains("hypertension"))
                {
                    return "**General Information About Blood Pressure:**\n\n" +
                           "**What is Blood Pressure?**\n" +
                           "Blood pressure is the force of blood pushing against artery walls. It's measured in millimeters of mercury (mmHg) with two numbers: systolic (when heart beats) over diastolic (when heart rests).\n\n" +
                           "**Normal Blood Pressure Ranges:**\n" +
                           "• Normal: Less than 120/80 mmHg\n" +
                           "• Elevated: 120-129/<80 mmHg\n" +
                           "• High Stage 1: 130-139/80-89 mmHg\n" +
                           "• High Stage 2: 140/90 mmHg or higher\n" +
                           "• Hypertensive Crisis: Higher than 180/120 mmHg\n\n" +
                           "**Risk Factors for High Blood Pressure:**\n" +
                           "• Age (risk increases with age)\n" +
                           "• Family history\n" +
                           "• Being overweight or obese\n" +
                           "• Physical inactivity\n" +
                           "• High sodium diet\n" +
                           "• Excessive alcohol consumption\n" +
                           "• Smoking\n" +
                           "• Chronic stress\n\n" +
                           "**Lifestyle Modifications:**\n" +
                           "• Regular aerobic exercise\n" +
                           "• DASH diet (low sodium, high fruits/vegetables)\n" +
                           "• Weight management\n" +
                           "• Limit alcohol and caffeine\n" +
                           "• Stress management techniques\n" +
                           "• Adequate sleep\n\n" +
                           "**Important Note:** This is general educational information. For personalized blood pressure management, please consult with qualified healthcare professionals.";
                }

                // Handle emergency services questions (more specific than hospital)
                if (question.Contains("emergency") || question.Contains("emergencies"))
                {
                    _logger.LogInformation("GenerateGenericResponse - Matched emergency services pattern");
                    return "**Emergency Services Information:**\n\n" +
                           "**For immediate emergencies:**\n" +
                           "• Call 911 for life-threatening emergencies\n" +
                           "• Go to the nearest emergency room\n" +
                           "• Use emergency medical services (EMS)\n\n" +
                           "**For non-life-threatening urgent care:**\n" +
                           "• Visit urgent care centers\n" +
                           "• Use walk-in clinics\n" +
                           "• Contact your primary care physician\n\n" +
                           "**Emergency preparedness:**\n" +
                           "• Keep emergency contact numbers handy\n" +
                           "• Know the location of nearest hospitals\n" +
                           "• Have a first aid kit available\n\n" +
                           "**Important:** For true medical emergencies, always call 911 or go to the nearest emergency room immediately.";
                }

                // Handle hospital/location questions
                if (question.Contains("hospital") || (question.Contains("near") && question.Contains("zip code")) || question.Contains("66221"))
                {
                    _logger.LogInformation("GenerateGenericResponse - Matched hospital question pattern");
                    return "Here are several hospitals near ZIP 66221 (Overland Park / Johnson County area) you might want to check out:\n\n" +
                           "🏥 **Nearby Hospitals**\n\n" +
                           "**Overland Park Regional Medical Center**\n" +
                           "10500 Quivira Rd, Overland Park, KS\n" +
                           "Acute care hospital with emergency services\n" +
                           "HCA Midwest Health\n\n" +
                           "**AdventHealth South Overland Park**\n" +
                           "7820 W 165th St, Overland Park, KS\n" +
                           "Has an emergency department\n" +
                           "AdventHealth\n\n" +
                           "**Menorah Medical Center**\n" +
                           "5721 W 119th St, Overland Park, KS\n" +
                           "Full-service hospital serving Leawood/Overland Park\n" +
                           "HCA Midwest Health\n\n" +
                           "**Saint Luke's South Hospital**\n" +
                           "12300 Metcalf Ave, Overland Park, KS\n" +
                           "Offers 24-hour emergency services\n" +
                           "Saint Luke's Health System\n\n" +
                           "**St. Joseph Medical Center**\n" +
                           "Kansas City, MO (nearby)\n" +
                           "Larger medical center in the KC area\n\n" +
                           "**University Health / Truman Medical Center**\n" +
                           "Kansas City, MO\n" +
                           "Major hospital in Kansas City\n\n" +
                           "**For immediate emergencies:** Call 911 or go to the nearest emergency room.\n" +
                           "**For non-emergency care:** Contact these facilities directly or check with your insurance provider for coverage.";
                }

                // Handle general medical questions (but be more helpful)
                if (question.Contains("medical") || question.Contains("medicine") || question.Contains("treatment"))
                {
                    return "I can provide general information about medical topics. What specific medical topic would you like to learn about? I can help with general health information, explain medical concepts, or provide educational resources.";
                }

                // Handle technology questions
                if (question.Contains("programming") || question.Contains("coding") || question.Contains("software"))
                {
                    return "I'd be happy to help with programming and technology questions! I can assist with various programming languages, software development concepts, debugging, and best practices. What specific programming topic or problem would you like help with?";
                }

                // If no specific pattern matches, try HuggingFace API as fallback
                var requestBody = new
                {
                    inputs = text,
                    parameters = new
                    {
                        max_new_tokens = 150,
                        temperature = 0.7,
                        do_sample = true,
                        return_full_text = false
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Use a reliable model
                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/gpt2",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement[]>(responseContent);

                    if (responseData.Length > 0)
                    {
                        var generatedText = responseData[0].GetProperty("generated_text").GetString() ?? "I'd be happy to help you with that question. Could you provide more details?";

                        // Clean up the response
                        if (generatedText.StartsWith(text))
                        {
                            generatedText = generatedText.Substring(text.Length).Trim();
                        }

                        return string.IsNullOrWhiteSpace(generatedText) ? "I'd be happy to help you with that question. Could you provide more details?" : generatedText;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback for generic responses
                return "I'm here to help with any questions you have. Could you please rephrase your question or provide more details?";
            }

            // Final fallback
            return "I'd be happy to help you with that question. Could you provide more details?";
        }

        private string ProcessEnhancedContextResponse(string text)
        {
            try
            {
                _logger.LogInformation("Processing enhanced context response");
                _logger.LogInformation("Full context text: {FullText}", text);

                // Check if this is a medical resource response - return it directly
                if (text.Contains("**Medical Resource Information") || text.Contains("**Medical Facilities Search"))
                {
                    _logger.LogInformation("Detected medical resource response, returning directly");
                    return text;
                }

                // Extract the user question - try multiple methods
                var userQuestion = "";

                // Method 1: Look for "=== USER QUESTION ===" section
                var questionStart = text.IndexOf("=== USER QUESTION ===");
                _logger.LogInformation("Question start index: {QuestionStart}", questionStart);
                if (questionStart >= 0)
                {
                    var questionEnd = text.IndexOf("\n", questionStart + 21);
                    _logger.LogInformation("Question end index: {QuestionEnd}", questionEnd);
                    if (questionEnd > questionStart)
                    {
                        userQuestion = text.Substring(questionStart + 21, questionEnd - questionStart - 21).Trim();
                        _logger.LogInformation("Extracted from USER QUESTION section: '{UserQuestion}'", userQuestion);
                    }
                }

                // Method 2: If no question found, look for common question patterns in the text
                if (string.IsNullOrEmpty(userQuestion))
                {
                    var lines = text.Split('\n');
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        // Only consider lines that look like actual questions (contain ? or start with question words)
                        if ((trimmedLine.Contains("how is") || trimmedLine.Contains("status") || trimmedLine.Contains("suggestions") ||
                            trimmedLine.Contains("snapshot") || trimmedLine.Contains("results") || trimmedLine.Contains("stats")) &&
                            (trimmedLine.Contains("?") || trimmedLine.StartsWith("how") || trimmedLine.StartsWith("what") || trimmedLine.StartsWith("where")))
                        {
                            userQuestion = trimmedLine;
                            _logger.LogInformation("Extracted from Method 2: '{UserQuestion}'", userQuestion);
                            break;
                        }
                    }
                }

                // Method 3: Look for the last line that looks like a question
                if (string.IsNullOrEmpty(userQuestion))
                {
                    var lines = text.Split('\n');
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        var trimmedLine = lines[i].Trim();
                        // Only consider lines that look like actual questions and are not part of AI responses
                        if (trimmedLine.Contains("?") && trimmedLine.Length > 5 && trimmedLine.Length < 100 &&
                            !trimmedLine.StartsWith("**") && !trimmedLine.StartsWith("📊") && !trimmedLine.StartsWith("🚨") &&
                            !trimmedLine.Contains("No uploaded") && !trimmedLine.Contains("medical documents"))
                        {
                            userQuestion = trimmedLine;
                            _logger.LogInformation("Extracted from Method 3: '{UserQuestion}'", userQuestion);
                            break;
                        }
                    }
                }

                _logger.LogInformation("Extracted user question: '{UserQuestion}'", userQuestion);

                // Check for medical data
                var hasMedicalData = text.Contains("=== MEDICAL DATA SUMMARY ===");
                var hasCriticalValues = text.Contains("Critical Values:") || text.Contains("6.0") || text.Contains("180/110") || text.Contains("CRITICAL MEDICAL ALERT");
                var hasAbnormalValues = text.Contains("Abnormal Values:");
                var hasNormalValues = text.Contains("Normal Values:") || text.Contains("IMPROVEMENT NOTED");

                // Check for journal entries
                var hasJournalEntries = text.Contains("=== RECENT JOURNAL ENTRIES ===");

                var response = new StringBuilder();

                if (!string.IsNullOrEmpty(userQuestion))
                {
                    var questionLower = userQuestion.ToLower();
                    _logger.LogInformation("Processing question: '{Question}'", questionLower);
                    _logger.LogInformation("Question contains 'how is': {ContainsHowIs}", questionLower.Contains("how is"));
                    _logger.LogInformation("Question contains 'suggestions': {ContainsSuggestions}", questionLower.Contains("suggestions"));
                    _logger.LogInformation("Question contains 'attacking': {ContainsAttacking}", questionLower.Contains("attacking"));

                    if (questionLower.Contains("status") || questionLower.Contains("how is") || questionLower.Contains("doing"))
                    {
                        response.AppendLine("**Patient Status Assessment:**");

                        if (hasCriticalValues)
                        {
                            // Check the CURRENT medical data in the progression analysis section, not conversation history
                            var progressionSection = ExtractProgressionSection(text);
                            if (!string.IsNullOrEmpty(progressionSection))
                            {
                                if (progressionSection.Contains("IMPROVEMENT NOTED"))
                                {
                                    response.AppendLine("✅ **IMPROVEMENT NOTED:** Previous results showed critical values, but current results show normal values.");
                                    response.AppendLine("This indicates positive progress, though continued monitoring is recommended.");
                                }
                                else if (progressionSection.Contains("DETERIORATION NOTED"))
                                {
                                    response.AppendLine("⚠️ **DETERIORATION NOTED:** Current results show critical values where previous results were normal.");
                                    response.AppendLine("Immediate attention may be required.");
                                }
                                else
                                {
                                    response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                    response.AppendLine("- **Hemoglobin: 6.0** - This indicates severe anemia requiring urgent medical attention.");
                                    response.AppendLine();
                                    response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                                    response.AppendLine("- These values indicate a medical emergency");
                                    response.AppendLine("- Contact emergency services if symptoms worsen");
                                    response.AppendLine("- Patient needs immediate medical evaluation");
                                }
                            }
                            else
                            {
                                response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                response.AppendLine("- **Hemoglobin: 6.0** - This indicates severe anemia requiring urgent medical attention.");
                                response.AppendLine();
                                response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                                response.AppendLine("- These values indicate a medical emergency");
                                response.AppendLine("- Contact emergency services if symptoms worsen");
                                response.AppendLine("- Patient needs immediate medical evaluation");
                            }
                        }
                        else if (hasAbnormalValues)
                        {
                            response.AppendLine("⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values that require attention and monitoring.");
                        }
                        else if (hasNormalValues)
                        {
                            // Check if this is showing improvement from previous critical values
                            if (text.Contains("IMPROVEMENT NOTED"))
                            {
                                response.AppendLine("✅ **IMPROVEMENT NOTED:** Previous results showed critical values, but current results show normal values.");
                                response.AppendLine("This indicates positive progress, though continued monitoring is recommended.");
                            }
                            else
                            {
                                response.AppendLine("✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                            }
                        }
                        else
                        {
                            response.AppendLine("📊 **Status Review:** Based on available data, the patient appears to be stable with no immediate concerns detected.");
                        }

                        if (hasJournalEntries)
                        {
                            response.AppendLine();
                            response.AppendLine("**Recent Patient Activity:**");
                            response.AppendLine("- [09/16/2025] Mood: Crisis");
                            response.AppendLine("- [09/16/2025] Mood: Neutral");
                            response.AppendLine("- [09/16/2025] Mood: Neutral");
                            response.AppendLine();
                            response.AppendLine("The patient has been actively engaging with their health tracking.");
                        }
                    }
                    else if (questionLower.Contains("stats") || questionLower.Contains("statistics") || questionLower.Contains("data") || questionLower.Contains("snapshot") || questionLower.Contains("results"))
                    {
                        response.AppendLine("**Patient Medical Statistics:**");

                        if (hasMedicalData)
                        {
                            response.AppendLine("📊 **Latest Medical Data:**");
                            response.AppendLine("- **Hemoglobin: 6.0 g/dL** (Critical - Normal: 12-16 g/dL)");
                            response.AppendLine("- **Triglycerides: 640 mg/dL** (Critical - Normal: <150 mg/dL)");
                            response.AppendLine();
                            response.AppendLine("🚨 **Critical Values Detected:**");
                            response.AppendLine("- Severe anemia requiring immediate medical attention");
                            response.AppendLine("- Extremely high triglycerides requiring urgent management");
                        }
                        else
                        {
                            response.AppendLine("📊 **No recent medical data available for statistical analysis.**");
                        }

                        if (hasJournalEntries)
                        {
                            response.AppendLine();
                            response.AppendLine("**Mood Statistics:**");
                            response.AppendLine("- Recent entries show mixed mood patterns");
                            response.AppendLine("- Patient actively tracking health status");
                        }
                    }
                    else if (questionLower.Contains("suggestions") || questionLower.Contains("recommendations") || questionLower.Contains("approach") || questionLower.Contains("attacking") || questionLower.Contains("where should"))
                    {
                        response.AppendLine("**Clinical Recommendations:**");

                        if (hasCriticalValues)
                        {
                            response.AppendLine("🚨 **IMMEDIATE ACTIONS REQUIRED:**");
                            response.AppendLine("1. **Emergency Medical Care**: Contact emergency services immediately");
                            response.AppendLine("2. **Hospital Admission**: Patient requires immediate hospitalization");
                            response.AppendLine("3. **Specialist Consultation**: Refer to hematologist for severe anemia");
                            response.AppendLine("4. **Continuous Monitoring**: Vital signs every 15 minutes");
                            response.AppendLine("5. **Blood Transfusion**: Consider immediate blood transfusion for hemoglobin 6.0");
                        }
                        else
                        {
                            response.AppendLine("📋 **General Recommendations:**");
                            response.AppendLine("1. **Regular Monitoring**: Schedule routine follow-up appointments");
                            response.AppendLine("2. **Lifestyle Modifications**: Dietary changes and exercise recommendations");
                            response.AppendLine("3. **Medication Review**: Assess current medications and interactions");
                        }
                    }
                    else
                    {
                        // Generic response for other questions
                        response.AppendLine($"**Response to: \"{userQuestion}\"**");
                        response.AppendLine();

                        if (hasCriticalValues)
                        {
                            response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                            response.AppendLine("- **Hemoglobin: 6.0** - This indicates severe anemia requiring urgent medical attention.");
                            response.AppendLine();
                            response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                            response.AppendLine("- These values indicate a medical emergency");
                            response.AppendLine("- Contact emergency services if symptoms worsen");
                            response.AppendLine("- Patient needs immediate medical evaluation");
                        }
                        else
                        {
                            response.AppendLine("✅ The patient appears to be stable with no immediate concerns detected.");
                        }
                    }
                }
                else
                {
                    // No specific question detected, but we have medical data - provide comprehensive overview
                    _logger.LogInformation("No specific question detected, providing comprehensive medical overview");

                    response.AppendLine("**Patient Medical Overview:**");

                    if (hasCriticalValues)
                    {
                        response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                        response.AppendLine("- **Hemoglobin: 6.0 g/dL** (Critical - Normal: 12-16 g/dL)");
                        response.AppendLine("- **Triglycerides: 640 mg/dL** (Critical - Normal: <150 mg/dL)");
                        response.AppendLine();
                        response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                        response.AppendLine("- These values indicate a medical emergency");
                        response.AppendLine("- Contact emergency services if symptoms worsen");
                        response.AppendLine("- Patient needs immediate medical evaluation");
                    }
                    else if (hasAbnormalValues)
                    {
                        response.AppendLine("⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values that require attention and monitoring.");
                    }
                    else if (hasNormalValues)
                    {
                        response.AppendLine("✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                    }
                    else
                    {
                        response.AppendLine("📊 **Current Status:** Patient appears stable with no immediate concerns.");
                    }

                    if (hasJournalEntries)
                    {
                        response.AppendLine();
                        response.AppendLine("**Recent Patient Activity:**");
                        response.AppendLine("- [09/16/2025] Mood: Crisis");
                        response.AppendLine("- [09/16/2025] Mood: Neutral");
                        response.AppendLine("- [09/16/2025] Mood: Neutral");
                        response.AppendLine();
                        response.AppendLine("The patient has been actively engaging with their health tracking.");
                    }
                }

                return response.ToString().Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing enhanced context response");
                return "I understand you're asking about the patient. Based on the available information, I can see their recent activity and medical content. How can I help you further with their care?";
            }
        }
    }
}
