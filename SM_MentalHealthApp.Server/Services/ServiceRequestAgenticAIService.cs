using SM_MentalHealthApp.Shared;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IServiceRequestAgenticAIService
    {
        Task<AgenticResponse> ProcessServiceRequestAsync(int clientId, string clientMessage, int? serviceRequestId = null, string? conversationHistory = null);
    }

    public class ServiceRequestAgenticAIService : IServiceRequestAgenticAIService
    {
        private readonly IClientProfileService _profileService;
        private readonly IClientAgentSessionService _agentSessionService;
        private readonly HuggingFaceService _huggingFaceService;
        private readonly LlmClient _llmClient;
        private readonly ILogger<ServiceRequestAgenticAIService> _logger;
        private readonly IServiceRequestService _serviceRequestService;
        private readonly IAppointmentService _appointmentService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceRequestAgenticAIService(
            IClientProfileService profileService,
            IClientAgentSessionService agentSessionService,
            HuggingFaceService huggingFaceService,
            LlmClient llmClient,
            ILogger<ServiceRequestAgenticAIService> logger,
            IServiceRequestService serviceRequestService,
            IAppointmentService appointmentService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _profileService = profileService;
            _agentSessionService = agentSessionService;
            _huggingFaceService = huggingFaceService;
            _llmClient = llmClient;
            _logger = logger;
            _serviceRequestService = serviceRequestService;
            _appointmentService = appointmentService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<AgenticResponse> ProcessServiceRequestAsync(int clientId, string clientMessage, int? serviceRequestId = null, string? conversationHistory = null)
        {
            try
            {
                _logger.LogInformation("Processing service request for client {ClientId}, SR: {ServiceRequestId}", clientId, serviceRequestId);

                // Step 1: Get or create client agent session (SR-first approach)
                var agentSession = await _agentSessionService.GetOrCreateSessionAsync(clientId);
                var sessionState = Enum.Parse<ClientAgentSessionState>(agentSession.State);
                
                // Step 2: Check if user is asking about which SR is active FIRST
                // This handles cases where user wants to know/confirm the current SR context
                if (IsAskingAboutSRContext(clientMessage))
                {
                    if (sessionState == ClientAgentSessionState.InSRContext && agentSession.CurrentServiceRequestId.HasValue)
                    {
                        // User is asking which SR - tell them and offer to switch
                        var sr = await _serviceRequestService.GetServiceRequestByIdAsync(agentSession.CurrentServiceRequestId.Value);
                        if (sr != null)
                        {
                            return new AgenticResponse
                            {
                                Message = $"We're currently working on Service Request #{sr.Id}: \"{sr.Title}\" (Status: {sr.Status}). Is this the correct request, or would you like to switch to a different one?",
                                Confidence = 0.9m,
                                SuggestedActions = new List<string> { "Continue with this SR", "Switch to different SR", "Create new SR" }
                            };
                        }
                    }
                    else
                    {
                        // No active SR - prompt for selection
                        return await PromptForSRContextAsync(clientId, clientMessage, agentSession);
                    }
                }
                
                // Step 3: Determine active SR context
                // Priority: 1) Selected SR from UI dropdown (serviceRequestId parameter), 2) Agent session's CurrentServiceRequestId (persistent context)
                // CRITICAL: If user selected an SR from UI dropdown, use it and set it as active
                int? activeServiceRequestId = null;
                
                if (serviceRequestId.HasValue)
                {
                    // User selected an SR from UI dropdown - use it and set it as active
                    activeServiceRequestId = serviceRequestId;
                    // Set it as active in ClientAgentSession
                    await _agentSessionService.SetActiveServiceRequestAsync(clientId, serviceRequestId.Value);
                    _logger.LogInformation("Using selected SR {ServiceRequestId} from UI dropdown for client {ClientId}", activeServiceRequestId, clientId);
                }
                else if (sessionState == ClientAgentSessionState.InSRContext && agentSession.CurrentServiceRequestId.HasValue)
                {
                    // Agent session has an active SR context - use it (user has already selected/created it)
                    activeServiceRequestId = agentSession.CurrentServiceRequestId;
                    _logger.LogInformation("Using active SR {ServiceRequestId} from ClientAgentSession for client {ClientId}", activeServiceRequestId, clientId);
                }
                else
                {
                    // CRITICAL SR-FIRST ENFORCEMENT: If session state is InSRContext but CurrentServiceRequestId is null, 
                    // this is a data inconsistency - clear it and force SR selection
                    if (sessionState == ClientAgentSessionState.InSRContext && !agentSession.CurrentServiceRequestId.HasValue)
                    {
                        _logger.LogWarning("Data inconsistency detected: Session state is InSRContext but CurrentServiceRequestId is null for client {ClientId}. Clearing state.", clientId);
                        await _agentSessionService.ClearActiveServiceRequestAsync(clientId);
                        sessionState = ClientAgentSessionState.NoActiveSRContext;
                    }
                }

                // Step 4: Check if we need SR context (SR-first enforcement)
                // CRITICAL: If no active SR, we MUST prompt for SR context - this is the SR-first approach
                if (!activeServiceRequestId.HasValue)
                {
                    // Check if we're in a selection/creation state and user is responding
                    if (sessionState == ClientAgentSessionState.SelectingExistingSR)
                    {
                        // User is selecting an SR - try to match their response
                        var selectedSR = await MatchSelectedSRAsync(clientId, clientMessage);
                        if (selectedSR.HasValue)
                        {
                            await _agentSessionService.SetActiveServiceRequestAsync(clientId, selectedSR.Value);
                            activeServiceRequestId = selectedSR.Value;
                            // Continue with normal flow below
                        }
                        else
                        {
                            // Couldn't match - ask again
                            return await HandleSRSelectionRequestAsync(clientId, clientMessage, agentSession);
                        }
                    }
                    else if (sessionState == ClientAgentSessionState.CreatingNewSR)
                    {
                        // User is creating an SR - handle creation
                        return await HandleSRCreationRequestAsync(clientId, clientMessage, agentSession);
                    }
                    else
                    {
                        // CRITICAL SR-FIRST ENFORCEMENT: No SR context - we MUST prompt for SR selection/creation
                        // Check if user is trying to reference/create one, but ALWAYS prompt if no clear intent
                        var srIntent = DetectServiceRequestIntent(clientMessage, clientId);
                        
                        _logger.LogInformation("No active SR for client {ClientId}. Intent detection: ReferencedSRId={ReferencedSRId}, RequiresSRSelection={RequiresSRSelection}, RequiresSRCreation={RequiresSRCreation}", 
                            clientId, srIntent.ReferencedSRId, srIntent.RequiresSRSelection, srIntent.RequiresSRCreation);
                        
                        if (srIntent.ReferencedSRId.HasValue)
                        {
                            // User referenced a specific SR - set it as active
                            await _agentSessionService.SetActiveServiceRequestAsync(clientId, srIntent.ReferencedSRId.Value);
                            activeServiceRequestId = srIntent.ReferencedSRId.Value;
                            _logger.LogInformation("User referenced SR {ServiceRequestId} - setting as active", activeServiceRequestId);
                            // Continue with normal flow below
                        }
                        else if (srIntent.RequiresSRSelection)
                        {
                            // User needs to select an existing SR
                            _logger.LogInformation("Intent requires SR selection - prompting user");
                            return await HandleSRSelectionRequestAsync(clientId, clientMessage, agentSession);
                        }
                        else if (srIntent.RequiresSRCreation)
                        {
                            // User wants to create a new SR
                            _logger.LogInformation("Intent requires SR creation - prompting user");
                            return await HandleSRCreationRequestAsync(clientId, clientMessage, agentSession);
                        }
                        else
                        {
                            // CRITICAL: User is chatting but no SR context - ALWAYS ask them to select/create
                            // This is the SR-first enforcement - we never proceed without an SR context
                            _logger.LogInformation("No clear SR intent detected - prompting user for SR context (SR-first enforcement)");
                            return await PromptForSRContextAsync(clientId, clientMessage, agentSession);
                        }
                    }
                }

                // Step 5: CRITICAL SR-FIRST ENFORCEMENT CHECK
                // We should NEVER reach here without an active SR - if we do, it's a bug
                if (!activeServiceRequestId.HasValue)
                {
                    _logger.LogError("CRITICAL: Reached normal flow without active SR for client {ClientId}. This should never happen. Forcing SR prompt.", clientId);
                    return await PromptForSRContextAsync(clientId, clientMessage, agentSession);
                }

                // Step 6: We have SR context - proceed with normal agentic AI flow
                // Update session state if needed
                if (sessionState != ClientAgentSessionState.InSRContext)
                {
                    await _agentSessionService.SetActiveServiceRequestAsync(clientId, activeServiceRequestId.Value);
                }

                // Step 5: Load or create client profile
                var profile = await _profileService.GetOrCreateProfileAsync(clientId);

                // Step 6: Analyze client message
                var analysis = await AnalyzeClientMessageAsync(clientMessage, profile);

                // Step 7: Determine response strategy
                var strategy = await DetermineResponseStrategyAsync(analysis, profile, activeServiceRequestId);

                // Step 8: Generate adaptive response
                var response = await GenerateAdaptiveResponseAsync(clientMessage, analysis, strategy, profile, activeServiceRequestId, conversationHistory);

                // Step 9: Learn from interaction (async, don't block)
                // CRITICAL: Create a new scope for background task to avoid DbContext disposal issues
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Create a new scope for the background task
                        // This ensures we have a fresh DbContext that won't be disposed
                        using var scope = _serviceScopeFactory.CreateScope();
                        var profileService = scope.ServiceProvider.GetRequiredService<IClientProfileService>();
                        
                        await LearnFromInteractionAsync(clientId, clientMessage, response, analysis, activeServiceRequestId, profileService);
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
            int? serviceRequestId,
            string? conversationHistory = null)
        {
            try
            {
                // Get service request context if available
                string? serviceRequestContext = null;
                string? appointmentsContext = null;
                if (serviceRequestId.HasValue)
                {
                    var sr = await _serviceRequestService.GetServiceRequestByIdAsync(serviceRequestId.Value);
                    if (sr != null)
                    {
                        serviceRequestContext = $"Service Request: {sr.Title} - {sr.Description}";
                        
                        // Get appointments for this service request
                        var allAppointments = await _appointmentService.GetAppointmentsAsync(patientId: profile.ClientId);
                        var srAppointments = allAppointments
                            .Where(a => a.ServiceRequestIds != null && a.ServiceRequestIds.Contains(serviceRequestId.Value))
                            .OrderBy(a => a.AppointmentDateTime)
                            .ToList();
                        
                        if (srAppointments.Any())
                        {
                            // Filter out cancelled appointments - they shouldn't be shown to the agentic AI
                            // Deleted appointments (IsActive=false) are already filtered by GetAppointmentsAsync
                            var activeAppointments = srAppointments
                                .Where(a => a.Status != AppointmentStatus.Cancelled)
                                .ToList();
                            
                            var upcomingAppointments = activeAppointments
                                .Where(a => a.AppointmentDateTime >= DateTime.UtcNow && 
                                       a.Status != AppointmentStatus.Completed)
                                .ToList();
                            
                            var pastAppointments = activeAppointments
                                .Where(a => a.AppointmentDateTime < DateTime.UtcNow || 
                                       a.Status == AppointmentStatus.Completed)
                                .ToList();
                            
                            var appointmentsInfo = new List<string>();
                            
                            if (upcomingAppointments.Any())
                            {
                                appointmentsInfo.Add($"UPCOMING APPOINTMENTS ({upcomingAppointments.Count}):");
                                foreach (var apt in upcomingAppointments.Take(3))
                                {
                                    var doctorName = apt.DoctorName ?? "Provider";
                                    var dateTime = apt.AppointmentDateTime.ToString("MM/dd/yyyy HH:mm");
                                    var status = apt.Status;
                                    appointmentsInfo.Add($"- {dateTime} with {doctorName} (Status: {status})");
                                }
                            }
                            
                            if (pastAppointments.Any())
                            {
                                appointmentsInfo.Add($"RECENT APPOINTMENTS ({pastAppointments.Count}):");
                                foreach (var apt in pastAppointments.OrderByDescending(a => a.AppointmentDateTime).Take(2))
                                {
                                    var doctorName = apt.DoctorName ?? "Provider";
                                    var dateTime = apt.AppointmentDateTime.ToString("MM/dd/yyyy HH:mm");
                                    var status = apt.Status;
                                    appointmentsInfo.Add($"- {dateTime} with {doctorName} (Status: {status})");
                                }
                            }
                            
                            appointmentsContext = string.Join("\n", appointmentsInfo);
                        }
                    }
                }

                // Get recent interaction history for context (from profile)
                var recentHistory = await _profileService.GetRecentInteractionHistoryAsync(profile.ClientId, 5);
                var profileHistoryContext = recentHistory.Any() 
                    ? $"Recent interactions show: {string.Join(", ", recentHistory.Take(3).Select(h => h.ClientReaction ?? "Neutral"))}"
                    : "This is a new interaction";

                // Use conversation history if provided (for maintaining context within the same chat session)
                var conversationContext = !string.IsNullOrEmpty(conversationHistory)
                    ? $"CONVERSATION HISTORY:\n{conversationHistory}\n\nIMPORTANT: The client's current message is a continuation of this conversation. Use the history to understand context and references."
                    : "This is the start of a new conversation.";

                // Build adaptive prompt
                var prompt = $@"You are a helpful, cordial SERVICE REQUEST assistant helping a client with NON-MEDICAL service requests (like plumbing, car repair, lawn care, legal assistance, home repairs, etc.).

**THIS IS NOT A MEDICAL CONVERSATION. DO NOT MENTION HEALTH, MEDICAL TOPICS, PATIENTS, OR MEDICAL CARE.**

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
{(!string.IsNullOrEmpty(appointmentsContext) ? $"APPOINTMENTS FOR THIS SERVICE REQUEST:\n{appointmentsContext}\n\nIMPORTANT: If appointments exist, acknowledge them in your response. Do NOT say you're 'looking for' or 'reaching out to' service providers if appointments are already scheduled. Instead, reference the existing appointments and provide updates based on what's already scheduled.\n" : "")}
{conversationContext}

PROFILE INTERACTION HISTORY:
{profileHistoryContext}

RESPONSE STRATEGY:
- Tone: {strategy.Tone} (Supportive=warm/empathetic, Professional=formal/business-like, Casual=friendly/informal)
- Information Level: {strategy.InformationLevel} (Minimal=brief/essential only, Moderate=balanced, Detailed=comprehensive)
- Approach: {strategy.Approach} (Reassuring=calm/comfort, Problem-Solving=action-oriented, Educational=informative)

CRITICAL INSTRUCTIONS:
1. **ABSOLUTELY DO NOT mention health, medical topics, patients, medical care, or ask about health concerns. This is a SERVICE REQUEST conversation only.**
2. Respond in a {strategy.Tone} tone that matches the client's emotional state
3. Provide {strategy.InformationLevel} information - NOT too much (overwhelming) and NOT too little (frustrating)
4. Make the client feel {strategy.Approach} - help them feel connected and supported, not overwhelmed
5. Address their main concern: {analysis.Concerns.FirstOrDefault() ?? "general service request"}
6. If urgency is {analysis.Urgency}, adjust your response accordingly
7. Be cordial and make the client feel heard and understood
8. If this is an urgent situation, provide immediate actionable steps
9. Keep the response conversational and natural, not robotic
10. Reference successful past resolutions if relevant (client has {profile.SuccessfulResolutions} successful resolutions)
11. Balance being helpful with not overwhelming the client
12. **CRITICAL**: If conversation history is provided above, use it to understand context. For example, if the client says ""Yes please"", ""That sounds good"", ""10-12 AM is good"", or ""thank you, that is all"", refer back to what was discussed in the conversation history and continue from there.
13. **CRITICAL**: If appointments are listed above, you MUST acknowledge them. Do NOT say you're ""gathering information"" or ""reaching out to shops"" if appointments already exist. Instead, reference the existing appointments (e.g., ""I see you have an appointment scheduled for [date/time] with [provider]""). If the client asks about status, provide information about the scheduled appointments.
13. **CRITICAL**: Maintain conversation continuity - if the client is responding to a previous question or suggestion, acknowledge that and continue from where you left off. Don't start a new topic.
14. **CRITICAL**: If the client says ""thank you, that is all I need for now"" or similar closing statements, acknowledge their service request is complete and offer to help if they need anything else with their SERVICE REQUEST. Do NOT mention health or medical topics.
15. **CRITICAL**: If the client says ""nope"" or ""no"" in response to a question, acknowledge their answer and continue with the service request conversation. Do NOT switch topics or mention medical/health topics.

Generate a helpful, personalized response that makes the client feel supported and guides them appropriately. Remember: This is ONLY about service requests, NOT medical or health topics:";

                var llmRequest = new LlmRequest
                {
                    Prompt = prompt,
                    Temperature = 0.7, // Creative but consistent
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
            int? serviceRequestId,
            IClientProfileService profileService)
        {
            try
            {
                var profile = await profileService.GetProfileAsync(clientId);
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

                await profileService.UpdateProfileAsync(profile);

                // Learn keyword reactions
                var keywords = ExtractKeywords(clientMessage);
                foreach (var keyword in keywords)
                {
                    var scoreDelta = analysis.Sentiment == Sentiment.Positive ? 1 : 
                                   (analysis.Sentiment == Sentiment.Negative ? -1 : 0);
                    await profileService.AddOrUpdateKeywordReactionAsync(clientId, keyword, scoreDelta);
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

                await profileService.AddInteractionHistoryAsync(history);

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

        /// <summary>
        /// Detects if the client message indicates they want to reference an existing SR or create a new one
        /// </summary>
        private (bool RequiresSRSelection, bool RequiresSRCreation, int? ReferencedSRId) DetectServiceRequestIntent(string message, int clientId)
        {
            var lowerMessage = message.ToLowerInvariant();
            
            // Check for SR number references (e.g., "SR-123", "request 123", "#123")
            var srNumberMatch = Regex.Match(lowerMessage, @"(?:sr-?|request|#)\s*(\d+)", RegexOptions.IgnoreCase);
            if (srNumberMatch.Success && int.TryParse(srNumberMatch.Groups[1].Value, out int srId))
            {
                return (true, false, srId);
            }

            // Check for status check intent
            if (Regex.IsMatch(lowerMessage, @"\b(status|update|progress|how.*going|what.*happening)\b", RegexOptions.IgnoreCase))
            {
                return (true, false, null); // Need to select which SR to check
            }

            // Check for new request intent
            if (Regex.IsMatch(lowerMessage, @"\b(new|create|start|another|different|also)\s+(request|issue|problem|service)", RegexOptions.IgnoreCase))
            {
                return (false, true, null);
            }

            // Check for explicit "existing" or "my request" references
            if (Regex.IsMatch(lowerMessage, @"\b(existing|my|previous|earlier|current)\s+(request|issue|problem|service)", RegexOptions.IgnoreCase))
            {
                return (true, false, null);
            }

            // Check for service-related keywords that indicate a new issue (car, plumbing, repair, etc.)
            // These suggest the user is reporting a new problem that needs an SR
            var serviceKeywords = new[] { 
                "issue", "problem", "broken", "not working", "leaking", "repair", "fix", 
                "car", "vehicle", "plumbing", "faucet", "sink", "toilet", "heater", "ac", "air conditioning",
                "appliance", "refrigerator", "washer", "dryer", "dishwasher", "oven", "stove",
                "electrical", "wiring", "outlet", "light", "brake", "engine", "transmission",
                "legal", "attorney", "lawyer", "lawsuit", "accident", "injury"
            };
            
            if (serviceKeywords.Any(keyword => lowerMessage.Contains(keyword)))
            {
                // This looks like a service request - but we'll let PromptForSRContextAsync handle it
                // by asking if it's existing or new
                return (false, false, null);
            }

            return (false, false, null);
        }

        /// <summary>
        /// Handles when user needs to select an existing SR
        /// </summary>
        private async Task<AgenticResponse> HandleSRSelectionRequestAsync(int clientId, string clientMessage, ClientAgentSession agentSession)
        {
            // Get client's service requests
            var serviceRequests = await _serviceRequestService.GetServiceRequestsAsync(clientId: clientId);
            var activeSRs = serviceRequests
                .Where(sr => sr.Status == "Active" || sr.Status == "Pending")
                .OrderByDescending(sr => sr.CreatedAt)
                .Take(5)
                .ToList();

            if (!activeSRs.Any())
            {
                // No existing SRs - suggest creating one
                await _agentSessionService.UpdateSessionStateAsync(clientId, ClientAgentSessionState.CreatingNewSR);
                return new AgenticResponse
                {
                    Message = "I don't see any existing service requests for you. Would you like me to create a new one? Just tell me what you need help with, and I'll set it up for you.",
                    Confidence = 0.8m,
                    SuggestedActions = new List<string> { "Create new service request" }
                };
            }

            // Update session state
            await _agentSessionService.UpdateSessionStateAsync(clientId, ClientAgentSessionState.SelectingExistingSR);

            // Build SR list for user
            var srList = string.Join("\n", activeSRs.Select((sr, idx) => 
                $"{idx + 1}. {sr.Title} (Status: {sr.Status}, Created: {sr.CreatedAt:MM/dd/yyyy})"));

            return new AgenticResponse
            {
                Message = $"I see you have {activeSRs.Count} active service request(s). Which one would you like to discuss?\n\n{srList}\n\nYou can tell me the number (1-{activeSRs.Count}), the title, or say \"new\" to create a new request.",
                Confidence = 0.8m,
                SuggestedActions = activeSRs.Select(sr => $"Select: {sr.Title}").ToList()
            };
        }

        /// <summary>
        /// Handles when user wants to create a new SR
        /// </summary>
        private async Task<AgenticResponse> HandleSRCreationRequestAsync(int clientId, string clientMessage, ClientAgentSession agentSession)
        {
            // Check if we're already in creation mode and have pending SR
            if (agentSession.PendingCreatedServiceRequestId.HasValue)
            {
                // Confirm the created SR
                await _agentSessionService.ConfirmCreatedServiceRequestAsync(clientId, agentSession.PendingCreatedServiceRequestId.Value);
                var sr = await _serviceRequestService.GetServiceRequestByIdAsync(agentSession.PendingCreatedServiceRequestId.Value);
                
                return new AgenticResponse
                {
                    Message = $"Great! I've created Service Request #{sr?.Id}: \"{sr?.Title}\". We're now working under this request. How can I help you with it?",
                    Confidence = 0.9m,
                    SuggestedActions = new List<string> { "Continue with service request" }
                };
            }

            // Extract SR details from message
            var title = ExtractSRTitle(clientMessage);
            var description = ExtractSRDescription(clientMessage);
            var type = ExtractSRType(clientMessage);

            // If we have enough info, create the SR
            if (!string.IsNullOrWhiteSpace(title))
            {
                try
                {
                    var createRequest = new CreateServiceRequestRequest
                    {
                        ClientId = clientId,
                        Title = title,
                        Description = description,
                        Type = type ?? "General",
                        Status = "Active"
                    };

                    var createdSR = await _serviceRequestService.CreateServiceRequestAsync(createRequest, clientId);
                    
                    // Set as pending for confirmation
                    await _agentSessionService.UpdateSessionStateAsync(clientId, ClientAgentSessionState.CreatingNewSR, createdSR.Id);
                    
                    return new AgenticResponse
                    {
                        Message = $"I've created Service Request #{createdSR.Id}: \"{createdSR.Title}\". We're now working under this request. How can I help you with it?",
                        Confidence = 0.9m,
                        SuggestedActions = new List<string> { "Continue with service request" }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating service request for client {ClientId}", clientId);
                    return new AgenticResponse
                    {
                        Message = "I had trouble creating your service request. Could you try again with a clear title? For example: \"Plumbing leak in kitchen\" or \"Car repair needed\".",
                        Confidence = 0.5m,
                        SuggestedActions = new List<string> { "Retry creating service request" }
                    };
                }
            }

            // Need more info
            await _agentSessionService.UpdateSessionStateAsync(clientId, ClientAgentSessionState.CreatingNewSR);
            
            return new AgenticResponse
            {
                Message = "I'd be happy to create a new service request for you! What would you like the title to be? For example: \"Plumbing leak in kitchen\" or \"Car repair needed\".",
                Confidence = 0.7m,
                SuggestedActions = new List<string> { "Provide service request title" }
            };
        }

        /// <summary>
        /// Prompts user to select or create an SR when no context exists
        /// </summary>
        private async Task<AgenticResponse> PromptForSRContextAsync(int clientId, string clientMessage, ClientAgentSession agentSession)
        {
            // Get client's service requests to see if they have any
            var serviceRequests = await _serviceRequestService.GetServiceRequestsAsync(clientId: clientId);
            var activeSRs = serviceRequests
                .Where(sr => sr.Status == "Active" || sr.Status == "Pending")
                .OrderByDescending(sr => sr.CreatedAt)
                .Take(3)
                .ToList();

            if (activeSRs.Any())
            {
                // Client has existing SRs - ask if this is about one of them or a new one
                await _agentSessionService.UpdateSessionStateAsync(clientId, ClientAgentSessionState.SelectingExistingSR);
                
                var srList = string.Join("\n", activeSRs.Select((sr, idx) => 
                    $"{idx + 1}. {sr.Title}"));
                
                return new AgenticResponse
                {
                    Message = $"I'd like to help you! Is this about one of your existing service requests, or is this a new issue?\n\nYour active requests:\n{srList}\n\nYou can:\n- Tell me the number or title of an existing request\n- Say \"new\" to create a new service request",
                    Confidence = 0.8m,
                    SuggestedActions = activeSRs.Select(sr => $"Select: {sr.Title}").Concat(new[] { "Create new request" }).ToList()
                };
            }
            else
            {
                // No existing SRs - suggest creating one
                await _agentSessionService.UpdateSessionStateAsync(clientId, ClientAgentSessionState.CreatingNewSR);
                
                return new AgenticResponse
                {
                    Message = "I'd be happy to help you! To get started, I need to create a service request for you. What would you like the title to be? For example: \"Plumbing leak in kitchen\" or \"Car repair needed\".",
                    Confidence = 0.8m,
                    SuggestedActions = new List<string> { "Provide service request title" }
                };
            }
        }

        /// <summary>
        /// Extracts a potential SR title from the client message
        /// </summary>
        private string? ExtractSRTitle(string message)
        {
            // Look for quoted text
            var quotedMatch = Regex.Match(message, @"""([^""]+)""");
            if (quotedMatch.Success)
            {
                return quotedMatch.Groups[1].Value.Trim();
            }

            // Look for "title: ..." pattern
            var titleMatch = Regex.Match(message, @"title\s*:?\s*(.+?)(?:\.|$)", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                return titleMatch.Groups[1].Value.Trim();
            }

            // If message is short and descriptive, use it as title
            if (message.Length > 10 && message.Length < 100 && !message.Contains('?'))
            {
                return message.Trim();
            }

            return null;
        }

        /// <summary>
        /// Extracts SR description from the client message
        /// </summary>
        private string? ExtractSRDescription(string message)
        {
            // If message is longer, use it as description
            if (message.Length > 50)
            {
                return message;
            }
            return null;
        }

        /// <summary>
        /// Matches user's response to an existing SR (by number, title, or index)
        /// </summary>
        private async Task<int?> MatchSelectedSRAsync(int clientId, string userResponse)
        {
            var serviceRequests = await _serviceRequestService.GetServiceRequestsAsync(clientId: clientId);
            var activeSRs = serviceRequests
                .Where(sr => sr.Status == "Active" || sr.Status == "Pending")
                .OrderByDescending(sr => sr.CreatedAt)
                .Take(5)
                .ToList();

            if (!activeSRs.Any())
                return null;

            var lowerResponse = userResponse.ToLowerInvariant().Trim();

            // Check for numeric index (1-5)
            if (int.TryParse(lowerResponse, out int index) && index >= 1 && index <= activeSRs.Count)
            {
                return activeSRs[index - 1].Id;
            }

            // Check for SR number reference
            var srNumberMatch = Regex.Match(lowerResponse, @"(?:sr-?|request|#)\s*(\d+)", RegexOptions.IgnoreCase);
            if (srNumberMatch.Success && int.TryParse(srNumberMatch.Groups[1].Value, out int srId))
            {
                var matchedSR = activeSRs.FirstOrDefault(sr => sr.Id == srId);
                if (matchedSR != null)
                    return matchedSR.Id;
            }

            // Check for title match (fuzzy)
            foreach (var sr in activeSRs)
            {
                var srTitleLower = sr.Title?.ToLowerInvariant() ?? "";
                if (srTitleLower.Contains(lowerResponse) || lowerResponse.Contains(srTitleLower))
                {
                    return sr.Id;
                }
            }

            // Check for keywords in title
            var responseWords = lowerResponse.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sr in activeSRs)
            {
                var srTitleLower = sr.Title?.ToLowerInvariant() ?? "";
                var matchCount = responseWords.Count(word => srTitleLower.Contains(word));
                if (matchCount >= 2) // At least 2 words match
                {
                    return sr.Id;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the user is asking about which SR is currently active
        /// </summary>
        private bool IsAskingAboutSRContext(string message)
        {
            var lowerMessage = message.ToLowerInvariant();
            
            // Patterns that indicate user is asking about SR context
            var contextPatterns = new[]
            {
                @"\b(which|what)\s+(service\s+)?request",
                @"\b(which|what)\s+sr",
                @"\bdo\s+you\s+know\s+which",
                @"\bwhat\s+are\s+we\s+talking\s+about",
                @"\bwhich\s+one\s+(are\s+we\s+)?(working\s+on|discussing)",
                @"\bwhat\s+request\s+(are\s+we\s+)?(working\s+on|discussing)",
                @"\bis\s+this\s+about",
                @"\bwhich\s+issue",
                @"\bwhat\s+issue"
            };
            
            foreach (var pattern in contextPatterns)
            {
                if (Regex.IsMatch(lowerMessage, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Extracts SR type from the client message
        /// </summary>
        private string? ExtractSRType(string message)
        {
            var lowerMessage = message.ToLowerInvariant();
            
            var typePatterns = new Dictionary<string, string>
            {
                { @"\b(plumb|pipe|water|leak|faucet|sink|toilet|drain)\b", "Plumbing" },
                { @"\b(car|vehicle|auto|automobile|tire|engine|brake)\b", "Car Repair" },
                { @"\b(legal|lawyer|attorney|law|case|lawsuit)\b", "Legal" },
                { @"\b(lawn|yard|grass|mow|landscap)\b", "Lawn Care" },
                { @"\b(electric|electrical|wiring|outlet|circuit)\b", "Electrical" },
                { @"\b(hvac|heating|cooling|ac|furnace|air)\b", "HVAC" }
            };

            foreach (var pattern in typePatterns)
            {
                if (Regex.IsMatch(lowerMessage, pattern.Key, RegexOptions.IgnoreCase))
                {
                    return pattern.Value;
                }
            }

            return null;
        }
    }
}

