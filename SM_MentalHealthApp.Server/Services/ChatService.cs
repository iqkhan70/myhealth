using System;
using System.Threading.Tasks;
using SM_MentalHealthApp.Server.Services;
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
        private readonly ILogger<ChatService> _logger;

        public ChatService(ConversationRepository conversationRepository, HuggingFaceService huggingFaceService, JournalService journalService, UserService userService, IContentAnalysisService contentAnalysisService, IIntelligentContextService intelligentContextService, IChatHistoryService chatHistoryService, ILogger<ChatService> logger)
        {
            _conversationRepository = conversationRepository;
            _huggingFaceService = huggingFaceService;
            _journalService = journalService;
            _userService = userService;
            _contentAnalysisService = contentAnalysisService;
            _intelligentContextService = intelligentContextService;
            _chatHistoryService = chatHistoryService;
            _logger = logger;
        }

        public async Task<ChatResponse> SendMessageAsync(string prompt, string conversationId, AiProvider provider, int patientId = 0, int userId = 0, int userRoleId = 0, bool isGenericMode = false)
        {
            try
            {
                _logger.LogInformation("Prompt: {Prompt}", prompt);
                _logger.LogInformation("PatientId: {PatientId}", patientId);
                _logger.LogInformation("UserId: {UserId}", userId);
                _logger.LogInformation("UserRoleId: {UserRoleId}", userRoleId);
                _logger.LogInformation("IsGenericMode: {IsGenericMode}", isGenericMode);

                // Get or create chat session
                var session = await _chatHistoryService.GetOrCreateSessionAsync(userId, patientId > 0 ? patientId : null);

                // Add user message to history
                var isMedicalData = ContainsMedicalData(prompt);
                var metadata = BuildMessageMetadata(userId, userRoleId, patientId);
                await _chatHistoryService.AddMessageAsync(session.Id, MessageRole.User, prompt, MessageType.Question, isMedicalData, metadata);

                // Build role-based prompt with chat history context
                string roleBasedPrompt;
                if (isGenericMode)
                {
                    _logger.LogInformation("Using generic mode");
                    roleBasedPrompt = await BuildGenericPromptWithHistory(prompt, userRoleId, session.Id);
                }
                else
                {
                    _logger.LogInformation("Using role-based prompt for patient {PatientId}", patientId);
                    roleBasedPrompt = await BuildRoleBasedPromptWithHistory(prompt, patientId, userId, userRoleId, session.Id);
                }

                // Use HuggingFace service for AI response
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
                context.AppendLine($"You are a mental health companion talking to {patient.FirstName} {patient.LastName}.");
            }
            else
            {
                context.AppendLine("You are a mental health companion talking to a patient.");
            }

            // Patient-specific instructions - conservative and supportive
            context.AppendLine("\nIMPORTANT GUIDELINES FOR PATIENT RESPONSES:");
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
                _logger.LogInformation("=== USING INTELLIGENT CONTEXT SERVICE FOR PATIENT ===");
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
                context.AppendLine("\nRespond as a supportive mental health companion, following the guidelines above.");
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
            context.AppendLine("- You can provide general mental health information for administrative purposes");
            context.AppendLine("- Be professional and administrative in tone");
            context.AppendLine("- Focus on system-wide insights rather than individual patient care");

            context.AppendLine($"\nAdmin asks: {originalPrompt}");
            context.AppendLine("\nRespond as an administrative AI assistant.");

            var prompt = context.ToString();
            Console.WriteLine($"=== ADMIN PROMPT ===");
            Console.WriteLine(prompt);
            Console.WriteLine("=== END ADMIN PROMPT ===");

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

                Console.WriteLine($"=== ENHANCED CONTEXT FOR PATIENT {patientId} ===");
                Console.WriteLine(enhancedContext);
                Console.WriteLine("=== END ENHANCED CONTEXT ===");

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
                    context.AppendLine($"Their recent mood patterns: {string.Join(", ", topMoods.Select(kvp => $"{kvp.Key} ({kvp.Value} times)"))}");
                }

                if (recentEntries.Any())
                {
                    var latestEntry = recentEntries.First();
                    context.AppendLine($"Their latest journal entry ({latestEntry.CreatedAt:MM/dd}): {latestEntry.Mood} - {latestEntry.Text.Substring(0, Math.Min(80, latestEntry.Text.Length))}...");
                }

                context.AppendLine($"\nUser asks: {originalPrompt}");
                context.AppendLine("Respond as a mental health companion with personalized context.");

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
                // Get conversation context from chat history
                var conversationContext = await _chatHistoryService.BuildConversationContextAsync(sessionId);

                // Build the base prompt
                var basePrompt = await BuildRoleBasedPrompt(originalPrompt, patientId, userId, userRoleId);

                // Combine with conversation context
                var contextBuilder = new System.Text.StringBuilder();
                contextBuilder.AppendLine(basePrompt);

                if (!string.IsNullOrEmpty(conversationContext))
                {
                    contextBuilder.AppendLine("\n=== CONVERSATION HISTORY ===");
                    contextBuilder.AppendLine(conversationContext);
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
