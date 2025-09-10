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
        private readonly PatientService _patientService;

        public ChatService(ConversationRepository conversationRepository, HuggingFaceService huggingFaceService, JournalService journalService, PatientService patientService)
        {
            _conversationRepository = conversationRepository;
            _huggingFaceService = huggingFaceService;
            _journalService = journalService;
            _patientService = patientService;
        }

        public async Task<ChatResponse> SendMessageAsync(string prompt, string conversationId, AiProvider provider, int patientId = 0)
        {
            try
            {
                Console.WriteLine($"ChatService.SendMessageAsync called with prompt: '{prompt}' for patient: {patientId}");
                
                // Parse conversationId to Guid
                if (!Guid.TryParse(conversationId, out Guid guidConversationId))
                {
                    guidConversationId = Guid.NewGuid();
                }

                // Get patient context for personalized responses
                string personalizedPrompt = await BuildPersonalizedPrompt(prompt, patientId);

                // Use HuggingFace service for AI response
                var response = await _huggingFaceService.GenerateResponse(personalizedPrompt);
                Console.WriteLine($"HuggingFace response: '{response}'");

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
                Console.WriteLine($"Error in SendMessageAsync: {ex.Message}");
                // Return a friendly error message instead of crashing
                return new ChatResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Message = $"I'm having trouble processing your request right now. Please try again later. (Error: {ex.Message})",
                    Provider = "HuggingFace"
                };
            }
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
                var patient = await _patientService.GetPatientByIdAsync(patientId);
                if (patient == null)
                {
                    return originalPrompt;
                }

                // Get recent journal entries for context
                var recentEntries = await _journalService.GetRecentEntriesForPatient(patientId, 7); // Last 7 days
                var moodDistribution = await _journalService.GetMoodDistributionForPatient(patientId, 30); // Last 30 days

                // Build personalized context - simplified for better API compatibility
                var context = new System.Text.StringBuilder();
                context.AppendLine($"You are talking to {patient.FirstName} {patient.LastName}.");
                
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

                var personalizedPrompt = context.ToString();
                Console.WriteLine($"=== PERSONALIZED PROMPT FOR PATIENT {patientId} ===");
                Console.WriteLine(personalizedPrompt);
                Console.WriteLine("=== END PERSONALIZED PROMPT ===");
                
                return personalizedPrompt;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building personalized prompt: {ex.Message}");
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

    }
}
