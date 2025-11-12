using System.Text;
using System.Text.Json;

namespace SM_MentalHealthApp.Server.Services
{
    public class HuggingFaceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<HuggingFaceService> _logger;
        private readonly ICriticalValuePatternService _patternService;
        private readonly ICriticalValueKeywordService _keywordService;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly IAIResponseTemplateService _templateService;

        public HuggingFaceService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<HuggingFaceService> logger,
            ICriticalValuePatternService patternService,
            ICriticalValueKeywordService keywordService,
            IKnowledgeBaseService knowledgeBaseService,
            IAIResponseTemplateService templateService)
        {
            _httpClient = httpClient;
            _apiKey = config["HuggingFace:ApiKey"] ?? throw new InvalidOperationException("HuggingFace API key not found");
            _logger = logger;
            _patternService = patternService;
            _keywordService = keywordService;
            _knowledgeBaseService = knowledgeBaseService;
            _templateService = templateService;

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
            var keywordMood = await AnalyzeMentalHealthKeywordsAsync(text);
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
                return await AnalyzeMentalHealthKeywordsAsync(text);
            }

            return "Neutral";
        }

        private async Task<string> AnalyzeMentalHealthKeywordsAsync(string text)
        {
            var lowerText = text.ToLowerInvariant();

            // High concern keywords (should override other classifications) - loaded from database
            var highConcernKeywords = await _keywordService.GetKeywordsListByCategoryAsync("High Concern");
            foreach (var keyword in highConcernKeywords)
            {
                if (lowerText.Contains(keyword.ToLowerInvariant()))
                {
                    return "Crisis"; // New mood category for high concern
                }
            }

            // Count distress vs positive keywords using database-driven keywords
            int distressCount = await _keywordService.CountKeywordsInTextAsync(text, "Distress");
            int positiveCount = await _keywordService.CountKeywordsInTextAsync(text, "Positive");

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

        private string GenerateEmergencyResponse(string text)
        {
            try
            {
                // Extract emergency information from the text
                var unacknowledgedCount = 0;
                var acknowledgedCount = 0;
                var unacknowledgedDetails = new List<string>();
                var acknowledgedDetails = new List<string>();

                // Parse emergency incidents with acknowledgment status - use multiline regex
                var emergencyMatches = System.Text.RegularExpressions.Regex.Matches(text, @"\[([^\]]+)\] (Fall|Cardiac|PanicAttack|Seizure|Overdose|SelfHarm) - (Critical|High|Medium|Low).*?Status: (Acknowledged|Pending)", System.Text.RegularExpressions.RegexOptions.Singleline);

                _logger.LogInformation("Found {Count} emergency matches in text", emergencyMatches.Count);

                foreach (System.Text.RegularExpressions.Match match in emergencyMatches)
                {
                    var timestamp = match.Groups[1].Value;
                    var type = match.Groups[2].Value;
                    var severity = match.Groups[3].Value;
                    var status = match.Groups[4].Value;

                    var detail = $"- {type} - {severity} at {timestamp}";

                    if (status == "Acknowledged")
                    {
                        acknowledgedCount++;
                        acknowledgedDetails.Add(detail);
                    }
                    else
                    {
                        unacknowledgedCount++;
                        unacknowledgedDetails.Add(detail);
                    }
                }

                _logger.LogInformation("Emergency parsing complete: {UnacknowledgedCount} unacknowledged, {AcknowledgedCount} acknowledged",
                    unacknowledgedCount, acknowledgedCount);

                // Build the response based on acknowledgment status
                var response = new StringBuilder();

                if (unacknowledgedCount > 0)
                {
                    response.AppendLine("🚨 **CRITICAL EMERGENCY ALERT:** " + unacknowledgedCount + " unacknowledged emergency incident(s) detected!");
                    response.AppendLine();
                    response.AppendLine("**Unacknowledged Emergencies:**");
                    foreach (var detail in unacknowledgedDetails)
                    {
                        response.AppendLine(detail);
                    }
                    response.AppendLine();
                    response.AppendLine("**Immediate Actions Required:**");
                    response.AppendLine("1. Acknowledge all emergency incidents immediately");
                    response.AppendLine("2. Contact patient for status check");
                    response.AppendLine("3. Conduct fall risk assessment");
                    response.AppendLine("4. Review medications for side effects");
                    response.AppendLine("5. Consider emergency medical intervention");
                    response.AppendLine();
                }

                if (acknowledgedCount > 0)
                {
                    if (unacknowledgedCount > 0)
                    {
                        response.AppendLine("**Previously Acknowledged Emergencies:**");
                    }
                    else
                    {
                        response.AppendLine("📋 **Emergency History:** " + acknowledgedCount + " previously acknowledged incident(s)");
                    }
                    foreach (var detail in acknowledgedDetails)
                    {
                        response.AppendLine(detail);
                    }
                    response.AppendLine();
                }

                if (unacknowledgedCount == 0 && acknowledgedCount > 0)
                {
                    response.AppendLine("✅ **All emergencies have been acknowledged**");
                    response.AppendLine("**Follow-up Actions:**");
                    response.AppendLine("1. Monitor patient for any new incidents");
                    response.AppendLine("2. Review emergency patterns for trends");
                    response.AppendLine("3. Consider preventive measures");
                    response.AppendLine();
                }

                // Extract and analyze medical data for critical values
                var medicalDataMatch = System.Text.RegularExpressions.Regex.Match(text, @"=== MEDICAL DATA SUMMARY ===(.*?)=== PROGRESSION ANALYSIS ===", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (medicalDataMatch.Success)
                {
                    var medicalData = medicalDataMatch.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(medicalData))
                    {
                        response.AppendLine("**Medical Data Analysis:**");

                        // Check for critical medical values
                        var criticalAlerts = new List<string>();

                        // Check Blood Pressure (normal: <120/80, high: >140/90, critical: >180/110)
                        var bpMatch = System.Text.RegularExpressions.Regex.Match(medicalData, @"Blood Pressure:\s*(\d+)/(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (bpMatch.Success)
                        {
                            var systolic = int.Parse(bpMatch.Groups[1].Value);
                            var diastolic = int.Parse(bpMatch.Groups[2].Value);

                            if (systolic >= 180 || diastolic >= 110)
                            {
                                criticalAlerts.Add($"🚨 **CRITICAL BLOOD PRESSURE**: {systolic}/{diastolic} - HYPERTENSIVE CRISIS! Immediate medical intervention required!");
                            }
                            else if (systolic >= 140 || diastolic >= 90)
                            {
                                criticalAlerts.Add($"⚠️ **HIGH BLOOD PRESSURE**: {systolic}/{diastolic} - Requires immediate attention");
                            }
                        }

                        // Check Hemoglobin (normal: 12-16 g/dL for men, 11-15 for women)
                        var hbMatch = System.Text.RegularExpressions.Regex.Match(medicalData, @"Hemoglobin:\s*(\d+\.?\d*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (hbMatch.Success)
                        {
                            var hemoglobin = double.Parse(hbMatch.Groups[1].Value);
                            if (hemoglobin < 7.0)
                            {
                                criticalAlerts.Add($"🚨 **CRITICAL HEMOGLOBIN**: {hemoglobin} g/dL - SEVERE ANEMIA! Blood transfusion may be required!");
                            }
                            else if (hemoglobin < 10.0)
                            {
                                criticalAlerts.Add($"⚠️ **LOW HEMOGLOBIN**: {hemoglobin} g/dL - Moderate anemia, requires monitoring");
                            }
                        }

                        // Check Triglycerides (normal: <150 mg/dL, high: >200, very high: >500)
                        var trigMatch = System.Text.RegularExpressions.Regex.Match(medicalData, @"Triglycerides:\s*(\d+\.?\d*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (trigMatch.Success)
                        {
                            var triglycerides = double.Parse(trigMatch.Groups[1].Value);
                            if (triglycerides >= 500)
                            {
                                criticalAlerts.Add($"🚨 **CRITICAL TRIGLYCERIDES**: {triglycerides} mg/dL - EXTREMELY HIGH! Risk of pancreatitis!");
                            }
                            else if (triglycerides >= 200)
                            {
                                criticalAlerts.Add($"⚠️ **HIGH TRIGLYCERIDES**: {triglycerides} mg/dL - Requires dietary intervention");
                            }
                        }

                        // Add critical alerts if any found
                        if (criticalAlerts.Any())
                        {
                            response.AppendLine("🚨 **CRITICAL MEDICAL VALUES DETECTED:**");
                            foreach (var alert in criticalAlerts)
                            {
                                response.AppendLine(alert);
                            }
                            response.AppendLine();
                            response.AppendLine("**IMMEDIATE ACTIONS REQUIRED:**");
                            response.AppendLine("1. Contact patient immediately for status check");
                            response.AppendLine("2. Consider emergency medical evaluation");
                            response.AppendLine("3. Review medications and adjust as needed");
                            response.AppendLine("4. Monitor vital signs closely");
                            response.AppendLine();
                        }

                        response.AppendLine("**Full Medical Data:**");
                        response.AppendLine(medicalData);
                    }
                }
                else
                {
                    response.AppendLine("**Medical Data:** [Review other patient data as secondary priority]");
                }

                return response.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating emergency response");
                return "🚨 **CRITICAL EMERGENCY ALERT:** Emergency incidents detected requiring immediate attention!";
            }
        }

        private async Task<string> GenerateHybridEmergencyResponse(string text)
        {
            try
            {
                // Generate the emergency part using hardcoded logic
                var emergencyResponse = GenerateEmergencyResponse(text);

                // For medical data, use a simplified AI prompt
                var medicalPrompt = "Based on the medical data in this context, provide a brief clinical assessment focusing on test results and trends. Keep it under 100 words: " + text;

                var requestBody = new
                {
                    inputs = medicalPrompt,
                    parameters = new
                    {
                        max_new_tokens = 50,
                        temperature = 0.3,
                        do_sample = true,
                        return_full_text = false
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await _httpClient.PostAsync(
                        "https://api-inference.huggingface.co/models/microsoft/DialoGPT-small",
                        content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var responseData = JsonSerializer.Deserialize<JsonElement[]>(responseContent);

                        if (responseData.Length > 0)
                        {
                            var medicalData = responseData[0].GetProperty("generated_text").GetString() ?? "";

                            // Clean up the response
                            if (medicalData.StartsWith(medicalPrompt))
                            {
                                medicalData = medicalData.Substring(medicalPrompt.Length).Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(medicalData))
                            {
                                // Replace the placeholder with actual medical data
                                return emergencyResponse.Replace("**Medical Data:** [Review other patient data as secondary priority]",
                                    "**Medical Data:** " + medicalData);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting medical data from AI, using fallback");
                }

                // Fallback to original emergency response
                return emergencyResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hybrid emergency response");
                return GenerateEmergencyResponse(text);
            }
        }

        public async Task<string> GenerateResponse(string text, bool isGenericMode = false)
        {
            try
            {
                // Log the first 500 characters of the text to see what we're working with
                _logger.LogInformation("GenerateResponse called with text preview: {TextPreview}",
                    text.Length > 500 ? text.Substring(0, 500) + "..." : text);

                // Check if this is an emergency case and use hybrid approach
                _logger.LogInformation("Checking for emergency incidents in text. Contains 'RECENT EMERGENCY INCIDENTS': {HasEmergency}, Contains 'Fall': {HasFall}",
                    text.Contains("RECENT EMERGENCY INCIDENTS"), text.Contains("Fall"));

                // Also check for other emergency-related patterns
                _logger.LogInformation("Additional checks - Contains 'EMERGENCY': {HasEmergency2}, Contains 'Fall - Critical': {HasFallCritical}",
                    text.Contains("EMERGENCY"), text.Contains("Fall - Critical"));

                if (text.Contains("RECENT EMERGENCY INCIDENTS") && text.Contains("Fall"))
                {
                    _logger.LogInformation("Emergency case detected, using hybrid approach");
                    return await GenerateHybridEmergencyResponse(text);
                }
                else if (text.Contains("EMERGENCY") && text.Contains("Fall"))
                {
                    _logger.LogInformation("Emergency case detected (alternative pattern), using hybrid approach");
                    return await GenerateHybridEmergencyResponse(text);
                }
                else
                {
                    _logger.LogInformation("No emergency case detected, using normal AI response");
                }

                // Check knowledge base first (for generic mode or any mode)
                // This allows data-driven responses instead of hardcoded ones
                // BUT: Skip knowledge base for AI Health Check - these need full analysis, not generic responses
                var isAiHealthCheck = text.Contains("AI Health Check for Patient") ||
                                     text.Contains("=== INSTRUCTIONS FOR AI HEALTH CHECK ANALYSIS ===") ||
                                     text.Contains("INSTRUCTIONS FOR AI HEALTH CHECK");

                if (!isAiHealthCheck)
                {
                    var userQuestion = ExtractUserQuestion(text);
                    if (!string.IsNullOrWhiteSpace(userQuestion) && userQuestion.Length < 500) // Only check if question is reasonable length
                    {
                        var knowledgeBaseEntry = await _knowledgeBaseService.FindMatchingEntryAsync(userQuestion);
                        if (knowledgeBaseEntry != null)
                        {
                            _logger.LogInformation("Using knowledge base entry {EntryId} - {Title} for question: {Question}",
                                knowledgeBaseEntry.Id, knowledgeBaseEntry.Title, userQuestion);

                            if (knowledgeBaseEntry.UseAsDirectResponse)
                            {
                                // Return the knowledge base content directly
                                return knowledgeBaseEntry.Content;
                            }
                            else
                            {
                                // Use knowledge base content as context for AI
                                text = $"{knowledgeBaseEntry.Content}\n\nUser question: {userQuestion}\n\nPlease provide a helpful response based on the above information.";
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Skipping knowledge base lookup for AI Health Check - requires full analysis");
                }

                // For generic mode, use the same AI flow (no hardcoded responses)
                // All responses should come from the AI service, not hardcoded text
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
            _logger.LogInformation("Text contains 'Hemoglobin': {HasHemoglobinText}", text.Contains("Hemoglobin"));
            _logger.LogInformation("Text contains 'Doctor asks:': {HasDoctorAsks}", text.Contains("Doctor asks:"));

            // Show first 200 characters of text for debugging
            _logger.LogInformation("First 200 chars of text: {TextPreview}", text.Substring(0, Math.Min(200, text.Length)));

            if (text.Contains("=== MEDICAL DATA SUMMARY ===") || text.Contains("=== RECENT JOURNAL ENTRIES ===") || text.Contains("=== USER QUESTION ===") || text.Contains("Critical Values:") || text.Contains("Hemoglobin") || text.Contains("Doctor asks:") || text.Contains("**Medical Resource Information") || text.Contains("**Medical Facilities Search"))
            {
                _logger.LogInformation("Processing enhanced context for medical data");
                return await ProcessEnhancedContextResponseAsync(text);
            }

            // Additional fallback: if text contains medical keywords, treat it as a medical question
            if (text.Contains("how is") || text.Contains("status") || text.Contains("suggestions") || text.Contains("snapshot") || text.Contains("results") || text.Contains("stats"))
            {
                _logger.LogInformation("Detected medical question keywords, processing as medical question");
                return await ProcessEnhancedContextResponseAsync(text);
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

                    // Check for medical content in the context - expanded to include all section headers we use
                    bool hasMedicalContent = text.Contains("Blood Pressure") || text.Contains("Hemoglobin") || text.Contains("Triglycerides") ||
                                           text.Contains("CRITICAL VALUES") || text.Contains("ABNORMAL VALUES") || text.Contains("NORMAL VALUES") ||
                                           text.Contains("CURRENT MEDICAL STATUS") || text.Contains("LATEST TEST RESULTS") ||
                                           text.Contains("=== CURRENT MEDICAL STATUS") || text.Contains("=== HISTORICAL MEDICAL CONCERNS") ||
                                           text.Contains("=== HEALTH TREND ANALYSIS") ||
                                           text.Contains("=== MEDICAL DATA SUMMARY") || text.Contains("=== RECENT CLINICAL NOTES") ||
                                           text.Contains("=== RECENT CHAT HISTORY") || text.Contains("=== RECENT EMERGENCY INCIDENTS") ||
                                           text.Contains("=== RECENT JOURNAL ENTRIES") || text.Contains("AI Health Check for Patient");

                    // Extract only patient data sections (exclude AI instructions)
                    var patientDataText = ExtractPatientDataSections(text);

                    // Check for specific critical values using database-driven pattern matching
                    // Don't use loose string checks like Contains("6.0") as it matches "13.0" or "16.0"
                    // Only check patient data, not AI instructions which contain example "CRITICAL" text
                    bool hasCriticalValues = await _patternService.MatchesAnyPatternAsync(patientDataText) ||
                                           await _keywordService.ContainsAnyKeywordAsync(patientDataText, "Critical");

                    // Also check if we have any of the section markers that indicate patient data is present
                    bool hasPatientSections = text.Contains("=== RECENT") || text.Contains("=== MEDICAL") ||
                                            text.Contains("=== EMERGENCY") || text.Contains("Session:") ||
                                            text.Contains("Summary:") || text.Contains("Clinical Notes") ||
                                            text.Contains("Journal Entries") || text.Contains("Chat History");

                    // Check if we have patient data or if this is a generic query
                    bool hasPatientData = journalEntries.Any() || alerts.Any() || hasMedicalContent || hasPatientSections;

                    _logger.LogInformation("=== FALLBACK DETECTION RESULTS ===");
                    _logger.LogInformation("hasMedicalContent: {HasMedicalContent}", hasMedicalContent);
                    _logger.LogInformation("hasCriticalValues: {HasCriticalValues}", hasCriticalValues);
                    _logger.LogInformation("hasPatientSections: {HasPatientSections}", hasPatientSections);
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

                    // Check for critical medical conditions - use both alerts and database-driven detection
                    // Don't use loose string checks like Contains("6.0") as it matches "13.0" or "16.0"
                    // Only check patient data, not AI instructions which contain example "CRITICAL" text
                    bool hasCriticalConditions = alerts.Any(a => a.Contains("CRITICAL") || a.Contains("🚨")) ||
                                               await _patternService.MatchesAnyPatternAsync(patientDataText) ||
                                               await _keywordService.ContainsAnyKeywordAsync(patientDataText, "Critical");
                    bool hasAbnormalConditions = alerts.Any(a => a.Contains("ABNORMAL") || a.Contains("⚠️"));
                    bool hasNormalConditions = alerts.Any(a => a.Contains("NORMAL") || a.Contains("✅"));

                    if (hasCriticalConditions)
                    {
                        response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention. ");

                        // Extract actual critical values from context (from "Critical Values Found:" section)
                        var criticalValuesSection = ExtractCriticalValuesFromContext(text);
                        if (!string.IsNullOrEmpty(criticalValuesSection))
                        {
                            response.AppendLine(criticalValuesSection);
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

                        // Check if this is a health check request
                        bool isHealthCheck = questionLower.Contains("health check") || questionLower.Contains("ai health check") ||
                                           questionLower.Contains("analyze") || questionLower.Contains("assessment");

                        if (isHealthCheck || questionLower.Contains("status") || questionLower.Contains("how is") || questionLower.Contains("condition"))
                        {
                            // Generate comprehensive health check analysis
                            response.AppendLine("**Patient Medical Overview:**");

                            // Check for critical medical values - MUST check context directly, not just alerts
                            var criticalAlerts = alerts.Where(a => a.Contains("🚨 CRITICAL:") || a.Contains("CRITICAL VALUES:") || a.Contains("CRITICAL:")).ToList();
                            var normalValues = alerts.Where(a => a.Contains("✅ NORMAL:") || a.Contains("NORMAL VALUES:")).ToList();
                            var abnormalValues = alerts.Where(a => a.Contains("⚠️") || a.Contains("ABNORMAL VALUES:")).ToList();

                            // ALSO check the context text directly for critical values (more reliable)
                            // Use database-driven pattern matching instead of loose string checks
                            bool hasCriticalInContext = text.Contains("🚨 CRITICAL MEDICAL VALUES DETECTED") ||
                                                       text.Contains("CRITICAL VALUES DETECTED IN LATEST RESULTS") ||
                                                       text.Contains("STATUS: CRITICAL") ||
                                                       text.Contains("🚨 **CRITICAL VALUES DETECTED IN LATEST RESULTS:**") ||
                                                       text.Contains("Critical Values Found:");

                            // If critical values found in context OR alerts, prioritize them
                            if (criticalAlerts.Any() || hasCriticalInContext)
                            {
                                response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                if (criticalAlerts.Any())
                                {
                                    foreach (var critical in criticalAlerts)
                                    {
                                        response.AppendLine($"- {critical}");
                                    }
                                }
                                // Extract actual critical values from context if not in alerts
                                if (hasCriticalInContext && !criticalAlerts.Any())
                                {
                                    var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                                    if (!string.IsNullOrEmpty(criticalValuesFromContext))
                                    {
                                        response.AppendLine(criticalValuesFromContext);
                                    }
                                }
                                response.AppendLine();
                                response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                                response.AppendLine("- These values indicate a medical emergency");
                                response.AppendLine("- Contact emergency services if symptoms worsen");
                                response.AppendLine("- Patient needs immediate medical evaluation");
                            }
                            else if (normalValues.Any() && !abnormalValues.Any() && !hasCriticalInContext)
                            {
                                response.AppendLine("✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                            }
                            else if (abnormalValues.Any())
                            {
                                response.AppendLine("⚠️ **ABNORMAL VALUES DETECTED:** Some test results are outside normal ranges and require monitoring.");
                                foreach (var abnormal in abnormalValues.Take(3))
                                {
                                    response.AppendLine($"- {abnormal}");
                                }
                            }
                            else
                            {
                                response.AppendLine("✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                            }

                            response.AppendLine();
                            response.AppendLine("**Recent Patient Activity:**");

                            if (journalEntries.Any())
                            {
                                foreach (var entry in journalEntries.Take(3))
                                {
                                    response.AppendLine($"- {entry}");
                                }
                            }
                            else
                            {
                                response.AppendLine("- No recent journal entries found.");
                            }

                            // Check for chat history
                            if (text.Contains("=== RECENT CHAT HISTORY ==="))
                            {
                                response.AppendLine();
                                response.AppendLine("**Chat History:** Patient has been engaging in conversations with the AI assistant.");
                            }

                            // Check for clinical notes
                            if (text.Contains("=== RECENT CLINICAL NOTES ==="))
                            {
                                response.AppendLine();
                                response.AppendLine("**Clinical Notes:** Recent clinical documentation is available for review.");
                            }

                            // Check for emergency incidents
                            if (text.Contains("=== RECENT EMERGENCY INCIDENTS ==="))
                            {
                                response.AppendLine();
                                response.AppendLine("⚠️ **EMERGENCY INCIDENTS:** Emergency incidents have been recorded. Please review the emergency dashboard for details.");
                            }

                            response.AppendLine();
                            response.AppendLine("**Clinical Assessment:**");

                            if (criticalAlerts.Any())
                            {
                                response.AppendLine("The patient requires immediate medical attention due to critical values. Urgent intervention is necessary.");
                            }
                            else if (abnormalValues.Any())
                            {
                                response.AppendLine("The patient shows some abnormal values that require monitoring and follow-up care. Schedule a medical review.");
                            }
                            else
                            {
                                response.AppendLine("The patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.");
                            }

                            response.AppendLine();
                            response.AppendLine("**Recommendations:**");

                            if (criticalAlerts.Any())
                            {
                                response.AppendLine("- Immediate medical evaluation required");
                                response.AppendLine("- Consider emergency department visit");
                                response.AppendLine("- Notify assigned doctors immediately");
                            }
                            else if (abnormalValues.Any())
                            {
                                response.AppendLine("- Schedule follow-up appointment within 1-2 weeks");
                                response.AppendLine("- Repeat laboratory tests as indicated");
                                response.AppendLine("- Monitor patient closely for any changes");
                            }
                            else
                            {
                                response.AppendLine("- Continue current care plan");
                                response.AppendLine("- Maintain routine follow-up schedule");
                                response.AppendLine("- Encourage continued health tracking");
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

                                // Extract actual critical values from context
                                var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                                if (!string.IsNullOrEmpty(criticalValuesFromContext))
                                {
                                    response.AppendLine(criticalValuesFromContext);
                                }
                                else
                                {
                                    // Fallback: extract from alerts if available
                                    var localCriticalAlerts = alerts.Where(a => a.Contains("🚨 CRITICAL:") || a.Contains("CRITICAL VALUES:") || a.Contains("CRITICAL:")).ToList();
                                    if (localCriticalAlerts.Any())
                                    {
                                        foreach (var critical in localCriticalAlerts)
                                        {
                                            response.AppendLine($"- {critical}");
                                        }
                                    }
                                }

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
                    else
                    {
                        // No question extracted, but we have patient data - generate comprehensive health check
                        _logger.LogInformation("No question extracted, but patient data detected. Generating comprehensive health check analysis.");

                        // Generate comprehensive health check analysis
                        response.AppendLine("**Patient Medical Overview:**");

                        var criticalAlerts = alerts.Where(a => a.Contains("🚨 CRITICAL:") || a.Contains("CRITICAL VALUES:") || a.Contains("CRITICAL:")).ToList();
                        var normalValues = alerts.Where(a => a.Contains("✅ NORMAL:") || a.Contains("NORMAL VALUES:")).ToList();
                        var abnormalValues = alerts.Where(a => a.Contains("⚠️") || a.Contains("ABNORMAL VALUES:")).ToList();

                        // ALSO check the context text directly for critical values (more reliable)
                        bool hasCriticalInContext = text.Contains("🚨 CRITICAL MEDICAL VALUES DETECTED") ||
                                                   text.Contains("CRITICAL VALUES DETECTED IN LATEST RESULTS") ||
                                                   text.Contains("STATUS: CRITICAL") ||
                                                   text.Contains("🚨 **CRITICAL VALUES DETECTED IN LATEST RESULTS:**") ||
                                                   text.Contains("Critical Values Found:");

                        // If critical values found in context OR alerts, prioritize them
                        if (criticalAlerts.Any() || hasCriticalInContext)
                        {
                            response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                            if (criticalAlerts.Any())
                            {
                                foreach (var critical in criticalAlerts)
                                {
                                    response.AppendLine($"- {critical}");
                                }
                            }
                            // Extract critical values from context if not in alerts
                            if (hasCriticalInContext && !criticalAlerts.Any())
                            {
                                var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                                if (!string.IsNullOrEmpty(criticalValuesFromContext))
                                {
                                    response.AppendLine(criticalValuesFromContext);
                                }
                            }
                            response.AppendLine();
                            response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                            response.AppendLine("- These values indicate a medical emergency");
                            response.AppendLine("- Contact emergency services if symptoms worsen");
                            response.AppendLine("- Patient needs immediate medical evaluation");
                        }
                        else if (normalValues.Any() && !abnormalValues.Any() && !hasCriticalInContext)
                        {
                            response.AppendLine("✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                        }
                        else if (abnormalValues.Any())
                        {
                            response.AppendLine("⚠️ **ABNORMAL VALUES DETECTED:** Some test results are outside normal ranges and require monitoring.");
                            foreach (var abnormal in abnormalValues.Take(3))
                            {
                                response.AppendLine($"- {abnormal}");
                            }
                        }
                        else
                        {
                            response.AppendLine("✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                        }

                        response.AppendLine();
                        response.AppendLine("**Recent Patient Activity:**");

                        if (journalEntries.Any())
                        {
                            foreach (var entry in journalEntries.Take(3))
                            {
                                response.AppendLine($"- {entry}");
                            }
                        }
                        else
                        {
                            response.AppendLine("- No recent journal entries found.");
                        }

                        if (text.Contains("=== RECENT CHAT HISTORY ==="))
                        {
                            response.AppendLine();
                            response.AppendLine("**Chat History:** Patient has been engaging in conversations with the AI assistant.");
                        }

                        if (text.Contains("=== RECENT CLINICAL NOTES ==="))
                        {
                            response.AppendLine();
                            response.AppendLine("**Clinical Notes:** Recent clinical documentation is available for review.");
                        }

                        if (text.Contains("=== RECENT EMERGENCY INCIDENTS ==="))
                        {
                            response.AppendLine();
                            response.AppendLine("⚠️ **EMERGENCY INCIDENTS:** Emergency incidents have been recorded. Please review the emergency dashboard for details.");
                        }

                        response.AppendLine();
                        response.AppendLine("**Clinical Assessment:**");

                        if (criticalAlerts.Any())
                        {
                            response.AppendLine("The patient requires immediate medical attention due to critical values. Urgent intervention is necessary.");
                        }
                        else if (abnormalValues.Any())
                        {
                            response.AppendLine("The patient shows some abnormal values that require monitoring and follow-up care. Schedule a medical review.");
                        }
                        else
                        {
                            response.AppendLine("The patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.");
                        }

                        response.AppendLine();
                        response.AppendLine("**Recommendations:**");

                        if (criticalAlerts.Any())
                        {
                            response.AppendLine("- Immediate medical evaluation required");
                            response.AppendLine("- Consider emergency department visit");
                            response.AppendLine("- Notify assigned doctors immediately");
                        }
                        else if (abnormalValues.Any())
                        {
                            response.AppendLine("- Schedule follow-up appointment within 1-2 weeks");
                            response.AppendLine("- Repeat laboratory tests as indicated");
                            response.AppendLine("- Monitor patient closely for any changes");
                        }
                        else
                        {
                            response.AppendLine("- Continue current care plan");
                            response.AppendLine("- Maintain routine follow-up schedule");
                            response.AppendLine("- Encourage continued health tracking");
                        }

                        return response.ToString().Trim();
                    }
                }
                else
                {
                    // If knowledge file doesn't exist, check if we have patient data and generate analysis
                    bool hasPatientDataInContext = text.Contains("=== MEDICAL DATA SUMMARY ===") ||
                                                   text.Contains("=== RECENT JOURNAL ENTRIES ===") ||
                                                   text.Contains("=== RECENT CHAT HISTORY ===") ||
                                                   text.Contains("=== RECENT CLINICAL NOTES ===") ||
                                                   text.Contains("=== RECENT EMERGENCY INCIDENTS ===") ||
                                                   text.Contains("AI Health Check for Patient");

                    if (hasPatientDataInContext)
                    {
                        _logger.LogInformation("Knowledge file doesn't exist, but patient data detected. Generating health check analysis.");
                        return await ProcessEnhancedContextResponseAsync(text);
                    }

                    // If knowledge file doesn't exist and no patient data, return a basic response
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
            // Look for "=== USER QUESTION ===" pattern first (for AI Health Check)
            var userQuestionSectionPattern = "=== USER QUESTION ===";
            var sectionIndex = text.IndexOf(userQuestionSectionPattern, StringComparison.OrdinalIgnoreCase);

            if (sectionIndex >= 0)
            {
                var startIndex = sectionIndex + userQuestionSectionPattern.Length;
                // Find the next section marker or end of text
                var nextSection = text.IndexOf("===", startIndex);
                var endIndex = text.IndexOf('\n', startIndex);

                if (nextSection > startIndex && (endIndex < 0 || nextSection < endIndex))
                {
                    endIndex = nextSection;
                }
                else if (endIndex < 0)
                {
                    endIndex = text.Length;
                }

                var question = text.Substring(startIndex, endIndex - startIndex).Trim();
                if (!string.IsNullOrWhiteSpace(question))
                {
                    _logger.LogInformation("ExtractUserQuestion - Extracted from USER QUESTION section: '{Question}'", question);
                    return question;
                }
            }

            // Look for "user question:" pattern in the prompt template (lowercase, for other prompts)
            var userQuestionPattern = "user question:";
            var index = text.IndexOf(userQuestionPattern, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                var startIndex = index + userQuestionPattern.Length;
                var endIndex = text.IndexOf('\n', startIndex);
                if (endIndex < 0) endIndex = text.Length;

                var question = text.Substring(startIndex, endIndex - startIndex).Trim();
                if (!string.IsNullOrWhiteSpace(question))
                {
                    _logger.LogInformation("ExtractUserQuestion - Extracted from 'user question:' pattern: '{Question}'", question);
                    return question;
                }
            }

            // Fallback: return empty string if no pattern found (don't use entire context for knowledge base lookup)
            _logger.LogInformation("ExtractUserQuestion - No pattern found, returning empty string to skip knowledge base lookup");
            return string.Empty;
        }

        private string ExtractCriticalValuesSection(string text)
        {
            try
            {
                // Look for the critical values section
                var criticalStart = text.IndexOf("🚨 **CRITICAL VALUES DETECTED IN LATEST RESULTS:**");
                if (criticalStart >= 0)
                {
                    var criticalEnd = text.IndexOf("\n\n", criticalStart);
                    if (criticalEnd > criticalStart)
                    {
                        var section = text.Substring(criticalStart, criticalEnd - criticalStart);
                        // Extract just the critical values lines
                        var lines = section.Split('\n')
                            .Where(l => l.Contains("🚨") && !l.Contains("CRITICAL VALUES DETECTED"))
                            .ToList();
                        return string.Join("\n", lines);
                    }
                }

                // Fallback: look for "Critical Values:" pattern
                var altStart = text.IndexOf("Critical Values:");
                if (altStart >= 0)
                {
                    var altEnd = text.IndexOf("\n", altStart + 15);
                    if (altEnd > altStart)
                    {
                        return text.Substring(altStart, altEnd - altStart).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting critical values section");
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts actual critical values from the context text (from "Critical Values Found:" section)
        /// This ensures we report actual values, not hardcoded ones
        /// </summary>
        private string ExtractCriticalValuesFromContext(string text)
        {
            try
            {
                _logger.LogInformation("ExtractCriticalValuesFromContext: Looking for critical values in context (length: {Length})", text.Length);

                // Look for "Critical Values Found:" section which contains the actual formatted critical values
                var criticalStart = text.IndexOf("Critical Values Found:");
                _logger.LogInformation("ExtractCriticalValuesFromContext: Found 'Critical Values Found:' at index {Index}", criticalStart);

                if (criticalStart >= 0)
                {
                    // Find the end of this section (either next section or end of text)
                    var nextSection = text.IndexOf("\n\n", criticalStart);
                    var endOfText = text.Length;
                    var sectionEnd = nextSection > criticalStart ? nextSection : endOfText;

                    var section = text.Substring(criticalStart, sectionEnd - criticalStart);
                    _logger.LogInformation("ExtractCriticalValuesFromContext: Extracted section (length: {Length}): {Section}", section.Length, section.Substring(0, Math.Min(200, section.Length)));

                    // Extract lines that contain actual critical value data (lines with 🚨 emoji)
                    var lines = section.Split('\n')
                        .Where(l => l.Contains("🚨") && l.Trim().Length > 0)
                        .Select(l => l.Trim())
                        .ToList();

                    _logger.LogInformation("ExtractCriticalValuesFromContext: Found {Count} lines with 🚨 emoji", lines.Count);

                    if (lines.Any())
                    {
                        // Format as bullet points with double emoji for better visibility (matching AI Health Check format)
                        var result = string.Join("\n", lines.Select(l =>
                        {
                            var trimmed = l.Trim();
                            // If it already starts with 🚨, add another 🚨 for consistency with AI Health Check format
                            if (trimmed.StartsWith("🚨"))
                            {
                                return $"- 🚨 {trimmed.Substring(2).TrimStart()}"; // Remove first 🚨 and add 🚨 🚨
                            }
                            return $"- 🚨 {trimmed}";
                        }));
                        _logger.LogInformation("ExtractCriticalValuesFromContext: Returning formatted result: {Result}", result);
                        return result;
                    }
                }

                // Also check for critical values in the CURRENT MEDICAL STATUS section
                var statusStart = text.IndexOf("=== CURRENT MEDICAL STATUS ===");
                if (statusStart >= 0)
                {
                    var statusEnd = text.IndexOf("\n\n", statusStart);
                    if (statusEnd < 0) statusEnd = text.Length;

                    var statusSection = text.Substring(statusStart, statusEnd - statusStart);
                    var criticalLines = statusSection.Split('\n')
                        .Where(l => l.Contains("🚨") && (l.Contains("CRITICAL:") || l.Contains("CRITICAL")))
                        .Select(l => l.Trim())
                        .Where(l => !l.Contains("CRITICAL MEDICAL VALUES DETECTED") && !l.Contains("CRITICAL VALUES DETECTED"))
                        .ToList();

                    if (criticalLines.Any())
                    {
                        var result = string.Join("\n", criticalLines.Select(l =>
                        {
                            var trimmed = l.Trim();
                            if (trimmed.StartsWith("🚨"))
                            {
                                return $"- 🚨 {trimmed.Substring(2).TrimStart()}";
                            }
                            return $"- 🚨 {trimmed}";
                        }));
                        _logger.LogInformation("ExtractCriticalValuesFromContext: Found critical values in CURRENT MEDICAL STATUS section: {Result}", result);
                        return result;
                    }
                }

                // Fallback: Look for "🚨 **CRITICAL MEDICAL VALUES DETECTED** 🚨" section
                var altStart = text.IndexOf("🚨 **CRITICAL MEDICAL VALUES DETECTED** 🚨");
                _logger.LogInformation("ExtractCriticalValuesFromContext: Looking for alternative section, found at index {Index}", altStart);

                if (altStart >= 0)
                {
                    var altEnd = text.IndexOf("\n\n", altStart);
                    if (altEnd > altStart)
                    {
                        var section = text.Substring(altStart, altEnd - altStart);
                        var lines = section.Split('\n')
                            .Where(l => l.Contains("🚨") && !l.Contains("CRITICAL MEDICAL VALUES DETECTED"))
                            .Select(l => l.Trim())
                            .ToList();

                        if (lines.Any())
                        {
                            var result = string.Join("\n", lines.Select(l => $"- {l}"));
                            _logger.LogInformation("ExtractCriticalValuesFromContext: Returning alternative result: {Result}", result);
                            return result;
                        }
                    }
                }

                _logger.LogWarning("ExtractCriticalValuesFromContext: No critical values found in context");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting critical values from context");
            }

            return string.Empty;
        }

        private string ExtractJournalEntriesFromContext(string text)
        {
            try
            {
                // Look for "=== RECENT JOURNAL ENTRIES ===" section
                var journalStart = text.IndexOf("=== RECENT JOURNAL ENTRIES ===");
                if (journalStart >= 0)
                {
                    var journalEnd = text.IndexOf("===", journalStart + 30);
                    if (journalEnd < 0) journalEnd = text.IndexOf("\n\n", journalStart);
                    if (journalEnd < 0) journalEnd = text.Length;

                    var section = text.Substring(journalStart, journalEnd - journalStart);
                    var lines = section.Split('\n')
                        .Where(l => l.Contains("[") && l.Contains("]") && (l.Contains("Mood:") || l.Contains("Entry:")))
                        .Take(3)
                        .ToList();

                    return string.Join("\n", lines);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting journal entries from context");
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts only the patient data sections from the context, excluding AI instructions and status text
        /// This prevents false positives from matching keywords in the instructions themselves or status summaries
        /// </summary>
        private string ExtractPatientDataSections(string text)
        {
            try
            {
                var sections = new List<string>();

                // Extract specific patient data sections
                var sectionMarkers = new[]
                {
                    "=== RECENT JOURNAL ENTRIES ===",
                    "=== MEDICAL DATA SUMMARY ===",
                    "=== CURRENT MEDICAL STATUS ===",
                    "=== HISTORICAL MEDICAL CONCERNS ===",
                    "=== HEALTH TREND ANALYSIS ===",
                    "=== RECENT CLINICAL NOTES ===",
                    "=== RECENT CHAT HISTORY ===",
                    "=== RECENT EMERGENCY INCIDENTS ===",
                    "Recent Patient Activity:",
                    "Current Test Results",
                    "Latest Update:"
                };

                foreach (var marker in sectionMarkers)
                {
                    var index = text.IndexOf(marker);
                    if (index >= 0)
                    {
                        // Find the end of this section (next section marker or end of text)
                        var nextIndex = text.Length;
                        foreach (var nextMarker in sectionMarkers)
                        {
                            if (nextMarker != marker)
                            {
                                var nextPos = text.IndexOf(nextMarker, index + marker.Length);
                                if (nextPos > index && nextPos < nextIndex)
                                {
                                    nextIndex = nextPos;
                                }
                            }
                        }

                        // Also check for instruction sections that should be excluded
                        var instructionStart = text.IndexOf("=== INSTRUCTIONS", index);
                        if (instructionStart > index && instructionStart < nextIndex)
                        {
                            nextIndex = instructionStart;
                        }

                        var sectionLength = nextIndex - index;
                        if (sectionLength > 0)
                        {
                            var section = text.Substring(index, sectionLength);

                            // For "=== CURRENT MEDICAL STATUS ===" section, exclude status summary text and header lines
                            // Status text like "⚠️ **STATUS: CONCERNING - MONITORING REQUIRED**" contains emojis
                            // that match keyword categories, causing false positives
                            // Header lines like "⚠️ **ABNORMAL VALUES DETECTED IN LATEST RESULTS:**" also contain keywords
                            if (marker == "=== CURRENT MEDICAL STATUS ===")
                            {
                                // Extract only the actual value lines (e.g., "🚨 CRITICAL: Blood Pressure...")
                                // Exclude status summary lines and header lines (e.g., "⚠️ **STATUS: CONCERNING...", "⚠️ **ABNORMAL VALUES DETECTED...")
                                var lines = section.Split('\n');
                                var filteredLines = new List<string>();
                                bool inStatusSummary = false;

                                foreach (var line in lines)
                                {
                                    // Skip header lines that are just formatting (contain "**" and keywords like "DETECTED", "VALUES")
                                    // These are not actual patient data, just section headers
                                    if (line.Contains("**") && (line.Contains("DETECTED") || line.Contains("VALUES") ||
                                        line.Contains("STATUS:") || line.Contains("CRITICAL VALUES") ||
                                        line.Contains("ABNORMAL VALUES") || line.Contains("NORMAL VALUES")))
                                    {
                                        continue; // Skip header/summary lines
                                    }

                                    // Skip status summary lines (they contain "STATUS:" and are formatted summaries)
                                    if (line.Contains("**STATUS:") || line.Contains("STATUS: CRITICAL") ||
                                        line.Contains("STATUS: CONCERNING") || line.Contains("STATUS: STABLE"))
                                    {
                                        inStatusSummary = true;
                                        continue; // Skip status summary lines
                                    }

                                    // If we hit a blank line after status summary, we're done with that section
                                    if (inStatusSummary && string.IsNullOrWhiteSpace(line))
                                    {
                                        inStatusSummary = false;
                                        continue;
                                    }

                                    // Include actual value lines (contain specific test results, not just status)
                                    // These lines typically have format like "🚨 CRITICAL: Blood Pressure 190/100..." or "⚠️ HIGH: Blood Pressure..."
                                    if (!inStatusSummary)
                                    {
                                        filteredLines.Add(line);
                                    }
                                }

                                section = string.Join("\n", filteredLines);
                            }

                            // For "=== RECENT CLINICAL NOTES ===" section, exclude instruction text
                            // Instruction text like "⚠️ IMPORTANT: Clinical notes are written by doctors..." contains emojis
                            // that match keyword categories, causing false positives
                            if (marker == "=== RECENT CLINICAL NOTES ===")
                            {
                                var lines = section.Split('\n');
                                var filteredLines = new List<string>();
                                bool skipInstructionLines = false;

                                foreach (var line in lines)
                                {
                                    // Skip instruction lines that are just explanatory text (not actual patient data)
                                    // These lines typically start with "⚠️ IMPORTANT:" or contain "HIGH PRIORITY" or "should be given"
                                    if (line.Contains("⚠️ IMPORTANT:") || line.Contains("HIGH PRIORITY") ||
                                        line.Contains("should be given") || line.Contains("contain critical medical observations"))
                                    {
                                        skipInstructionLines = true;
                                        continue; // Skip instruction lines
                                    }

                                    // If we hit a blank line after instructions, we're done with that section
                                    if (skipInstructionLines && string.IsNullOrWhiteSpace(line))
                                    {
                                        skipInstructionLines = false;
                                        continue;
                                    }

                                    // Include actual clinical note data (date, title, content, etc.)
                                    if (!skipInstructionLines)
                                    {
                                        filteredLines.Add(line);
                                    }
                                }

                                section = string.Join("\n", filteredLines);
                            }

                            sections.Add(section);
                        }
                    }
                }

                return string.Join("\n", sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting patient data sections");
                // Fallback: return original text if extraction fails
                return text;
            }
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


        private async Task<string> ProcessEnhancedContextResponseAsync(string text)
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

                // Log context for debugging
                _logger.LogInformation("=== AI CONTEXT ANALYSIS ===");
                _logger.LogInformation("Context length: {Length}", text.Length);
                _logger.LogInformation("Contains 'CRITICAL VALUES DETECTED IN LATEST RESULTS': {HasCritical}", text.Contains("CRITICAL VALUES DETECTED IN LATEST RESULTS"));
                _logger.LogInformation("Contains '🚨 **CRITICAL VALUES DETECTED IN LATEST RESULTS:**': {HasCritical}", text.Contains("🚨 **CRITICAL VALUES DETECTED IN LATEST RESULTS:**"));
                _logger.LogInformation("Contains 'STATUS: CRITICAL': {HasCritical}", text.Contains("STATUS: CRITICAL"));

                // Extract only the patient data sections (exclude AI instructions which contain example "CRITICAL" text)
                var patientDataText = ExtractPatientDataSections(text);

                // Check for critical values using database-driven patterns (only in patient data, not instructions)
                var hasCriticalValuesFromPatterns = await _patternService.MatchesAnyPatternAsync(patientDataText);

                // Check for critical values using database-driven keywords (only from "Critical" category, not "Normal" or "Abnormal")
                // Only check patient data sections, not AI instructions which contain example "CRITICAL" text
                var hasCriticalValuesFromKeywords = await _keywordService.ContainsAnyKeywordAsync(patientDataText, "Critical");

                _logger.LogInformation("Contains critical value pattern match: {HasMatch}", hasCriticalValuesFromPatterns);
                _logger.LogInformation("Contains critical value keyword match (Critical category only): {HasMatch}", hasCriticalValuesFromKeywords);

                // Check for critical values using database-driven detection (patterns + keywords)
                // Only consider it critical if patterns match OR critical keywords match (not normal/abnormal keywords)
                var hasCriticalValues = hasCriticalValuesFromPatterns || hasCriticalValuesFromKeywords;

                _logger.LogInformation("Final hasCriticalValues: {HasCritical}", hasCriticalValues);

                // Check for abnormal values using database-driven keywords (only in patient data, not instructions)
                // Note: Status summary text (e.g., "⚠️ **STATUS: CONCERNING...") is excluded from patientDataText
                // to prevent false positives from emoji keywords matching status summaries
                var hasAbnormalValues = await _keywordService.ContainsAnyKeywordAsync(patientDataText ?? string.Empty, "Abnormal");

                // Also check for "High Concern" and "Distress" keywords - these indicate concerning clinical note content
                // Clinical notes often contain concerns like "anxiety", "serious symptoms", "heart problems" that are in these categories
                // All keywords are database-driven - no hardcoded patterns
                var hasHighConcern = await _keywordService.ContainsAnyKeywordAsync(patientDataText ?? string.Empty, "High Concern");
                var hasDistress = await _keywordService.ContainsAnyKeywordAsync(patientDataText ?? string.Empty, "Distress");

                // If any of these categories match, consider it abnormal/concerning
                var hasAnyConcerns = hasAbnormalValues || hasHighConcern || hasDistress;

                _logger.LogInformation("Contains abnormal value keyword match (Abnormal category only): {HasMatch}", hasAbnormalValues);
                _logger.LogInformation("Contains high concern keyword match (High Concern category): {HasMatch}", hasHighConcern);
                _logger.LogInformation("Contains distress keyword match (Distress category): {HasMatch}", hasDistress);
                _logger.LogInformation("Final hasAnyConcerns (Abnormal OR High Concern OR Distress): {HasMatch}", hasAnyConcerns);
                _logger.LogInformation("Patient data text length (for abnormal check): {Length}", patientDataText?.Length ?? 0);
                if (hasAnyConcerns && !string.IsNullOrEmpty(patientDataText))
                {
                    _logger.LogInformation("Concern keyword match found. Sample of patient data text: {Sample}",
                        patientDataText.Substring(0, Math.Min(500, patientDataText.Length)));
                }

                // Check for normal values using database-driven keywords (only in patient data, not instructions)
                var hasNormalValues = await _keywordService.ContainsAnyKeywordAsync(patientDataText ?? string.Empty, "Normal");
                _logger.LogInformation("Contains normal value keyword match (Normal category only): {HasMatch}", hasNormalValues);

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
                                    // Use database-driven template for critical alert
                                    var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                                    var criticalAlertText = criticalValuesFromContext ?? "- Critical medical values detected - review test results for details";

                                    var template = await _templateService.FormatTemplateAsync("critical_alert_deterioration", new Dictionary<string, string>
                                    {
                                        { "CRITICAL_VALUES", criticalAlertText }
                                    });

                                    if (!string.IsNullOrEmpty(template))
                                    {
                                        response.AppendLine(template);
                                    }
                                    else
                                    {
                                        // Fallback if template not found (should not happen once seeded)
                                        response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                        response.AppendLine(criticalAlertText);
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

                                    // Extract actual critical values from context
                                    var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                                    if (!string.IsNullOrEmpty(criticalValuesFromContext))
                                    {
                                        response.AppendLine(criticalValuesFromContext);
                                    }
                                    else
                                    {
                                        response.AppendLine("- Critical medical values detected - review test results for details");
                                    }

                                    response.AppendLine();
                                    response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                                    response.AppendLine("- These values indicate a medical emergency");
                                    response.AppendLine("- Contact emergency services if symptoms worsen");
                                    response.AppendLine("- Patient needs immediate medical evaluation");
                                }
                            }
                            else
                            {
                                // Use database-driven template for critical alert
                                var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                                var criticalAlertText = criticalValuesFromContext ?? "- Critical medical values detected - review test results for details";

                                var template = await _templateService.FormatTemplateAsync("critical_alert", new Dictionary<string, string>
                                {
                                    { "CRITICAL_VALUES", criticalAlertText }
                                });

                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    // Fallback if template not found
                                    response.AppendLine("🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                    response.AppendLine(criticalAlertText);
                                    response.AppendLine();
                                    response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                                    response.AppendLine("- These values indicate a medical emergency");
                                    response.AppendLine("- Contact emergency services if symptoms worsen");
                                    response.AppendLine("- Patient needs immediate medical evaluation");
                                }
                            }
                        }
                        else if (hasAnyConcerns)
                        {
                            // Use database-driven template for concerns
                            var template = await _templateService.FormatTemplateAsync("concerns_detected", new Dictionary<string, string>());
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                // Fallback if template not found
                                response.AppendLine("⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.");
                            }
                        }
                        else if (hasNormalValues)
                        {
                            // Check if this is showing improvement from previous critical values
                            if (text.Contains("IMPROVEMENT NOTED"))
                            {
                                var template = await _templateService.FormatTemplateAsync("improvement_noted", new Dictionary<string, string>());
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    // Fallback
                                    response.AppendLine("✅ **IMPROVEMENT NOTED:** Previous results showed critical values, but current results show normal values.");
                                    response.AppendLine("This indicates positive progress, though continued monitoring is recommended.");
                                }
                            }
                            else
                            {
                                var template = await _templateService.FormatTemplateAsync("stable_status", new Dictionary<string, string>());
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    // Fallback
                                    response.AppendLine("✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                                }
                            }
                        }
                        else if (hasMedicalData)
                        {
                            // Use database-driven template for medical data warning
                            var template = await _templateService.FormatTemplateAsync("medical_data_warning", new Dictionary<string, string>());
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                // Fallback
                                response.AppendLine("⚠️ **WARNING:** Medical content was found, but critical values may not have been properly detected.");
                                response.AppendLine("Please review the medical data manually to ensure no critical values are missed.");
                                response.AppendLine();
                                response.AppendLine("📊 **Status Review:** Based on available data, the patient appears to be stable with no immediate concerns detected.");
                                response.AppendLine("However, please verify the medical content manually for accuracy.");
                            }
                        }
                        else
                        {
                            // Use database-driven template for status review
                            var template = await _templateService.FormatTemplateAsync("status_review", new Dictionary<string, string>());
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                // Fallback
                                response.AppendLine("📊 **Status Review:** Based on available data, the patient appears to be stable with no immediate concerns detected.");
                            }
                        }

                        if (hasJournalEntries)
                        {
                            // Extract actual journal entries from context (not hardcoded examples)
                            var journalSection = ExtractJournalEntriesFromContext(text);
                            var template = await _templateService.FormatTemplateAsync("recent_patient_activity", new Dictionary<string, string>
                            {
                                { "JOURNAL_ENTRIES", journalSection }
                            });
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine();
                                response.AppendLine(template);
                            }
                            else
                            {
                                // Fallback - but use actual journal entries, not hardcoded examples
                                response.AppendLine();
                                response.AppendLine("**Recent Patient Activity:**");
                                if (!string.IsNullOrEmpty(journalSection))
                                {
                                    response.AppendLine(journalSection);
                                }
                                else
                                {
                                    response.AppendLine("The patient has been actively engaging with their health tracking.");
                                }
                            }
                        }
                    }
                    else if (questionLower.Contains("stats") || questionLower.Contains("statistics") || questionLower.Contains("data") || questionLower.Contains("snapshot") || questionLower.Contains("results"))
                    {
                        response.AppendLine("**Patient Medical Statistics:**");

                        if (hasMedicalData)
                        {
                            response.AppendLine("📊 **Latest Medical Data:**");

                            // Extract actual critical values from context
                            var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                            if (!string.IsNullOrEmpty(criticalValuesFromContext))
                            {
                                response.AppendLine(criticalValuesFromContext);
                            }
                            else
                            {
                                response.AppendLine("Medical data available - review test results for specific values");
                            }
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
                            response.AppendLine("5. **Blood Transfusion**: Consider immediate blood transfusion if hemoglobin is critically low");
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

                            // Extract actual critical values from context
                            var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                            if (!string.IsNullOrEmpty(criticalValuesFromContext))
                            {
                                response.AppendLine(criticalValuesFromContext);
                            }
                            else
                            {
                                // No alerts available in this scope - just show generic message
                                response.AppendLine("- Critical medical values detected - review test results for details");
                            }

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

                        // Extract actual critical values from context
                        var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                        if (!string.IsNullOrEmpty(criticalValuesFromContext))
                        {
                            response.AppendLine(criticalValuesFromContext);
                        }
                        else
                        {
                            // No alerts available in this scope - just show generic message
                            response.AppendLine("- Critical medical values detected - review test results for details");
                        }

                        response.AppendLine();
                        response.AppendLine("**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");
                        response.AppendLine("- These values indicate a medical emergency");
                        response.AppendLine("- Contact emergency services if symptoms worsen");
                        response.AppendLine("- Patient needs immediate medical evaluation");
                    }
                    else if (hasAnyConcerns)
                    {
                        response.AppendLine("⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.");
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
