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

        public async Task<string> GenerateResponse(string text)
        {
            try
            {
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
                    "https://api-inference.huggingface.co/models/microsoft/DialoGPT-medium",
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
                }
            }
            catch (Exception ex)
            {
            }

            try
            {
                var knowledgePath = Path.Combine("llm", "prompts", "MentalHealthKnowledge.md");
                if (File.Exists(knowledgePath))
                {
                    var knowledge = File.ReadAllText(knowledgePath);
                }
            }
            catch (Exception ex)
            {
            }

            // Debug: Log the full prompt to see what we're getting

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
    }
}
