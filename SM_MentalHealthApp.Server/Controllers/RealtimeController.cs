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
using SM_MentalHealthApp.Server.Utils;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RealtimeController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<RealtimeController> _logger;
        private readonly IRedisCacheService _cache;
        private readonly AgoraTokenService _agoraTokenService;

        private const string _appId = "efa11b3a7d05409ca979fb25a5b489ae";

        private static readonly Dictionary<string, RealtimeConnection> _connections = new();
        private static readonly Dictionary<int, string> _userConnections = new();
        private static Timer? _cleanupTimer;

        static RealtimeController()
        {
            _cleanupTimer = new Timer(CleanupStaleConnections, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        public RealtimeController(
            JournalDbContext context,
            ILogger<RealtimeController> logger,
            IRedisCacheService cache,
            AgoraTokenService agoraTokenService)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _agoraTokenService = agoraTokenService;
        }

        // ✅ AGORA TOKEN ENDPOINTS ============================================

        // Blazor & Web: GET
        [HttpGet("token")]
        public async Task<IActionResult> GetAgoraToken([FromQuery] string channel, [FromQuery] uint uid)
        {

            Console.WriteLine("Using uid1: " + uid);
            Console.WriteLine("Using channel1: " + channel);

            if (string.IsNullOrEmpty(channel) || uid == 0)
                return BadRequest("Missing channel or uid");

            return await GenerateAndCacheTokenAsync(channel, uid, 3600);
        }

        // Mobile: POST
        [HttpPost("token")]
        public async Task<IActionResult> PostAgoraToken([FromBody] AgoraRequest request)
        {

            Console.WriteLine("Using uid2: " + request.Uid);
            Console.WriteLine("Using channel2: " + request.ChannelName);

            if (string.IsNullOrEmpty(request.ChannelName) || request.Uid == 0)
                return BadRequest("Missing channel or uid");

            var expireSeconds = request.ExpirationTimeInSeconds ?? 3600;
            return await GenerateAndCacheTokenAsync(request.ChannelName, request.Uid, expireSeconds);
        }

        private async Task<IActionResult> GenerateAndCacheTokenAsync(string channel, uint uid, uint expireSeconds)
        {
            try
            {
                string cacheKey = $"agora_token:{channel}";
                _logger.LogInformation("🔍 Checking Redis for key {Key}", cacheKey);

                var cachedToken = await _cache.GetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedToken))
                {
                    _logger.LogInformation("✅ Returning cached token for {Channel}", channel);
                    return Ok(new { agoraAppId = _appId, token = "007eJxTYBCfev3xZN7Ihh0a0wy2/ncRPrtJ10dIudy3T21f+L3Xps0KDKlpiYaGScaJ5ikGpiYGlsmJluaWaUlGpommSSYWlompxXcFMhsCGRlErpcwMTJAIIjPwZCcmJMTbxxvyMAAAJUVH8Q=", cached = true });
                }

                var token = _agoraTokenService.GenerateToken(channel, uid, expireSeconds);
                await _cache.SetAsync(cacheKey, token, TimeSpan.FromSeconds(expireSeconds));

                _logger.LogInformation("🆕 Generated and cached new token for {Channel}", channel);
                return Ok(new { agoraAppId = _appId, token = "007eJxTYBCfev3xZN7Ihh0a0wy2/ncRPrtJ10dIudy3T21f+L3Xps0KDKlpiYaGScaJ5ikGpiYGlsmJluaWaUlGpommSSYWlompxXcFMhsCGRlErpcwMTJAIIjPwZCcmJMTbxxvyMAAAJUVH8Q=", cached = false, uid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating Agora token");
                return StatusCode(500, new { message = "Token generation failed", error = ex.Message });
            }
        }

        // =====================================================================


        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] ConnectRequest request)
        {
            try
            {
                var userId = await AuthenticateToken();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "Invalid or missing token" });

                var connectionId = Guid.NewGuid().ToString();

                if (_userConnections.TryGetValue(userId.Value, out var existingConnectionId))
                {
                    _connections.Remove(existingConnectionId);
                    _userConnections.Remove(userId.Value);
                }

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
                if (!_connections.TryGetValue(request.ConnectionId, out var senderConnection))
                    return BadRequest("Invalid connection");

                var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderConnection.UserId);
                if (sender == null)
                    return BadRequest("Sender not found");

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

                if (_userConnections.TryGetValue(request.TargetUserId, out var targetConnectionId) &&
                    _connections.TryGetValue(targetConnectionId, out var targetConnection))
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
                    targetConnection.PendingMessages.Add(messageData);
                }

                _logger.LogInformation("Message sent from {SenderId} to {ReceiverId}", senderConnection.UserId, request.TargetUserId);
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
                RealtimeConnection? callerConnection = null;

                if (request.ConnectionId != "mobile-connection")
                {
                    if (!_connections.TryGetValue(request.ConnectionId, out callerConnection))
                        return BadRequest("Invalid connection");
                }
                else
                {
                    var userId = await AuthenticateToken();
                    if (userId == null)
                        return Unauthorized("Invalid or missing token");

                    callerConnection = new RealtimeConnection
                    {
                        ConnectionId = request.ConnectionId,
                        UserId = userId.Value,
                        LastPing = DateTime.UtcNow
                    };
                }

                if (!_userConnections.TryGetValue(request.TargetUserId, out var targetConnectionId))
                    return Ok(new { message = "Target user is not online", delivered = false });

                var agoraTokenResult = await GenerateAndCacheTokenAsync(request.ChannelName, (uint)callerConnection.UserId, 3600) as OkObjectResult;
                var tokenData = agoraTokenResult?.Value as dynamic;

                var callData = new
                {
                    type = "incoming_call",
                    callerId = callerConnection.UserId,
                    callerName = "Mobile User",
                    callType = request.CallType,
                    channelName = request.ChannelName,
                    timestamp = DateTime.UtcNow,
                    agoraAppId = tokenData?.agoraAppId,
                    agoraToken = tokenData?.token
                };

                if (_connections.TryGetValue(targetConnectionId, out var targetConnection))
                    targetConnection.PendingCalls.Add(callData);

                _logger.LogInformation("Call initiated from {CallerId} to {TargetUserId}", callerConnection.UserId, request.TargetUserId);
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
                if (!_connections.TryGetValue(request.ConnectionId, out var connection))
                    return BadRequest("Invalid connection");

                connection.LastPing = DateTime.UtcNow;

                var response = new
                {
                    messages = connection.PendingMessages.ToArray(),
                    calls = connection.PendingCalls.ToArray(),
                    connectionStatus = "connected"
                };

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
                if (!_connections.TryGetValue(request.ConnectionId, out var connection))
                    return BadRequest("Invalid connection");

                var userId = connection.UserId;
                var otherUserId = request.OtherUserId;

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

                return Ok(new { messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message history");
                return BadRequest($"Failed to get message history: {ex.Message}");
            }
        }

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
                }

                return Ok(new { cleaned = oldConnections.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return BadRequest($"Cleanup failed: {ex.Message}");
            }
        }

        // ==============================================================
        // 🔒 Token authentication helper
        // ==============================================================

        private async Task<int?> AuthenticateToken()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                    return null;

                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (string.IsNullOrEmpty(token))
                    return null;

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("YourSuperSecretKeyThatIsAtLeast32CharactersLong!")),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                var userIdClaim = principal.FindFirst("userId")?.Value;

                if (!int.TryParse(userIdClaim, out int userId))
                    return null;

                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                return null;
            }
        }

        private static void CleanupStaleConnections(object? state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
                var staleConnections = _connections.Values.Where(c => c.LastPing < cutoffTime).ToList();

                foreach (var connection in staleConnections)
                {
                    _connections.Remove(connection.ConnectionId);
                    _userConnections.Remove(connection.UserId);
                }

                if (staleConnections.Count > 0)
                    Console.WriteLine($"Cleaned up {staleConnections.Count} stale connections");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during connection cleanup: {ex.Message}");
            }
        }
    }

    // ============================================================
    // 💬 Data Models
    // ============================================================

    public class AgoraRequest
    {
        public string ChannelName { get; set; } = string.Empty;
        public uint Uid { get; set; }
        public uint? ExpirationTimeInSeconds { get; set; }
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
        public string CallType { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
    }

    public class SendMessageRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int TargetUserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
