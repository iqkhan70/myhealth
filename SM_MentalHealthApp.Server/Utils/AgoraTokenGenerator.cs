using System;
using System.Security.Cryptography;
using System.Text;

namespace SM_MentalHealthApp.Server.Utils
{
    public static class AgoraTokenGenerator
    {
        public static string GenerateToken(string appId, string appCertificate, string channelName, int uid, int expireSeconds = 3600)
        {
            var ts = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            var expiredTs = ts + expireSeconds;
            var raw = $"{appId}{appCertificate}{channelName}{uid}{expiredTs}";

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var sig = BitConverter.ToString(hash).Replace("-", "").ToLower();

            // This is a pseudo-token just for dev testing.
            // Agora’s real token uses a more complex signing, but this will allow your client to proceed.
            return $"DEV_{sig}_{expiredTs}";
        }
    }
}
