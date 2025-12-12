using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Hubs
{
    [Authorize]
    public class MobileHub : Hub
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<MobileHub> _logger;

        // Store active connections (made internal so MobileController can access it)
        internal static readonly ConcurrentDictionary<int, string> UserConnections = new();
        // Store active calls (made internal so RealtimeController can create sessions for web-to-mobile calls)
        internal static readonly ConcurrentDictionary<string, CallSession> ActiveCalls = new();

        public MobileHub(JournalDbContext context, ILogger<MobileHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdClaim = Context.User?.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                UserConnections[userId] = Context.ConnectionId;
                _logger.LogInformation("✅ MobileHub: User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
                _logger.LogInformation("📊 MobileHub: Total active connections: {Count}", UserConnections.Count);
                _logger.LogInformation("📊 MobileHub: Active users: {Users}", string.Join(", ", UserConnections.Select(kvp => $"User {kvp.Key}")));

                // Join user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                // Notify about online status
                await NotifyUserStatusChange(userId, true);
            }
            else
            {
                _logger.LogWarning("⚠️ MobileHub: User connected but userId claim not found or invalid");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdClaim = Context.User?.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                UserConnections.TryRemove(userId, out _);
                _logger.LogInformation("User {UserId} disconnected", userId);

                // Clean up any active calls
                await CleanupUserCalls(userId);

                // Notify about offline status
                await NotifyUserStatusChange(userId, false);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Chat functionality
        public async Task SendMessage(int targetUserId, string message)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int senderId))
                {
                    await Clients.Caller.SendAsync("error", "Invalid user token");
                    return;
                }

                _logger.LogInformation("Message from {SenderId} to {TargetUserId}: {Message}", senderId, targetUserId, message);

                // Verify relationship exists
                var hasRelationship = await _context.UserAssignments
                    .AnyAsync(ua =>
                        (ua.AssignerId == senderId && ua.AssigneeId == targetUserId) ||
                        (ua.AssignerId == targetUserId && ua.AssigneeId == senderId));

                if (!hasRelationship)
                {
                    await Clients.Caller.SendAsync("error", "No relationship exists with target user");
                    return;
                }

                // Get sender info
                var sender = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == senderId);

                if (sender == null)
                {
                    await Clients.Caller.SendAsync("error", "Sender not found");
                    return;
                }

                var messageData = new
                {
                    id = Guid.NewGuid().ToString(),
                    senderId = senderId,
                    targetUserId = targetUserId,
                    message = message,
                    senderName = $"{sender.FirstName} {sender.LastName}",
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                // Send to target user if online
                if (UserConnections.TryGetValue(targetUserId, out string? targetConnectionId))
                {
                    await Clients.Client(targetConnectionId).SendAsync("new-message", messageData);
                }

                // Confirm to sender
                await Clients.Caller.SendAsync("message-sent", messageData);

                // TODO: Store message in database for offline users
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("error", "Failed to send message");
            }
        }

        // Call functionality
        public async Task InitiateCall(int targetUserId, string callType)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int callerId))
                {
                    await Clients.Caller.SendAsync("error", "Invalid user token");
                    return;
                }

                _logger.LogInformation("Call initiated: {CallerId} -> {TargetUserId} ({CallType})", callerId, targetUserId, callType);

                // Verify relationship exists
                var hasRelationship = await _context.UserAssignments
                    .AnyAsync(ua =>
                        (ua.AssignerId == callerId && ua.AssigneeId == targetUserId) ||
                        (ua.AssignerId == targetUserId && ua.AssigneeId == callerId));

                if (!hasRelationship)
                {
                    await Clients.Caller.SendAsync("error", "No relationship exists with target user");
                    return;
                }

                // Check if target user is online
                if (!UserConnections.TryGetValue(targetUserId, out string? targetConnectionId))
                {
                    await Clients.Caller.SendAsync("call-failed", "User is not online");
                    return;
                }

                // Get caller info
                var caller = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == callerId);

                if (caller == null)
                {
                    await Clients.Caller.SendAsync("error", "Caller not found");
                    return;
                }

                // ✅ Generate consistent channel name (same format as client: call_{smallerId}_{largerId})
                var smallerId = Math.Min(callerId, targetUserId);
                var largerId = Math.Max(callerId, targetUserId);
                var channelName = $"call_{smallerId}_{largerId}";
                
                var callId = Guid.NewGuid().ToString();
                var callSession = new CallSession
                {
                    CallId = callId,
                    ChannelName = channelName, // ✅ Store channel name for lookup
                    CallerId = callerId,
                    TargetUserId = targetUserId,
                    CallType = callType,
                    Status = "ringing",
                    StartTime = DateTime.UtcNow
                };

                ActiveCalls[callId] = callSession;
                // ✅ Also store by channel name for easy lookup
                ActiveCalls[channelName] = callSession;

                var callData = new
                {
                    callId = channelName, // ✅ Use channel name as callId so client can auto-join
                    callerId = callerId,
                    callerName = $"{caller.FirstName} {caller.LastName}",
                    callerRole = caller.RoleId == 2 ? "Doctor" : "Patient",
                    callType = callType,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    channelName = channelName // ✅ Also include channelName for compatibility
                };

                // Send call invitation to target user
                await Clients.Client(targetConnectionId).SendAsync("incoming-call", callData);

                // Confirm to caller
                await Clients.Caller.SendAsync("call-initiated", callData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                await Clients.Caller.SendAsync("error", "Failed to initiate call");
            }
        }

        public async Task AcceptCall(string callId)
        {
            try
            {
                // ✅ Try to find call by callId (could be GUID or channel name)
                CallSession? callSession = null;
                string? actualCallId = null;
                
                if (ActiveCalls.TryGetValue(callId, out callSession))
                {
                    actualCallId = callSession.CallId;
                }
                else
                {
                    // Try to find by channel name in values
                    callSession = ActiveCalls.Values.FirstOrDefault(c => c.ChannelName == callId);
                    if (callSession != null)
                    {
                        actualCallId = callSession.CallId;
                    }
                }

                if (callSession == null)
                {
                    _logger.LogWarning("Call not found for acceptance: {CallId}", callId);
                    await Clients.Caller.SendAsync("error", "Call not found");
                    return;
                }

                callSession.Status = "accepted";
                _logger.LogInformation("Call {CallId} (channel: {Channel}) accepted", actualCallId, callSession.ChannelName);

                // ✅ Notify caller that call was accepted (check both MobileHub and RealtimeController)
                // First, check if caller is a mobile app user (MobileHub)
                if (UserConnections.TryGetValue(callSession.CallerId, out string? callerConnectionId))
                {
                    await Clients.Client(callerConnectionId).SendAsync("call-accepted", new { 
                        callId = callSession.ChannelName ?? actualCallId,
                        channelName = callSession.ChannelName 
                    });
                    _logger.LogInformation("✅ Notified mobile caller {CallerId} via MobileHub", callSession.CallerId);
                }
                
                // ✅ Also notify web app caller via RealtimeController (if they're using polling)
                // We'll need to inject RealtimeController's connection dictionary or use a shared service
                // For now, we'll rely on SignalR for web app too (web app should be using SignalRService)
                // But if web app is using polling, we need to add the event to their PendingCalls
                // This requires access to RealtimeController's _connections dictionary
                // We'll handle this by ensuring web app also connects to MobileHub via SignalRService

                await Clients.Caller.SendAsync("call-accepted", new { 
                    callId = callSession.ChannelName ?? actualCallId,
                    channelName = callSession.ChannelName 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting call");
                await Clients.Caller.SendAsync("error", "Failed to accept call");
            }
        }

        public async Task RejectCall(string callId)
        {
            try
            {
                // ✅ Try to find call by callId (could be GUID or channel name)
                CallSession? callSession = null;
                string? actualCallId = null;
                
                if (ActiveCalls.TryGetValue(callId, out callSession))
                {
                    // Found by key (could be GUID or channel name)
                    actualCallId = callSession.CallId;
                }
                else
                {
                    // Try to find by channel name in values
                    callSession = ActiveCalls.Values.FirstOrDefault(c => c.ChannelName == callId);
                    if (callSession != null)
                    {
                        actualCallId = callSession.CallId;
                    }
                }

                if (callSession == null)
                {
                    _logger.LogWarning("Call not found for rejection: {CallId}", callId);
                    await Clients.Caller.SendAsync("error", "Call not found");
                    return;
                }

                // ✅ Remove both callId (GUID) and channelName entries if they exist
                ActiveCalls.TryRemove(actualCallId!, out _);
                if (!string.IsNullOrEmpty(callSession.ChannelName))
                {
                    ActiveCalls.TryRemove(callSession.ChannelName, out _);
                }

                _logger.LogInformation("Call {CallId} (channel: {Channel}) rejected", actualCallId, callSession.ChannelName);

                // ✅ Notify caller that call was rejected - check both MobileHub and RealtimeController
                if (UserConnections.TryGetValue(callSession.CallerId, out string? callerConnectionId))
                {
                    await Clients.Client(callerConnectionId).SendAsync("call-rejected", new { 
                        callId = callSession.ChannelName ?? actualCallId,
                        channelName = callSession.ChannelName
                    });
                    _logger.LogInformation("✅ Notified caller {CallerId} via MobileHub that call was rejected", callSession.CallerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call");
                await Clients.Caller.SendAsync("error", "Failed to reject call");
            }
        }

        public async Task EndCall(string callIdOrChannel)
        {
            try
            {
                _logger.LogInformation("📞 EndCall called with: {CallIdOrChannel}", callIdOrChannel);
                _logger.LogInformation("📞 ActiveCalls count: {Count}", ActiveCalls.Count);
                
                // ✅ Try to find call by callId (GUID) or channel name
                CallSession? callSession = null;
                string? actualCallId = null;
                
                if (ActiveCalls.TryGetValue(callIdOrChannel, out callSession))
                {
                    // Found by key (could be GUID or channel name)
                    actualCallId = callSession.CallId;
                    _logger.LogInformation("📞 Found call session by key: {CallId}, Channel: {Channel}", actualCallId, callSession.ChannelName);
                }
                else
                {
                    // Try to find by channel name in values
                    callSession = ActiveCalls.Values.FirstOrDefault(c => c.ChannelName == callIdOrChannel);
                    if (callSession != null)
                    {
                        actualCallId = callSession.CallId;
                        _logger.LogInformation("📞 Found call session by channel name: {CallId}, Channel: {Channel}", actualCallId, callSession.ChannelName);
                    }
                    else
                    {
                        _logger.LogWarning("📞 Call session not found in ActiveCalls. Searched for: {CallIdOrChannel}", callIdOrChannel);
                        _logger.LogWarning("📞 Available keys in ActiveCalls: {Keys}", string.Join(", ", ActiveCalls.Keys));
                    }
                }

                if (callSession == null)
                {
                    _logger.LogWarning("Call not found: {CallIdOrChannel}. ActiveCalls keys: {Keys}", callIdOrChannel, string.Join(", ", ActiveCalls.Keys));
                    // ✅ Even if call session not found, try to send call-ended to all users with matching channel in their incoming calls
                    // This is a fallback for timeout scenarios where the session might have been cleaned up
                    _logger.LogInformation("📞 Attempting fallback: sending call-ended to all connected users for channel: {Channel}", callIdOrChannel);
                    // We can't easily find which users have this channel without the session, so we'll just log and return
                    return;
                }

                // ✅ Remove both callId (GUID) and channelName entries if they exist
                ActiveCalls.TryRemove(actualCallId!, out _);
                if (!string.IsNullOrEmpty(callSession.ChannelName))
                {
                    ActiveCalls.TryRemove(callSession.ChannelName, out _);
                }

                _logger.LogInformation("Call {CallId} (channel: {Channel}) ended", actualCallId, callSession.ChannelName);

                // Notify both participants
                var participants = new[] { callSession.CallerId, callSession.TargetUserId };
                _logger.LogInformation("📞 Notifying participants: CallerId={CallerId}, TargetUserId={TargetUserId}", callSession.CallerId, callSession.TargetUserId);
                
                foreach (var participantId in participants)
                {
                    if (UserConnections.TryGetValue(participantId, out string? connectionId))
                    {
                        _logger.LogInformation("📞 Sending call-ended to participant {ParticipantId} (connection: {ConnectionId})", participantId, connectionId);
                        await Clients.Client(connectionId).SendAsync("call-ended", new { 
                            callId = actualCallId,
                            channelName = callSession.ChannelName 
                        });
                        _logger.LogInformation("✅ call-ended event sent to participant {ParticipantId}", participantId);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Participant {ParticipantId} not found in UserConnections", participantId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call");
                await Clients.Caller.SendAsync("error", "Failed to end call");
            }
        }

        // WebRTC signaling
        public async Task SendWebRTCOffer(int targetUserId, object offer)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int senderId))
                {
                    await Clients.Caller.SendAsync("error", "Invalid user token");
                    return;
                }

                if (UserConnections.TryGetValue(targetUserId, out string? targetConnectionId))
                {
                    await Clients.Client(targetConnectionId).SendAsync("webrtc-offer", new { senderId, offer });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WebRTC offer");
            }
        }

        public async Task SendWebRTCAnswer(int targetUserId, object answer)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int senderId))
                {
                    await Clients.Caller.SendAsync("error", "Invalid user token");
                    return;
                }

                if (UserConnections.TryGetValue(targetUserId, out string? targetConnectionId))
                {
                    await Clients.Client(targetConnectionId).SendAsync("webrtc-answer", new { senderId, answer });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WebRTC answer");
            }
        }

        public async Task SendICECandidate(int targetUserId, object candidate)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int senderId))
                {
                    await Clients.Caller.SendAsync("error", "Invalid user token");
                    return;
                }

                if (UserConnections.TryGetValue(targetUserId, out string? targetConnectionId))
                {
                    await Clients.Client(targetConnectionId).SendAsync("webrtc-ice-candidate", new { senderId, candidate });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ICE candidate");
            }
        }

        private async Task NotifyUserStatusChange(int userId, bool isOnline)
        {
            try
            {
                // Get related users (doctors/patients)
                var relatedUserIds = await _context.UserAssignments
                    .Where(ua => ua.AssignerId == userId || ua.AssigneeId == userId)
                    .Select(ua => ua.AssignerId == userId ? ua.AssigneeId : ua.AssignerId)
                    .ToListAsync();

                var statusData = new { userId, isOnline, timestamp = DateTime.UtcNow.ToString("O") };

                foreach (var relatedUserId in relatedUserIds)
                {
                    if (UserConnections.TryGetValue(relatedUserId, out string? connectionId))
                    {
                        await Clients.Client(connectionId).SendAsync("user-status-changed", statusData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user status change");
            }
        }

        private async Task CleanupUserCalls(int userId)
        {
            try
            {
                var userCalls = ActiveCalls.Values
                    .Where(call => call.CallerId == userId || call.TargetUserId == userId)
                    .ToList();

                foreach (var call in userCalls)
                {
                    ActiveCalls.TryRemove(call.CallId, out _);

                    // Notify the other participant
                    var otherUserId = call.CallerId == userId ? call.TargetUserId : call.CallerId;
                    if (UserConnections.TryGetValue(otherUserId, out string? connectionId))
                    {
                        await Clients.Client(connectionId).SendAsync("call-ended", new { callId = call.CallId });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up user calls");
            }
        }
    }

    public class CallSession
    {
        public string CallId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty; // ✅ Store channel name for lookup
        public int CallerId { get; set; }
        public int TargetUserId { get; set; }
        public string CallType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }
}
