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
        private readonly ILogger<ChatService> _logger;

        public ChatService(ConversationRepository conversationRepository, HuggingFaceService huggingFaceService, JournalService journalService, UserService userService, IContentAnalysisService contentAnalysisService, ILogger<ChatService> logger)
        {
            _conversationRepository = conversationRepository;
            _huggingFaceService = huggingFaceService;
            _journalService = journalService;
            _userService = userService;
            _contentAnalysisService = contentAnalysisService;
            _logger = logger;
        }

        public async Task<ChatResponse> SendMessageAsync(string prompt, string conversationId, AiProvider provider, int patientId = 0, int userId = 0, int userRoleId = 0, bool isGenericMode = false)
        {
            try
            {
                _logger.LogInformation("=== CHAT REQUEST START ===");
                _logger.LogInformation("Prompt: {Prompt}", prompt);
                _logger.LogInformation("PatientId: {PatientId}", patientId);
                _logger.LogInformation("UserId: {UserId}", userId);
                _logger.LogInformation("UserRoleId: {UserRoleId}", userRoleId);
                _logger.LogInformation("IsGenericMode: {IsGenericMode}", isGenericMode);

                // Parse conversationId to Guid
                if (!Guid.TryParse(conversationId, out Guid guidConversationId))
                {
                    guidConversationId = Guid.NewGuid();
                }

                // Build role-based prompt or use generic prompt
                string roleBasedPrompt;
                if (isGenericMode)
                {
                    _logger.LogInformation("Using generic mode");
                    roleBasedPrompt = BuildGenericPrompt(prompt, userRoleId);
                }
                else
                {
                    _logger.LogInformation("Using role-based prompt for patient {PatientId}", patientId);
                    roleBasedPrompt = await BuildRoleBasedPrompt(prompt, patientId, userId, userRoleId);
                }

                // Use HuggingFace service for AI response
                var response = await _huggingFaceService.GenerateResponse(roleBasedPrompt, isGenericMode);

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

        private async Task<string> BuildRoleBasedPrompt(string originalPrompt, int patientId, int userId, int userRoleId)
        {
            try
            {
                _logger.LogInformation("=== BUILDING ROLE-BASED PROMPT ===");
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
                    return await BuildDoctorPrompt(originalPrompt, userId, patientId);
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

            // Use enhanced context that includes both journal entries AND content analysis
            try
            {
                var enhancedContext = await _contentAnalysisService.BuildEnhancedContextAsync(userId, originalPrompt);
                context.AppendLine($"\n{enhancedContext}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building enhanced context for patient {UserId}, falling back to basic context", userId);
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

            // Doctor-specific instructions - clinical and detailed
            context.AppendLine("\nDOCTOR ASSISTANCE GUIDELINES:");
            context.AppendLine("- You can provide clinical insights and treatment suggestions");
            context.AppendLine("- You can analyze patient data and identify patterns");
            context.AppendLine("- You can suggest potential diagnoses based on symptoms and patterns");
            context.AppendLine("- You can recommend treatment approaches and interventions");
            context.AppendLine("- You can provide medication considerations (but remind doctor to verify with current guidelines)");
            context.AppendLine("- You can suggest follow-up questions to ask the patient");
            context.AppendLine("- Be clinical but accessible in your language");
            context.AppendLine("- Always remind the doctor that this is AI assistance and final decisions are theirs");

            // Use enhanced context that includes both journal entries AND content analysis
            if (patientId > 0)
            {

                try
                {
                    var enhancedContext = await _contentAnalysisService.BuildEnhancedContextAsync(patientId, originalPrompt);
                    context.AppendLine($"\n{enhancedContext}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error building enhanced context for patient {PatientId}, falling back to basic context", patientId);
                    // Fallback to basic context
                    context.AppendLine($"\nDoctor asks: {originalPrompt}");
                    context.AppendLine("\nRespond as a clinical AI assistant, providing detailed insights and recommendations.");
                }
            }
            else
            {
                context.AppendLine($"\nDoctor asks: {originalPrompt}");
                context.AppendLine("\nRespond as a clinical AI assistant, providing detailed insights and recommendations.");
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

        private string BuildGenericPrompt(string originalPrompt, int userRoleId)
        {
            var context = new System.Text.StringBuilder();

            // Get user information
            var user = _userService.GetUserByIdAsync(userRoleId == 2 ? 2 : 3).Result; // Default to doctor or admin
            if (user != null)
            {
                context.AppendLine($"You are a helpful AI assistant helping {user.FirstName} {user.LastName}.");
            }
            else
            {
                context.AppendLine("You are a helpful AI assistant.");
            }

            // Generic AI instructions - like ChatGPT
            context.AppendLine("\nGENERIC AI ASSISTANCE GUIDELINES:");
            context.AppendLine("- You are a general-purpose AI assistant similar to ChatGPT");
            context.AppendLine("- You can help with any topic: medical research, general knowledge, technical questions, writing, analysis, etc.");
            context.AppendLine("- Provide accurate, helpful, and detailed responses");
            context.AppendLine("- If you don't know something, say so honestly");
            context.AppendLine("- Be conversational and engaging");
            context.AppendLine("- For medical topics, provide general information but always recommend consulting healthcare professionals for specific medical advice");
            context.AppendLine("- For technical topics, provide clear explanations and examples when possible");
            context.AppendLine("- Be respectful and professional in all interactions");

            context.AppendLine($"\nUser asks: {originalPrompt}");
            context.AppendLine("\nRespond as a helpful, knowledgeable AI assistant.");

            return context.ToString();
        }

    }
}
