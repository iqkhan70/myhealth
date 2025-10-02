using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace SM_MentalHealthApp.Client.Services
{
    public class SignalRService : ISignalRService, IAsyncDisposable
    {
        private readonly IAuthService _authService;
        private HubConnection? _connection;
        private readonly string _hubUrl;

        public HubConnection? Connection => _connection;
        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public event Action<CallInvitation>? OnIncomingCall;
        public event Action<ChatMessage>? OnNewMessage;
        public event Action<string>? OnCallAccepted;
        public event Action<string>? OnCallRejected;
        public event Action<string>? OnCallEnded;
        public event Action<UserStatusChange>? OnUserStatusChanged;
        public event Action<bool>? OnConnectionChanged;

        public SignalRService(IAuthService authService)
        {
            _authService = authService;
            _hubUrl = "http://localhost:5262/mobilehub"; // Connect to local server
        }

        public async Task StartAsync()
        {
            if (_connection != null)
            {
                await StopAsync();
            }

            var token = _authService.Token;
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("No auth token available for SignalR connection");
                return;
            }

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(token!);
                })
                .WithAutomaticReconnect()
                .Build();

            // Set up event handlers
            SetupEventHandlers();

            try
            {
                await _connection.StartAsync();
                Console.WriteLine($"SignalR connection started successfully to {_hubUrl}");
                OnConnectionChanged?.Invoke(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting SignalR connection to {_hubUrl}: {ex.Message}");
                Console.WriteLine($"Full error: {ex}");
                OnConnectionChanged?.Invoke(false);
            }

            _connection.Reconnecting += (error) =>
            {
                Console.WriteLine($"SignalR reconnecting: {error?.Message}");
                OnConnectionChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            _connection.Reconnected += (connectionId) =>
            {
                Console.WriteLine($"SignalR reconnected: {connectionId}");
                OnConnectionChanged?.Invoke(true);
                return Task.CompletedTask;
            };

            _connection.Closed += (error) =>
            {
                Console.WriteLine($"SignalR connection closed: {error?.Message}");
                OnConnectionChanged?.Invoke(false);
                return Task.CompletedTask;
            };
        }

        public async Task StopAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
                OnConnectionChanged?.Invoke(false);
            }
        }

        private void SetupEventHandlers()
        {
            if (_connection == null) return;

            // Incoming call
            _connection.On<object>("incoming-call", (callData) =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(callData);
                    var call = JsonSerializer.Deserialize<CallInvitation>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (call != null)
                    {
                        OnIncomingCall?.Invoke(call);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing incoming call: {ex.Message}");
                }
            });

            // New message
            _connection.On<object>("new-message", (messageData) =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(messageData);
                    var message = JsonSerializer.Deserialize<ChatMessage>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (message != null)
                    {
                        OnNewMessage?.Invoke(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing new message: {ex.Message}");
                }
            });

            // Call events
            _connection.On<object>("call-accepted", (data) =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(data);
                    var callData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (callData?.ContainsKey("callId") == true)
                    {
                        OnCallAccepted?.Invoke(callData["callId"].ToString() ?? "");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing call accepted: {ex.Message}");
                }
            });

            _connection.On<object>("call-rejected", (data) =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(data);
                    var callData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (callData?.ContainsKey("callId") == true)
                    {
                        OnCallRejected?.Invoke(callData["callId"].ToString() ?? "");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing call rejected: {ex.Message}");
                }
            });

            _connection.On<object>("call-ended", (data) =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(data);
                    var callData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (callData?.ContainsKey("callId") == true)
                    {
                        OnCallEnded?.Invoke(callData["callId"].ToString() ?? "");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing call ended: {ex.Message}");
                }
            });

            // User status changes
            _connection.On<object>("user-status-changed", (statusData) =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(statusData);
                    var status = JsonSerializer.Deserialize<UserStatusChange>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (status != null)
                    {
                        OnUserStatusChanged?.Invoke(status);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing user status change: {ex.Message}");
                }
            });

            // Error handling
            _connection.On<string>("error", (error) =>
            {
                Console.WriteLine($"SignalR error: {error}");
            });
        }

        public async Task SendMessageAsync(int targetUserId, string message)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendMessage", targetUserId, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                }
            }
        }

        public async Task InitiateCallAsync(int targetUserId, string callType)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("InitiateCall", targetUserId, callType);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initiating call: {ex.Message}");
                }
            }
        }

        public async Task AcceptCallAsync(string callId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("AcceptCall", callId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting call: {ex.Message}");
                }
            }
        }

        public async Task RejectCallAsync(string callId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("RejectCall", callId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error rejecting call: {ex.Message}");
                }
            }
        }

        public async Task EndCallAsync(string callId)
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("EndCall", callId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error ending call: {ex.Message}");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }
}
