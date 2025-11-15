using System.Text.Json;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services
{
    public interface IRealtimeService
    {
        bool IsConnected { get; }
        string? ConnectionId { get; }
        event Action<ChatMessage>? OnNewMessage;
        event Action<CallInvitation>? OnIncomingCall;
        event Action<string>? OnCallAccepted;
        event Action<string>? OnCallRejected;
        event Action<string>? OnCallEnded;
        event Action<bool>? OnConnectionChanged;

        Task StartAsync();
        Task StopAsync();
        Task SendMessageAsync(int targetUserId, string message);
        Task InitiateCallAsync(int targetUserId, string callType);
        Task AcceptCallAsync(string callId);
        Task RejectCallAsync(string callId);
        Task EndCallAsync(string callId);
    }

    public class RealtimeService : IRealtimeService, IAsyncDisposable
    {
        private readonly IAuthService _authService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RealtimeService> _logger;
        private string? _connectionId;

        public string? ConnectionId => _connectionId;
        private Timer? _pollingTimer;
        private Timer? _healthCheckTimer;
        private Timer? _pollingRestartTimer;
        private bool _isPolling = false;
        private bool _isConnecting = false;
        private DateTime _lastSuccessfulPoll = DateTime.UtcNow;

        public bool IsConnected => !string.IsNullOrEmpty(_connectionId) && _isPolling;

        public event Action<ChatMessage>? OnNewMessage;
        public event Action<CallInvitation>? OnIncomingCall;
        public event Action<string>? OnCallAccepted;
        public event Action<string>? OnCallRejected;
        public event Action<string>? OnCallEnded;
        public event Action<bool>? OnConnectionChanged;

        public RealtimeService(IAuthService authService, HttpClient httpClient, ILogger<RealtimeService> logger)
        {
            _authService = authService;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            // Prevent multiple simultaneous connection attempts
            if (_isConnecting)
            {
                _logger.LogInformation("Connection already in progress, skipping...");
                return;
            }

            _isConnecting = true;

            try
            {
                // Always stop any existing connection first
                if (_isPolling || !string.IsNullOrEmpty(_connectionId))
                {
                    _logger.LogInformation("Stopping existing connection before starting new one");
                    await StopAsync();
                    await Task.Delay(1000); // Wait a bit for cleanup
                }

                var token = _authService.Token;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No auth token available for realtime connection");
                    return;
                }

                var request = new
                {
                    UserId = _authService.CurrentUser?.Id ?? 0
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync("/api/realtime/connect", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (result.TryGetProperty("connectionId", out var connectionIdElement))
                    {
                        _connectionId = connectionIdElement.GetString();
                        _isPolling = true;

                        // Start polling for messages
                        _pollingTimer = new Timer(PollForMessages, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

                        // Start health check timer (every 30 seconds)
                        _healthCheckTimer = new Timer(HealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

                        // Start polling restart timer (every 2 minutes) to detect if polling stops
                        _pollingRestartTimer = new Timer(CheckPollingHealth, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));

                        _logger.LogInformation("Realtime connection established: {ConnectionId} (polling every 5 seconds)", _connectionId);
                        OnConnectionChanged?.Invoke(true);
                    }
                }
                else
                {
                    _logger.LogError("Failed to connect to realtime service: {StatusCode}", response.StatusCode);
                    OnConnectionChanged?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting realtime connection");
                OnConnectionChanged?.Invoke(false);
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public async Task StopAsync()
        {
            if (!string.IsNullOrEmpty(_connectionId))
            {
                try
                {
                    var request = new { ConnectionId = _connectionId };
                    var json = JsonSerializer.Serialize(request);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    await _httpClient.PostAsync("/api/realtime/disconnect", content);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting from realtime service");
                }
            }

            _pollingTimer?.Dispose();
            _pollingTimer = null;
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;
            _pollingRestartTimer?.Dispose();
            _pollingRestartTimer = null;
            _isPolling = false;
            _connectionId = null;
            OnConnectionChanged?.Invoke(false);
        }

        private async void PollForMessages(object? state)
        {
            if (string.IsNullOrEmpty(_connectionId) || !_isPolling)
            {
                _logger.LogDebug("Polling skipped - ConnectionId: {ConnectionId}, IsPolling: {IsPolling}", _connectionId, _isPolling);
                return;
            }

            try
            {
                _logger.LogDebug("Polling for messages with connection: {ConnectionId}", _connectionId);
                var request = new { ConnectionId = _connectionId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/realtime/poll", content);
                _logger.LogDebug("Poll response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    _lastSuccessfulPoll = DateTime.UtcNow; // Track successful polls
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (result.TryGetProperty("messages", out var messagesElement))
                    {
                        var messages = messagesElement.EnumerateArray();
                        var messageCount = 0;
                        foreach (var message in messages)
                        {
                            ProcessMessage(message);
                            messageCount++;
                        }
                        if (messageCount > 0)
                        {
                            _logger.LogDebug("Processed {MessageCount} messages", messageCount);
                        }
                    }

                    if (result.TryGetProperty("calls", out var callsElement))
                    {
                        var calls = callsElement.EnumerateArray();
                        var callCount = 0;
                        foreach (var call in calls)
                        {
                            ProcessCall(call);
                            callCount++;
                        }
                        if (callCount > 0)
                        {
                            _logger.LogDebug("Processed {CallCount} calls", callCount);
                        }
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    // Invalid connection ID - stop polling and let health check handle reconnection
                    _logger.LogWarning("Invalid connection ID detected, stopping polling...");
                    _isPolling = false; // Stop polling immediately
                    _connectionId = null; // Clear connection ID
                    OnConnectionChanged?.Invoke(false); // Notify UI of disconnection
                    _logger.LogInformation("Connection stopped due to invalid ID. Health check will attempt reconnection.");
                }
                else
                {
                    _logger.LogWarning("Polling failed with status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling for messages");
                // On error, just log it and let health check handle reconnection
                _logger.LogInformation("Polling error occurred, health check will handle reconnection if needed.");
            }
        }

        private async void HealthCheck(object? state)
        {
            // If not connected, try to reconnect
            if (string.IsNullOrEmpty(_connectionId) || !_isPolling)
            {
                _logger.LogInformation("Health check: Not connected, attempting to reconnect...");
                if (!_isConnecting) // Prevent multiple simultaneous connection attempts
                {
                    await StartAsync();
                }
                return;
            }

            try
            {
                // Test the connection by making a simple poll request
                var request = new { ConnectionId = _connectionId };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/realtime/poll", content);

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    _logger.LogWarning("Health check failed - connection invalid, reconnecting...");
                    await StopAsync();
                    await Task.Delay(2000); // Brief delay before reconnecting
                    if (!_isConnecting)
                    {
                        await StartAsync();
                    }
                }
                else if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Health check failed with status: {StatusCode}, reconnecting...", response.StatusCode);
                    await StopAsync();
                    await Task.Delay(2000);
                    if (!_isConnecting)
                    {
                        await StartAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check error, reconnecting...");
                await StopAsync();
                await Task.Delay(2000);
                if (!_isConnecting)
                {
                    await StartAsync();
                }
            }
        }

        private void ProcessMessage(JsonElement message)
        {
            try
            {
                if (message.TryGetProperty("type", out var typeElement))
                {
                    var messageType = typeElement.GetString();

                    switch (messageType)
                    {
                        case "new-message":
                            var chatMessage = new ChatMessage
                            {
                                Id = message.GetProperty("id").GetString() ?? "",
                                SenderId = message.GetProperty("senderId").GetInt32(),
                                TargetUserId = message.GetProperty("targetUserId").GetInt32(),
                                Message = message.GetProperty("message").GetString() ?? "",
                                SenderName = message.GetProperty("senderName").GetString() ?? "",
                                Timestamp = DateTime.Parse(message.GetProperty("timestamp").GetString() ?? DateTime.UtcNow.ToString())
                            };
                            OnNewMessage?.Invoke(chatMessage);
                            break;

                        case "incoming-call":
                            // ✅ Try to get channelName first, fallback to callId
                            var channelName = message.TryGetProperty("channelName", out var channelNameElement)
                                ? channelNameElement.GetString()
                                : message.GetProperty("callId").GetString();

                            var callInvitation = new CallInvitation
                            {
                                CallId = channelName ?? "", // ✅ Use channel name as CallId
                                CallerId = message.GetProperty("callerId").GetInt32(),
                                CallerName = message.GetProperty("callerName").GetString() ?? "",
                                CallerRole = message.GetProperty("callerRole").GetString() ?? "",
                                CallType = message.GetProperty("callType").GetString() ?? "",
                                Timestamp = DateTime.Parse(message.GetProperty("timestamp").GetString() ?? DateTime.UtcNow.ToString())
                            };
                            OnIncomingCall?.Invoke(callInvitation);
                            break;

                        case "call-accepted":
                            OnCallAccepted?.Invoke(message.GetProperty("callId").GetString() ?? "");
                            break;

                        case "call-rejected":
                            OnCallRejected?.Invoke(message.GetProperty("callId").GetString() ?? "");
                            break;

                        case "call-ended":
                            OnCallEnded?.Invoke(message.GetProperty("callId").GetString() ?? "");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", message);
            }
        }

        private void ProcessCall(JsonElement call)
        {
            try
            {
                if (call.TryGetProperty("type", out var typeElement))
                {
                    var callType = typeElement.GetString();

                    if (callType == "incoming_call")
                    {
                        var callInvitation = new CallInvitation
                        {
                            CallId = call.GetProperty("channelName").GetString() ?? "",
                            CallerId = call.GetProperty("callerId").GetInt32(),
                            CallerName = call.GetProperty("callerName").GetString() ?? "",
                            CallerRole = "Unknown", // We don't have this in the new format
                            CallType = call.GetProperty("callType").GetString() ?? "",
                            Timestamp = DateTime.Parse(call.GetProperty("timestamp").GetString() ?? DateTime.UtcNow.ToString())
                        };

                        // Store Agora details for the call
                        if (call.TryGetProperty("agoraAppId", out var appIdElement))
                        {
                            // Store Agora app ID and token for later use
                            _logger.LogInformation("Agora App ID: {AppId}", appIdElement.GetString());
                        }

                        if (call.TryGetProperty("agoraToken", out var tokenElement))
                        {
                            // Store Agora token for later use
                            _logger.LogInformation("Agora Token: {Token}", tokenElement.GetString());
                        }

                        OnIncomingCall?.Invoke(callInvitation);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing call");
            }
        }

        public async Task SendMessageAsync(int targetUserId, string message)
        {
            if (!IsConnected || string.IsNullOrEmpty(_connectionId))
            {
                _logger.LogWarning("Cannot send message: not connected");
                return;
            }

            try
            {
                var request = new
                {
                    ConnectionId = _connectionId,
                    TargetUserId = targetUserId,
                    Message = message
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                await _httpClient.PostAsync("/api/realtime/send-message", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
            }
        }

        public async Task InitiateCallAsync(int targetUserId, string callType)
        {
            if (!IsConnected || string.IsNullOrEmpty(_connectionId))
            {
                _logger.LogWarning("Cannot initiate call: not connected");
                return;
            }

            try
            {
                var request = new
                {
                    ConnectionId = _connectionId,
                    TargetUserId = targetUserId,
                    CallType = callType
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                await _httpClient.PostAsync("/api/realtime/initiate-call", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
            }
        }

        public async Task AcceptCallAsync(string callId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_connectionId))
            {
                _logger.LogWarning("Cannot accept call: not connected");
                return;
            }

            try
            {
                var request = new
                {
                    ConnectionId = _connectionId,
                    CallId = callId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                await _httpClient.PostAsync("/api/realtime/accept-call", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting call");
            }
        }

        public async Task RejectCallAsync(string callId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_connectionId))
            {
                _logger.LogWarning("Cannot reject call: not connected");
                return;
            }

            try
            {
                var request = new
                {
                    ConnectionId = _connectionId,
                    CallId = callId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                await _httpClient.PostAsync("/api/realtime/reject-call", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call");
            }
        }

        public async Task EndCallAsync(string callId)
        {
            if (!IsConnected || string.IsNullOrEmpty(_connectionId))
            {
                _logger.LogWarning("Cannot end call: not connected");
                return;
            }

            try
            {
                var request = new
                {
                    ConnectionId = _connectionId,
                    CallId = callId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                await _httpClient.PostAsync("/api/realtime/end-call", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call");
            }
        }

        private async void CheckPollingHealth(object? state)
        {
            if (!_isPolling || string.IsNullOrEmpty(_connectionId))
                return;

            var timeSinceLastPoll = DateTime.UtcNow - _lastSuccessfulPoll;
            if (timeSinceLastPoll.TotalMinutes > 2) // If no successful poll for 2 minutes
            {
                _logger.LogWarning("Polling appears to have stopped - no successful polls for {Minutes} minutes. Restarting...", timeSinceLastPoll.TotalMinutes);
                _isPolling = false; // Stop current polling
                _connectionId = null; // Clear connection
                OnConnectionChanged?.Invoke(false); // Notify UI

                // Restart connection
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    await StartAsync();
                });
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}
