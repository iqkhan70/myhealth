using SM_MentalHealthApp.Shared;
using System.Text.RegularExpressions;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IServiceRequestAgenticAIService
    {
        Task<AgenticResponse> ProcessServiceRequestAsync(int clientId, string clientMessage, int? serviceRequestId = null);
    }

    public class ServiceRequestAgenticAIService : IServiceRequestAgenticAIService
    {
        private readonly IClientProfileService _profileService;
        private readonly HuggingFaceService _huggingFaceService;
        private readonly LlmClient _llmClient;
        private readonly ILogger<ServiceRequestAgenticAIService> _logger;
        private readonly IServiceRequestService _serviceRequestService;

        public ServiceRequestAgenticAIService(
            IClientProfileService profileService,
            HuggingFaceService huggingFaceService,
            LlmClient llmClient,
            ILogger<ServiceRequestAgenticAIService> logger,
            IServiceRequestService serviceRequestService)
        {
            _profileService = profileService;
            _huggingFaceService = huggingFaceService;
            _llmClient = llmClient;
            _logger = logger;
            _serviceRequestService = serviceRequestService;
        }

        public async Task<AgenticResponse> ProcessServiceRequestAsync(int clientId, string clientMessage, int? serviceRequestId = null)
        {
            try
            {
                _logger.LogInformation("Processing service request for client {ClientId}, SR: {ServiceRequestId}", clientId, serviceRequestId);

                // Step 1: Load or create client profile
                var profile = await _profileService.GetOrCreateProfileAsync(clientId);

                // Step 2: Analyze client message
                var analysis = await AnalyzeClientMessageAsync(clientMessage, profile);

                // Step 3: Determine response strategy
                var strategy = await DetermineResponseStrategyAsync(analysis, profile, serviceRequestId);

                // Step 4: Generate adaptive response
                var response = await GenerateAdaptiveResponseAsync(clientMessage, analysis, strategy, profile, serviceRequestId);

                // Step 5: Learn from interaction (async, don't block)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await LearnFromInteractionAsync(clientId, clientMessage, response, analysis, serviceRequestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in background learning for client {ClientId}", clientId);
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing service request for client {ClientId}", clientId);
                
                // Fallback response
                return new AgenticResponse
                {
                    Message = "I understand you need help. Let me assist you with your service request. Could you provide a bit more detail about what you're experiencing?",
                    Confidence = 0.3m,
                    SuggestedActions = new List<string> { "Provide more details", "Contact support" }
                };
            }
        }

        private async Task<MessageAnalysis> AnalyzeClientMessageAsync(string message, ClientProfile profile)
        {
            var analysis = new MessageAnalysis();

            try
            {
                // Analyze sentiment using HuggingFace
                var (sentimentText, mood) = await _huggingFaceService.AnalyzeEntry(message);
                
                // Map mood to sentiment enum
                analysis.Sentiment = mood switch
                {
                    "Happy" => Sentiment.Positive,
                    "Sad" or "Distressed" => Sentiment.Negative,
                    "Crisis" => Sentiment.Panic,
                    _ => Sentiment.Neutral
                };

                // Detect urgency
                analysis.Urgency = DetectUrgency(message);

                // Detect information need based on message length and complexity
                analysis.InformationNeed = DetectInformationNeed(message, profile);

                // Detect emotional state
                analysis.EmotionalState = DetectEmotionalState(message, profile);

                // Extract concerns
                analysis.Concerns = ExtractConcerns(message);

                // Analyze keyword reactions
                analysis.KeywordScores = await AnalyzeKeywordsAsync(message, profile);

                // Determine client reaction
                analysis.ClientReaction = DetermineReaction(message, analysis, profile);

                _logger.LogInformation("Message analysis: Sentiment={Sentiment}, Urgency={Urgency}, InfoNeed={InfoNeed}, EmotionalState={EmotionalState}",
                    analysis.Sentiment, analysis.Urgency, analysis.InformationNeed, analysis.EmotionalState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing client message");
                // Default values
                analysis.Sentiment = Sentiment.Neutral;
                analysis.Urgency = Urgency.Medium;
                analysis.InformationNeed = InformationLevel.Moderate;
            }

            return analysis;
        }

        private Urgency DetectUrgency(string message)
        {
            var lowerMessage = message.ToLowerInvariant();
            
            // Critical urgency indicators
            if (Regex.IsMatch(lowerMessage, @"\b(emergency|urgent|critical|immediately|right now|asap|disaster|flooding|fire|dangerous)\b", RegexOptions.IgnoreCase))
                return Urgency.Critical;
            
            // High urgency indicators
            if (Regex.IsMatch(lowerMessage, @"\b(important|soon|quickly|broken|not working|stopped|leaking|overflowing)\b", RegexOptions.IgnoreCase))
                return Urgency.High;
            
            // Low urgency indicators
            if (Regex.IsMatch(lowerMessage, @"\b(whenever|eventually|sometime|no rush|not urgent|minor|small)\b", RegexOptions.IgnoreCase))
                return Urgency.Low;
            
            return Urgency.Medium;
        }

        private InformationLevel DetectInformationNeed(string message, ClientProfile profile)
        {
            // Check message characteristics
            var wordCount = message.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var hasQuestions = message.Contains('?');
            var hasMultipleSentences = message.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length > 2;

            // If client asked specific questions, they need more information
            if (hasQuestions && wordCount > 20)
                return InformationLevel.Detailed;
            
            // If message is very brief, client might be overwhelmed
            if (wordCount < 10)
                return InformationLevel.Minimal;
            
            // Use profile's information tolerance
            if (profile.InformationTolerance < 0.3m)
                return InformationLevel.Minimal;
            if (profile.InformationTolerance > 0.7m)
                return InformationLevel.Detailed;
            
            return InformationLevel.Moderate;
        }

        private Sentiment DetectEmotionalState(string message, ClientProfile profile)
        {
            var lowerMessage = message.ToLowerInvariant();
            
            // Panic indicators
            if (Regex.IsMatch(lowerMessage, @"\b(panic|terrified|scared|afraid|worried|anxious|stressed|overwhelmed)\b", RegexOptions.IgnoreCase))
                return Sentiment.Panic;
            
            // Frustrated indicators
            if (Regex.IsMatch(lowerMessage, @"\b(frustrated|annoyed|angry|mad|upset|irritated|fed up)\b", RegexOptions.IgnoreCase))
                return Sentiment.Frustrated;
            
            // Positive indicators
            if (Regex.IsMatch(lowerMessage, @"\b(thanks|thank you|appreciate|great|good|happy|pleased)\b", RegexOptions.IgnoreCase))
                return Sentiment.Positive;
            
            return Sentiment.Neutral;
        }

        private List<string> ExtractConcerns(string message)
        {
            var concerns = new List<string>();
            var lowerMessage = message.ToLowerInvariant();
            
            // Common service request concerns
            var concernPatterns = new Dictionary<string, string>
            {
                { @"\b(leak|leaking|water|flood)\b", "Water/Plumbing Issue" },
                { @"\b(broken|not working|stopped|malfunction)\b", "Equipment Malfunction" },
                { @"\b(cost|price|expensive|afford|budget)\b", "Cost Concern" },
                { @"\b(time|when|how long|schedule)\b", "Timing Concern" },
                { @"\b(quality|good|bad|satisfied)\b", "Quality Concern" },
                { @"\b(safety|safe|dangerous|hazard)\b", "Safety Concern" }
            };
            
            foreach (var pattern in concernPatterns)
            {
                if (Regex.IsMatch(lowerMessage, pattern.Key, RegexOptions.IgnoreCase))
                {
                    concerns.Add(pattern.Value);
                }
            }
            
            return concerns.Distinct().ToList();
        }

        private async Task<Dictionary<string, double>> AnalyzeKeywordsAsync(string message, ClientProfile profile)
        {
            var keywordScores = new Dictionary<string, double>();
            var keywords = await _profileService.GetKeywordReactionsAsync(profile.ClientId);
            
            var lowerMessage = message.ToLowerInvariant();
            
            foreach (var keyword in keywords)
            {
                if (lowerMessage.Contains(keyword.Keyword))
                {
                    // Normalize score to -1 to 1 range
                    var normalizedScore = Math.Max(-1.0, Math.Min(1.0, keyword.ReactionScore / (double)Math.Max(1, keyword.OccurrenceCount)));
                    keywordScores[keyword.Keyword] = normalizedScore;
                }
            }
            
            return keywordScores;
        }

        private ClientReaction? DetermineReaction(string message, MessageAnalysis analysis, ClientProfile profile)
        {
            // If sentiment is very negative, likely frustrated
            if (analysis.Sentiment == Sentiment.Frustrated || analysis.Sentiment == Sentiment.Panic)
                return ClientReaction.Frustrated;
            
            // If information need is high but message is brief, might be confused
            if (analysis.InformationNeed == InformationLevel.Detailed && message.Length < 50)
                return ClientReaction.Confused;
            
            // If sentiment is positive, likely satisfied
            if (analysis.Sentiment == Sentiment.Positive)
                return ClientReaction.Satisfied;
            
            return ClientReaction.Neutral;
        }

        private async Task<ResponseStrategy> DetermineResponseStrategyAsync(MessageAnalysis analysis, ClientProfile profile, int? serviceRequestId)
        {
            var strategy = new ResponseStrategy();

            // Determine tone based on profile and emotional state
            strategy.Tone = DetermineTone(analysis, profile);

            // Determine information level based on analysis and profile
            strategy.InformationLevel = DetermineInformationLevel(analysis, profile);

            // Determine approach
            strategy.Approach = DetermineApproach(analysis, profile);

            // Generate suggested actions
            strategy.SuggestedActions = await GenerateSuggestedActionsAsync(analysis, profile, serviceRequestId);

            // Calculate confidence
            strategy.Confidence = CalculateConfidence(analysis, profile);

            return strategy;
        }

        private PreferredTone DetermineTone(MessageAnalysis analysis, ClientProfile profile)
        {
            // If client is in panic, use supportive tone
            if (analysis.EmotionalState == Sentiment.Panic || analysis.Urgency == Urgency.Critical)
                return PreferredTone.Supportive;
            
            // If client prefers professional tone and not in distress
            if (profile.PreferredTone == "Professional" && analysis.Sentiment != Sentiment.Negative)
                return PreferredTone.Professional;
            
            // Default to supportive
            return PreferredTone.Supportive;
        }

        private InformationLevel DetermineInformationLevel(MessageAnalysis analysis, ClientProfile profile)
        {
            // If client is overwhelmed (high emotional sensitivity + negative sentiment), reduce info
            if (profile.EmotionalSensitivity > 0.7m && analysis.Sentiment == Sentiment.Negative)
                return InformationLevel.Minimal;
            
            // If client asked detailed questions, provide detailed info
            if (analysis.InformationNeed == InformationLevel.Detailed)
                return InformationLevel.Detailed;
            
            // Use profile's information tolerance
            if (profile.InformationTolerance < 0.4m)
                return InformationLevel.Minimal;
            if (profile.InformationTolerance > 0.6m)
                return InformationLevel.Detailed;
            
            return InformationLevel.Moderate;
        }

        private string DetermineApproach(MessageAnalysis analysis, ClientProfile profile)
        {
            // If urgent and panicked, reassure first
            if (analysis.Urgency == Urgency.Critical && analysis.EmotionalState == Sentiment.Panic)
                return "Reassuring";
            
            // If client is frustrated, be problem-solving
            if (analysis.Sentiment == Sentiment.Frustrated)
                return "Problem-Solving";
            
            // If client asked questions, be educational
            if (analysis.InformationNeed == InformationLevel.Detailed)
                return "Educational";
            
            return "Supportive";
        }

        private async Task<List<string>> GenerateSuggestedActionsAsync(MessageAnalysis analysis, ClientProfile profile, int? serviceRequestId)
        {
            var actions = new List<string>();
            
            // If urgent, suggest immediate actions
            if (analysis.Urgency == Urgency.Critical)
            {
                actions.Add("Assess immediate safety");
                actions.Add("Contact emergency services if needed");
            }
            
            // If service request exists, suggest checking status
            if (serviceRequestId.HasValue)
            {
                actions.Add("Review service request details");
            }
            
            // Based on concerns
            if (analysis.Concerns.Contains("Cost Concern"))
            {
                actions.Add("Discuss pricing options");
            }
            
            if (analysis.Concerns.Contains("Timing Concern"))
            {
                actions.Add("Provide timeline estimate");
            }
            
            return actions;
        }

        private decimal CalculateConfidence(MessageAnalysis analysis, ClientProfile profile)
        {
            decimal confidence = 0.5m;
            
            // More interactions = higher confidence
            if (profile.TotalInteractions > 10)
                confidence += 0.2m;
            
            // Clear sentiment = higher confidence
            if (analysis.Sentiment != Sentiment.Neutral)
                confidence += 0.1m;
            
            // Clear urgency = higher confidence
            if (analysis.Urgency != Urgency.Medium)
                confidence += 0.1m;
            
            return Math.Min(1.0m, confidence);
        }

        private async Task<AgenticResponse> GenerateAdaptiveResponseAsync(
            string clientMessage,
            MessageAnalysis analysis,
            ResponseStrategy strategy,
            ClientProfile profile,
            int? serviceRequestId)
        {
            try
            {
                // Get service request context if available
                string? serviceRequestContext = null;
                if (serviceRequestId.HasValue)
                {
                    var sr = await _serviceRequestService.GetServiceRequestByIdAsync(serviceRequestId.Value);
                    if (sr != null)
                    {
                        serviceRequestContext = $"Service Request: {sr.Title} - {sr.Description}";
                    }
                }

                // Get recent interaction history for context
                var recentHistory = await _profileService.GetRecentInteractionHistoryAsync(profile.ClientId, 5);
                var historyContext = recentHistory.Any() 
                    ? $"Recent interactions show: {string.Join(", ", recentHistory.Take(3).Select(h => h.ClientReaction ?? "Neutral"))}"
                    : "This is a new interaction";

                // Build adaptive prompt
                var prompt = $@"You are a helpful, cordial service assistant helping a client with a service request (like plumbing, car repair, lawn care, etc.).

CLIENT PROFILE:
- Communication Style: {profile.CommunicationStyle}
- Information Tolerance: {profile.InformationTolerance:F2} (0=minimal info, 1=detailed info)
- Emotional Sensitivity: {profile.EmotionalSensitivity:F2} (0=low, 1=high)
- Preferred Tone: {profile.PreferredTone}
- Total Past Interactions: {profile.TotalInteractions}
- Successful Resolutions: {profile.SuccessfulResolutions}

CURRENT SITUATION:
- Client Message: ""{clientMessage}""
- Detected Urgency: {analysis.Urgency}
- Client Emotional State: {analysis.EmotionalState}
- Client Sentiment: {analysis.Sentiment}
- Information Need: {analysis.InformationNeed}
- Main Concerns: {string.Join(", ", analysis.Concerns.Take(3))}
- Client Reaction: {analysis.ClientReaction}

{(!string.IsNullOrEmpty(serviceRequestContext) ? $"SERVICE REQUEST CONTEXT:\n{serviceRequestContext}\n" : "")}
INTERACTION HISTORY:
{historyContext}

RESPONSE STRATEGY:
- Tone: {strategy.Tone} (Supportive=warm/empathetic, Professional=formal/business-like, Casual=friendly/informal)
- Information Level: {strategy.InformationLevel} (Minimal=brief/essential only, Moderate=balanced, Detailed=comprehensive)
- Approach: {strategy.Approach} (Reassuring=calm/comfort, Problem-Solving=action-oriented, Educational=informative)

CRITICAL INSTRUCTIONS:
1. Respond in a {strategy.Tone} tone that matches the client's emotional state
2. Provide {strategy.InformationLevel} information - NOT too much (overwhelming) and NOT too little (frustrating)
3. Make the client feel {strategy.Approach} - help them feel connected and supported, not overwhelmed
4. Address their main concern: {analysis.Concerns.FirstOrDefault() ?? "general service request"}
5. If urgency is {analysis.Urgency}, adjust your response accordingly
6. Be cordial and make the client feel heard and understood
7. If this is an urgent situation, provide immediate actionable steps
8. Keep the response conversational and natural, not robotic
9. Reference successful past resolutions if relevant (client has {profile.SuccessfulResolutions} successful resolutions)
10. Balance being helpful with not overwhelming the client

Generate a helpful, personalized response that makes the client feel supported and guides them appropriately:";

                var llmRequest = new LlmRequest
                {
                    Prompt = prompt,
                    Temperature = 0.7m, // Creative but consistent
                    MaxTokens = 300,
                    Provider = AiProvider.OpenAI
                };

                var llmResponse = await _llmClient.GenerateTextAsync(llmRequest);
                
                return new AgenticResponse
                {
                    Message = llmResponse.Text.Trim(),
                    SuggestedActions = strategy.SuggestedActions,
                    Confidence = strategy.Confidence,
                    Analysis = analysis,
                    Strategy = strategy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating adaptive response");
                
                // Fallback response
                return new AgenticResponse
                {
                    Message = "I understand you need help with your service request. Let me assist you. Could you provide a bit more detail about what you're experiencing?",
                    Confidence = 0.3m,
                    SuggestedActions = new List<string> { "Provide more details" }
                };
            }
        }

        private async Task LearnFromInteractionAsync(
            int clientId,
            string clientMessage,
            AgenticResponse response,
            MessageAnalysis analysis,
            int? serviceRequestId)
        {
            try
            {
                var profile = await _profileService.GetProfileAsync(clientId);
                if (profile == null) return;

                // Learn information tolerance
                if (analysis.InformationNeed == InformationLevel.TooMuch)
                {
                    profile.InformationTolerance = Math.Max(0m, profile.InformationTolerance - 0.1m);
                }
                else if (analysis.InformationNeed == InformationLevel.TooLittle)
                {
                    profile.InformationTolerance = Math.Min(1.0m, profile.InformationTolerance + 0.1m);
                }

                // Learn emotional sensitivity
                if (analysis.EmotionalState == Sentiment.Panic || analysis.EmotionalState == Sentiment.Frustrated)
                {
                    profile.EmotionalSensitivity = Math.Min(1.0m, profile.EmotionalSensitivity + 0.05m);
                }
                else if (analysis.Sentiment == Sentiment.Positive)
                {
                    profile.EmotionalSensitivity = Math.Max(0m, profile.EmotionalSensitivity - 0.02m);
                }

                // Update interaction count
                profile.TotalInteractions++;
                profile.LastUpdated = DateTime.UtcNow;

                await _profileService.UpdateProfileAsync(profile);

                // Learn keyword reactions
                var keywords = ExtractKeywords(clientMessage);
                foreach (var keyword in keywords)
                {
                    var scoreDelta = analysis.Sentiment == Sentiment.Positive ? 1 : 
                                   (analysis.Sentiment == Sentiment.Negative ? -1 : 0);
                    await _profileService.AddOrUpdateKeywordReactionAsync(clientId, keyword, scoreDelta);
                }

                // Store interaction history
                var history = new ClientInteractionHistory
                {
                    ClientId = clientId,
                    ServiceRequestId = serviceRequestId,
                    InteractionType = "Message",
                    ClientMessage = clientMessage,
                    AgentResponse = response.Message,
                    Sentiment = analysis.Sentiment.ToString(),
                    Urgency = analysis.Urgency.ToString(),
                    InformationLevel = analysis.InformationNeed.ToString(),
                    ClientReaction = analysis.ClientReaction?.ToString()
                };

                await _profileService.AddInteractionHistoryAsync(history);

                _logger.LogInformation("Learned from interaction for client {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error learning from interaction for client {ClientId}", clientId);
            }
        }

        private List<string> ExtractKeywords(string message)
        {
            var keywords = new List<string>();
            var words = message.ToLowerInvariant()
                .Split(new[] { ' ', '\n', '\t', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3) // Only meaningful words
                .ToList();

            // Common service-related keywords
            var serviceKeywords = new[] { "plumbing", "leak", "repair", "broken", "fix", "service", "issue", "problem", "help", "urgent", "emergency" };
            
            foreach (var word in words)
            {
                if (serviceKeywords.Contains(word) || word.Length > 5)
                {
                    keywords.Add(word);
                }
            }

            return keywords.Distinct().Take(10).ToList(); // Limit to 10 keywords
        }
    }
}

