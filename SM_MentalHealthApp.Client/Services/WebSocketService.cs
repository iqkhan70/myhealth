using System.Text.Json;
using System.Net.WebSockets;

namespace SM_MentalHealthApp.Client.Services
{
    public class WebSocketService : IWebSocketService, IAsyncDisposable
    {
        private readonly IAuthService _authService;
        private ClientWebSocket? _webSocket;
        private readonly string _hubUrl;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        public event Action<CallInvitation>? OnIncomingCall;
        public event Action<ChatMessage>? OnNewMessage;
        public event Action<string>? OnCallAccepted;
        public event Action<string>? OnCallRejected;
        public event Action<string>? OnCallEnded;
        public event Action<UserStatusChange>? OnUserStatusChanged;
        public event Action<bool>? OnConnectionChanged;

        public WebSocketService(IAuthService authService)
        {
            _authService = authService;
            _hubUrl = "ws://localhost:5262/mobilehub";
        }

        public async Task StartAsync()
        {
            if (_webSocket != null)
            {
                await StopAsync();
            }

            var token = _authService.Token;
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("No auth token available for WebSocket connection");
                return;
            }

            try
            {
                var uri = new Uri($"{_hubUrl}?access_token={token}");
                _webSocket = new ClientWebSocket();

                await _webSocket.ConnectAsync(uri, CancellationToken.None);
                Console.WriteLine($"WebSocket connection started successfully to {_hubUrl}");
                OnConnectionChanged?.Invoke(true);

                // Send SignalR handshake
                var handshake = new
                {
                    protocol = "json",
                    version = 1
                };
                var handshakeJson = JsonSerializer.Serialize(handshake);
                var handshakeBytes = System.Text.Encoding.UTF8.GetBytes(handshakeJson);
                await _webSocket.SendAsync(new ArraySegment<byte>(handshakeBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                // Start listening for messages
                _ = Task.Run(ReceiveMessagesAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting WebSocket connection to {_hubUrl}: {ex.Message}");
                Console.WriteLine($"Full error: {ex}");
                OnConnectionChanged?.Invoke(false);
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];
            while (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("WebSocket connection closed by server");
                        OnConnectionChanged?.Invoke(false);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving WebSocket message: {ex.Message}");
                    OnConnectionChanged?.Invoke(false);
                    break;
                }
            }
        }

        private async Task ProcessMessage(string message)
        {
            try
            {
                var data = JsonSerializer.Deserialize<JsonElement>(message);

                if (data.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetInt32();

                    switch (type)
                    {
                        case 1: // Invocation message
                            await HandleInvocationMessage(data);
                            break;
                        case 2: // Stream item
                            Console.WriteLine("Stream item received");
                            break;
                        case 3: // Completion
                            Console.WriteLine("Completion received");
                            break;
                        case 6: // Ping
                            Console.WriteLine("Ping received");
                            break;
                        case 7: // Pong
                            Console.WriteLine("Pong received");
                            break;
                        default:
                            Console.WriteLine($"Unknown message type: {type}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing WebSocket message: {ex.Message}");
                Console.WriteLine($"Raw message: {message}");
            }
        }

        private async Task HandleInvocationMessage(JsonElement data)
        {
            if (data.TryGetProperty("target", out var targetElement))
            {
                var method = targetElement.GetString();
                var args = data.TryGetProperty("arguments", out var argsElement) ? argsElement : default;

                switch (method)
                {
                    case "new-message":
                        if (args.ValueKind == JsonValueKind.Array && args.GetArrayLength() > 0)
                        {
                            var message = JsonSerializer.Deserialize<ChatMessage>(args[0].GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            OnNewMessage?.Invoke(message);
                        }
                        break;
                    case "incoming-call":
                        if (args.ValueKind == JsonValueKind.Array && args.GetArrayLength() > 0)
                        {
                            var call = JsonSerializer.Deserialize<CallInvitation>(args[0].GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            OnIncomingCall?.Invoke(call);
                        }
                        break;
                    case "call-accepted":
                        if (args.ValueKind == JsonValueKind.Array && args.GetArrayLength() > 0)
                        {
                            var callId = args[0].GetString() ?? "";
                            OnCallAccepted?.Invoke(callId);
                        }
                        break;
                    case "call-rejected":
                        if (args.ValueKind == JsonValueKind.Array && args.GetArrayLength() > 0)
                        {
                            var callId = args[0].GetString() ?? "";
                            OnCallRejected?.Invoke(callId);
                        }
                        break;
                    case "call-ended":
                        if (args.ValueKind == JsonValueKind.Array && args.GetArrayLength() > 0)
                        {
                            var callId = args[0].GetString() ?? "";
                            OnCallEnded?.Invoke(callId);
                        }
                        break;
                    case "user-status-changed":
                        if (args.ValueKind == JsonValueKind.Array && args.GetArrayLength() > 0)
                        {
                            var status = JsonSerializer.Deserialize<UserStatusChange>(args[0].GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            OnUserStatusChanged?.Invoke(status);
                        }
                        break;
                    default:
                        Console.WriteLine($"Unknown method: {method}");
                        break;
                }
            }
        }

        public async Task StopAsync()
        {
            if (_webSocket != null)
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                _webSocket.Dispose();
                _webSocket = null;
                OnConnectionChanged?.Invoke(false);
            }
        }

        public async Task SendMessageAsync(int targetUserId, string message)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var messageData = new
                    {
                        type = 1,
                        target = "SendMessage",
                        arguments = new object[] { targetUserId, message }
                    };
                    var json = JsonSerializer.Serialize(messageData);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                }
            }
        }

        public async Task InitiateCallAsync(int targetUserId, string callType)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var messageData = new
                    {
                        type = 1,
                        target = "InitiateCall",
                        arguments = new object[] { targetUserId, callType }
                    };
                    var json = JsonSerializer.Serialize(messageData);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initiating call: {ex.Message}");
                }
            }
        }

        public async Task AcceptCallAsync(string callId)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var messageData = new
                    {
                        type = 1,
                        target = "AcceptCall",
                        arguments = new object[] { callId }
                    };
                    var json = JsonSerializer.Serialize(messageData);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting call: {ex.Message}");
                }
            }
        }

        public async Task RejectCallAsync(string callId)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var messageData = new
                    {
                        type = 1,
                        target = "RejectCall",
                        arguments = new object[] { callId }
                    };
                    var json = JsonSerializer.Serialize(messageData);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error rejecting call: {ex.Message}");
                }
            }
        }

        public async Task EndCallAsync(string callId)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var messageData = new
                    {
                        type = 1,
                        target = "EndCall",
                        arguments = new object[] { callId }
                    };
                    var json = JsonSerializer.Serialize(messageData);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
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

    public interface IWebSocketService
    {
        bool IsConnected { get; }
        event Action<CallInvitation>? OnIncomingCall;
        event Action<ChatMessage>? OnNewMessage;
        event Action<string>? OnCallAccepted;
        event Action<string>? OnCallRejected;
        event Action<string>? OnCallEnded;
        event Action<UserStatusChange>? OnUserStatusChanged;
        event Action<bool>? OnConnectionChanged;
        Task StartAsync();
        Task StopAsync();
        Task SendMessageAsync(int targetUserId, string message);
        Task InitiateCallAsync(int targetUserId, string callType);
        Task AcceptCallAsync(string callId);
        Task RejectCallAsync(string callId);
        Task EndCallAsync(string callId);
    }
}
