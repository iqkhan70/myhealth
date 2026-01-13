using System;
using System.Threading.Tasks;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Services
{
    public class ChatService
    {
        private readonly ConversationRepository _conversationRepository;
        private readonly HuggingFaceService _huggingFaceService;
        private readonly JournalService _journalService;
        private readonly UserService _userService;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly IIntelligentContextService _intelligentContextService;
        private readonly IChatHistoryService _chatHistoryService;
        private readonly IServiceRequestService _serviceRequestService;
        private readonly IServiceRequestAgenticAIService? _agenticAIService; // Optional - only for service requests
        private readonly IRedisCacheService? _redisCache; // Optional - for caching conversation history
        private readonly JournalDbContext _context;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ConversationRepository conversationRepository, HuggingFaceService huggingFaceService, JournalService journalService, UserService userService, IContentAnalysisService contentAnalysisService, IIntelligentContextService intelligentContextService, IChatHistoryService chatHistoryService, IServiceRequestService serviceRequestService, JournalDbContext context, ILogger<ChatService> logger, IServiceRequestAgenticAIService? agenticAIService = null, IRedisCacheService? redisCache = null)
        {
            _conversationRepository = conversationRepository;
            _huggingFaceService = huggingFaceService;
            _journalService = journalService;
            _userService = userService;
            _contentAnalysisService = contentAnalysisService;
            _intelligentContextService = intelligentContextService;
            _chatHistoryService = chatHistoryService;
            _serviceRequestService = serviceRequestService;
            _agenticAIService = agenticAIService;
            _redisCache = redisCache;
            _context = context;
            _logger = logger;
        }

        public async Task<ChatResponse> SendMessageAsync(string prompt, string conversationId, AiProvider provider, int patientId = 0, int userId = 0, int userRoleId = 0, bool isGenericMode = false, bool forceServiceRequestMode = false, int? selectedServiceRequestId = null)
        {
            try
            {
                _logger.LogInformation("Prompt: {Prompt}", prompt);
                _logger.LogInformation("PatientId: {PatientId}", patientId);
                _logger.LogInformation("UserId: {UserId}", userId);
                _logger.LogInformation("UserRoleId: {UserRoleId}", userRoleId);
                _logger.LogInformation("IsGenericMode: {IsGenericMode}", isGenericMode);

                // Determine ServiceRequestId for patient mode chats
                int? serviceRequestId = null;
                if (!isGenericMode && patientId > 0 && (userRoleId == Shared.Constants.Roles.Doctor || userRoleId == Shared.Constants.Roles.Attorney || userRoleId == Shared.Constants.Roles.Sme))
                {
                    // Get default ServiceRequest for this patient
                    var defaultSr = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(patientId);
                    if (defaultSr != null)
                    {
                        // Verify user is assigned to this SR
                        var isAssigned = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(defaultSr.Id, userId);
                        if (isAssigned)
                        {
                            serviceRequestId = defaultSr.Id;
                            _logger.LogInformation("Using ServiceRequest {ServiceRequestId} for chat session", serviceRequestId);
                        }
                    }
                }
                // NOTE: We do NOT auto-select an SR here for patients in Service Request mode
                // The agentic AI will handle SR selection/creation via ClientAgentSession (SR-first approach)
                // Only use explicit serviceRequestId if it's already set (e.g., from a previous message in the same session)

                // Get or create chat session
                // IMPORTANT: Respect the client's mode choice
                // - If forceServiceRequestMode=true: Look for service request sessions
                // - If forceServiceRequestMode=false: Look for medical sessions (no ServiceRequestId)
                ChatSession? session = null;
                if (!isGenericMode && userRoleId == Shared.Constants.Roles.Patient && patientId == userId)
                {
                    var existingSessions = await _chatHistoryService.GetUserSessionsAsync(userId, patientId);
                    
                    if (forceServiceRequestMode)
                    {
                        // User is in Service Request Chat mode - look for service request sessions
                        // NOTE: We find the session, but don't auto-use its ServiceRequestId
                        // The agentic AI will check ClientAgentSession to determine the active SR (SR-first approach)
                        var todaySession = existingSessions
                            .Where(s => s.IsActive && s.CreatedAt.Date == DateTime.UtcNow.Date && s.ServiceRequestId.HasValue)
                            .OrderByDescending(s => s.LastActivityAt)
                            .FirstOrDefault();
                        
                        if (todaySession != null)
                        {
                            session = todaySession;
                            // Don't auto-set serviceRequestId - let agentic AI determine from ClientAgentSession
                            // Only use it if ClientAgentSession confirms it's the active SR
                            _logger.LogInformation("Found existing service request session {SessionId} with ServiceRequestId {ServiceRequestId}. Agentic AI will determine active SR from ClientAgentSession.", 
                                session.Id, todaySession.ServiceRequestId);
                        }
                    }
                    else
                    {
                        // User is in Medical Chat mode - look for medical sessions (no ServiceRequestId)
                        // CRITICAL: Don't use service request sessions for medical chats
                        var todaySession = existingSessions
                            .Where(s => s.IsActive && s.CreatedAt.Date == DateTime.UtcNow.Date && !s.ServiceRequestId.HasValue)
                            .OrderByDescending(s => s.LastActivityAt)
                            .FirstOrDefault();
                        
                        if (todaySession != null)
                        {
                            session = todaySession;
                            // Ensure serviceRequestId is null for medical chats
                            serviceRequestId = null;
                            _logger.LogInformation("Found existing medical chat session {SessionId} (no ServiceRequestId)", session.Id);
                        }
                    }
                }
                
                // If no existing session found, get or create one
                // CRITICAL: For medical chats (forceServiceRequestMode=false), ensure serviceRequestId is null
                if (session == null)
                {
                    var srIdToUse = forceServiceRequestMode ? serviceRequestId : null;
                    session = await _chatHistoryService.GetOrCreateSessionAsync(userId, patientId > 0 ? patientId : null, srIdToUse);
                    _logger.LogInformation("Created/retrieved session {SessionId} with ServiceRequestId={ServiceRequestId}, forceServiceRequestMode={ForceMode}", 
                        session.Id, session.ServiceRequestId, forceServiceRequestMode);
                }

                if (session == null)
                {
                    _logger.LogError("Failed to get or create chat session for userId: {UserId}, patientId: {PatientId}", userId, patientId);
                    throw new Exception("Failed to create or retrieve chat session");
                }
                
                // CRITICAL SAFEGUARD: If we're in Medical Chat mode but session has ServiceRequestId,
                // create a new medical session to prevent context mixing
                if (!forceServiceRequestMode && session.ServiceRequestId.HasValue && 
                    userRoleId == Shared.Constants.Roles.Patient && patientId == userId)
                {
                    _logger.LogWarning("Session {SessionId} has ServiceRequestId={ServiceRequestId} but user is in Medical Chat mode. Creating new medical session to prevent context mixing.", 
                        session.Id, session.ServiceRequestId);
                    // Create a new medical session (no ServiceRequestId)
                    session = await _chatHistoryService.GetOrCreateSessionAsync(userId, patientId > 0 ? patientId : null, null);
                    _logger.LogInformation("Created new medical session {SessionId} (no ServiceRequestId)", session.Id);
                }

                // Add user message to history
                var isMedicalData = ContainsMedicalData(prompt);
                var metadata = BuildMessageMetadata(userId, userRoleId, patientId);
                await _chatHistoryService.AddMessageAsync(session.Id, MessageRole.User, prompt, MessageType.Question, isMedicalData, metadata);

                // Check if we should use agentic AI for service requests
                // IMPORTANT: Only use agentic AI for service requests, NOT for medical chats
                // This preserves content analysis for medical questions
                // CRITICAL: The user's mode choice (forceServiceRequestMode) is the PRIMARY decision factor
                bool sessionHasServiceRequest = session.ServiceRequestId.HasValue;
                
                // CRITICAL: Respect the client's mode choice FIRST
                // - If forceServiceRequestMode=false (Medical Chat), NEVER use agentic AI, regardless of session state
                // - If forceServiceRequestMode=true (Service Request Chat), use agentic AI
                bool shouldUseAgenticAI = false;
                
                // ONLY consider agentic AI if user explicitly chose Service Request Chat mode
                if (forceServiceRequestMode && 
                    (serviceRequestId.HasValue || sessionHasServiceRequest) && 
                    userRoleId == Shared.Constants.Roles.Patient && 
                    patientId == userId && 
                    _agenticAIService != null)
                {
                    // User is in Service Request Chat mode - use agentic AI
                    shouldUseAgenticAI = true;
                    _logger.LogInformation("Using agentic AI: forceServiceRequestMode=true for session {SessionId} with ServiceRequestId={ServiceRequestId}", 
                        session.Id, session.ServiceRequestId);
                }
                else if (!forceServiceRequestMode && (serviceRequestId.HasValue || sessionHasServiceRequest))
                {
                    // User is in Medical Chat mode but session has ServiceRequestId
                    // This can happen if user switched from Service Request to Medical mode
                    // CRITICAL: Do NOT use agentic AI - use medical chat instead
                    _logger.LogInformation("NOT using agentic AI: user is in Medical Chat mode (forceServiceRequestMode=false) for session {SessionId} with ServiceRequestId={ServiceRequestId}. Using medical chat instead.", 
                        session.Id, session.ServiceRequestId);
                    shouldUseAgenticAI = false;
                }
                
                if (shouldUseAgenticAI)
                {
                    try
                    {
                        // Priority: 1) Selected SR from UI dropdown, 2) Let agentic AI determine from ClientAgentSession
                        // If user selected an SR from UI, use it and set it as active in ClientAgentSession
                        int? srIdToUse = selectedServiceRequestId;
                        if (selectedServiceRequestId.HasValue)
                        {
                            _logger.LogInformation("Using selected SR {ServiceRequestId} from UI dropdown for client {ClientId}", selectedServiceRequestId, patientId);
                            // Set it as active in ClientAgentSession (via agentic AI - it will handle this)
                        }
                        else
                        {
                            // No SR selected in UI - let agentic AI determine from ClientAgentSession (SR-first enforcement)
                            srIdToUse = null;
                            _logger.LogInformation("No SR selected in UI. Agentic AI will determine active SR from ClientAgentSession (SR-first enforcement).");
                        }
                        
                        // Get conversation history for context (with Redis caching)
                        string historyContext;
                        var cacheKey = $"chat:history:{session.Id}";
                        
                        // Try Redis cache first (if available)
                        if (_redisCache != null)
                        {
                            var cachedHistory = await _redisCache.GetAsync(cacheKey);
                            if (!string.IsNullOrEmpty(cachedHistory))
                            {
                                _logger.LogDebug("Using cached conversation history for session {SessionId}", session.Id);
                                historyContext = cachedHistory;
                            }
                            else
                            {
                                // Load from database and cache it
                                var conversationHistory = await _chatHistoryService.GetRecentMessagesAsync(session.Id, maxMessages: 10);
                                historyContext = string.Join("\n", conversationHistory
                                    .OrderBy(m => m.Timestamp)
                                    .Take(8) // Last 8 messages (4 exchanges)
                                    .Select(m => $"{m.Role}: {m.Content}"));
                                
                                // Cache for 1 hour (active chat sessions)
                                await _redisCache.SetAsync(cacheKey, historyContext, TimeSpan.FromHours(1));
                            }
                        }
                        else
                        {
                            // No Redis, load directly from database
                            var conversationHistory = await _chatHistoryService.GetRecentMessagesAsync(session.Id, maxMessages: 10);
                            historyContext = string.Join("\n", conversationHistory
                                .OrderBy(m => m.Timestamp)
                                .Take(8) // Last 8 messages (4 exchanges)
                                .Select(m => $"{m.Role}: {m.Content}"));
                        }
                        
                        // Use agentic AI for service request assistance with conversation context
                        // _agenticAIService is guaranteed to be non-null here due to the check above
                        var agenticResponse = await _agenticAIService!.ProcessServiceRequestAsync(
                            patientId,
                            prompt,
                            srIdToUse,
                            conversationHistory: historyContext);
                        
                        // Add AI response to history
                        await _chatHistoryService.AddMessageAsync(
                            session.Id, 
                            MessageRole.Assistant, 
                            agenticResponse.Message, 
                            MessageType.Response, 
                            false, 
                            null);
                        
                        // Invalidate Redis cache since we added a new message
                        if (_redisCache != null)
                        {
                            await _redisCache.RemoveAsync(cacheKey);
                            _logger.LogDebug("Invalidated conversation history cache for session {SessionId}", session.Id);
                        }
                        
                        await _chatHistoryService.UpdateSessionActivityAsync(session.Id);
                        
                        return new ChatResponse
                        {
                            Id = Guid.NewGuid().ToString(),
                            Message = agenticResponse.Message,
                            Provider = "AgenticAI"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error using agentic AI for session {SessionId} with ServiceRequestId {ServiceRequestId}", 
                            session.Id, session.ServiceRequestId);
                        
                        // CRITICAL: If session has ServiceRequestId, we MUST stay in service request mode
                        // Don't fall back to medical chat - return a service request appropriate error message
                        if (sessionHasServiceRequest)
                        {
                            _logger.LogWarning("Session has ServiceRequestId but agentic AI failed. Returning service request error message instead of falling back to medical chat.");
                            return new ChatResponse
                            {
                                Id = Guid.NewGuid().ToString(),
                                Message = "I apologize, but I'm having trouble processing your service request right now. Please try again in a moment, or contact support if the issue persists.",
                                Provider = "AgenticAI-Error"
                            };
                        }
                        
                        // Only fall back to regular chat if this wasn't a service request session
                        _logger.LogInformation("Falling back to regular chat (not a service request session)");
                    }
                }

                // Build role-based prompt with chat history context
                // This path is used for:
                // 1. Medical chats (content analysis preserved)
                // 2. Generic mode
                // 3. Doctor/Admin chats
                // 4. Fallback if agentic AI fails
                string roleBasedPrompt;
                if (isGenericMode)
                {
                    _logger.LogInformation("Using generic mode");
                    roleBasedPrompt = await BuildGenericPromptWithHistory(prompt, userRoleId, session.Id);
                }
                else
                {
                    _logger.LogInformation("Using role-based prompt for patient {PatientId}", patientId);
                    // Content analysis is preserved here for medical questions
                    roleBasedPrompt = await BuildRoleBasedPromptWithHistory(prompt, patientId, userId, userRoleId, session.Id);
                }

                // Use HuggingFace service for AI response
                // Content analysis context is included in roleBasedPrompt for medical questions
                var response = await _huggingFaceService.GenerateResponse(roleBasedPrompt, isGenericMode);

                // Add AI response to history
                await _chatHistoryService.AddMessageAsync(session.Id, MessageRole.Assistant, response, MessageType.Response, false, null);

                // Update session activity
                await _chatHistoryService.UpdateSessionActivityAsync(session.Id);

                // Generate a simple response ID
                var responseId = Guid.NewGuid().ToString();

                return new ChatResponse
                {
                    Id = responseId,
                    Message = response,
                    Provider = "HuggingFace"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessageAsync");
                // Return a friendly error message instead of crashing
                return new ChatResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = $"I'm having trouble processing your request right now. Please try again later. (Error: {ex.Message})",
                    Provider = "HuggingFace"
                };
            }
        }

        private async Task<string> BuildRoleBasedPrompt(string originalPrompt, int patientId, int userId, int userRoleId)
        {
            try
            {
                _logger.LogInformation("Original prompt: {OriginalPrompt}", originalPrompt);
                _logger.LogInformation("PatientId: {PatientId}, UserId: {UserId}, UserRoleId: {UserRoleId}", patientId, userId, userRoleId);

                // If role is 0 or unknown, try to determine role from user data
                if (userRoleId == 0 && userId > 0)
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        userRoleId = user.RoleId;
                        _logger.LogInformation("Determined user role from database: {UserRoleId}", userRoleId);
                    }
                }

                // Role-based prompt building
                if (userRoleId == 1) // Patient
                {
                    _logger.LogInformation("Building patient prompt for user {UserId}", userId);
                    return await BuildPatientPrompt(originalPrompt, userId);
                }
                else if (userRoleId == 2) // Doctor
                {
                    _logger.LogInformation("Building doctor prompt for doctor {UserId}, patient {PatientId}", userId, patientId);
                    _logger.LogInformation("About to call BuildDoctorPrompt with patientId={PatientId}", patientId);
                    _logger.LogInformation("Calling BuildDoctorPrompt now...");
                    var result = await BuildDoctorPrompt(originalPrompt, userId, patientId);
                    _logger.LogInformation("BuildDoctorPrompt completed, result length: {Length}", result.Length);
                    return result;
                }
                else if (userRoleId == 3) // Admin
                {
                    _logger.LogInformation("Building admin prompt for admin {UserId}, patient {PatientId}", userId, patientId);
                    return await BuildAdminPrompt(originalPrompt, userId, patientId);
                }
                else
                {
                    _logger.LogWarning("Unknown user role {UserRoleId}, falling back to patient prompt", userRoleId);
                    // Default to patient prompt for unknown roles
                    return await BuildPatientPrompt(originalPrompt, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building role-based prompt");
                return originalPrompt;
            }
        }

        private async Task<string> BuildPatientPrompt(string originalPrompt, int userId)
        {
            var context = new System.Text.StringBuilder();

            // Get patient information
            var patient = await _userService.GetUserByIdAsync(userId);
            if (patient != null)
            {
                context.AppendLine($"You are a health companion talking to {patient.FirstName} {patient.LastName}.");
            }
            else
            {
                context.AppendLine("You are a health companion talking to a patient.");
            }

            // Patient-specific instructions - conservative and supportive
            context.AppendLine("\nIMPORTANT GUIDELINES FOR CLIENT RESPONSES:");
            context.AppendLine("- You are NOT a doctor and cannot provide medical advice, diagnoses, or prescriptions");
            context.AppendLine("- For any medical concerns, symptoms, or requests for prescriptions, ALWAYS direct them to consult their doctor");
            context.AppendLine("- You can provide general wellness advice like exercise, diet, sleep, and stress management");
            context.AppendLine("- You can offer emotional support and active listening");
            context.AppendLine("- You can suggest relaxation techniques, breathing exercises, and mindfulness");
            context.AppendLine("- If they mention specific symptoms or ask for medical advice, respond with: 'I understand your concern, but I'm not qualified to provide medical advice. Please consult with your doctor about this.'");
            context.AppendLine("- Be supportive, empathetic, and encouraging");
            context.AppendLine("- Keep responses conversational and non-clinical");

            // Use intelligent context service for smart question processing
            try
            {
                _logger.LogInformation("=== USING INTELLIGENT CONTEXT SERVICE FOR CLIENT ===");
                _logger.LogInformation("Patient ID: {PatientId}, User ID: {UserId}, Original Prompt: {OriginalPrompt}", userId, userId, originalPrompt);
                var intelligentContext = await _intelligentContextService.ProcessQuestionAsync(originalPrompt, userId, userId);
                _logger.LogInformation("Intelligent context length: {Length}", intelligentContext.Length);
                _logger.LogInformation("Intelligent context preview: {Preview}", intelligentContext.Substring(0, Math.Min(200, intelligentContext.Length)));
                context.AppendLine($"\n{intelligentContext}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using intelligent context service for patient, falling back to basic context");
                // Fallback to basic context
                context.AppendLine($"\nPatient asks: {originalPrompt}");
                context.AppendLine("\nRespond as a supportive health companion, following the guidelines above.");
            }

            var prompt = context.ToString();
            return prompt;
        }

        private async Task<string> BuildDoctorPrompt(string originalPrompt, int doctorId, int patientId)
        {
            var context = new System.Text.StringBuilder();

            // Get doctor information
            var doctor = await _userService.GetUserByIdAsync(doctorId);
            if (doctor != null)
            {
                context.AppendLine($"You are an AI assistant helping Dr. {doctor.FirstName} {doctor.LastName} with patient care.");
            }
            else
            {
                context.AppendLine("You are an AI assistant helping a doctor with patient care.");
            }

            // Get patient information for basic context (detailed data handled by ContentAnalysisService)
            if (patientId > 0)
            {
                var patient = await _userService.GetUserByIdAsync(patientId);
                if (patient != null)
                {
                    context.AppendLine($"\nPATIENT INFORMATION:");
                    context.AppendLine($"Name: {patient.FirstName} {patient.LastName}");
                    context.AppendLine($"Age: {CalculateAge(patient.DateOfBirth)}");
                    context.AppendLine($"Gender: {patient.Gender}");
                    context.AppendLine($"Status: {(patient.IsActive ? "Active" : "Inactive")}");
                }
            }

            // Doctor-specific instructions - clinical and professional
            context.AppendLine("\nDOCTOR ASSISTANCE GUIDELINES:");
            context.AppendLine("- Provide clinical insights and evidence-based recommendations");
            context.AppendLine("- Analyze patient data and identify relevant patterns");
            context.AppendLine("- Suggest potential diagnoses based on available information");
            context.AppendLine("- Recommend appropriate treatment approaches and interventions");
            context.AppendLine("- Provide medication considerations (remind doctor to verify with current guidelines)");
            context.AppendLine("- Suggest relevant follow-up questions for the patient");
            context.AppendLine("- Maintain a professional, clinical tone throughout");
            context.AppendLine("- Be concise and avoid repetitive information");
            context.AppendLine("- Always remind that this is AI assistance and final decisions are the doctor's responsibility");

            // ALWAYS use direct content analysis for medical questions to ensure progression analysis works
            _logger.LogInformation("=== FORCING DIRECT CONTENT ANALYSIS ===");
            _logger.LogInformation("Patient ID: {PatientId}, Doctor ID: {DoctorId}, Original Prompt: {OriginalPrompt}", patientId, doctorId, originalPrompt);

            try
            {
                var enhancedContext = await _contentAnalysisService.BuildEnhancedContextAsync(patientId, originalPrompt);
                _logger.LogInformation("Enhanced context length: {Length}", enhancedContext.Length);
                _logger.LogInformation("Enhanced context preview: {Preview}", enhancedContext.Substring(0, Math.Min(500, enhancedContext.Length)));
                context.AppendLine($"\n{enhancedContext}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using content analysis service, falling back to intelligent context");
                var intelligentContext = await _intelligentContextService.ProcessQuestionAsync(originalPrompt, patientId, doctorId);
                context.AppendLine($"\n{intelligentContext}");
            }

            var prompt = context.ToString();
            return prompt;
        }

        private async Task<string> BuildAdminPrompt(string originalPrompt, int adminId, int patientId)
        {
            var context = new System.Text.StringBuilder();

            context.AppendLine("You are an AI assistant helping an administrator with system management and patient oversight.");

            // Admin-specific instructions
            context.AppendLine("\nADMIN ASSISTANCE GUIDELINES:");
            context.AppendLine("- You can provide insights about patient trends and patterns");
            context.AppendLine("- You can suggest system improvements and monitoring approaches");
            context.AppendLine("- You can help with data analysis and reporting insights");
            context.AppendLine("- You can provide general health information for administrative purposes");
            context.AppendLine("- Be professional and administrative in tone");
            context.AppendLine("- Focus on system-wide insights rather than individual patient care");

            context.AppendLine($"\nAdmin asks: {originalPrompt}");
            context.AppendLine("\nRespond as an administrative AI assistant.");

            var prompt = context.ToString();

            return prompt;
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }

        private async Task<string> BuildPersonalizedPrompt(string originalPrompt, int patientId)
        {
            if (patientId <= 0)
            {
                return originalPrompt;
            }

            try
            {
                // Get patient information
                var patient = await _userService.GetUserByIdAsync(patientId);
                if (patient == null)
                {
                    return originalPrompt;
                }

                // Use enhanced context that includes both journal entries AND content analysis
                var enhancedContext = await _contentAnalysisService.BuildEnhancedContextAsync(patientId, originalPrompt);

                return enhancedContext;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building enhanced prompt: {ex.Message}");
                // Fallback to original personalized prompt
                return await BuildFallbackPersonalizedPrompt(originalPrompt, patientId);
            }
        }

        private async Task<string> BuildFallbackPersonalizedPrompt(string originalPrompt, int patientId)
        {
            try
            {
                // Get recent journal entries for context (fallback)
                var recentEntries = await _journalService.GetRecentEntriesForUser(patientId, 7);
                var moodDistribution = await _journalService.GetMoodDistributionForUser(patientId, 30);

                var context = new System.Text.StringBuilder();
                context.AppendLine($"You are talking to a patient (ID: {patientId}).");

                if (moodDistribution.Any())
                {
                    var topMoods = moodDistribution.OrderByDescending(kvp => kvp.Value).Take(2);
                    context.AppendLine($"Their recent journalentry patterns: {string.Join(", ", topMoods.Select(kvp => $"{kvp.Key} ({kvp.Value} times)"))}");
                }

                if (recentEntries.Any())
                {
                    var latestEntry = recentEntries.First();
                    context.AppendLine($"Their latest journal entry ({latestEntry.CreatedAt:MM/dd}): {latestEntry.Mood} - {latestEntry.Text.Substring(0, Math.Min(80, latestEntry.Text.Length))}...");
                }

                context.AppendLine($"\nUser asks: {originalPrompt}");
                context.AppendLine("Respond as a health companion with personalized context.");

                return context.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building fallback prompt: {ex.Message}");
                return originalPrompt;
            }
        }

        public async Task<ChatResponse> SendRegularMessageAsync(string prompt, string conversationId, AiProvider provider)
        {
            try
            {
                // Parse conversationId to Guid
                if (!Guid.TryParse(conversationId, out Guid guidConversationId))
                {
                    guidConversationId = Guid.NewGuid();
                }

                // Use HuggingFace service for AI response
                var response = await _huggingFaceService.GenerateResponse(prompt);

                // Generate a simple response ID
                var responseId = Guid.NewGuid().ToString();

                return new ChatResponse
                {
                    Id = responseId,
                    Message = response,
                    Provider = "HuggingFace"
                };
            }
            catch (Exception ex)
            {
                // Return a friendly error message instead of crashing
                return new ChatResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = $"I'm having trouble processing your request right now. Please try again later. (Error: {ex.Message})",
                    Provider = "HuggingFace"
                };
            }
        }

        private async Task<string> BuildRoleBasedPromptWithHistory(string originalPrompt, int patientId, int userId, int userRoleId, int sessionId)
        {
            try
            {
                // Build the base prompt
                var basePrompt = await BuildRoleBasedPrompt(originalPrompt, patientId, userId, userRoleId);

                // Check if there are emergency incidents for this patient
                var hasEmergencies = await _context.EmergencyIncidents
                    .AnyAsync(e => e.PatientId == patientId && !e.IsAcknowledged);

                var contextBuilder = new System.Text.StringBuilder();
                contextBuilder.AppendLine(basePrompt);

                // If there are emergency incidents, SKIP conversation history entirely
                if (hasEmergencies)
                {
                    _logger.LogInformation("Emergency incidents detected for patient {PatientId}, skipping conversation history", patientId);
                    contextBuilder.AppendLine("\n=== EMERGENCY MODE ===");
                    contextBuilder.AppendLine("START YOUR RESPONSE WITH: 🚨 CRITICAL EMERGENCY ALERT:");
                    contextBuilder.AppendLine("THEN LIST THE EMERGENCY INCIDENTS");
                    contextBuilder.AppendLine("THEN DISCUSS OTHER MEDICAL DATA");
                }
                else
                {
                    // Only add conversation history if no emergency incidents
                    var conversationContext = await _chatHistoryService.BuildConversationContextAsync(sessionId);
                    if (!string.IsNullOrEmpty(conversationContext))
                    {
                        contextBuilder.AppendLine("\n=== CONVERSATION HISTORY ===");
                        contextBuilder.AppendLine(conversationContext);
                    }
                }

                return contextBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building role-based prompt with history");
                return await BuildRoleBasedPrompt(originalPrompt, patientId, userId, userRoleId);
            }
        }

        private async Task<string> BuildGenericPromptWithHistory(string originalPrompt, int userRoleId, int sessionId)
        {
            try
            {
                // For generic mode, we want a fresh start without medical-focused conversation history
                // This ensures the AI responds like ChatGPT rather than continuing medical patterns
                var basePrompt = BuildGenericPrompt(originalPrompt, userRoleId);

                // Only include very recent conversation context (last 3 messages) to maintain some continuity
                // but avoid the medical pattern repetition
                var recentMessages = await _chatHistoryService.GetRecentMessagesAsync(sessionId, 3);

                var contextBuilder = new System.Text.StringBuilder();
                contextBuilder.AppendLine(basePrompt);

                if (recentMessages.Any())
                {
                    contextBuilder.AppendLine("\n=== RECENT CONVERSATION ===");
                    foreach (var message in recentMessages)
                    {
                        var rolePrefix = message.Role switch
                        {
                            MessageRole.User => "User",
                            MessageRole.Assistant => "AI",
                            _ => "System"
                        };
                        contextBuilder.AppendLine($"{rolePrefix}: {message.Content}");
                    }
                }

                return contextBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building generic prompt with history");
                return BuildGenericPrompt(originalPrompt, userRoleId);
            }
        }

        private bool ContainsMedicalData(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;

            var medicalKeywords = new[]
            {
                "blood pressure", "bp", "hemoglobin", "triglycerides", "cholesterol",
                "heart rate", "temperature", "pulse", "oxygen", "glucose", "sugar",
                "medication", "prescription", "dosage", "mg", "ml", "tablet", "capsule",
                "symptom", "pain", "ache", "fever", "nausea", "dizziness", "fatigue",
                "diagnosis", "condition", "disease", "illness", "treatment", "therapy"
            };

            var lowerContent = content.ToLower();
            return medicalKeywords.Any(keyword => lowerContent.Contains(keyword));
        }

        private string BuildMessageMetadata(int userId, int userRoleId, int patientId)
        {
            var metadata = new
            {
                UserId = userId,
                UserRoleId = userRoleId,
                PatientId = patientId > 0 ? patientId : (int?)null,
                Timestamp = DateTime.UtcNow
            };

            return System.Text.Json.JsonSerializer.Serialize(metadata);
        }

        private string BuildGenericPrompt(string originalPrompt, int userRoleId)
        {
            var context = new System.Text.StringBuilder();

            // Get user information
            var user = _userService.GetUserByIdAsync(userRoleId == 2 ? 2 : 3).Result; // Default to doctor or admin
            if (user != null)
            {
                context.AppendLine($"Hello! I'm your AI assistant. I can help you with any topic - medical research, general knowledge, technical questions, writing, analysis, or any other questions you have. How can I assist you today?");
            }
            else
            {
                context.AppendLine("Hello! I'm your AI assistant. I can help you with any topic - medical research, general knowledge, technical questions, writing, analysis, or any other questions you have. How can I assist you today?");
            }

            // Direct question without medical disclaimers
            context.AppendLine($"\nUser question: {originalPrompt}");
            context.AppendLine("\nPlease provide a helpful, informative response to this question.");

            return context.ToString();
        }

    }
}
