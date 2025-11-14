using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Text.Json;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IChatHistoryService
    {
        Task<ChatSession> GetOrCreateSessionAsync(int userId, int? patientId = null);
        Task<ChatMessage> AddMessageAsync(int sessionId, MessageRole role, string content, MessageType messageType = MessageType.Question, bool isMedicalData = false, string? metadata = null);
        Task<List<ChatMessage>> GetRecentMessagesAsync(int sessionId, int maxMessages = 20, JournalDbContext? dbContext = null);
        Task<string> BuildConversationContextAsync(int sessionId, JournalDbContext? dbContext = null);
        Task UpdateSessionActivityAsync(int sessionId);
        Task CleanupExpiredDataAsync();
        Task<ChatSession?> GetSessionAsync(int sessionId);
        Task<List<ChatSession>> GetUserSessionsAsync(int userId, int? patientId = null);
        Task DeleteSessionAsync(int sessionId);
        Task GenerateSessionSummaryAsync(int sessionId);
        Task ToggleIgnoreAsync(int sessionId, int doctorId);
    }

    public class ChatHistoryService : IChatHistoryService
    {
        private readonly JournalDbContext _context;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ChatHistoryService> _logger;
        private const int MAX_CONTEXT_MESSAGES = 20;
        private const int SUMMARY_THRESHOLD = 50;

        public ChatHistoryService(JournalDbContext context, IServiceScopeFactory serviceScopeFactory, ILogger<ChatHistoryService> logger)
        {
            _context = context;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task<ChatSession> GetOrCreateSessionAsync(int userId, int? patientId = null)
        {
            try
            {
                // Check if there's an active, non-ignored session for TODAY only
                // If a session is ignored, we should create a new session for new messages
                // This ensures that new messages after ignoring are not excluded from AI analysis
                // IMPORTANT: If multiple non-ignored sessions exist for the same day, use the most recently active one
                // This handles the case where Session1 was ignored, Session2 was created, then Session1 was unignored
                // In that case, Session2 should continue to be used (it's the most recent)
                var today = DateTime.UtcNow.Date;
                var existingSession = await _context.ChatSessions
                    .Where(s => s.UserId == userId &&
                               s.PatientId == patientId &&
                               s.IsActive &&
                               !s.IsIgnoredByDoctor && // Exclude ignored sessions
                               s.CreatedAt.Date == today)
                    .OrderByDescending(s => s.LastActivityAt) // Use most recently active session
                    .ThenByDescending(s => s.CreatedAt) // If same activity time, use most recently created
                    .FirstOrDefaultAsync();

                if (existingSession != null)
                {
                    _logger.LogInformation("Found existing active, non-ignored session for today {SessionId} for user {UserId}, patient {PatientId} (LastActivity: {LastActivity})",
                        existingSession.SessionId, userId, patientId, existingSession.LastActivityAt);
                    return existingSession;
                }

                // Only deactivate sessions from PREVIOUS days (not today)
                // IsActive should only be set to false for soft delete, not when creating a new session after ignoring
                // Ignored sessions should remain active - they're just marked as ignored for AI analysis exclusion
                var previousDaySessions = await _context.ChatSessions
                    .Where(s => s.UserId == userId &&
                               s.PatientId == patientId &&
                               s.IsActive &&
                               s.CreatedAt.Date < today) // Only sessions from previous days
                    .ToListAsync();

                foreach (var oldSession in previousDaySessions)
                {
                    oldSession.IsActive = false;

                    // Generate summary for deactivated sessions that don't have one
                    if (string.IsNullOrEmpty(oldSession.Summary) && oldSession.MessageCount > 0)
                    {
                        _ = Task.Run(async () => await GenerateSessionSummaryAsync(oldSession.Id));
                    }
                }

                // Create new session for today
                var sessionId = Guid.NewGuid().ToString();
                var newSession = new ChatSession
                {
                    UserId = userId,
                    PatientId = patientId,
                    SessionId = sessionId,
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IsActive = true,
                    PrivacyLevel = PrivacyLevel.Full,
                    MessageCount = 0
                };

                _context.ChatSessions.Add(newSession);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new session {SessionId} for user {UserId}, patient {PatientId}",
                    sessionId, userId, patientId);

                return newSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating session for user {UserId}, patient {PatientId}", userId, patientId);
                throw;
            }
        }

        public async Task<ChatMessage> AddMessageAsync(int sessionId, MessageRole role, string content, MessageType messageType = MessageType.Question, bool isMedicalData = false, string? metadata = null)
        {
            try
            {
                var message = new ChatMessage
                {
                    SessionId = sessionId,
                    Role = role,
                    Content = content,
                    MessageType = messageType,
                    IsMedicalData = isMedicalData,
                    Metadata = metadata,
                    Timestamp = DateTime.UtcNow
                };

                _context.ChatMessages.Add(message);

                // Update session activity and message count
                var session = await _context.ChatSessions.FindAsync(sessionId);
                if (session != null)
                {
                    session.LastActivityAt = DateTime.UtcNow;
                    session.MessageCount++;
                }

                await _context.SaveChangesAsync();

                // Generate summary if session has enough messages and no summary exists
                if (session.MessageCount >= 4 && string.IsNullOrEmpty(session.Summary))
                {
                    // Generate summary asynchronously to avoid blocking the response
                    _ = Task.Run(async () => await GenerateSessionSummaryAsync(sessionId));
                }

                _logger.LogInformation("Added message to session {SessionId}, role: {Role}, type: {MessageType}, medical: {IsMedicalData}",
                    sessionId, role, messageType, isMedicalData);

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding message to session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<List<ChatMessage>> GetRecentMessagesAsync(int sessionId, int maxMessages = 20, JournalDbContext? dbContext = null)
        {
            try
            {
                var context = dbContext ?? _context;
                var messages = await context.ChatMessages
                    .Where(m => m.SessionId == sessionId && m.IsActive)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(maxMessages)
                    .OrderBy(m => m.Timestamp) // Re-order chronologically for context
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} recent messages for session {SessionId}", messages.Count, sessionId);
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent messages for session {SessionId}", sessionId);
                return new List<ChatMessage>();
            }
        }

        public async Task<string> BuildConversationContextAsync(int sessionId, JournalDbContext? dbContext = null)
        {
            try
            {
                // Use provided context or fall back to injected context
                var context = dbContext ?? _context;

                var session = await context.ChatSessions
                    .Include(s => s.Patient)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive);

                if (session == null)
                {
                    _logger.LogWarning("Session {SessionId} not found", sessionId);
                    return string.Empty;
                }

                var contextBuilder = new List<string>();

                // Add session summary if available
                if (!string.IsNullOrEmpty(session.Summary))
                {
                    contextBuilder.Add($"=== CONVERSATION SUMMARY ===\n{session.Summary}\n");
                }

                // Add recent messages (filter out old medical alerts)
                var recentMessages = await GetRecentMessagesAsync(sessionId, MAX_CONTEXT_MESSAGES, context);
                if (recentMessages.Any())
                {
                    contextBuilder.Add("=== RECENT CONVERSATION ===\n");
                    foreach (var message in recentMessages)
                    {
                        // Filter out old AI responses that ignore emergency incidents
                        if (message.Role == MessageRole.Assistant &&
                            (message.Content.Contains("IMPROVEMENT NOTED") ||
                             message.Content.Contains("DETERIORATION NOTED")) &&
                            !message.Content.Contains("EMERGENCY") &&
                            !message.Content.Contains("Fall") &&
                            !message.Content.Contains("CRITICAL ALERT"))
                        {
                            // Skip old AI responses that don't prioritize emergency incidents
                            continue;
                        }

                        var rolePrefix = message.Role switch
                        {
                            MessageRole.User => "User",
                            MessageRole.Assistant => "AI",
                            MessageRole.System => "System",
                            _ => "Unknown"
                        };

                        var medicalFlag = message.IsMedicalData ? " [MEDICAL DATA]" : "";
                        contextBuilder.Add($"{rolePrefix}{medicalFlag}: {message.Content}");
                    }
                }

                var conversationContext = string.Join("\n", contextBuilder);
                _logger.LogInformation("Built conversation context for session {SessionId}, length: {Length}", sessionId, conversationContext.Length);
                return conversationContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building conversation context for session {SessionId}", sessionId);
                return string.Empty;
            }
        }

        public async Task UpdateSessionActivityAsync(int sessionId)
        {
            try
            {
                var session = await _context.ChatSessions.FindAsync(sessionId);
                if (session != null)
                {
                    // Only update LastActivityAt if the session was created today
                    var today = DateTime.UtcNow.Date;
                    if (session.CreatedAt.Date == today)
                    {
                        session.LastActivityAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session activity for session {SessionId}", sessionId);
            }
        }

        public async Task CleanupExpiredDataAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep messages for 30 days
                var summaryCutoffDate = DateTime.UtcNow.AddDays(-90); // Keep summaries for 90 days

                // Soft delete old messages (instead of hard delete)
                var oldMessages = await _context.ChatMessages
                    .Where(m => m.Timestamp < cutoffDate && m.IsActive)
                    .ToListAsync();

                if (oldMessages.Any())
                {
                    foreach (var message in oldMessages)
                    {
                        message.IsActive = false;
                    }
                    _logger.LogInformation("Soft deleted {Count} old messages", oldMessages.Count);
                }

                // Delete old sessions with no recent activity
                var oldSessions = await _context.ChatSessions
                    .Where(s => s.LastActivityAt < cutoffDate && s.MessageCount == 0)
                    .ToListAsync();

                if (oldSessions.Any())
                {
                    _context.ChatSessions.RemoveRange(oldSessions);
                    _logger.LogInformation("Cleaned up {Count} old sessions", oldSessions.Count);
                }

                // Clear summaries from very old sessions
                var veryOldSessions = await _context.ChatSessions
                    .Where(s => s.LastActivityAt < summaryCutoffDate && !string.IsNullOrEmpty(s.Summary))
                    .ToListAsync();

                foreach (var session in veryOldSessions)
                {
                    session.Summary = null;
                }

                if (veryOldSessions.Any())
                {
                    _logger.LogInformation("Cleared summaries from {Count} very old sessions", veryOldSessions.Count);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Completed cleanup of expired chat data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during chat data cleanup");
            }
        }

        public async Task<ChatSession?> GetSessionAsync(int sessionId)
        {
            try
            {
                var session = await _context.ChatSessions
                    .Include(s => s.Patient)
                    .Include(s => s.User)
                    .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                // Filter out soft-deleted messages
                if (session != null && session.Messages != null)
                {
                    session.Messages = session.Messages.Where(m => m.IsActive).ToList();
                }

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<List<ChatSession>> GetUserSessionsAsync(int userId, int? patientId = null)
        {
            try
            {
                // Get user's role to determine what sessions they can see
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return new List<ChatSession>();
                }

                var query = _context.ChatSessions
                    .Include(s => s.Patient)
                    .Include(s => s.User)
                    .AsQueryable();

                // Role-based session filtering
                if (user.RoleId == Shared.Constants.Roles.Patient)
                {
                    // Patients see their own direct conversations and when doctors chat about them
                    query = query.Where(s => s.UserId == userId || s.PatientId == userId);
                }
                else if (user.RoleId == Shared.Constants.Roles.Doctor)
                {
                    // Doctors see their own conversations
                    query = query.Where(s => s.UserId == userId);

                    // If patientId is specified, filter to only sessions about that patient
                    if (patientId.HasValue)
                    {
                        query = query.Where(s => s.PatientId == patientId.Value);
                    }
                }
                else if (user.RoleId == Shared.Constants.Roles.Admin)
                {
                    // Admins see all sessions
                    // If patientId is specified, filter to only sessions about that patient
                    if (patientId.HasValue)
                    {
                        query = query.Where(s => s.PatientId == patientId.Value);
                    }
                }

                // Return all active sessions (both ignored and non-ignored)
                // IsActive = soft delete flag (only show active sessions)
                // IsIgnoredByDoctor = doctor's choice to exclude from AI analysis, but still show in grid
                // Show all sessions - don't group them, let each session be its own row
                // The grouping was causing sessions to be hidden when they should be separate rows
                var sessions = await query
                    .Where(s => s.IsActive) // Only show active sessions (soft delete filter)
                                            // Note: IsIgnoredByDoctor is NOT filtered here - we show both ignored and non-ignored in the grid
                                            // The ignore flag only affects AI analysis, not grid visibility
                    .OrderByDescending(s => s.LastActivityAt)
                    .ThenByDescending(s => s.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("GetUserSessionsAsync: Found {Count} active sessions for user {UserId}, patient {PatientId}. Sessions: {Sessions}",
                    sessions.Count,
                    userId,
                    patientId,
                    string.Join(", ", sessions.Select(s => $"Id={s.Id}, Created={s.CreatedAt:yyyy-MM-dd HH:mm}, Ignored={s.IsIgnoredByDoctor}, Messages={s.MessageCount}")));

                // Pre-load a sample of recent messages for each session to help with concern detection
                // This allows the client to analyze message content even when Messages collection isn't fully loaded
                var sessionIds = sessions.Select(s => s.Id).ToList();
                var recentMessagesBySession = await _context.ChatMessages
                    .Where(m => sessionIds.Contains(m.SessionId) && m.IsActive)
                    .GroupBy(m => m.SessionId)
                    .Select(g => new
                    {
                        SessionId = g.Key,
                        Messages = g.OrderByDescending(m => m.Timestamp)
                            .Take(10)
                            .Select(m => new { m.Content, m.IsMedicalData })
                            .ToList()
                    })
                    .ToListAsync();

                // Attach recent messages to sessions for concern detection
                foreach (var session in sessions)
                {
                    var sessionMessages = recentMessagesBySession.FirstOrDefault(m => m.SessionId == session.Id);
                    if (sessionMessages != null && sessionMessages.Messages.Any())
                    {
                        // Initialize Messages collection if null
                        if (session.Messages == null)
                        {
                            session.Messages = new List<ChatMessage>();
                        }

                        // Add sample messages to help with concern detection
                        // Note: We're creating lightweight ChatMessage objects just for concern analysis
                        foreach (var msg in sessionMessages.Messages)
                        {
                            session.Messages.Add(new ChatMessage
                            {
                                Content = msg.Content ?? string.Empty,
                                IsMedicalData = msg.IsMedicalData
                            });
                        }
                    }
                }

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sessions for user {UserId}", userId);
                return new List<ChatSession>();
            }
        }

        public async Task DeleteSessionAsync(int sessionId)
        {
            try
            {
                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive);

                if (session != null)
                {
                    // Soft delete
                    session.IsActive = false;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Session {SessionId} soft deleted successfully", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task GenerateSessionSummaryAsync(int sessionId)
        {
            try
            {
                // Create a new DbContext instance for this background operation
                // Use IServiceScopeFactory to create a scope that won't be disposed with the request
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<JournalDbContext>();

                var session = await context.ChatSessions
                    .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                    .Include(s => s.Patient)
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                // Filter out soft-deleted messages
                if (session != null && session.Messages != null)
                {
                    session.Messages = session.Messages.Where(m => m.IsActive).ToList();
                }

                if (session == null || !session.Messages.Any())
                {
                    _logger.LogWarning("Session {SessionId} not found or has no messages", sessionId);
                    return;
                }

                // Don't generate summary if one already exists
                if (!string.IsNullOrEmpty(session.Summary))
                {
                    _logger.LogInformation("Session {SessionId} already has a summary", sessionId);
                    return;
                }

                // Build conversation context for summary using the scoped context
                var conversationContext = await BuildConversationContextAsync(sessionId, context);

                // Create summary prompt
                var summaryPrompt = $@"
Please provide a concise clinical summary of this health conversation. Focus on:

1. **Key Topics Discussed**: Main themes, concerns, or issues raised
2. **Patient Concerns**: Specific worries, symptoms, or challenges mentioned
3. **AI Recommendations**: Advice, coping strategies, or suggestions provided
4. **Patient Response**: How the patient reacted to suggestions
5. **Clinical Notes**: Any important observations for healthcare providers

Conversation:
{conversationContext}

Please provide a structured summary in 2-3 sentences that would be useful for clinical review and follow-up care.";

                // For now, we'll use a simple summary generation
                // In production, you'd call your AI service here
                var summary = await GenerateAISummary(summaryPrompt, session);

                if (!string.IsNullOrEmpty(summary))
                {
                    session.Summary = summary;
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Generated summary for session {SessionId}", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary for session {SessionId}", sessionId);
            }
        }

        private async Task<string> GenerateAISummary(string prompt, ChatSession session)
        {
            try
            {
                // Extract key information from the conversation
                var userMessages = session.Messages.Where(m => m.Role == MessageRole.User).ToList();
                var aiMessages = session.Messages.Where(m => m.Role == MessageRole.Assistant).ToList();

                if (!userMessages.Any() || !aiMessages.Any())
                {
                    return "Conversation summary not available - insufficient message data.";
                }

                // Simple rule-based summary generation
                var topics = new List<string>();
                var concerns = new List<string>();
                var recommendations = new List<string>();

                // Analyze user messages for topics and concerns
                foreach (var msg in userMessages)
                {
                    var content = msg.Content.ToLower();
                    if (content.Contains("anxiety") || content.Contains("worried") || content.Contains("stress"))
                        concerns.Add("Anxiety/Stress");
                    if (content.Contains("sleep") || content.Contains("insomnia"))
                        concerns.Add("Sleep Issues");
                    if (content.Contains("depressed") || content.Contains("sad") || content.Contains("down"))
                        concerns.Add("Mood/Depression");
                    if (content.Contains("work") || content.Contains("job"))
                        topics.Add("Work-related concerns");
                    if (content.Contains("family") || content.Contains("relationship"))
                        topics.Add("Family/Relationships");
                }

                // Analyze AI messages for recommendations
                foreach (var msg in aiMessages)
                {
                    var content = msg.Content.ToLower();
                    if (content.Contains("breathing") || content.Contains("meditation"))
                        recommendations.Add("Breathing/Meditation techniques");
                    if (content.Contains("exercise") || content.Contains("physical activity"))
                        recommendations.Add("Physical activity suggestions");
                    if (content.Contains("sleep") || content.Contains("bedtime"))
                        recommendations.Add("Sleep hygiene advice");
                }

                // Build summary
                var summaryParts = new List<string>();

                if (concerns.Any())
                {
                    summaryParts.Add($"Patient discussed: {string.Join(", ", concerns.Distinct())}");
                }

                if (topics.Any())
                {
                    summaryParts.Add($"Topics covered: {string.Join(", ", topics.Distinct())}");
                }

                if (recommendations.Any())
                {
                    summaryParts.Add($"AI provided: {string.Join(", ", recommendations.Distinct())}");
                }

                summaryParts.Add($"Session included {userMessages.Count} patient messages and {aiMessages.Count} AI responses.");

                return string.Join(" ", summaryParts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI summary generation");
                return "Summary generation failed - manual review recommended.";
            }
        }

        public async Task ToggleIgnoreAsync(int sessionId, int doctorId)
        {
            try
            {
                var session = await _context.ChatSessions.FindAsync(sessionId);
                if (session == null)
                {
                    throw new ArgumentException("Session not found");
                }

                // Toggle ignore status
                if (session.IsIgnoredByDoctor)
                {
                    // Unignore
                    session.IsIgnoredByDoctor = false;
                    session.IgnoredByDoctorId = null;
                    session.IgnoredAt = null;
                    _logger.LogInformation("Session {SessionId} unignored by doctor {DoctorId}", sessionId, doctorId);
                }
                else
                {
                    // Ignore
                    session.IsIgnoredByDoctor = true;
                    session.IgnoredByDoctorId = doctorId;
                    session.IgnoredAt = DateTime.UtcNow;
                    _logger.LogInformation("Session {SessionId} ignored by doctor {DoctorId}", sessionId, doctorId);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling ignore status for session {SessionId}", sessionId);
                throw;
            }
        }
    }
}
