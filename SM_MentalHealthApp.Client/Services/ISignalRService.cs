using Microsoft.AspNetCore.SignalR.Client;

namespace SM_MentalHealthApp.Client.Services
{
    public interface ISignalRService
    {
        HubConnection? Connection { get; }
        bool IsConnected { get; }

        Task StartAsync();
        Task StopAsync();
        Task SendMessageAsync(int targetUserId, string message);
        Task InitiateCallAsync(int targetUserId, string callType);
        Task AcceptCallAsync(string callId);
        Task RejectCallAsync(string callId);
        Task EndCallAsync(string callId);

        event Action<CallInvitation>? OnIncomingCall;
        event Action<ChatMessage>? OnNewMessage;
        event Action<string>? OnCallAccepted;
        event Action<string>? OnCallRejected;
        event Action<string>? OnCallEnded;
        event Action<UserStatusChange>? OnUserStatusChanged;
        event Action<bool>? OnConnectionChanged;
    }

    public class CallInvitation
    {
        public string CallId { get; set; } = string.Empty;
        public int CallerId { get; set; }
        public string CallerName { get; set; } = string.Empty;
        public string CallerRole { get; set; } = string.Empty;
        public string CallType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ChatMessage
    {
        public string Id { get; set; } = string.Empty;
        public int SenderId { get; set; }
        public int TargetUserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class UserStatusChange
    {
        public int UserId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
