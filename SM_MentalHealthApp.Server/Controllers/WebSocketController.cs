using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebSocketController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<WebSocketController> _logger;
        private static readonly Dictionary<string, WebSocket> _connections = new();
        private static readonly Dictionary<string, int> _userConnections = new();

        public WebSocketController(JournalDbContext context, ILogger<WebSocketController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("connect")]
        public async Task<IActionResult> Connect()
        {
            _logger.LogInformation("WebSocket connection attempt from {RemoteIpAddress}", HttpContext.Connection.RemoteIpAddress);
            _logger.LogInformation("Request headers: {Headers}", string.Join(", ", HttpContext.Request.Headers.Select(h => $"{h.Key}: {h.Value}")));

            // Check if it's a WebSocket request or if it's coming from a mobile app
            var isWebSocketRequest = HttpContext.WebSockets.IsWebSocketRequest;
            var hasWebSocketHeaders = HttpContext.Request.Headers.ContainsKey("Upgrade") &&
                                    HttpContext.Request.Headers.ContainsKey("Connection");
            var isMobileRequest = HttpContext.Request.Headers.UserAgent.ToString().Contains("Expo") ||
                                 HttpContext.Request.Headers.UserAgent.ToString().Contains("ReactNative");

            _logger.LogInformation("WebSocket check - IsWebSocketRequest: {IsWebSocket}, HasHeaders: {HasHeaders}, IsMobile: {IsMobile}",
                isWebSocketRequest, hasWebSocketHeaders, isMobileRequest);

            if (isWebSocketRequest || (hasWebSocketHeaders && isMobileRequest))
            {
                try
                {
                    var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    var connectionId = Guid.NewGuid().ToString();
                    _connections[connectionId] = webSocket;

                    _logger.LogInformation("WebSocket connection established: {ConnectionId}", connectionId);

                    // Start handling messages
                    _ = Task.Run(() => HandleWebSocketAsync(webSocket, connectionId));

                    return new EmptyResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting WebSocket connection");
                    return BadRequest($"WebSocket connection failed: {ex.Message}");
                }
            }
            else
            {
                _logger.LogWarning("Not a WebSocket request. Headers: {Headers}",
                    string.Join(", ", HttpContext.Request.Headers.Select(h => $"{h.Key}: {h.Value}")));
                return BadRequest("WebSocket connection required");
            }
        }

        private async Task HandleWebSocketAsync(WebSocket webSocket, string connectionId)
        {
            var buffer = new byte[4096];
            int? userId = null;

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var (response, newUserId) = await ProcessMessage(message, connectionId, userId);

                        if (newUserId.HasValue)
                        {
                            userId = newUserId.Value;
                        }

                        if (!string.IsNullOrEmpty(response))
                        {
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket connection closed: {ConnectionId}", connectionId);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection: {ConnectionId}", connectionId);
            }
            finally
            {
                if (userId.HasValue)
                {
                    _userConnections.Remove(connectionId);
                    _logger.LogInformation("User {UserId} disconnected", userId.Value);
                }
                _connections.Remove(connectionId);
                webSocket.Dispose();
            }
        }

        private async Task<(string? response, int? newUserId)> ProcessMessage(string message, string connectionId, int? currentUserId)
        {
            try
            {
                var data = JsonSerializer.Deserialize<JsonElement>(message);
                var messageType = data.GetProperty("type").GetString();

                switch (messageType)
                {
                    case "auth":
                        var authResult = await HandleAuth(data, connectionId);
                        return (authResult.response, authResult.userId);
                    case "send-message":
                        var sendResult = await HandleSendMessage(data, currentUserId);
                        return (sendResult, currentUserId);
                    case "initiate-call":
                        var callResult = await HandleInitiateCall(data, currentUserId);
                        return (callResult, currentUserId);
                    case "accept-call":
                        var acceptResult = await HandleAcceptCall(data, currentUserId);
                        return (acceptResult, currentUserId);
                    case "reject-call":
                        var rejectResult = await HandleRejectCall(data, currentUserId);
                        return (rejectResult, currentUserId);
                    case "end-call":
                        var endResult = await HandleEndCall(data, currentUserId);
                        return (endResult, currentUserId);
                    default:
                        return (JsonSerializer.Serialize(new { type = "error", message = "Unknown message type" }), currentUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", message);
                return (JsonSerializer.Serialize(new { type = "error", message = "Invalid message format" }), currentUserId);
            }
        }

        private async Task<(string response, int? userId)> HandleAuth(JsonElement data, string connectionId)
        {
            try
            {
                var token = data.GetProperty("token").GetString();
                if (string.IsNullOrEmpty(token))
                {
                    return (JsonSerializer.Serialize(new { type = "auth-error", message = "Token required" }), null);
                }

                // For now, we'll use a simple token validation
                // In production, you should validate the JWT token properly
                if (token.Length > 10) // Simple validation
                {
                    // Extract user ID from token (simplified)
                    var userId = 1; // This should be extracted from the actual JWT token
                    _userConnections[connectionId] = userId;

                    _logger.LogInformation("User {UserId} authenticated with connection {ConnectionId}", userId, connectionId);

                    return (JsonSerializer.Serialize(new
                    {
                        type = "auth-success",
                        message = "Authentication successful",
                        userId = userId
                    }), userId);
                }
                else
                {
                    return (JsonSerializer.Serialize(new { type = "auth-error", message = "Invalid token" }), null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling authentication");
                return (JsonSerializer.Serialize(new { type = "auth-error", message = "Authentication failed" }), null);
            }
        }

        private async Task<string> HandleSendMessage(JsonElement data, int? userId)
        {
            if (!userId.HasValue)
            {
                return JsonSerializer.Serialize(new { type = "error", message = "Not authenticated" });
            }

            try
            {
                var targetUserId = data.GetProperty("targetUserId").GetInt32();
                var message = data.GetProperty("message").GetString();

                var messageData = new
                {
                    type = "new-message",
                    id = Guid.NewGuid().ToString(),
                    senderId = userId.Value,
                    targetUserId = targetUserId,
                    message = message,
                    senderName = "User", // This should be fetched from database
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                // Send to target user if connected
                var targetConnection = _userConnections.FirstOrDefault(kvp => kvp.Value == targetUserId);
                if (targetConnection.Key != null && _connections.TryGetValue(targetConnection.Key, out var targetWebSocket))
                {
                    var messageJson = JsonSerializer.Serialize(messageData);
                    var messageBytes = Encoding.UTF8.GetBytes(messageJson);
                    await targetWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                return JsonSerializer.Serialize(new { type = "message-sent", message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return JsonSerializer.Serialize(new { type = "error", message = "Failed to send message" });
            }
        }

        private async Task<string> HandleInitiateCall(JsonElement data, int? userId)
        {
            if (!userId.HasValue)
            {
                return JsonSerializer.Serialize(new { type = "error", message = "Not authenticated" });
            }

            try
            {
                var targetUserId = data.GetProperty("targetUserId").GetInt32();
                var callType = data.GetProperty("callType").GetString();

                var callData = new
                {
                    type = "incoming-call",
                    callId = Guid.NewGuid().ToString(),
                    callerId = userId.Value,
                    callerName = "User", // This should be fetched from database
                    callType = callType,
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                // Send to target user if connected
                var targetConnection = _userConnections.FirstOrDefault(kvp => kvp.Value == targetUserId);
                if (targetConnection.Key != null && _connections.TryGetValue(targetConnection.Key, out var targetWebSocket))
                {
                    var callJson = JsonSerializer.Serialize(callData);
                    var callBytes = Encoding.UTF8.GetBytes(callJson);
                    await targetWebSocket.SendAsync(new ArraySegment<byte>(callBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                return JsonSerializer.Serialize(new { type = "call-initiated", message = "Call initiated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                return JsonSerializer.Serialize(new { type = "error", message = "Failed to initiate call" });
            }
        }

        private async Task<string> HandleAcceptCall(JsonElement data, int? userId)
        {
            if (!userId.HasValue)
            {
                return JsonSerializer.Serialize(new { type = "error", message = "Not authenticated" });
            }

            try
            {
                var callId = data.GetProperty("callId").GetString();

                return JsonSerializer.Serialize(new
                {
                    type = "call-accepted",
                    message = "Call accepted successfully",
                    callId = callId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting call");
                return JsonSerializer.Serialize(new { type = "error", message = "Failed to accept call" });
            }
        }

        private async Task<string> HandleRejectCall(JsonElement data, int? userId)
        {
            if (!userId.HasValue)
            {
                return JsonSerializer.Serialize(new { type = "error", message = "Not authenticated" });
            }

            try
            {
                var callId = data.GetProperty("callId").GetString();

                return JsonSerializer.Serialize(new
                {
                    type = "call-rejected",
                    message = "Call rejected successfully",
                    callId = callId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call");
                return JsonSerializer.Serialize(new { type = "error", message = "Failed to reject call" });
            }
        }

        private async Task<string> HandleEndCall(JsonElement data, int? userId)
        {
            if (!userId.HasValue)
            {
                return JsonSerializer.Serialize(new { type = "error", message = "Not authenticated" });
            }

            try
            {
                var callId = data.GetProperty("callId").GetString();

                return JsonSerializer.Serialize(new
                {
                    type = "call-ended",
                    message = "Call ended successfully",
                    callId = callId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call");
                return JsonSerializer.Serialize(new { type = "error", message = "Failed to end call" });
            }
        }
    }
}
