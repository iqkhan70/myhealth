namespace SM_MentalHealthApp.Server.Models
{
    public class AgoraRequest
    {
        public string ChannelName { get; set; } = string.Empty;
        public uint Uid { get; set; }
        public int? ExpirationTimeInSeconds { get; set; }
    }
}
