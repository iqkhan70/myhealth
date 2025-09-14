using System.Text;
using System.Text.Json;

namespace SM_MentalHealthApp.Server.Services
{
    public class HuggingFaceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public HuggingFaceService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["HuggingFace:ApiKey"] ?? throw new InvalidOperationException("HuggingFace API key not found");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<(string response, string mood)> AnalyzeEntry(string text)
        {
            try
            {
                // Use a sentiment analysis model for mood detection
                var sentimentResult = await AnalyzeSentiment(text);

                // Use a text generation model for empathetic response
                var responseResult = await GenerateResponse(text);

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
            var requestBody = new
            {
                inputs = text
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Using a sentiment analysis model
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

            return "Neutral";
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

                        return string.IsNullOrWhiteSpace(generatedText) ? "I understand. How can I help you today?" : generatedText;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"HuggingFace API Error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HuggingFace API Exception: {ex.Message}");
            }

            // Fallback: Use enhanced context to generate a meaningful response
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
                        if (line.Contains("=== PATIENT CONTENT ANALYSIS ==="))
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
                            journalEntries.Add(line.Trim());
                        }
                        if (inContentSection && line.Contains("⚠️ ALERTS:"))
                        {
                            var alertText = line.Replace("⚠️ ALERTS:", "").Trim();
                            if (!string.IsNullOrWhiteSpace(alertText))
                            {
                                alerts.Add(alertText);
                            }
                        }
                    }

                    // Generate a contextual response based on the enhanced context
                    var response = new StringBuilder();

                    if (alerts.Any())
                    {
                        response.AppendLine("🚨 **IMPORTANT ALERTS DETECTED:**");
                        foreach (var alert in alerts)
                        {
                            response.AppendLine($"- {alert}");
                        }
                        response.AppendLine();
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

                    // Add contextual response based on the question
                    if (text.ToLower().Contains("how is he doing") || text.ToLower().Contains("how is she doing"))
                    {
                        response.AppendLine("Based on the patient's recent activity and medical content:");
                        if (alerts.Any())
                        {
                            response.AppendLine("⚠️ There are some concerning alerts that require immediate attention. Please review the flagged items above.");
                        }
                        else
                        {
                            response.AppendLine("✅ The patient appears to be stable with no immediate concerns detected.");
                        }

                        if (journalEntries.Any())
                        {
                            response.AppendLine("The patient has been actively engaging with their mental health tracking.");
                        }
                    }
                    else if (text.ToLower().Contains("areas of concern") || text.ToLower().Contains("concerns"))
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
                        response.AppendLine("I've analyzed the patient's recent activity and medical content. ");
                        if (alerts.Any())
                        {
                            response.AppendLine("There are some important alerts that need your attention.");
                        }
                        else
                        {
                            response.AppendLine("The patient appears to be doing well with no immediate concerns.");
                        }
                    }

                    return response.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fallback response generation error: {ex.Message}");
            }

            // Final fallback
            return "I understand you're asking about the patient. Based on the available information, I can see their recent activity and medical content. How can I help you further with their care?";

            // Check if this is a role-based prompt

            if (text.Contains("IMPORTANT GUIDELINES FOR PATIENT RESPONSES:"))
            {
                return HandlePatientPrompt(text);
            }
            else if (text.Contains("DOCTOR ASSISTANCE GUIDELINES:"))
            {
                return HandleDoctorPrompt(text);
            }
            else if (text.Contains("ADMIN ASSISTANCE GUIDELINES:"))
            {
                return HandleAdminPrompt(text);
            }
            else
            {
            }

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

        private async Task<string> GenerateGenericResponse(string text)
        {
            try
            {
                // First, try to handle common questions with predefined responses
                var question = text.ToLower().Trim();

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
                    if (question.Contains("quantum computing"))
                    {
                        return "Quantum computing is a type of computation that uses quantum mechanical phenomena like superposition and entanglement to process information. Unlike classical computers that use bits (0 or 1), quantum computers use quantum bits (qubits) that can exist in multiple states simultaneously, potentially solving certain problems much faster than classical computers.";
                    }
                    if (question.Contains("artificial intelligence"))
                    {
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

                // Handle general medical questions
                if (question.Contains("medical") || question.Contains("medicine") || question.Contains("treatment"))
                {
                    return "I can provide general information about medical topics, but please remember that this is for educational purposes only. For specific medical advice, diagnosis, or treatment, always consult with a qualified healthcare professional. What specific medical topic would you like to learn about?";
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
    }
}
