using System.Security.Cryptography;
using System.Text;

namespace SM_MentalHealthApp.Server.Services
{
    public class AgoraTokenService
    {
        private readonly string _appId;
        private readonly string _appCertificate;
        private readonly ILogger<AgoraTokenService> _logger;

        public AgoraTokenService(IConfiguration configuration, ILogger<AgoraTokenService> logger)
        {
            _appId = configuration["Agora:AppId"] ?? throw new ArgumentNullException("Agora:AppId not configured");
            _appCertificate = configuration["Agora:AppCertificate"] ?? throw new ArgumentNullException("Agora:AppCertificate not configured");
            _logger = logger;
        }

        public string GenerateToken(string channelName, uint uid, uint expirationTimeInSeconds = 3600)
        {
            try
            {
                var currentTimestamp = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                var privilegeExpiredTs = currentTimestamp + expirationTimeInSeconds;

                // Create privilege object
                var privilege = new Dictionary<string, uint>
                {
                    { "join_channel", privilegeExpiredTs },
                    { "publish_audio_stream", privilegeExpiredTs },
                    { "publish_video_stream", privilegeExpiredTs },
                    { "publish_data_stream", privilegeExpiredTs }
                };

                // Generate token
                var token = GenerateTokenInternal(_appId, _appCertificate, channelName, uid, privilege);

                _logger.LogInformation($"Generated Agora token for channel: {channelName}, uid: {uid}");
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Agora token");
                throw;
            }
        }

        private string GenerateTokenInternal(string appId, string appCertificate, string channelName, uint uid, Dictionary<string, uint> privilege)
        {
            // This is a simplified token generation
            // In production, you should use the official Agora token generation library
            var message = $"{appId}:{channelName}:{uid}:{privilege["join_channel"]}";
            var signature = ComputeHMACSHA256(message, appCertificate);

            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appId}:{channelName}:{uid}:{privilege["join_channel"]}:{signature}"));
        }

        private string ComputeHMACSHA256(string message, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToBase64String(hash);
        }
    }
}
