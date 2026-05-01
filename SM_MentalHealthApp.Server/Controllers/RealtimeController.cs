using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SM_MentalHealthApp.Server.Hubs;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class RealtimeController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<RealtimeController> _logger;
        private readonly IRedisCacheService _cache;
        private readonly AgoraTokenService _agoraTokenService;
        private readonly IHubContext<MobileHub> _hubContext;
        private readonly bool _useTokens;
        private readonly IConfiguration _configuration;

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
            AgoraTokenService agoraTokenService,
            IHubContext<MobileHub> hubContext,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _agoraTokenService = agoraTokenService;
            _hubContext = hubContext;
            _useTokens = configuration.GetValue<bool>("Agora:UseTokens", true);
            _configuration = configuration;
        }

        // ===================== AGORA TOKEN ENDPOINTS ==========================

        // Blazor & Web: GET
        [HttpGet("token")]
        public async Task<IActionResult> GetAgoraToken([FromQuery] string channel, [FromQuery] uint uid)
        {
            _logger.LogInformation("📞 Token request - Channel: {Channel}, Requested UID: {Uid}", channel, uid);

            if (string.IsNullOrEmpty(channel))
                return BadRequest("Missing channel");

            return await GenerateAndCacheTokenAsync(channel, uid, 3600);
        }

        // Mobile: POST
        [HttpPost("token")]
        public async Task<IActionResult> PostAgoraToken([FromBody] AgoraRequest request)
        {
            _logger.LogInformation("📞 Token request - Channel: {Channel}, Requested UID: {Uid}", request.ChannelName, request.Uid);

            if (string.IsNullOrEmpty(request.ChannelName))
                return BadRequest("Missing channel");

            var expireSeconds = request.ExpirationTimeInSeconds ?? 3600;
            return await GenerateAndCacheTokenAsync(request.ChannelName, request.Uid, expireSeconds);
        }

        private async Task<IActionResult> GenerateAndCacheTokenAsync(string channel, uint uid, uint expireSeconds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channel))
                {
                    _logger.LogError("❌ Empty or null channel name provided");
                    return BadRequest(new { message = "Channel name cannot be empty or null" });
                }

                if (!_useTokens)
                {
                    _logger.LogInformation("🔓 Token authentication disabled - returning empty token for {Channel}", channel);
                    return Ok(new
                    {
                        agoraAppId = _agoraTokenService.AppId,
                        token = "",
                        cached = false,
                        useTokens = false
                    });
                }

                string cacheKey = $"agora_token:{channel}";
                _logger.LogInformation("🔍 Checking Redis for key {Key}", cacheKey);

                var cachedToken = await _cache.GetAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedToken))
                {
                    _logger.LogInformation("✅ Returning cached token for {Channel} (shared by all users)", channel);
                    return Ok(new
                    {
                        agoraAppId = _agoraTokenService.AppId,
                        token = cachedToken,
                        cached = true,
                        useTokens = true
                    });
                }

                // Token for UID 0: shared by any UID in that channel
                var token = _agoraTokenService.GenerateToken(channel, 0, expireSeconds);
                await _cache.SetAsync(cacheKey, token, TimeSpan.FromSeconds(expireSeconds));

                _logger.LogInformation("🆕 Generated and cached new token for {Channel} (shared token, UID=0)", channel);
                return Ok(new
                {
                    agoraAppId = _agoraTokenService.AppId,
                    token = token,
                    cached = false,
                    useTokens = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating Agora token");
                return StatusCode(500, new { message = "Token generation failed", error = ex.Message });
            }
        }

        // ===================== REALTIME CONNECTIONS ==========================

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

                var messageData = new
                {
                    id = smsMessage.Id.ToString(),
                    senderId = senderConnection.UserId,
                    targetUserId = request.TargetUserId,
                    message = request.Message,
                    senderName = $"{sender.FirstName} {sender.LastName}",
                    timestamp = smsMessage.SentAt.ToString("O")
                };

                // Add to polling queue for browsers using RealtimeService
                if (_userConnections.TryGetValue(request.TargetUserId, out var targetConnectionId) &&
                    _connections.TryGetValue(targetConnectionId, out var targetConnection))
                {
                    var pollingMessageData = new
                    {
                        type = "new-message",
                        id = smsMessage.Id.ToString(),
                        senderId = senderConnection.UserId,
                        targetUserId = request.TargetUserId,
                        message = request.Message,
                        senderName = $"{sender.FirstName} {sender.LastName}",
                        timestamp = smsMessage.SentAt.ToString("O")
                    };
                    targetConnection.PendingMessages.Add(pollingMessageData);
                }

                // Also send SignalR notification for mobile apps and browsers using SignalR
                if (MobileHub.UserConnections.TryGetValue(request.TargetUserId, out string? targetSignalRConnectionId))
                {
                    _logger.LogInformation("📨 Sending SignalR notification to user {TargetUserId} via connection {ConnectionId} for message {MessageId} from {SenderId}",
                        request.TargetUserId, targetSignalRConnectionId, smsMessage.Id, senderConnection.UserId);
                    await _hubContext.Clients.Client(targetSignalRConnectionId).SendAsync("new-message", messageData);
                    _logger.LogInformation("✅ SignalR notification sent successfully to connection {ConnectionId} for message {MessageId} from {SenderId} to {ReceiverId}",
                        targetSignalRConnectionId, smsMessage.Id, senderConnection.UserId, request.TargetUserId);
                }
                else
                {
                    _logger.LogWarning("⚠️ Target user {TargetUserId} is not connected via SignalR. Available connections: {Connections}",
                        request.TargetUserId, string.Join(", ", MobileHub.UserConnections.Select(kvp => $"User {kvp.Key}: {kvp.Value}")));
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

                var channelName = request.ChannelName;
                if (string.IsNullOrWhiteSpace(channelName))
                {
                    var smallerId = Math.Min(callerConnection.UserId, request.TargetUserId);
                    var largerId = Math.Max(callerConnection.UserId, request.TargetUserId);
                    channelName = $"call_{smallerId}_{largerId}";
                    _logger.LogInformation("Generated channel name: {ChannelName}", channelName);
                }

                var agoraTokenResult = await GenerateAndCacheTokenAsync(channelName, (uint)callerConnection.UserId, 3600) as OkObjectResult;
                var tokenData = agoraTokenResult?.Value as dynamic;

                var caller = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == callerConnection.UserId);

                var callerName = caller != null
                    ? $"{caller.FirstName} {caller.LastName}"
                    : "Unknown User";

                var callerRole = caller != null && caller.RoleId == 2 ? "Doctor" : "Patient";

                // ✅ Check if target user is connected via web app (polling) or mobile app (SignalR)
                bool delivered = false;
                
                // First, check if target is a web app user (polling connection)
                if (_userConnections.TryGetValue(request.TargetUserId, out var targetConnectionId))
                {
                    if (_connections.TryGetValue(targetConnectionId, out var targetConnection))
                    {
                        // ✅ Check if this call has already been rejected by the receiver
                        if (targetConnection.RejectedCalls.Contains(channelName))
                        {
                            _logger.LogInformation("📞 Call {ChannelName} was previously rejected by {TargetUserId}, not re-adding to PendingCalls", channelName, request.TargetUserId);
                            return Ok(new { message = "Call was previously rejected", delivered = false });
                        }

                        // ✅ Check if there's already a call in PendingCalls for this channel
                        var existingCall = targetConnection.PendingCalls.FirstOrDefault(c =>
                        {
                            if (c is JsonElement jsonElement)
                            {
                                if (jsonElement.TryGetProperty("channelName", out var channelNameProp))
                                {
                                    return channelNameProp.GetString() == channelName;
                                }
                            }
                            return false;
                        });

                        if (existingCall != null)
                        {
                            _logger.LogInformation("📞 Call {ChannelName} already exists in PendingCalls for {TargetUserId}, not adding duplicate", channelName, request.TargetUserId);
                            delivered = true; // Call already exists, consider it delivered
                        }
                        else
                        {
                            var callData = new
                            {
                                type = "incoming_call",
                                callerId = callerConnection.UserId,
                                callerName = callerName,
                                callType = request.CallType,
                                channelName = channelName,
                                timestamp = DateTime.UtcNow,
                                agoraAppId = tokenData?.agoraAppId,
                                agoraToken = tokenData?.token
                            };

                            targetConnection.PendingCalls.Add(callData);
                            delivered = true;
                            _logger.LogInformation("📞 Call initiated from {CallerId} to {TargetUserId} (web app user) via polling", callerConnection.UserId, request.TargetUserId);
                        }
                    }
                }
                
                // ✅ Also check if target is a mobile app user (SignalR connection)
                if (MobileHub.UserConnections.TryGetValue(request.TargetUserId, out string? targetSignalRConnectionId))
                {
                    var callData = new
                    {
                        callId = channelName, // Use channel name as callId for consistency
                        callerId = callerConnection.UserId,
                        callerName = callerName,
                        callerRole = callerRole,
                        callType = request.CallType,
                        channelName = channelName,
                        targetUserId = request.TargetUserId, // ✅ Include targetUserId so client can filter
                        timestamp = DateTime.UtcNow.ToString("O")
                    };

                    // ✅ Create a CallSession in MobileHub.ActiveCalls so mobile can accept/reject/end the call
                    var callId = Guid.NewGuid().ToString();
                    var callSession = new Hubs.CallSession
                    {
                        CallId = callId,
                        ChannelName = channelName,
                        CallerId = callerConnection.UserId,
                        TargetUserId = request.TargetUserId,
                        CallType = request.CallType,
                        Status = "ringing",
                        StartTime = DateTime.UtcNow
                    };
                    
                    // Store by both callId (GUID) and channelName for easy lookup
                    MobileHub.ActiveCalls[callId] = callSession;
                    MobileHub.ActiveCalls[channelName] = callSession;
                    
                    _logger.LogInformation("📞 Created CallSession for web-to-mobile call: CallId={CallId}, Channel={Channel}, Caller={CallerId}, Target={TargetUserId}",
                        callId, channelName, callerConnection.UserId, request.TargetUserId);

                    _logger.LogInformation("📞 Sending SignalR call invitation to user {TargetUserId} via connection {ConnectionId} from {CallerId}",
                        request.TargetUserId, targetSignalRConnectionId, callerConnection.UserId);
                    
                    await _hubContext.Clients.Client(targetSignalRConnectionId).SendAsync("incoming-call", callData);
                    
                    _logger.LogInformation("✅ SignalR call invitation sent successfully to connection {ConnectionId} from {CallerId} to {TargetUserId}",
                        targetSignalRConnectionId, callerConnection.UserId, request.TargetUserId);
                    
                    delivered = true;
                }

                if (!delivered)
                {
                    _logger.LogWarning("⚠️ Target user {TargetUserId} is not online. Web connections: {WebConnections}, Mobile connections: {MobileConnections}",
                        request.TargetUserId,
                        string.Join(", ", _userConnections.Select(kvp => $"User {kvp.Key}")),
                        string.Join(", ", MobileHub.UserConnections.Select(kvp => $"User {kvp.Key}")));
                    return Ok(new { message = "Target user is not online", delivered = false });
                }

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

                // ✅ Filter out rejected calls before returning
                var validCalls = connection.PendingCalls.Where(c =>
                {
                    if (c is JsonElement jsonElement)
                    {
                        string? channelName = null;
                        if (jsonElement.TryGetProperty("channelName", out var channelNameProp))
                        {
                            channelName = channelNameProp.GetString();
                        }
                        else if (jsonElement.TryGetProperty("callId", out var callIdProp))
                        {
                            channelName = callIdProp.GetString();
                        }

                        if (!string.IsNullOrEmpty(channelName))
                        {
                            // Don't return calls that have been rejected
                            return !connection.RejectedCalls.Contains(channelName);
                        }
                    }
                    return true; // If we can't determine the channel, include it (shouldn't happen)
                }).ToArray();

                var response = new
                {
                    messages = connection.PendingMessages.ToArray(),
                    calls = validCalls,
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

        [HttpPost("reject-call")]
        public async Task<IActionResult> RejectCall([FromBody] RejectCallRequest request)
        {
            try
            {
                if (!_connections.TryGetValue(request.ConnectionId, out var connection))
                    return BadRequest("Invalid connection");

                var receiverUserId = connection.UserId;
                _logger.LogInformation("📞 RejectCall: Receiver {ReceiverId} rejecting call {CallId}", receiverUserId, request.CallId);

                // ✅ Remove ALL matching calls from receiver's PendingCalls to prevent it from reappearing
                // Extract channel name from request.CallId for matching
                string? channelNameToMatch = request.CallId;
                var callsToRemove = new List<object>();
                
                foreach (var call in connection.PendingCalls)
                {
                    if (call is JsonElement jsonElement)
                    {
                        string? callChannelName = null;
                        if (jsonElement.TryGetProperty("channelName", out var channelNameProp))
                        {
                            callChannelName = channelNameProp.GetString();
                        }
                        else if (jsonElement.TryGetProperty("callId", out var callIdProp))
                        {
                            callChannelName = callIdProp.GetString();
                        }

                        // Match by channel name or callId
                        if (!string.IsNullOrEmpty(callChannelName) && 
                            (callChannelName == request.CallId || 
                             request.CallId == callChannelName ||
                             request.CallId.Contains(callChannelName) ||
                             callChannelName.Contains(request.CallId)))
                        {
                            callsToRemove.Add(call);
                            if (string.IsNullOrEmpty(channelNameToMatch) || channelNameToMatch == request.CallId)
                            {
                                channelNameToMatch = callChannelName; // Use the actual channel name from the call
                            }
                        }
                    }
                }

                // Remove all matching calls
                foreach (var callToRemove in callsToRemove)
                {
                    connection.PendingCalls.Remove(callToRemove);
                }

                if (callsToRemove.Count > 0)
                {
                    _logger.LogInformation("✅ Removed {Count} call(s) matching {CallId} from receiver's PendingCalls", callsToRemove.Count, request.CallId);
                }

                var firstCallToRemove = callsToRemove.FirstOrDefault(); // Keep for extracting callerId below

                // ✅ Try to find the caller and notify them
                // Extract callerId from the call data if available
                int? callerId = null;
                if (firstCallToRemove is JsonElement jsonCall)
                {
                    if (jsonCall.TryGetProperty("callerId", out var callerIdProp))
                    {
                        callerId = callerIdProp.GetInt32();
                    }
                }

                // ✅ Also check MobileHub.ActiveCalls to find the call session and notify caller
                CallSession? callSession = null;
                if (MobileHub.ActiveCalls.TryGetValue(request.CallId, out callSession))
                {
                    callerId = callSession.CallerId;
                }
                else
                {
                    // Try to find by channel name
                    callSession = MobileHub.ActiveCalls.Values.FirstOrDefault(c => c.ChannelName == request.CallId);
                    if (callSession != null)
                    {
                        callerId = callSession.CallerId;
                    }
                }

                // ✅ Mark this call as rejected to prevent it from being re-added
                // Use the channel name we extracted, or fall back to callSession or request.CallId
                string channelNameToReject = channelNameToMatch ?? request.CallId;
                if (callSession != null && !string.IsNullOrEmpty(callSession.ChannelName))
                {
                    channelNameToReject = callSession.ChannelName;
                }

                connection.RejectedCalls.Add(channelNameToReject);
                _logger.LogInformation("✅ Marked call {ChannelName} as rejected to prevent re-adding (RejectedCalls count: {Count})", channelNameToReject, connection.RejectedCalls.Count);

                // ✅ Remove from MobileHub.ActiveCalls if found
                if (callSession != null)
                {
                    MobileHub.ActiveCalls.TryRemove(callSession.CallId, out _);
                    if (!string.IsNullOrEmpty(callSession.ChannelName))
                    {
                        MobileHub.ActiveCalls.TryRemove(callSession.ChannelName, out _);
                    }
                    _logger.LogInformation("✅ Removed call session from MobileHub.ActiveCalls");
                }

                // ✅ Notify caller via SignalR if they're connected
                if (callerId.HasValue)
                {
                    // Check if caller is a mobile app user (SignalR)
                    if (MobileHub.UserConnections.TryGetValue(callerId.Value, out string? callerConnectionId))
                    {
                        await _hubContext.Clients.Client(callerConnectionId).SendAsync("call-rejected", new
                        {
                            callId = request.CallId,
                            channelName = callSession?.ChannelName ?? request.CallId
                        });
                        _logger.LogInformation("✅ Notified caller {CallerId} via SignalR that call was rejected", callerId.Value);
                    }

                    // ✅ Also notify caller if they're a web app user (polling)
                    if (_userConnections.TryGetValue(callerId.Value, out var callerWebConnectionId))
                    {
                        if (_connections.TryGetValue(callerWebConnectionId, out var callerConnection))
                        {
                            // Add call-rejected event to caller's pending messages (they'll get it on next poll)
                            callerConnection.PendingMessages.Add(new
                            {
                                type = "call-rejected",
                                callId = request.CallId,
                                channelName = callSession?.ChannelName ?? request.CallId
                            });
                            _logger.LogInformation("✅ Added call-rejected event to caller's PendingMessages");
                        }
                    }
                }

                return Ok(new { message = "Call rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call");
                return BadRequest($"Failed to reject call: {ex.Message}");
            }
        }

        [HttpPost("end-call")]
        public async Task<IActionResult> EndCall([FromBody] EndCallRequest request)
        {
            try
            {
                if (!_connections.TryGetValue(request.ConnectionId, out var connection))
                    return BadRequest("Invalid connection");

                var userId = connection.UserId;
                _logger.LogInformation("📞 EndCall: User {UserId} ending call {CallId}", userId, request.CallId);

                // ✅ Try to find the call session in MobileHub.ActiveCalls
                CallSession? callSession = null;
                if (MobileHub.ActiveCalls.TryGetValue(request.CallId, out callSession))
                {
                    // Found by key
                }
                else
                {
                    // Try to find by channel name
                    callSession = MobileHub.ActiveCalls.Values.FirstOrDefault(c => c.ChannelName == request.CallId);
                }

                // ✅ Remove from MobileHub.ActiveCalls if found
                if (callSession != null)
                {
                    MobileHub.ActiveCalls.TryRemove(callSession.CallId, out _);
                    if (!string.IsNullOrEmpty(callSession.ChannelName))
                    {
                        MobileHub.ActiveCalls.TryRemove(callSession.ChannelName, out _);
                    }
                    _logger.LogInformation("✅ Removed call session from MobileHub.ActiveCalls");

                    // ✅ Notify both participants
                    var participants = new[] { callSession.CallerId, callSession.TargetUserId };
                    foreach (var participantId in participants)
                    {
                        // Notify via SignalR (mobile app)
                        if (MobileHub.UserConnections.TryGetValue(participantId, out string? participantConnectionId))
                        {
                            await _hubContext.Clients.Client(participantConnectionId).SendAsync("call-ended", new
                            {
                                callId = callSession.CallId,
                                channelName = callSession.ChannelName
                            });
                            _logger.LogInformation("✅ Notified participant {ParticipantId} via SignalR that call ended", participantId);
                        }

                        // Notify via polling (web app)
                        if (_userConnections.TryGetValue(participantId, out var participantWebConnectionId))
                        {
                            if (_connections.TryGetValue(participantWebConnectionId, out var participantConnection))
                            {
                                participantConnection.PendingMessages.Add(new
                                {
                                    type = "call-ended",
                                    callId = callSession.CallId,
                                    channelName = callSession.ChannelName
                                });
                                _logger.LogInformation("✅ Added call-ended event to participant's PendingMessages");
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Call session not found for ending: {CallId}", request.CallId);
                }

                return Ok(new { message = "Call ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call");
                return BadRequest($"Failed to end call: {ex.Message}");
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

                _logger.LogInformation("Getting message history for users {UserId} and {OtherUserId}", userId, otherUserId);

                var messages = await _context.SmsMessages
                    .Include(m => m.Sender)
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

                _logger.LogInformation("Retrieved {Count} messages", messages.Count);
                return Ok(new { messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message history: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
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

        // ===================== AUTH HELPERS ==========================

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

                // Use the same JWT key from configuration as the main authentication
                var jwtKey = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
                
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
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

    // ===================== DTOs ==========================

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
        public HashSet<string> RejectedCalls { get; set; } = new(); // Track rejected calls to prevent re-adding
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

    public class RejectCallRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
    }

    public class EndCallRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
    }
}
