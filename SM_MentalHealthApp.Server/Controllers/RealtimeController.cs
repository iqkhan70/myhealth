using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RealtimeController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<RealtimeController> _logger;
        private static readonly Dictionary<string, RealtimeConnection> _connections = new();
        private static readonly Dictionary<int, string> _userConnections = new();
        private static Timer? _cleanupTimer;

        static RealtimeController()
        {
            // Start cleanup timer to remove stale connections every 2 minutes
            _cleanupTimer = new Timer(CleanupStaleConnections, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        public RealtimeController(JournalDbContext context, ILogger<RealtimeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] ConnectRequest request)
        {
            try
            {
                // Validate JWT token and get user ID
                var userId = await AuthenticateToken();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid or missing token" });
                }

                var connectionId = Guid.NewGuid().ToString();
                // Remove any existing connection for this user
                if (_userConnections.TryGetValue(userId.Value, out var existingConnectionId) && !string.IsNullOrEmpty(existingConnectionId))
                {
                    _connections.Remove(existingConnectionId);
                    _userConnections.Remove(userId.Value);
                    _logger.LogInformation("Removed existing connection {ExistingConnectionId} for user {UserId}", existingConnectionId, userId.Value);
                }

                // Create new connection
                var connection = new RealtimeConnection
                {
                    ConnectionId = connectionId,
                    UserId = userId.Value,
                    LastPing = DateTime.UtcNow
                };

                _connections[connectionId] = connection;
                _userConnections[userId.Value] = connectionId;

                _logger.LogInformation("Realtime connection established: {ConnectionId} for user {UserId}", connectionId, userId.Value);

                return Ok(new { connectionId, message = "Connected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error establishing realtime connection");
                return BadRequest($"Connection failed: {ex.Message}");
            }
        }

        [HttpPost("disconnect")]
        public IActionResult Disconnect([FromBody] DisconnectRequest request)
        {
            try
            {
                if (_connections.TryGetValue(request.ConnectionId, out var connection))
                {
                    _connections.Remove(request.ConnectionId);
                    _userConnections.Remove(connection.UserId);
                    _logger.LogInformation("Realtime connection closed: {ConnectionId}", request.ConnectionId);
                }

                return Ok(new { message = "Disconnected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting");
                return BadRequest($"Disconnect failed: {ex.Message}");
            }
        }


        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                // Verify connection exists
                if (!_connections.TryGetValue(request.ConnectionId, out var senderConnection))
                {
                    return BadRequest("Invalid connection");
                }

                // Get sender information from database
                var sender = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == senderConnection.UserId);

                if (sender == null)
                {
                    return BadRequest("Sender not found");
                }

                // Save message to database
                var smsMessage = new SmsMessage
                {
                    SenderId = senderConnection.UserId,
                    ReceiverId = request.TargetUserId,
                    Message = request.Message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.SmsMessages.Add(smsMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved message: '{Message}' from {SenderId} to {ReceiverId} at {Timestamp}",
                    request.Message, senderConnection.UserId, request.TargetUserId, smsMessage.SentAt);

                // Check if target user is connected and queue for immediate delivery
                if (_userConnections.TryGetValue(request.TargetUserId, out var targetConnectionId))
                {
                    var messageData = new
                    {
                        type = "new-message",
                        id = smsMessage.Id.ToString(),
                        senderId = senderConnection.UserId,
                        targetUserId = request.TargetUserId,
                        message = request.Message,
                        senderName = $"{sender.FirstName} {sender.LastName}",
                        timestamp = smsMessage.SentAt.ToString("O")
                    };

                    // Store the message for the target user to poll
                    if (_connections.TryGetValue(targetConnectionId, out var targetConnection))
                    {
                        targetConnection.PendingMessages.Add(messageData);
                    }
                }

                _logger.LogInformation("SMS message saved and sent from {SenderId} to {TargetUserId}: {Message}",
                    senderConnection.UserId, request.TargetUserId, request.Message);

                return Ok(new { message = "Message sent successfully", delivered = true, messageId = smsMessage.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return BadRequest($"Failed to send message: {ex.Message}");
            }
        }

        [HttpPost("initiate-call")]
        public async Task<IActionResult> InitiateCall([FromBody] InitiateCallRequest request)
        {
            try
            {
                // For mobile connections, we don't need to verify the connection exists
                // as mobile apps use a fixed connection ID
                RealtimeConnection? callerConnection = null;
                if (request.ConnectionId != "mobile-connection")
                {
                    if (!_connections.TryGetValue(request.ConnectionId, out callerConnection))
                    {
                        return BadRequest("Invalid connection");
                    }
                }
                else
                {
                    // For mobile, get the caller ID from the JWT token
                    var userId = await AuthenticateToken();
                    if (userId == null)
                    {
                        return Unauthorized("Invalid or missing token");
                    }

                    // Create a temporary connection object for mobile
                    callerConnection = new RealtimeConnection
                    {
                        ConnectionId = request.ConnectionId,
                        UserId = userId.Value,
                        LastPing = DateTime.UtcNow
                    };
                }

                // Check if target user is connected
                if (!_userConnections.TryGetValue(request.TargetUserId, out var targetConnectionId))
                {
                    return Ok(new { message = "Target user is not online", delivered = false });
                }

                var callData = new
                {
                    type = "incoming_call",
                    callerId = callerConnection.UserId,
                    callerName = "Mobile User", // TODO: Get actual user name from database
                    callType = request.CallType,
                    channelName = request.ChannelName,
                    timestamp = DateTime.UtcNow,
                    agoraAppId = "efa11b3a7d05409ca979fb25a5b489ae", // Replace with your actual Agora App ID
                    agoraToken = GenerateAgoraToken(request.ChannelName, callerConnection.UserId)
                };

                // Store the call for the target user to poll
                if (_connections.TryGetValue(targetConnectionId, out var targetConnection))
                {
                    targetConnection.PendingCalls.Add(callData);
                }

                _logger.LogInformation("Call initiated from {CallerId} to {TargetUserId}: {CallType}",
                    callerConnection.UserId, request.TargetUserId, request.CallType);

                return Ok(new { message = "Call initiated successfully", delivered = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                return BadRequest($"Failed to initiate call: {ex.Message}");
            }
        }

        [HttpPost("poll")]
        public IActionResult Poll([FromBody] PollRequest request)
        {
            try
            {
                _logger.LogInformation("Poll request from connection: {ConnectionId}", request.ConnectionId);

                if (!_connections.TryGetValue(request.ConnectionId, out var connection))
                {
                    _logger.LogWarning("Invalid connection ID: {ConnectionId}", request.ConnectionId);
                    return BadRequest("Invalid connection");
                }

                // Update last ping
                connection.LastPing = DateTime.UtcNow;

                var messageCount = connection.PendingMessages.Count;
                var callCount = connection.PendingCalls.Count;

                _logger.LogInformation("Poll response - Messages: {MessageCount}, Calls: {CallCount}", messageCount, callCount);

                var response = new
                {
                    messages = connection.PendingMessages.ToArray(),
                    calls = connection.PendingCalls.ToArray(),
                    connectionStatus = "connected"
                };

                // Clear pending messages and calls
                connection.PendingMessages.Clear();
                connection.PendingCalls.Clear();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling");
                return BadRequest($"Poll failed: {ex.Message}");
            }
        }

        [HttpPost("get-message-history")]
        public async Task<IActionResult> GetMessageHistory([FromBody] MessageHistoryRequest request)
        {
            try
            {
                // Verify connection exists
                if (!_connections.TryGetValue(request.ConnectionId, out var connection))
                {
                    return BadRequest("Invalid connection");
                }

                var userId = connection.UserId;
                var otherUserId = request.OtherUserId;

                // Get message history between the two users
                var messages = await _context.SmsMessages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                               (m.SenderId == otherUserId && m.ReceiverId == userId))
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        id = m.Id,
                        senderId = m.SenderId,
                        receiverId = m.ReceiverId,
                        message = m.Message,
                        senderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Unknown",
                        timestamp = m.SentAt.ToString("O"),
                        isRead = m.IsRead
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} messages for users {UserId} and {OtherUserId}",
                    messages.Count, userId, otherUserId);

                if (messages.Count > 0)
                {
                    _logger.LogInformation("First message: {Message} at {Timestamp}",
                        messages.First().message, messages.First().timestamp);
                    _logger.LogInformation("Last message: {Message} at {Timestamp}",
                        messages.Last().message, messages.Last().timestamp);
                }

                return Ok(new { messages = messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message history");
                return BadRequest($"Failed to get message history: {ex.Message}");
            }
        }

        // Cleanup old connections periodically
        [HttpPost("cleanup")]
        public IActionResult Cleanup()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
                var oldConnections = _connections.Where(kvp => kvp.Value.LastPing < cutoffTime).ToList();

                foreach (var oldConnection in oldConnections)
                {
                    _connections.Remove(oldConnection.Key);
                    _userConnections.Remove(oldConnection.Value.UserId);
                    _logger.LogInformation("Cleaned up old connection: {ConnectionId}", oldConnection.Key);
                }

                return Ok(new { cleaned = oldConnections.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return BadRequest($"Cleanup failed: {ex.Message}");
            }
        }

        private string GenerateAgoraToken(string channelName, int userId)
        {
            // For now, return a placeholder token
            // In a real implementation, you would generate a proper Agora token
            // using Agora's token generation service
            return $"agora_token_{channelName}_{userId}_{DateTime.UtcNow.Ticks}";
        }

        private async Task<int?> AuthenticateToken()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return null;
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                // Validate JWT token and extract user ID
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("YourSuperSecretKeyThatIsAtLeast32CharactersLong!")),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                ClaimsPrincipal principal;
                try
                {
                    principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token validation failed");
                    return null;
                }

                var userIdClaim = principal.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token");
                    return null;
                }

                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token authentication");
                return null;
            }
        }

        private static void CleanupStaleConnections(object? state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-5); // Remove connections older than 5 minutes
                var staleConnections = _connections.Values
                    .Where(c => c.LastPing < cutoffTime)
                    .ToList();

                foreach (var connection in staleConnections)
                {
                    _connections.Remove(connection.ConnectionId);
                    _userConnections.Remove(connection.UserId);
                }

                if (staleConnections.Count > 0)
                {
                    Console.WriteLine($"Cleaned up {staleConnections.Count} stale connections");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during connection cleanup: {ex.Message}");
            }
        }
    }

    public class RealtimeConnection
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime LastPing { get; set; }
        public List<object> PendingMessages { get; set; } = new();
        public List<object> PendingCalls { get; set; } = new();
    }

    public class ConnectRequest
    {
        public int UserId { get; set; }
    }

    public class DisconnectRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
    }


    public class PollRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
    }

    public class MessageHistoryRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int OtherUserId { get; set; }
    }

    public class InitiateCallRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int TargetUserId { get; set; }
        public string CallType { get; set; } = string.Empty; // "video" or "audio"
        public string ChannelName { get; set; } = string.Empty;
    }

    public class SendMessageRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int TargetUserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
