using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Text.Json;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IChatHistoryService
    {
        Task<ChatSession> GetOrCreateSessionAsync(int userId, int? patientId = null);
        Task<ChatMessage> AddMessageAsync(int sessionId, MessageRole role, string content, MessageType messageType = MessageType.Question, bool isMedicalData = false, string? metadata = null);
        Task<List<ChatMessage>> GetRecentMessagesAsync(int sessionId, int maxMessages = 20);
        Task<string> BuildConversationContextAsync(int sessionId);
        Task UpdateSessionActivityAsync(int sessionId);
        Task CleanupExpiredDataAsync();
        Task<ChatSession?> GetSessionAsync(int sessionId);
        Task<List<ChatSession>> GetUserSessionsAsync(int userId, int? patientId = null);
        Task DeleteSessionAsync(int sessionId);
    }

    public class ChatHistoryService : IChatHistoryService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ChatHistoryService> _logger;
        private const int MAX_CONTEXT_MESSAGES = 20;
        private const int SUMMARY_THRESHOLD = 50;

        public ChatHistoryService(JournalDbContext context, ILogger<ChatHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ChatSession> GetOrCreateSessionAsync(int userId, int? patientId = null)
        {
            try
            {
                // Try to find an active session for this user and patient combination
                var existingSession = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId &&
                                            s.PatientId == patientId &&
                                            s.IsActive);

                if (existingSession != null)
                {
                    _logger.LogInformation("Found existing active session {SessionId} for user {UserId}, patient {PatientId}",
                        existingSession.SessionId, userId, patientId);
                    return existingSession;
                }

                // Create new session
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

        public async Task<List<ChatMessage>> GetRecentMessagesAsync(int sessionId, int maxMessages = 20)
        {
            try
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId)
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

        public async Task<string> BuildConversationContextAsync(int sessionId)
        {
            try
            {
                var session = await _context.ChatSessions
                    .Include(s => s.Patient)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

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

                // Add recent messages
                var recentMessages = await GetRecentMessagesAsync(sessionId, MAX_CONTEXT_MESSAGES);
                if (recentMessages.Any())
                {
                    contextBuilder.Add("=== RECENT CONVERSATION ===\n");
                    foreach (var message in recentMessages)
                    {
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

                var context = string.Join("\n", contextBuilder);
                _logger.LogInformation("Built conversation context for session {SessionId}, length: {Length}", sessionId, context.Length);
                return context;
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
                    session.LastActivityAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
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

                // Delete old messages
                var oldMessages = await _context.ChatMessages
                    .Where(m => m.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldMessages.Any())
                {
                    _context.ChatMessages.RemoveRange(oldMessages);
                    _logger.LogInformation("Cleaned up {Count} old messages", oldMessages.Count);
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
                return await _context.ChatSessions
                    .Include(s => s.Patient)
                    .Include(s => s.User)
                    .Include(s => s.Messages.OrderBy(m => m.Timestamp))
                    .FirstOrDefaultAsync(s => s.Id == sessionId);
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
                if (user.RoleId == 1) // Patient
                {
                    // Patients see their own direct conversations and when doctors chat about them
                    query = query.Where(s => s.UserId == userId || s.PatientId == userId);
                }
                else if (user.RoleId == 2) // Doctor
                {
                    // Doctors see their own conversations
                    query = query.Where(s => s.UserId == userId);

                    // If patientId is specified, filter to only sessions about that patient
                    if (patientId.HasValue)
                    {
                        query = query.Where(s => s.PatientId == patientId.Value);
                    }
                }
                else if (user.RoleId == 3) // Admin
                {
                    // Admins see all sessions
                    // If patientId is specified, filter to only sessions about that patient
                    if (patientId.HasValue)
                    {
                        query = query.Where(s => s.PatientId == patientId.Value);
                    }
                }

                var sessions = await query
                    .OrderByDescending(s => s.LastActivityAt)
                    .ToListAsync();

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
                var session = await _context.ChatSessions.FindAsync(sessionId);
                if (session != null)
                {
                    _context.ChatSessions.Remove(session);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Session {SessionId} deleted successfully", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
                throw;
            }
        }
    }
}
