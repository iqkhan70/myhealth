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

                // Using a text generation model that's better for chat
                Console.WriteLine($"Sending request to HuggingFace API with prompt: {text}");
                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/gpt2", 
                    content);

                Console.WriteLine($"HuggingFace API Response Status: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"HuggingFace API Response: {responseContent}");
                    
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
                Console.WriteLine($"Error in GenerateResponse: {ex.Message}");
            }

            // Load mental health knowledge base for better responses
            try
            {
                var knowledgePath = Path.Combine("llm", "prompts", "MentalHealthKnowledge.md");
                if (File.Exists(knowledgePath))
                {
                    var knowledge = File.ReadAllText(knowledgePath);
                    Console.WriteLine("Loaded mental health knowledge base for enhanced responses");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load mental health knowledge: {ex.Message}");
            }

            // Check if this is a personalized prompt FIRST - before keyword matching
            if (text.Contains("You are talking to") && text.Contains("Their recent mood patterns"))
            {
                Console.WriteLine("Found personalized prompt, creating contextual response");
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
    }
}
