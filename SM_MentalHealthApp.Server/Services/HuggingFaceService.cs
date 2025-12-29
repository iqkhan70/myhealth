using System.Text;
using System.Text.Json;
using SM_MentalHealthApp.Shared;

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
        private readonly IGenericQuestionPatternService _genericQuestionPatternService;
        private readonly IMedicalThresholdService _thresholdService;
        private readonly EnhancedContextResponseService _enhancedContextResponseService;
        private readonly ISectionMarkerService _sectionMarkerService;
        private readonly LlmClient _llmClient;

        public HuggingFaceService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<HuggingFaceService> logger,
            ICriticalValuePatternService patternService,
            ICriticalValueKeywordService keywordService,
            IKnowledgeBaseService knowledgeBaseService,
            IAIResponseTemplateService templateService,
            IGenericQuestionPatternService genericQuestionPatternService,
            IMedicalThresholdService thresholdService,
            EnhancedContextResponseService enhancedContextResponseService,
            ISectionMarkerService sectionMarkerService,
            LlmClient llmClient)
        {
            _httpClient = httpClient;
            _apiKey = config["HuggingFace:ApiKey"] ?? throw new InvalidOperationException("HuggingFace API key not found");
            _logger = logger;
            _patternService = patternService;
            _keywordService = keywordService;
            _knowledgeBaseService = knowledgeBaseService;
            _templateService = templateService;
            _genericQuestionPatternService = genericQuestionPatternService;
            _thresholdService = thresholdService;
            _enhancedContextResponseService = enhancedContextResponseService;
            _sectionMarkerService = sectionMarkerService;
            _llmClient = llmClient;

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
                // Fallback response if API fails - use template
                var fallbackTemplate = await _templateService.FormatTemplateAsync("journal_fallback_neutral", null);
                var fallbackMessage = !string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : await _templateService.FormatTemplateAsync("fallback_journal_neutral", null) ?? "I understand you're sharing your thoughts with me. Thank you for trusting me with your feelings.";
                return (fallbackMessage, "Neutral");
            }
        }

        private async Task<string> AnalyzeSentiment(string text)
        {
            // First, try keyword-based analysis for health context
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
                var prompt = await BuildJournalPromptAsync(text, mood);

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
                        return await CleanJournalResponseAsync(generatedText);
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to fallback
            }

            // Fallback response based on mood
            return await GetJournalFallbackResponseAsync(mood);
        }

        private async Task<string> BuildJournalPromptAsync(string text, string mood)
        {
            var moodContextTemplateKey = mood switch
            {
                "Crisis" => "journal_prompt_crisis",
                "Distressed" => "journal_prompt_distressed",
                "Sad" => "journal_prompt_sad",
                "Anxious" => "journal_prompt_anxious",
                "Happy" => "journal_prompt_happy",
                _ => "journal_prompt_neutral"
            };

            var moodContext = await _templateService.FormatTemplateAsync(moodContextTemplateKey, null);
            if (string.IsNullOrEmpty(moodContext))
            {
                moodContext = await _templateService.FormatTemplateAsync("fallback_mood_context", null) ?? "The person is sharing their thoughts. Respond with empathy and understanding.";
            }

            var baseTemplate = await _templateService.FormatTemplateAsync("journal_prompt_base", new Dictionary<string, string>
            {
                { "JOURNAL_TEXT", text },
                { "MOOD_CONTEXT", moodContext }
            });

            if (!string.IsNullOrEmpty(baseTemplate)) return baseTemplate;

            var fallbackPrompt = await _templateService.FormatTemplateAsync("fallback_journal_prompt", new Dictionary<string, string>
            {
                { "JOURNAL_TEXT", text },
                { "MOOD_CONTEXT", moodContext }
            });
            return !string.IsNullOrEmpty(fallbackPrompt) ? fallbackPrompt : $"You are a compassionate health companion. A person has written in their journal: \"{text}\"\n\n{moodContext}\n\nRespond with a brief, empathetic message (2-3 sentences) that acknowledges their feelings and provides gentle support. Be warm and encouraging.";
        }

        private async Task<string> CleanJournalResponseAsync(string response)
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

            return string.IsNullOrWhiteSpace(response) ? await GetJournalFallbackResponseAsync("Neutral") : response;
        }

        private async Task<string> GetJournalFallbackResponseAsync(string mood)
        {
            var templateKey = mood switch
            {
                "Crisis" => "journal_fallback_crisis",
                "Distressed" => "journal_fallback_distressed",
                "Sad" => "journal_fallback_sad",
                "Anxious" => "journal_fallback_anxious",
                "Happy" => "journal_fallback_happy",
                _ => "journal_fallback_neutral"
            };

            var template = await _templateService.FormatTemplateAsync(templateKey, null);
            if (!string.IsNullOrEmpty(template)) return template;

            var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_journal_thanks", null);
            return !string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "Thank you for sharing your thoughts with me.";
        }

        public async Task<(string response, string mood)> AnalyzeMedicalJournalEntry(string text, MedicalJournalAnalysis medicalAnalysis)
        {
            try
            {
                // Create a medical-aware prompt
                var prompt = await BuildMedicalJournalPromptAsync(text, medicalAnalysis);

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
                        var cleanedResponse = await CleanMedicalJournalResponseAsync(generatedText);
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
            return (await GetMedicalJournalFallbackResponseAsync(medicalAnalysis), DetermineMedicalMood(medicalAnalysis));
        }

        private async Task<string> BuildMedicalJournalPromptAsync(string text, MedicalJournalAnalysis medicalAnalysis)
        {
            var medicalAnalysisParts = new List<string>();

            if (medicalAnalysis.HasCriticalValues)
            {
                var criticalValuesText = string.Join("\n", medicalAnalysis.CriticalValues.Select(v => $"  {v}"));
                var criticalTemplate = await _templateService.FormatTemplateAsync("medical_journal_prompt_critical", new Dictionary<string, string>
                {
                    { "CRITICAL_VALUES", criticalValuesText }
                });
                if (!string.IsNullOrEmpty(criticalTemplate))
                {
                    medicalAnalysisParts.Add(criticalTemplate);
                }
            }

            if (medicalAnalysis.HasAbnormalValues)
            {
                var abnormalValuesText = string.Join("\n", medicalAnalysis.AbnormalValues.Select(v => $"  {v}"));
                var abnormalTemplate = await _templateService.FormatTemplateAsync("medical_journal_prompt_abnormal", new Dictionary<string, string>
                {
                    { "ABNORMAL_VALUES", abnormalValuesText }
                });
                if (!string.IsNullOrEmpty(abnormalTemplate))
                {
                    medicalAnalysisParts.Add(abnormalTemplate);
                }
            }

            if (medicalAnalysis.NormalValues.Any())
            {
                var normalValuesText = string.Join("\n", medicalAnalysis.NormalValues.Select(v => $"  {v}"));
                var normalTemplate = await _templateService.FormatTemplateAsync("medical_journal_prompt_normal", new Dictionary<string, string>
                {
                    { "NORMAL_VALUES", normalValuesText }
                });
                if (!string.IsNullOrEmpty(normalTemplate))
                {
                    medicalAnalysisParts.Add(normalTemplate);
                }
            }

            var medicalAnalysisText = string.Join("\n\n", medicalAnalysisParts);
            var baseTemplate = await _templateService.FormatTemplateAsync("medical_journal_prompt_base", new Dictionary<string, string>
            {
                { "JOURNAL_TEXT", text },
                { "MEDICAL_ANALYSIS", medicalAnalysisText }
            });

            if (!string.IsNullOrEmpty(baseTemplate)) return baseTemplate;

            var fallbackPrompt = await _templateService.FormatTemplateAsync("fallback_medical_journal_prompt", new Dictionary<string, string>
            {
                { "JOURNAL_TEXT", text },
                { "MEDICAL_ANALYSIS", medicalAnalysisText }
            });
            return !string.IsNullOrEmpty(fallbackPrompt) ? fallbackPrompt : $"You are a medical AI assistant analyzing a journal entry that contains medical data.\n\nJournal Entry: \"{text}\"\n\n{medicalAnalysisText}\n\nProvide a medical assessment that acknowledges the medical data presented, provides appropriate medical context and interpretation, gives clear recommendations based on the values, maintains a professional caring tone, and emphasizes the importance of professional medical consultation when appropriate.";
        }

        private async Task<string> CleanMedicalJournalResponseAsync(string response)
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

            return string.IsNullOrWhiteSpace(response) ? await GetMedicalJournalFallbackResponseAsync(new MedicalJournalAnalysis()) : response;
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

        private async Task<string> GetMedicalJournalFallbackResponseAsync(MedicalJournalAnalysis medicalAnalysis)
        {
            if (medicalAnalysis.HasCriticalValues)
            {
                var criticalValuesList = string.Join("\n", medicalAnalysis.CriticalValues.Select(v => $"• {v}"));
                return await _templateService.FormatTemplateAsync("medical_journal_critical", new Dictionary<string, string>
                {
                    { "CRITICAL_VALUES", criticalValuesList }
                });
            }
            else if (medicalAnalysis.HasAbnormalValues)
            {
                var abnormalValuesList = string.Join("\n", medicalAnalysis.AbnormalValues.Select(v => $"• {v}"));
                return await _templateService.FormatTemplateAsync("medical_journal_abnormal", new Dictionary<string, string>
                {
                    { "ABNORMAL_VALUES", abnormalValuesList }
                });
            }
            else if (medicalAnalysis.HasMedicalContent)
            {
                var normalValuesList = medicalAnalysis.NormalValues.Any()
                    ? string.Join("\n", medicalAnalysis.NormalValues.Select(v => $"• {v}"))
                    : "";
                return await _templateService.FormatTemplateAsync("medical_journal_normal", new Dictionary<string, string>
                {
                    { "NORMAL_VALUES", normalValuesList }
                });
            }
            else
            {
                return await _templateService.FormatTemplateAsync("medical_journal_generic", null);
            }
        }

        private async Task<string> GenerateEmergencyResponseAsync(string text)
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

                // Build the response based on acknowledgment status using templates
                var response = new StringBuilder();

                if (unacknowledgedCount > 0)
                {
                    var unacknowledgedDetailsText = string.Join("\n", unacknowledgedDetails);
                    var template = await _templateService.FormatTemplateAsync("emergency_unacknowledged_alert", new Dictionary<string, string>
                    {
                        { "COUNT", unacknowledgedCount.ToString() },
                        { "UNACKNOWLEDGED_DETAILS", unacknowledgedDetailsText }
                    });
                    if (!string.IsNullOrEmpty(template))
                    {
                        response.AppendLine(template);
                    }
                }

                if (acknowledgedCount > 0)
                {
                    var acknowledgedDetailsText = string.Join("\n", acknowledgedDetails);
                    if (unacknowledgedCount > 0 && acknowledgedCount > 0)
                    {
                        var sectionTemplate = await _templateService.FormatTemplateAsync("section_previously_acknowledged", null);
                        if (!string.IsNullOrEmpty(sectionTemplate))
                        {
                            response.AppendLine(sectionTemplate);
                        }
                        else
                        {
                            var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_previously_acknowledged", null);
                            var finalTemplate = !string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : await _templateService.FormatTemplateAsync("section_previously_acknowledged_fallback", null) ?? "**Previously Acknowledged Emergencies:**";
                            response.AppendLine(finalTemplate);
                        }
                        response.AppendLine(acknowledgedDetailsText);
                        response.AppendLine();
                    }
                    else
                    {
                        var template = await _templateService.FormatTemplateAsync("emergency_acknowledged_history", new Dictionary<string, string>
                        {
                            { "COUNT", acknowledgedCount.ToString() },
                            { "ACKNOWLEDGED_DETAILS", acknowledgedDetailsText }
                        });
                        if (!string.IsNullOrEmpty(template))
                        {
                            response.AppendLine(template);
                        }
                    }
                }

                if (unacknowledgedCount == 0 && acknowledgedCount > 0)
                {
                    var template = await _templateService.FormatTemplateAsync("emergency_all_acknowledged", null);
                    if (!string.IsNullOrEmpty(template))
                    {
                        response.AppendLine(template);
                    }
                }

                // Extract and analyze medical data for critical values using section markers
                var medicalDataSection = await _sectionMarkerService.ExtractSectionAsync(text, "MEDICAL DATA SUMMARY");
                if (!string.IsNullOrEmpty(medicalDataSection))
                {
                    // Extract just the content between markers (remove the marker itself)
                    var medicalData = medicalDataSection;
                    var markerIndex = medicalData.IndexOf("=== MEDICAL DATA SUMMARY ===", StringComparison.OrdinalIgnoreCase);
                    if (markerIndex >= 0)
                    {
                        medicalData = medicalData.Substring(markerIndex + "=== MEDICAL DATA SUMMARY ===".Length).Trim();
                        var progressionIndex = medicalData.IndexOf("=== PROGRESSION ANALYSIS ===", StringComparison.OrdinalIgnoreCase);
                        if (progressionIndex >= 0)
                        {
                            medicalData = medicalData.Substring(0, progressionIndex).Trim();
                        }
                    }

                    if (!string.IsNullOrEmpty(medicalData))
                    {
                        var sectionHeader = await _templateService.FormatTemplateAsync("section_medical_data_analysis", null);
                        if (!string.IsNullOrEmpty(sectionHeader))
                        {
                            response.AppendLine(sectionHeader);
                        }
                        else
                        {
                            var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_medical_data_analysis", null);
                            var finalTemplate = !string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : await _templateService.FormatTemplateAsync("section_medical_data_analysis_fallback", null) ?? "**Medical Data Analysis:**";
                            response.AppendLine(finalTemplate);
                        }

                        // Check for critical medical values
                        var criticalAlerts = new List<string>();

                        // Check Blood Pressure using database-driven thresholds
                        var bpMatch = System.Text.RegularExpressions.Regex.Match(medicalData, @"Blood Pressure:\s*(\d+)/(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (bpMatch.Success)
                        {
                            var systolic = double.Parse(bpMatch.Groups[1].Value);
                            var diastolic = double.Parse(bpMatch.Groups[2].Value);

                            var threshold = await _thresholdService.GetMatchingThresholdAsync("Blood Pressure", systolic, diastolic);
                            if (threshold != null)
                            {
                                var templateKey = threshold.SeverityLevel?.ToLowerInvariant() switch
                                {
                                    "critical" => "alert_critical_blood_pressure",
                                    "high" => "alert_high_blood_pressure",
                                    _ => "alert_high_blood_pressure"
                                };

                                var alertTemplate = await _templateService.FormatTemplateAsync(templateKey, new Dictionary<string, string>
                                {
                                    { "SYSTOLIC", systolic.ToString("F0") },
                                    { "DIASTOLIC", diastolic.ToString("F0") }
                                });
                                var alertMessage = !string.IsNullOrEmpty(alertTemplate)
                                    ? alertTemplate
                                    : $"🚨 **CRITICAL BLOOD PRESSURE**: {systolic:F0}/{diastolic:F0} - HYPERTENSIVE CRISIS! Immediate medical intervention required!";
                                criticalAlerts.Add(alertMessage);
                            }
                        }

                        // Check Hemoglobin using database-driven thresholds
                        var hbMatch = System.Text.RegularExpressions.Regex.Match(medicalData, @"Hemoglobin:\s*(\d+\.?\d*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (hbMatch.Success)
                        {
                            var hemoglobin = double.Parse(hbMatch.Groups[1].Value);
                            var threshold = await _thresholdService.GetMatchingThresholdAsync("Hemoglobin", hemoglobin);
                            if (threshold != null)
                            {
                                var templateKey = threshold.SeverityLevel?.ToLowerInvariant() switch
                                {
                                    "critical" => "alert_critical_hemoglobin",
                                    "low" => "alert_low_hemoglobin",
                                    _ => "alert_low_hemoglobin"
                                };

                                var alertTemplate = await _templateService.FormatTemplateAsync(templateKey, new Dictionary<string, string>
                                {
                                    { "HEMOGLOBIN_VALUE", hemoglobin.ToString("F1") }
                                });
                                var alertMessage = !string.IsNullOrEmpty(alertTemplate)
                                    ? alertTemplate
                                    : $"🚨 **CRITICAL HEMOGLOBIN**: {hemoglobin:F1} g/dL - SEVERE ANEMIA! Blood transfusion may be required!";
                                criticalAlerts.Add(alertMessage);
                            }
                        }

                        // Check Triglycerides using database-driven thresholds
                        var trigMatch = System.Text.RegularExpressions.Regex.Match(medicalData, @"Triglycerides:\s*(\d+\.?\d*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (trigMatch.Success)
                        {
                            var triglycerides = double.Parse(trigMatch.Groups[1].Value);
                            var threshold = await _thresholdService.GetMatchingThresholdAsync("Triglycerides", triglycerides);
                            if (threshold != null)
                            {
                                var templateKey = threshold.SeverityLevel?.ToLowerInvariant() switch
                                {
                                    "critical" => "alert_critical_triglycerides",
                                    "high" => "alert_high_triglycerides",
                                    _ => "alert_high_triglycerides"
                                };

                                var alertTemplate = await _templateService.FormatTemplateAsync(templateKey, new Dictionary<string, string>
                                {
                                    { "TRIGLYCERIDES_VALUE", triglycerides.ToString("F1") }
                                });
                                var alertMessage = !string.IsNullOrEmpty(alertTemplate)
                                    ? alertTemplate
                                    : $"🚨 **CRITICAL TRIGLYCERIDES**: {triglycerides:F1} mg/dL - EXTREMELY HIGH! Risk of pancreatitis!";
                                criticalAlerts.Add(alertMessage);
                            }
                        }

                        // Add critical alerts if any found
                        if (criticalAlerts.Any())
                        {
                            var criticalAlertsText = string.Join("\n", criticalAlerts);
                            var template = await _templateService.FormatTemplateAsync("emergency_critical_medical", new Dictionary<string, string>
                            {
                                { "CRITICAL_ALERTS", criticalAlertsText }
                            });
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                        }

                        var medicalDataSectionHeader = await _templateService.FormatTemplateAsync("section_medical_data_analysis", null);
                        if (!string.IsNullOrEmpty(medicalDataSectionHeader))
                        {
                            response.AppendLine(medicalDataSectionHeader);
                        }
                        else
                        {
                            var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_medical_data_analysis", null);
                            var finalTemplate = !string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : await _templateService.FormatTemplateAsync("section_medical_data_analysis_fallback", null) ?? "**Medical Data Analysis:**";
                            response.AppendLine(finalTemplate);
                        }

                        var medicalDataTemplate = await _templateService.FormatTemplateAsync("emergency_medical_data", new Dictionary<string, string>
                        {
                            { "MEDICAL_DATA", medicalData }
                        });
                        if (!string.IsNullOrEmpty(medicalDataTemplate))
                        {
                            response.AppendLine(medicalDataTemplate);
                        }
                        else
                        {
                            response.AppendLine(medicalData);
                        }
                    }
                }

                return response.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating emergency response");
                var fallbackTemplate = await _templateService.FormatTemplateAsync("emergency_fallback", null);
                if (!string.IsNullOrEmpty(fallbackTemplate)) return fallbackTemplate;

                var finalFallback = await _templateService.FormatTemplateAsync("fallback_emergency_alert", null);
                return !string.IsNullOrEmpty(finalFallback) ? finalFallback : "🚨 **CRITICAL EMERGENCY ALERT:** Emergency incidents detected requiring immediate attention!";
            }
        }

        private async Task<string> GenerateHybridEmergencyResponse(string text)
        {
            try
            {
                // Generate the emergency part using templates
                var emergencyResponse = await GenerateEmergencyResponseAsync(text);

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
                return await GenerateEmergencyResponseAsync(text);
            }
        }

        public async Task<string> GenerateResponse(string text, bool isGenericMode = false)
        {
            try
            {
                // Log the first 500 characters of the text to see what we're working with
                _logger.LogInformation("GenerateResponse called with text preview: {TextPreview}",
                    text.Length > 500 ? text.Substring(0, 500) + "..." : text);

                // Check if this is an emergency case and use hybrid approach using section markers
                var hasEmergencyIncidents = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT EMERGENCY INCIDENTS");
                var hasEmergency = text.Contains("EMERGENCY", StringComparison.OrdinalIgnoreCase);
                var hasFall = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Fall");

                _logger.LogInformation("Checking for emergency incidents. HasEmergencyIncidents: {HasEmergencyIncidents}, HasEmergency: {HasEmergency}, HasFall: {HasFall}",
                    hasEmergencyIncidents, hasEmergency, hasFall);

                if ((hasEmergencyIncidents || hasEmergency) && hasFall)
                {
                    _logger.LogInformation("Emergency case detected, using hybrid approach");
                    return await GenerateHybridEmergencyResponse(text);
                }
                else
                {
                    _logger.LogInformation("No emergency case detected, using normal AI response");
                }

                // Check knowledge base first (for generic mode or any mode)
                // This allows data-driven responses instead of hardcoded ones
                // BUT: Skip knowledge base for AI Health Check - these need full analysis, not generic responses
                var isAiHealthCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "AI Health Check") ||
                                     await _sectionMarkerService.ContainsSectionMarkerAsync(text, "INSTRUCTIONS FOR AI HEALTH CHECK");

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
                        var fallbackText = await _templateService.FormatTemplateAsync("fallback_generic", null);
                        var defaultFallback = !string.IsNullOrEmpty(fallbackText) ? fallbackText : await _templateService.FormatTemplateAsync("fallback_generic_response", null) ?? "I understand. How can I help you today?";
                        var generatedText = responseData[0].GetProperty("generated_text").GetString() ?? defaultFallback;

                        // Clean up the response - remove the original input if it's included
                        if (generatedText.StartsWith(text))
                        {
                            generatedText = generatedText.Substring(text.Length).Trim();
                        }

                        var fallbackText2 = await _templateService.FormatTemplateAsync("fallback_generic", null);
                        var defaultFallback2 = !string.IsNullOrEmpty(fallbackText2) ? fallbackText2 : "I understand. How can I help you today?";
                        var finalResponse = string.IsNullOrWhiteSpace(generatedText) ? defaultFallback2 : generatedText;
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
            _logger.LogInformation("IsGenericMode: {IsGenericMode}", isGenericMode);

            // IMPORTANT: In generic mode, do NOT route to patient analysis
            // Generic questions should get factual answers, not patient status assessments
            if (isGenericMode)
            {
                _logger.LogInformation("Generic mode detected - using OpenAI for ChatGPT-like responses");

                // Extract the actual user question from the prompt text
                // The text might contain "User question: ..." or just be the question itself
                var userQuestion = ExtractUserQuestion(text);
                if (string.IsNullOrWhiteSpace(userQuestion))
                {
                    // Try to extract from "User question:" pattern (case-insensitive)
                    var userQuestionPattern = "user question:";
                    var index = text.IndexOf(userQuestionPattern, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        var startIndex = index + userQuestionPattern.Length;
                        // Get everything after "User question:" until the next instruction or end
                        var remainingText = text.Substring(startIndex).Trim();
                        // Take the first line or first sentence
                        var newlineIndex = remainingText.IndexOf('\n');
                        var periodIndex = remainingText.IndexOf('.');
                        var endIndex = newlineIndex >= 0 ? newlineIndex :
                                      (periodIndex >= 0 && periodIndex < 100 ? periodIndex : remainingText.Length);
                        userQuestion = remainingText.Substring(0, endIndex).Trim();
                    }

                    // If still empty, use the last meaningful line (might be just the question)
                    if (string.IsNullOrWhiteSpace(userQuestion))
                    {
                        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        // Look for the last line that looks like a question (contains ? or starts with question word)
                        for (int i = lines.Length - 1; i >= 0; i--)
                        {
                            var line = lines[i].Trim();
                            if (!string.IsNullOrWhiteSpace(line) &&
                                (line.Contains("?") ||
                                 line.StartsWith("what ", StringComparison.OrdinalIgnoreCase) ||
                                 line.StartsWith("who ", StringComparison.OrdinalIgnoreCase) ||
                                 line.StartsWith("where ", StringComparison.OrdinalIgnoreCase) ||
                                 line.StartsWith("when ", StringComparison.OrdinalIgnoreCase) ||
                                 line.StartsWith("why ", StringComparison.OrdinalIgnoreCase) ||
                                 line.StartsWith("how ", StringComparison.OrdinalIgnoreCase)))
                            {
                                userQuestion = line;
                                break;
                            }
                        }

                        // Final fallback: use the entire text if it's short enough (might be just the question)
                        if (string.IsNullOrWhiteSpace(userQuestion) && text.Length < 200)
                        {
                            userQuestion = text.Trim();
                        }
                    }
                }

                try
                {
                    // Use OpenAI via LlmClient for generic mode - this provides ChatGPT-like responses
                    var llmRequest = new LlmRequest
                    {
                        Model = "gpt-4o-mini",
                        Instructions = "You are a helpful AI assistant. Answer questions clearly and informatively, similar to ChatGPT. Provide factual information, explanations, and helpful responses to any question asked.",
                        Prompt = userQuestion,
                        Temperature = 0.7,
                        MaxTokens = 1000,
                        Provider = AiProvider.OpenAI
                    };

                    var llmResponse = await _llmClient.GenerateTextAsync(llmRequest);

                    if (!string.IsNullOrWhiteSpace(llmResponse?.Text))
                    {
                        _logger.LogInformation("OpenAI response generated successfully for generic question");
                        return llmResponse.Text.Trim();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling OpenAI for generic mode question: {Question}", userQuestion);
                }

                // Fallback if OpenAI fails - try to provide a helpful message
                _logger.LogWarning("OpenAI call failed, using fallback response");
                return $"I apologize, but I'm having trouble processing your question right now. Please try again in a moment. (Question: {userQuestion})";
            }

            // Check if this is a role-based prompt with enhanced context (only for non-generic mode)
            _logger.LogInformation("=== ENHANCED CONTEXT DETECTION ===");
            _logger.LogInformation("Text length: {TextLength}", text.Length);
            var hasMedicalDataLog = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "MEDICAL DATA SUMMARY");
            var hasJournalEntriesLog = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT JOURNAL ENTRIES");
            var hasUserQuestionLog = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "USER QUESTION");
            var hasDoctorAsksLog = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Doctor asks");

            _logger.LogInformation("Text contains '=== MEDICAL DATA SUMMARY ===': {HasMedicalData}", hasMedicalDataLog);
            _logger.LogInformation("Text contains '=== RECENT JOURNAL ENTRIES ===': {HasJournalEntries}", hasJournalEntriesLog);
            _logger.LogInformation("Text contains '=== USER QUESTION ===': {HasUserQuestion}", hasUserQuestionLog);
            _logger.LogInformation("Text contains 'Critical Values:': {HasCriticalValues}", text.Contains("Critical Values:"));
            _logger.LogInformation("Text contains 'Hemoglobin': {HasHemoglobinText}", text.Contains("Hemoglobin"));
            _logger.LogInformation("Text contains 'Doctor asks:': {HasDoctorAsks}", hasDoctorAsksLog);

            // Show first 200 characters of text for debugging
            _logger.LogInformation("First 200 chars of text: {TextPreview}", text.Substring(0, Math.Min(200, text.Length)));

            // Only route to patient analysis if we have actual patient context sections using section markers
            // Don't route based on just medical keywords - that would catch generic questions too
            var hasMedicalDataRoute = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "MEDICAL DATA SUMMARY");
            var hasJournalEntriesRoute = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT JOURNAL ENTRIES");
            var hasUserQuestionRoute = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "USER QUESTION");
            var hasClinicalNotesRoute = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CLINICAL NOTES");
            var hasDoctorAsksRoute = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Doctor asks");
            var hasMedicalResourceRoute = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Medical Resource Information");
            var hasMedicalFacilitiesRoute = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Medical Facilities Search");

            if (hasMedicalDataRoute || hasJournalEntriesRoute || hasUserQuestionRoute || hasClinicalNotesRoute || hasDoctorAsksRoute || hasMedicalResourceRoute || hasMedicalFacilitiesRoute)
            {
                _logger.LogInformation("Processing enhanced context for medical data");
                return await ProcessEnhancedContextResponseAsync(text);
            }

            // Additional fallback: if text contains patient-specific question patterns, treat it as a medical question
            // But only if it's clearly a patient status question, not a general knowledge question
            var hasCurrentStatus = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "CURRENT MEDICAL STATUS");
            if ((text.Contains("how is", StringComparison.OrdinalIgnoreCase) || text.Contains("status", StringComparison.OrdinalIgnoreCase) || text.Contains("suggestions", StringComparison.OrdinalIgnoreCase) || text.Contains("snapshot", StringComparison.OrdinalIgnoreCase) || text.Contains("results", StringComparison.OrdinalIgnoreCase) || text.Contains("stats", StringComparison.OrdinalIgnoreCase))
                && (text.Contains("patient", StringComparison.OrdinalIgnoreCase) || hasCurrentStatus))
            {
                _logger.LogInformation("Detected patient-specific medical question keywords, processing as medical question");
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

                    // Check for medical content in the context using section markers and medical parameter names
                    var hasCurrentStatusMarker = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "CURRENT MEDICAL STATUS");
                    var hasHistoricalConcerns = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "HISTORICAL MEDICAL CONCERNS");
                    var hasHealthTrend = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "HEALTH TREND ANALYSIS");
                    var hasMedicalDataSummary = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "MEDICAL DATA SUMMARY");
                    var hasRecentClinical = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CLINICAL NOTES");
                    var hasRecentChat = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CHAT HISTORY");
                    var hasRecentEmergency = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT EMERGENCY INCIDENTS");
                    var hasRecentJournal = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT JOURNAL ENTRIES");
                    var hasAiHealthCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "AI Health Check");

                    bool hasMedicalContent = text.Contains("Blood Pressure", StringComparison.OrdinalIgnoreCase) ||
                                           text.Contains("Hemoglobin", StringComparison.OrdinalIgnoreCase) ||
                                           text.Contains("Triglycerides", StringComparison.OrdinalIgnoreCase) ||
                                           text.Contains("CRITICAL VALUES", StringComparison.OrdinalIgnoreCase) ||
                                           text.Contains("ABNORMAL VALUES", StringComparison.OrdinalIgnoreCase) ||
                                           text.Contains("NORMAL VALUES", StringComparison.OrdinalIgnoreCase) ||
                                           text.Contains("CURRENT MEDICAL STATUS", StringComparison.OrdinalIgnoreCase) ||
                                           text.Contains("LATEST TEST RESULTS", StringComparison.OrdinalIgnoreCase) ||
                                           hasCurrentStatusMarker || hasHistoricalConcerns || hasHealthTrend ||
                                           hasMedicalDataSummary || hasRecentClinical || hasRecentChat ||
                                           hasRecentEmergency || hasRecentJournal || hasAiHealthCheck;

                    // Extract only patient data sections (exclude AI instructions)
                    var patientDataText = ExtractPatientDataSections(text);

                    // Check for specific critical values using database-driven pattern matching
                    // Don't use loose string checks like Contains("6.0") as it matches "13.0" or "16.0"
                    // Only check patient data, not AI instructions which contain example "CRITICAL" text
                    bool hasCriticalValues = await _patternService.MatchesAnyPatternAsync(patientDataText) ||
                                           await _keywordService.ContainsAnyKeywordAsync(patientDataText, "Critical");

                    // Also check if we have any of the section markers that indicate patient data is present using section markers
                    var hasRecentSectionsCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT");
                    var hasMedicalSectionsCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "MEDICAL");
                    var hasEmergencySectionsCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "EMERGENCY");
                    var hasSessionCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Session");
                    var hasSummaryCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Summary");
                    var hasClinicalNotesCheck2 = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Clinical Notes");
                    var hasJournalEntriesCheck2 = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Journal Entries");
                    var hasChatHistoryCheck2 = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "Chat History");

                    bool hasPatientSections = hasRecentSectionsCheck || hasMedicalSectionsCheck || hasEmergencySectionsCheck ||
                                            hasSessionCheck || hasSummaryCheck || hasClinicalNotesCheck2 || hasJournalEntriesCheck2 || hasChatHistoryCheck2;

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
                        // No client selected  or no data available - use template
                        var template = await _templateService.FormatTemplateAsync("fallback_no_patient_selected", null);
                        if (!string.IsNullOrEmpty(template)) return template;

                        var noPatientFallback = await _templateService.FormatTemplateAsync("no_patient_selected_fallback", null);
                        return !string.IsNullOrEmpty(noPatientFallback) ? noPatientFallback : "⚠️ **No Patient Selected**\n\nTo provide personalized insights about a specific patient, please select a patient from the dropdown above.";
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
                        var criticalValuesSection = ExtractCriticalValuesFromContext(text);
                        var criticalAlertText = criticalValuesSection ?? "- Critical medical values detected - review test results for details";

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
                            var fallbackHeader = await _templateService.FormatTemplateAsync("fallback_critical_alert_header", null);
                            response.AppendLine(!string.IsNullOrEmpty(fallbackHeader) ? fallbackHeader : "🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention. ");
                            response.AppendLine(criticalAlertText);
                            response.AppendLine();

                            var fallbackImmediate = await _templateService.FormatTemplateAsync("fallback_immediate_attention", null);
                            response.AppendLine(!string.IsNullOrEmpty(fallbackImmediate) ? fallbackImmediate : "**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");

                            var fallbackAction1 = await _templateService.FormatTemplateAsync("fallback_emergency_action1", null);
                            var fallbackAction2 = await _templateService.FormatTemplateAsync("fallback_emergency_action2", null);
                            var fallbackAction3 = await _templateService.FormatTemplateAsync("fallback_emergency_action3", null);
                            response.AppendLine(!string.IsNullOrEmpty(fallbackAction1) ? fallbackAction1 : "- These values indicate a medical emergency");
                            response.AppendLine(!string.IsNullOrEmpty(fallbackAction2) ? fallbackAction2 : "- Contact emergency services if symptoms worsen");
                            response.AppendLine(!string.IsNullOrEmpty(fallbackAction3) ? fallbackAction3 : "- Patient needs immediate medical evaluation");
                        }
                    }
                    else if (alerts.Any())
                    {
                        var alertsText = string.Join("\n", alerts.Select(a => $"- {a}"));
                        var alertsHeader = await _templateService.FormatTemplateAsync("status_medical_alerts_detected", null);
                        if (!string.IsNullOrEmpty(alertsHeader))
                        {
                            response.AppendLine(alertsHeader);
                        }
                        else
                        {
                            var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_medical_alerts_detected", null);
                            response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "🚨 **MEDICAL ALERTS DETECTED:**");
                        }
                        response.AppendLine(alertsText);
                        response.AppendLine();

                        if (hasAbnormalConditions)
                        {
                            var template = await _templateService.FormatTemplateAsync("status_medical_monitoring_needed", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_medical_monitoring_needed", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**MEDICAL MONITORING NEEDED:** Abnormal values detected that require medical attention.");
                            }
                        }
                        else if (hasNormalConditions)
                        {
                            var template = await _templateService.FormatTemplateAsync("status_continued_monitoring", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_continued_monitoring", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**CURRENT STATUS:** Patient shows normal values, but previous concerning results require continued monitoring.");
                            }
                        }
                    }
                    else if (hasMedicalContent)
                    {
                        if (!hasCriticalValues)
                        {
                            var template = await _templateService.FormatTemplateAsync("medical_content_warning", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var analysisTemplate = await _templateService.FormatTemplateAsync("medical_content_analysis", null);
                                if (!string.IsNullOrEmpty(analysisTemplate))
                                {
                                    response.AppendLine(analysisTemplate);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_medical_content_analysis", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "📊 **Medical Content Analysis:** I've reviewed the patient's medical content. ");
                                }

                                // Use medical_content_warning template which includes both messages
                                var warningTemplate = await _templateService.FormatTemplateAsync("medical_content_warning", null);
                                if (!string.IsNullOrEmpty(warningTemplate))
                                {
                                    response.AppendLine(warningTemplate);
                                }
                                else
                                {
                                    var fallbackWarning = await _templateService.FormatTemplateAsync("fallback_medical_content_important", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackWarning) ? fallbackWarning : "⚠️ **IMPORTANT:** While medical content was found, I was unable to detect specific critical values in the current analysis. \nPlease ensure all test results are properly formatted and accessible for accurate medical assessment.");
                                }
                            }
                        }
                        else
                        {
                            var template = await _templateService.FormatTemplateAsync("medical_content_critical_care", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_medical_content_critical_care", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "Please ensure all critical values are properly addressed with appropriate medical care.");
                            }
                        }
                    }

                    if (journalEntries.Any())
                    {
                        var journalSection = string.Join("\n", journalEntries.Take(3).Select(e => $"- {e}"));
                        var sectionHeader = await _templateService.FormatTemplateAsync("section_recent_activity", null);
                        var template = await _templateService.FormatTemplateAsync("recent_patient_activity", new Dictionary<string, string>
                        {
                            { "JOURNAL_ENTRIES", journalSection }
                        });
                        if (!string.IsNullOrEmpty(template))
                        {
                            response.AppendLine(template);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(sectionHeader))
                            {
                                response.AppendLine(sectionHeader);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_recent_activity_header", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "📝 **Recent Patient Activity:**");
                            }
                            response.AppendLine(journalSection);
                            response.AppendLine();
                        }
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
                            var overviewHeader2 = await _templateService.FormatTemplateAsync("section_patient_overview", null);
                            if (!string.IsNullOrEmpty(overviewHeader2))
                            {
                                response.AppendLine(overviewHeader2);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_patient_overview", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**Patient Medical Overview:**");
                            }

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
                                var criticalValuesText = criticalAlerts.Any()
                                    ? string.Join("\n", criticalAlerts.Select(c => $"- {c}"))
                                    : ExtractCriticalValuesFromContext(text);

                                var criticalAlertText = criticalValuesText ?? "- Critical medical values detected - review test results for details";

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
                                    var fallbackHeader = await _templateService.FormatTemplateAsync("fallback_critical_alert_header", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackHeader) ? fallbackHeader : "🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                    response.AppendLine(criticalAlertText);
                                    response.AppendLine();

                                    var fallbackImmediate = await _templateService.FormatTemplateAsync("fallback_immediate_attention", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackImmediate) ? fallbackImmediate : "**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");

                                    var fallbackAction1 = await _templateService.FormatTemplateAsync("fallback_emergency_action1", null);
                                    var fallbackAction2 = await _templateService.FormatTemplateAsync("fallback_emergency_action2", null);
                                    var fallbackAction3 = await _templateService.FormatTemplateAsync("fallback_emergency_action3", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackAction1) ? fallbackAction1 : "- These values indicate a medical emergency");
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackAction2) ? fallbackAction2 : "- Contact emergency services if symptoms worsen");
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackAction3) ? fallbackAction3 : "- Patient needs immediate medical evaluation");
                                }
                            }
                            else if (normalValues.Any() && !abnormalValues.Any() && !hasCriticalInContext)
                            {
                                var template = await _templateService.FormatTemplateAsync("stable_status", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_stable_status", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                                }
                            }
                            else if (abnormalValues.Any())
                            {
                                var abnormalText = string.Join("\n", abnormalValues.Take(3).Select(a => $"- {a}"));
                                var template = await _templateService.FormatTemplateAsync("concerns_detected", new Dictionary<string, string>
                                {
                                    { "ABNORMAL_VALUES", abnormalText }
                                });
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_abnormal_values", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "⚠️ **ABNORMAL VALUES DETECTED:** Some test results are outside normal ranges and require monitoring.");
                                    response.AppendLine(abnormalText);
                                }
                            }
                            else
                            {
                                var template = await _templateService.FormatTemplateAsync("stable_status", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_stable_status", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                                }
                            }

                            response.AppendLine();
                            var activityHeader = await _templateService.FormatTemplateAsync("section_recent_activity", null);
                            if (!string.IsNullOrEmpty(activityHeader))
                            {
                                response.AppendLine(activityHeader);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_recent_activity", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**Recent Patient Activity:**");
                            }

                            if (journalEntries.Any())
                            {
                                var journalSection = string.Join("\n", journalEntries.Take(3).Select(e => $"- {e}"));
                                var template = await _templateService.FormatTemplateAsync("recent_patient_activity", new Dictionary<string, string>
                                {
                                    { "JOURNAL_ENTRIES", journalSection }
                                });
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    response.AppendLine(journalSection);
                                }
                            }
                            else
                            {
                                var noEntriesTemplate = await _templateService.FormatTemplateAsync("status_no_journal_entries", null);
                                if (!string.IsNullOrEmpty(noEntriesTemplate))
                                {
                                    response.AppendLine(noEntriesTemplate);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_no_journal_entries", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "- No recent journal entries found.");
                                }
                            }

                            // Check for chat history using section markers
                            var hasChatHistorySection = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CHAT HISTORY");
                            if (hasChatHistorySection)
                            {
                                response.AppendLine();
                                var chatTemplate = await _templateService.FormatTemplateAsync("section_chat_history", null);
                                if (!string.IsNullOrEmpty(chatTemplate))
                                {
                                    response.AppendLine(chatTemplate);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_chat_history", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**Chat History:** Patient has been engaging in conversations with the AI assistant.");
                                }
                            }

                            // Check for clinical notes using section markers
                            var hasClinicalNotesSection = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CLINICAL NOTES");
                            if (hasClinicalNotesSection)
                            {
                                response.AppendLine();
                                var notesTemplate = await _templateService.FormatTemplateAsync("section_clinical_notes", null);
                                if (!string.IsNullOrEmpty(notesTemplate))
                                {
                                    response.AppendLine(notesTemplate);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_clinical_notes", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**Clinical Notes:** Recent clinical documentation is available for review.");
                                }
                            }

                            // Check for emergency incidents using section markers
                            var hasEmergencyIncidentsSection = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT EMERGENCY INCIDENTS");
                            if (hasEmergencyIncidentsSection)
                            {
                                response.AppendLine();
                                var emergencyTemplate = await _templateService.FormatTemplateAsync("section_emergency_incidents", null);
                                if (!string.IsNullOrEmpty(emergencyTemplate))
                                {
                                    response.AppendLine(emergencyTemplate);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_emergency_incidents", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "⚠️ **EMERGENCY INCIDENTS:** Emergency incidents have been recorded. Please review the emergency dashboard for details.");
                                }
                            }

                            response.AppendLine();
                            var assessmentHeader = await _templateService.FormatTemplateAsync("section_clinical_assessment", null);
                            if (!string.IsNullOrEmpty(assessmentHeader))
                            {
                                response.AppendLine(assessmentHeader);
                            }
                            else
                            {
                                var assessmentHeaderFallback = await _templateService.FormatTemplateAsync("section_clinical_assessment", null);
                                response.AppendLine(!string.IsNullOrEmpty(assessmentHeaderFallback) ? assessmentHeaderFallback : "**Clinical Assessment:**");
                            }

                            if (criticalAlerts.Any())
                            {
                                var template = await _templateService.FormatTemplateAsync("assessment_critical_intervention", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_critical_intervention", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "The patient requires immediate medical attention due to critical values. Urgent intervention is necessary.");
                                }
                            }
                            else if (abnormalValues.Any())
                            {
                                var template = await _templateService.FormatTemplateAsync("assessment_abnormal_monitoring", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_abnormal_monitoring", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "The patient shows some abnormal values that require monitoring and follow-up care. Schedule a medical review.");
                                }
                            }
                            else
                            {
                                var template = await _templateService.FormatTemplateAsync("assessment_stable_condition", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_stable_condition", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "The patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.");
                                }
                            }

                            response.AppendLine();
                            var recommendationsHeader = await _templateService.FormatTemplateAsync("section_recommendations", null);
                            if (!string.IsNullOrEmpty(recommendationsHeader))
                            {
                                response.AppendLine(recommendationsHeader);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_recommendations", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**Recommendations:**");
                            }

                            if (criticalAlerts.Any())
                            {
                                var template = await _templateService.FormatTemplateAsync("recommendations_critical", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var action1 = await _templateService.FormatTemplateAsync("action_immediate_evaluation", null);
                                    var action2 = await _templateService.FormatTemplateAsync("action_emergency_department", null);
                                    var action3 = await _templateService.FormatTemplateAsync("action_notify_doctors", null);
                                    response.AppendLine(!string.IsNullOrEmpty(action1) ? action1 : "- Immediate medical evaluation required");
                                    response.AppendLine(!string.IsNullOrEmpty(action2) ? action2 : "- Consider emergency department visit");
                                    response.AppendLine(!string.IsNullOrEmpty(action3) ? action3 : "- Notify assigned doctors immediately");
                                }
                            }
                            else if (abnormalValues.Any())
                            {
                                var template = await _templateService.FormatTemplateAsync("recommendations_abnormal", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var action1 = await _templateService.FormatTemplateAsync("action_followup_appointment", null);
                                    var action2 = await _templateService.FormatTemplateAsync("action_repeat_tests", null);
                                    var action3 = await _templateService.FormatTemplateAsync("action_monitor_patient", null);
                                    response.AppendLine(!string.IsNullOrEmpty(action1) ? action1 : "- Schedule follow-up appointment within 1-2 weeks");
                                    response.AppendLine(!string.IsNullOrEmpty(action2) ? action2 : "- Repeat laboratory tests as indicated");
                                    response.AppendLine(!string.IsNullOrEmpty(action3) ? action3 : "- Monitor patient closely for any changes");
                                }
                            }
                            else
                            {
                                var template = await _templateService.FormatTemplateAsync("recommendations_stable", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var action1 = await _templateService.FormatTemplateAsync("action_continue_care", null);
                                    var action2 = await _templateService.FormatTemplateAsync("action_maintain_schedule", null);
                                    var action3 = await _templateService.FormatTemplateAsync("action_encourage_tracking", null);
                                    response.AppendLine(!string.IsNullOrEmpty(action1) ? action1 : "- Continue current care plan");
                                    response.AppendLine(!string.IsNullOrEmpty(action2) ? action2 : "- Maintain routine follow-up schedule");
                                    response.AppendLine(!string.IsNullOrEmpty(action3) ? action3 : "- Encourage continued health tracking");
                                }
                            }
                        }
                        else if (questionLower.Contains("suggestions") || questionLower.Contains("approach") || questionLower.Contains("recommendations") || questionLower.Contains("what should"))
                        {
                            var recommendationsHeader = await _templateService.FormatTemplateAsync("section_clinical_recommendations", null);
                            if (!string.IsNullOrEmpty(recommendationsHeader))
                            {
                                response.AppendLine(recommendationsHeader);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_recommendations", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**Clinical Recommendations:**");
                            }

                            if (hasCriticalConditions)
                            {
                                var template = await _templateService.FormatTemplateAsync("recommendations_critical", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var detailedTemplate = await _templateService.FormatTemplateAsync("recommendations_critical_detailed", null);
                                    if (!string.IsNullOrEmpty(detailedTemplate))
                                    {
                                        response.AppendLine(detailedTemplate);
                                    }
                                    else
                                    {
                                        var fallbackDetailedTemplate = await _templateService.FormatTemplateAsync("recommendations_critical_detailed", null);
                                        response.AppendLine(!string.IsNullOrEmpty(fallbackDetailedTemplate) ? fallbackDetailedTemplate : "🚨 **IMMEDIATE ACTIONS REQUIRED:**\n1. **Emergency Medical Care**: Contact emergency services immediately\n2. **Hospital Admission**: Patient requires immediate hospitalization\n3. **Specialist Consultation**: Refer to appropriate specialist\n4. **Continuous Monitoring**: Vital signs every 15 minutes\n5. **Immediate Intervention**: Consider immediate medical intervention based on critical values");
                                    }
                                }
                            }
                            else if (hasAbnormalConditions)
                            {
                                var template = await _templateService.FormatTemplateAsync("recommendations_abnormal", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var detailedTemplate = await _templateService.FormatTemplateAsync("recommendations_abnormal_detailed", null);
                                    if (!string.IsNullOrEmpty(detailedTemplate))
                                    {
                                        response.AppendLine(detailedTemplate);
                                    }
                                    else
                                    {
                                        var fallbackDetailedTemplate = await _templateService.FormatTemplateAsync("recommendations_abnormal_detailed", null);
                                        response.AppendLine(!string.IsNullOrEmpty(fallbackDetailedTemplate) ? fallbackDetailedTemplate : "⚠️ **MEDICAL MANAGEMENT NEEDED:**\n1. **Primary Care Follow-up**: Schedule appointment within 24-48 hours\n2. **Laboratory Monitoring**: Repeat blood work in 1-2 weeks\n3. **Lifestyle Modifications**: Dietary changes and exercise recommendations\n4. **Medication Review**: Assess current medications and interactions");
                                    }
                                }
                            }
                            else
                            {
                                var template = await _templateService.FormatTemplateAsync("recommendations_stable", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var detailedTemplate = await _templateService.FormatTemplateAsync("recommendations_stable_detailed", null);
                                    if (!string.IsNullOrEmpty(detailedTemplate))
                                    {
                                        response.AppendLine(detailedTemplate);
                                    }
                                    else
                                    {
                                        var fallbackDetailedTemplate = await _templateService.FormatTemplateAsync("recommendations_stable_detailed", null);
                                        response.AppendLine(!string.IsNullOrEmpty(fallbackDetailedTemplate) ? fallbackDetailedTemplate : "✅ **CURRENT STATUS: STABLE**\n1. **Continue Current Care**: Maintain existing treatment plan\n2. **Regular Monitoring**: Schedule routine follow-up appointments\n3. **Preventive Care**: Focus on maintaining current health status");
                                    }
                                }
                            }
                        }
                        else if (questionLower.Contains("areas of concern") || questionLower.Contains("concerns"))
                        {
                            var concernHeader = await _templateService.FormatTemplateAsync("section_areas_of_concern", null);
                            if (!string.IsNullOrEmpty(concernHeader))
                            {
                                response.AppendLine(concernHeader);
                            }
                            else
                            {
                                var fallbackHeaderTemplate = await _templateService.FormatTemplateAsync("section_areas_of_concern", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackHeaderTemplate) ? fallbackHeaderTemplate : "**Areas of Concern Analysis:**");
                            }
                            if (alerts.Any())
                            {
                                var alertsText = string.Join("\n", alerts.Select(a => $"- {a}"));
                                var template = await _templateService.FormatTemplateAsync("concerns_detected", new Dictionary<string, string>
                                {
                                    { "CONCERNS_LIST", alertsText }
                                });
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("concerns_detected", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "🚨 **High Priority Concerns:**");
                                    response.AppendLine(alertsText);
                                }
                            }
                            else
                            {
                                var template = await _templateService.FormatTemplateAsync("stable_status", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("stable_status", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "✅ No immediate concerns detected in the current data.");
                                }
                            }
                        }
                        else
                        {
                            // Generic response for other questions
                            var assessmentHeader = await _templateService.FormatTemplateAsync("section_clinical_assessment", null);
                            if (!string.IsNullOrEmpty(assessmentHeader))
                            {
                                response.AppendLine(assessmentHeader);
                            }
                            else
                            {
                                var assessmentHeaderFallback = await _templateService.FormatTemplateAsync("section_clinical_assessment", null);
                                response.AppendLine(!string.IsNullOrEmpty(assessmentHeaderFallback) ? assessmentHeaderFallback : "**Clinical Assessment:**");
                            }

                            var inResponseTemplate = await _templateService.FormatTemplateAsync("assessment_in_response", new Dictionary<string, string>
                            {
                                { "USER_QUESTION", userQuestion }
                            });
                            if (!string.IsNullOrEmpty(inResponseTemplate))
                            {
                                response.AppendLine(inResponseTemplate);
                            }
                            else
                            {
                                response.AppendLine($"In response to your question: \"{userQuestion}\"");
                            }
                            response.AppendLine();

                            if (hasCriticalConditions)
                            {
                                var criticalValuesFromContext = ExtractCriticalValuesFromContext(text);
                                if (string.IsNullOrEmpty(criticalValuesFromContext))
                                {
                                    // Fallback: extract from alerts if available
                                    var localCriticalAlerts = alerts.Where(a => a.Contains("🚨 CRITICAL:") || a.Contains("CRITICAL VALUES:") || a.Contains("CRITICAL:")).ToList();
                                    if (localCriticalAlerts.Any())
                                    {
                                        criticalValuesFromContext = string.Join("\n", localCriticalAlerts.Select(c => $"- {c}"));
                                    }
                                }

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
                                    var fallbackHeader = await _templateService.FormatTemplateAsync("fallback_critical_alert_header", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackHeader) ? fallbackHeader : "🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                    response.AppendLine(criticalAlertText);
                                    response.AppendLine();

                                    var fallbackImmediate = await _templateService.FormatTemplateAsync("fallback_immediate_attention", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackImmediate) ? fallbackImmediate : "**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");

                                    var fallbackAction1 = await _templateService.FormatTemplateAsync("fallback_emergency_action1", null);
                                    var fallbackAction2 = await _templateService.FormatTemplateAsync("fallback_emergency_action2", null);
                                    var fallbackAction3 = await _templateService.FormatTemplateAsync("fallback_emergency_action3", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackAction1) ? fallbackAction1 : "- These values indicate a medical emergency");
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackAction2) ? fallbackAction2 : "- Contact emergency services if symptoms worsen");
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackAction3) ? fallbackAction3 : "- Patient needs immediate medical evaluation");
                                }
                            }
                            else if (hasAbnormalConditions)
                            {
                                var template = await _templateService.FormatTemplateAsync("concerns_detected", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_medical_monitoring_needed", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values that require attention and monitoring.");
                                }
                            }
                            else
                            {
                                var template = await _templateService.FormatTemplateAsync("stable_status", null);
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var stableStatus = await _templateService.FormatTemplateAsync("stable_status", null);
                                    response.AppendLine(!string.IsNullOrEmpty(stableStatus) ? stableStatus : "✅ The patient appears to be stable with no immediate concerns detected.");
                                }
                            }

                            if (journalEntries.Any())
                            {
                                var journalSection = string.Join("\n", journalEntries.Take(3).Select(e => $"- {e}"));
                                var template = await _templateService.FormatTemplateAsync("mood_statistics", new Dictionary<string, string>
                                {
                                    { "JOURNAL_ENTRIES", journalSection }
                                });
                                if (!string.IsNullOrEmpty(template))
                                {
                                    response.AppendLine(template);
                                }
                                else
                                {
                                    var fallbackTemplate = await _templateService.FormatTemplateAsync("status_patient_tracking", null);
                                    response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "The patient has been actively engaging with their health tracking.");
                                }
                            }
                        }

                        return response.ToString().Trim();
                    }
                    else
                    {
                        // No question extracted, but we have patient data - generate comprehensive health check
                        _logger.LogInformation("No question extracted, but patient data detected. Generating comprehensive health check analysis.");

                        // Generate comprehensive health check analysis
                        var overviewHeader = await _templateService.FormatTemplateAsync("section_patient_overview", null);
                        if (!string.IsNullOrEmpty(overviewHeader))
                        {
                            response.AppendLine(overviewHeader);
                        }
                        else
                        {
                            var overviewHeaderFallback = await _templateService.FormatTemplateAsync("section_patient_overview", null);
                            response.AppendLine(!string.IsNullOrEmpty(overviewHeaderFallback) ? overviewHeaderFallback : "**Patient Medical Overview:**");
                        }

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
                            var criticalValuesText = criticalAlerts.Any()
                                ? string.Join("\n", criticalAlerts.Select(c => $"- {c}"))
                                : ExtractCriticalValuesFromContext(text);

                            var criticalAlertText = criticalValuesText ?? "- Critical medical values detected - review test results for details";

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
                                var fallbackHeader = await _templateService.FormatTemplateAsync("fallback_critical_alert_header", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackHeader) ? fallbackHeader : "🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.");
                                response.AppendLine(criticalAlertText);
                                response.AppendLine();

                                var fallbackImmediate = await _templateService.FormatTemplateAsync("fallback_immediate_attention", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackImmediate) ? fallbackImmediate : "**IMMEDIATE MEDICAL ATTENTION REQUIRED:**");

                                var fallbackAction1 = await _templateService.FormatTemplateAsync("fallback_emergency_action1", null);
                                var fallbackAction2 = await _templateService.FormatTemplateAsync("fallback_emergency_action2", null);
                                var fallbackAction3 = await _templateService.FormatTemplateAsync("fallback_emergency_action3", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackAction1) ? fallbackAction1 : "- These values indicate a medical emergency");
                                response.AppendLine(!string.IsNullOrEmpty(fallbackAction2) ? fallbackAction2 : "- Contact emergency services if symptoms worsen");
                                response.AppendLine(!string.IsNullOrEmpty(fallbackAction3) ? fallbackAction3 : "- Patient needs immediate medical evaluation");
                            }
                        }
                        else if (normalValues.Any() && !abnormalValues.Any() && !hasCriticalInContext)
                        {
                            var template = await _templateService.FormatTemplateAsync("stable_status", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var stableStatusFull = await _templateService.FormatTemplateAsync("stable_status", null);
                                response.AppendLine(!string.IsNullOrEmpty(stableStatusFull) ? stableStatusFull : "✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                            }
                        }
                        else if (abnormalValues.Any())
                        {
                            var abnormalText = string.Join("\n", abnormalValues.Take(3).Select(a => $"- {a}"));
                            var template = await _templateService.FormatTemplateAsync("concerns_detected", new Dictionary<string, string>
                            {
                                { "ABNORMAL_VALUES", abnormalText }
                            });
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var abnormalDetected = await _templateService.FormatTemplateAsync("concerns_detected", null);
                                response.AppendLine(!string.IsNullOrEmpty(abnormalDetected) ? abnormalDetected : "⚠️ **ABNORMAL VALUES DETECTED:** Some test results are outside normal ranges and require monitoring.");
                                response.AppendLine(abnormalText);
                            }
                        }
                        else
                        {
                            var template = await _templateService.FormatTemplateAsync("stable_status", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var stableStatusFull = await _templateService.FormatTemplateAsync("stable_status", null);
                                response.AppendLine(!string.IsNullOrEmpty(stableStatusFull) ? stableStatusFull : "✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.");
                            }
                        }

                        response.AppendLine();
                        var activityHeader = await _templateService.FormatTemplateAsync("section_recent_activity", null);
                        if (!string.IsNullOrEmpty(activityHeader))
                        {
                            response.AppendLine(activityHeader);
                        }
                        else
                        {
                            var activityHeaderFallback = await _templateService.FormatTemplateAsync("section_recent_activity", null);
                            response.AppendLine(!string.IsNullOrEmpty(activityHeaderFallback) ? activityHeaderFallback : "**Recent Patient Activity:**");
                        }

                        if (journalEntries.Any())
                        {
                            var journalSection = string.Join("\n", journalEntries.Take(3).Select(e => $"- {e}"));
                            var template = await _templateService.FormatTemplateAsync("recent_patient_activity", new Dictionary<string, string>
                            {
                                { "JOURNAL_ENTRIES", journalSection }
                            });
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                response.AppendLine(journalSection);
                            }
                        }
                        else
                        {
                            var noEntriesTemplate = await _templateService.FormatTemplateAsync("status_no_journal_entries", null);
                            if (!string.IsNullOrEmpty(noEntriesTemplate))
                            {
                                response.AppendLine(noEntriesTemplate);
                            }
                            else
                            {
                                response.AppendLine("- No recent journal entries found.");
                            }
                        }

                        // Check for chat history using section markers
                        var hasChatHistoryCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CHAT HISTORY");
                        if (hasChatHistoryCheck)
                        {
                            response.AppendLine();
                            var chatTemplate = await _templateService.FormatTemplateAsync("section_chat_history", null);
                            if (!string.IsNullOrEmpty(chatTemplate))
                            {
                                response.AppendLine(chatTemplate);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_chat_history", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**Chat History:** Patient has been engaging in conversations with the AI assistant.");
                            }
                        }

                        // Check for clinical notes using section markers
                        var hasClinicalNotesCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CLINICAL NOTES");
                        if (hasClinicalNotesCheck)
                        {
                            response.AppendLine();
                            var notesTemplate = await _templateService.FormatTemplateAsync("section_clinical_notes", null);
                            if (!string.IsNullOrEmpty(notesTemplate))
                            {
                                response.AppendLine(notesTemplate);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_clinical_notes", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "**Clinical Notes:** Recent clinical documentation is available for review.");
                            }
                        }

                        // Check for emergency incidents using section markers
                        var hasEmergencyIncidentsCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT EMERGENCY INCIDENTS");
                        if (hasEmergencyIncidentsCheck)
                        {
                            response.AppendLine();
                            var emergencyTemplate = await _templateService.FormatTemplateAsync("section_emergency_incidents", null);
                            if (!string.IsNullOrEmpty(emergencyTemplate))
                            {
                                response.AppendLine(emergencyTemplate);
                            }
                            else
                            {
                                var fallbackTemplate = await _templateService.FormatTemplateAsync("fallback_emergency_incidents", null);
                                response.AppendLine(!string.IsNullOrEmpty(fallbackTemplate) ? fallbackTemplate : "⚠️ **EMERGENCY INCIDENTS:** Emergency incidents have been recorded. Please review the emergency dashboard for details.");
                            }
                        }

                        response.AppendLine();
                        var assessmentHeader2 = await _templateService.FormatTemplateAsync("section_clinical_assessment", null);
                        if (!string.IsNullOrEmpty(assessmentHeader2))
                        {
                            response.AppendLine(assessmentHeader2);
                        }
                        else
                        {
                            var assessmentHeader = await _templateService.FormatTemplateAsync("section_clinical_assessment", null);
                            response.AppendLine(!string.IsNullOrEmpty(assessmentHeader) ? assessmentHeader : "**Clinical Assessment:**");
                        }

                        if (criticalAlerts.Any())
                        {
                            var template = await _templateService.FormatTemplateAsync("assessment_critical_intervention", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var criticalIntervention = await _templateService.FormatTemplateAsync("assessment_critical_intervention", null);
                                response.AppendLine(!string.IsNullOrEmpty(criticalIntervention) ? criticalIntervention : "The patient requires immediate medical attention due to critical values. Urgent intervention is necessary.");
                            }
                        }
                        else if (abnormalValues.Any())
                        {
                            var template = await _templateService.FormatTemplateAsync("assessment_abnormal_monitoring", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var abnormalMonitoring = await _templateService.FormatTemplateAsync("assessment_abnormal_monitoring", null);
                                response.AppendLine(!string.IsNullOrEmpty(abnormalMonitoring) ? abnormalMonitoring : "The patient shows some abnormal values that require monitoring and follow-up care. Schedule a medical review.");
                            }
                        }
                        else
                        {
                            var template = await _templateService.FormatTemplateAsync("assessment_stable_condition", null);
                            if (!string.IsNullOrEmpty(template))
                            {
                                response.AppendLine(template);
                            }
                            else
                            {
                                var stableCondition = await _templateService.FormatTemplateAsync("assessment_stable_condition", null);
                                response.AppendLine(!string.IsNullOrEmpty(stableCondition) ? stableCondition : "The patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.");
                            }
                        }

                        response.AppendLine();
                        var recommendationsHeaderFinal = await _templateService.FormatTemplateAsync("section_recommendations", null);
                        if (!string.IsNullOrEmpty(recommendationsHeaderFinal))
                        {
                            response.AppendLine(recommendationsHeaderFinal);
                        }
                        else
                        {
                            var recommendationsHeader = await _templateService.FormatTemplateAsync("section_recommendations", null);
                            response.AppendLine(!string.IsNullOrEmpty(recommendationsHeader) ? recommendationsHeader : "**Recommendations:**");
                        }

                        if (criticalAlerts.Any())
                        {
                            var action1 = await _templateService.FormatTemplateAsync("action_immediate_evaluation", null);
                            var action2 = await _templateService.FormatTemplateAsync("action_emergency_department", null);
                            var action3 = await _templateService.FormatTemplateAsync("action_notify_doctors", null);
                            response.AppendLine(!string.IsNullOrEmpty(action1) ? action1 : "- Immediate medical evaluation required");
                            response.AppendLine(!string.IsNullOrEmpty(action2) ? action2 : "- Consider emergency department visit");
                            response.AppendLine(!string.IsNullOrEmpty(action3) ? action3 : "- Notify assigned doctors immediately");
                        }
                        else if (abnormalValues.Any())
                        {
                            var action1 = await _templateService.FormatTemplateAsync("action_followup_appointment", null);
                            var action2 = await _templateService.FormatTemplateAsync("action_repeat_tests", null);
                            var action3 = await _templateService.FormatTemplateAsync("action_monitor_patient", null);
                            response.AppendLine(!string.IsNullOrEmpty(action1) ? action1 : "- Schedule follow-up appointment within 1-2 weeks");
                            response.AppendLine(!string.IsNullOrEmpty(action2) ? action2 : "- Repeat laboratory tests as indicated");
                            response.AppendLine(!string.IsNullOrEmpty(action3) ? action3 : "- Monitor patient closely for any changes");
                        }
                        else
                        {
                            var action1 = await _templateService.FormatTemplateAsync("action_continue_care", null);
                            var action2 = await _templateService.FormatTemplateAsync("action_maintain_schedule", null);
                            var action3 = await _templateService.FormatTemplateAsync("action_encourage_tracking", null);
                            response.AppendLine(!string.IsNullOrEmpty(action1) ? action1 : "- Continue current care plan");
                            response.AppendLine(!string.IsNullOrEmpty(action2) ? action2 : "- Maintain routine follow-up schedule");
                            response.AppendLine(!string.IsNullOrEmpty(action3) ? action3 : "- Encourage continued health tracking");
                        }

                        return response.ToString().Trim();
                    }
                }
                else
                {
                    // If knowledge file doesn't exist, check if we have patient data and generate analysis using section markers
                    var hasMedicalDataSummaryFinal = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "MEDICAL DATA SUMMARY");
                    var hasRecentJournalFinal = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT JOURNAL ENTRIES");
                    var hasRecentChatFinal = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CHAT HISTORY");
                    var hasRecentClinicalFinal = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT CLINICAL NOTES");
                    var hasRecentEmergencyFinal = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT EMERGENCY INCIDENTS");
                    var hasAiHealthCheckFinal = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "AI Health Check");

                    bool hasPatientDataInContext = hasMedicalDataSummaryFinal || hasRecentJournalFinal || hasRecentChatFinal ||
                                                   hasRecentClinicalFinal || hasRecentEmergencyFinal || hasAiHealthCheckFinal;

                    if (hasPatientDataInContext)
                    {
                        _logger.LogInformation("Knowledge file doesn't exist, but patient data detected. Generating health check analysis.");
                        return await ProcessEnhancedContextResponseAsync(text);
                    }

                    // If knowledge file doesn't exist and no patient data, return a basic response
                    var errorFallback = await _templateService.FormatTemplateAsync("fallback_patient_query", null);
                    return !string.IsNullOrEmpty(errorFallback) ? errorFallback : "I understand you're asking about the patient. Based on the available information, I can see their recent activity and medical content. How can I help you further with their care?";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fallback response generation");
            }

            // Final fallback
            var finalFallback = await _templateService.FormatTemplateAsync("fallback_patient_query", null);
            return !string.IsNullOrEmpty(finalFallback) ? finalFallback : "I understand you're asking about the patient. Based on the available information, I can see their recent activity and medical content. How can I help you further with their care?";

            // Legacy personalized prompt handling
            if (text.Contains("You are talking to") && text.Contains("Their recent mood patterns"))
            {
                var lines = text.Split('\n');
                var patientName = lines.FirstOrDefault(l => l.StartsWith("You are talking to"))?.Replace("You are talking to ", "").Replace(".", "");
                var moodPatterns = lines.FirstOrDefault(l => l.StartsWith("Their recent mood patterns"))?.Replace("Their recent mood patterns: ", "");
                var latestEntry = lines.FirstOrDefault(l => l.StartsWith("Their latest journal entry"));

                // Legacy prompt handling - use templates
                var legacyTemplate = await _templateService.FormatTemplateAsync("legacy_personalized_greeting", new Dictionary<string, string>
                {
                    { "PATIENT_NAME", patientName ?? "" },
                    { "MOOD_PATTERNS", moodPatterns?.ToLower() ?? "" },
                    { "HAS_LATEST_ENTRY", latestEntry != null ? "true" : "false" }
                });

                if (!string.IsNullOrEmpty(legacyTemplate)) return legacyTemplate;

                // Fallback to hardcoded if template not found
                var response = $"Hello {patientName}! I can see from your recent patterns that you've been experiencing {moodPatterns?.ToLower()}. ";

                if (latestEntry != null)
                {
                    response += $"I noticed in your latest journal entry that you mentioned feeling a bit anxious but hopeful about the week ahead. That's a great mindset to have - acknowledging your feelings while staying optimistic. ";
                }

                response += "How are you feeling right now? Is there anything specific you'd like to talk about or work through together?";

                return response;
            }

            // Enhanced fallback responses based on health context - using templates
            var lowerText = text.ToLower();

            if (lowerText.Contains("health") || lowerText.Contains("wellness"))
            {
                var template = await _templateService.FormatTemplateAsync("fallback_mental_health", null);
                if (!string.IsNullOrEmpty(template)) return template;
            }

            if (lowerText.Contains("mood") || lowerText.Contains("feeling"))
            {
                var template = await _templateService.FormatTemplateAsync("fallback_mood_feeling", null);
                if (!string.IsNullOrEmpty(template)) return template;
            }

            if (lowerText.Contains("anxiety") || lowerText.Contains("worried") || lowerText.Contains("nervous"))
            {
                var template = await _templateService.FormatTemplateAsync("fallback_anxiety", null);
                if (!string.IsNullOrEmpty(template)) return template;
            }

            if (lowerText.Contains("sad") || lowerText.Contains("depressed") || lowerText.Contains("down"))
            {
                var template = await _templateService.FormatTemplateAsync("fallback_sad_depressed", null);
                if (!string.IsNullOrEmpty(template)) return template;
            }

            if (lowerText.Contains("help") || lowerText.Contains("support"))
            {
                var template = await _templateService.FormatTemplateAsync("fallback_help_support", null);
                if (!string.IsNullOrEmpty(template)) return template;
            }

            var defaultTemplate = await _templateService.FormatTemplateAsync("fallback_default", null);
            if (!string.IsNullOrEmpty(defaultTemplate)) return defaultTemplate;

            var finalDefault = await _templateService.FormatTemplateAsync("fallback_default_response", null);
            return !string.IsNullOrEmpty(finalDefault) ? finalDefault : "I'm here as your health companion to listen and support you. How are you feeling today?";
        }

        private async Task<string> HandlePatientPromptAsync(string text)
        {
            var lines = text.Split('\n');
            var patientName = lines.FirstOrDefault(l => l.Contains("You are a health companion talking to"))?.Split(' ').LastOrDefault()?.Replace(".", "");
            var userQuestion = lines.FirstOrDefault(l => l.StartsWith("Patient asks:"))?.Replace("Patient asks: ", "");

            if (string.IsNullOrEmpty(userQuestion))
            {
                var template = await _templateService.FormatTemplateAsync("fallback_generic", null);
                return !string.IsNullOrEmpty(template) ? template : "I'm here to support you. How can I help you today?";
            }

            var question = userQuestion.ToLower();

            // Handle specific patient questions with appropriate responses using templates
            if (question.Contains("general wellness") || question.Contains("wellness advice") || question.Contains("general guidelines"))
            {
                return await _templateService.FormatTemplateAsync("patient_wellness_guidelines", null);
            }
            else if (question.Contains("medication") || question.Contains("prescription") || question.Contains("treatments"))
            {
                return await _templateService.FormatTemplateAsync("patient_medication_disclaimer", null);
            }
            else if (question.Contains("anxiety") || question.Contains("worried") || question.Contains("nervous"))
            {
                return await _templateService.FormatTemplateAsync("patient_anxiety_response", null);
            }
            else if (question.Contains("depressed") || question.Contains("sad") || question.Contains("down"))
            {
                return await _templateService.FormatTemplateAsync("patient_depression_response", null);
            }
            else
            {
                return await _templateService.FormatTemplateAsync("patient_generic_response", null);
            }
        }

        private async Task<string> HandleDoctorPromptAsync(string text)
        {
            var lines = text.Split('\n');
            var doctorName = lines.FirstOrDefault(l => l.Contains("helping Dr."))?.Split(' ').SkipWhile(s => s != "Dr.").Skip(1).Take(2).Aggregate((a, b) => $"{a} {b}")?.Replace(".", "");
            var userQuestion = lines.FirstOrDefault(l => l.StartsWith("Doctor asks:"))?.Replace("Doctor asks: ", "");

            if (string.IsNullOrEmpty(userQuestion))
            {
                var template = await _templateService.FormatTemplateAsync("fallback_generic", null);
                return !string.IsNullOrEmpty(template) ? template : "I'm here to assist you with patient care. What would you like to know?";
            }

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
                return await _templateService.FormatTemplateAsync("doctor_patient_not_found", null);
            }

            // Handle specific doctor questions using templates
            if (hasNoData)
            {
                return await _templateService.FormatTemplateAsync("doctor_no_data", null);
            }

            if (question.Contains("anxiety") || question.Contains("anxious"))
            {
                return await _templateService.FormatTemplateAsync("doctor_anxiety_recommendations", new Dictionary<string, string>
                {
                    { "MOOD_PATTERNS", patientInfo.MoodPatterns },
                    { "RECENT_PATTERNS", patientInfo.RecentPatterns }
                });
            }
            else if (question.Contains("depression") || question.Contains("depressed"))
            {
                return await _templateService.FormatTemplateAsync("doctor_depression_recommendations", new Dictionary<string, string>
                {
                    { "MOOD_PATTERNS", patientInfo.MoodPatterns },
                    { "RECENT_PATTERNS", patientInfo.RecentPatterns }
                });
            }
            else if (question.Contains("medication"))
            {
                return await _templateService.FormatTemplateAsync("doctor_medication_considerations", new Dictionary<string, string>
                {
                    { "MOOD_PATTERNS", patientInfo.MoodPatterns },
                    { "RECENT_PATTERNS", patientInfo.RecentPatterns }
                });
            }
            else
            {
                return await _templateService.FormatTemplateAsync("doctor_generic_response", new Dictionary<string, string>
                {
                    { "MOOD_PATTERNS", patientInfo.MoodPatterns },
                    { "RECENT_PATTERNS", patientInfo.RecentPatterns }
                });
            }
        }

        private async Task<string> HandleAdminPromptAsync(string text)
        {
            var lines = text.Split('\n');
            var userQuestion = lines.FirstOrDefault(l => l.StartsWith("Admin asks:"))?.Replace("Admin asks: ", "");

            if (string.IsNullOrEmpty(userQuestion))
            {
                var template = await _templateService.FormatTemplateAsync("fallback_generic", null);
                return !string.IsNullOrEmpty(template) ? template : "I'm here to assist with administrative tasks and system management. How can I help?";
            }

            var question = userQuestion.ToLower();

            if (question.Contains("trend") || question.Contains("pattern"))
            {
                return await _templateService.FormatTemplateAsync("admin_trend_analysis", null);
            }
            else if (question.Contains("improve") || question.Contains("enhance"))
            {
                return await _templateService.FormatTemplateAsync("admin_system_improvements", null);
            }
            else
            {
                return await _templateService.FormatTemplateAsync("admin_generic_response", null);
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
                var recentLines = lines.SkipWhile(l => !l.Contains("RECENT JOURNAL ENTRIES")).Skip(1).TakeWhile(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("SME"));
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
            // Use the new refactored service instead of the massive if-else nightmare
            // This replaces 700+ lines of nested conditionals with a clean handler pattern
            return await _enhancedContextResponseService.ProcessAsync(text);
        }

        /// <summary>
        /// Determines if a message is a generic knowledge question (not a patient-specific concern)
        /// Generic questions like "what are normal values of glucose?" should be excluded from AI Health Check analysis
        /// </summary>
        private async Task<bool> IsGenericKnowledgeQuestionAsync(string messageContent)
        {
            return await _genericQuestionPatternService.IsGenericKnowledgeQuestionAsync(messageContent);
        }
    }
}
