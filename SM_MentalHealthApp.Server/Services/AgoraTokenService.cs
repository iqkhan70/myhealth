using System;
using AgoraIO.Media;
using Microsoft.Extensions.Configuration;

namespace SM_MentalHealthApp.Server.Services
{
    public class AgoraTokenService
    {
        private readonly string _appId;
        private readonly string _appCertificate;

        public AgoraTokenService(IConfiguration configuration)
        {
            _appId = configuration["Agora:AppId"]
                     ?? throw new InvalidOperationException("Agora:AppId is not configured.");

            _appCertificate = configuration["Agora:AppCertificate"]
                              ?? throw new InvalidOperationException("Agora:AppCertificate is not configured.");

            if (string.IsNullOrWhiteSpace(_appId))
                throw new InvalidOperationException("Agora App ID is empty. Check configuration Agora:AppId.");

            if (string.IsNullOrWhiteSpace(_appCertificate))
                throw new InvalidOperationException("Agora App Certificate is empty. Check configuration Agora:AppCertificate.");
        }

        /// <summary>
        /// Expose AppId so controllers can send it to the client.
        /// </summary>
        public string AppId => _appId;

        /// <summary>
        /// Generate an RTC token for the given channel and uid.
        /// </summary>
        public string GenerateToken(string channelName, uint uid, uint expirationTimeInSeconds = 3600)
        {
            if (string.IsNullOrWhiteSpace(channelName))
                throw new ArgumentException("Channel name is required.", nameof(channelName));

            var privilegeExpiredTs = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expirationTimeInSeconds);

            // Uses Agora's C# token builder (from AgoraIO.Media)
            var token = RtcTokenBuilder.buildTokenWithUID(
                _appId,
                _appCertificate,
                channelName,
                uid,
                RtcTokenBuilder.Role.RolePublisher,
                privilegeExpiredTs
            );

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Failed to build Agora token. Check AppId/AppCertificate and RtcTokenBuilder version.");

            return token;
        }
    }
}
