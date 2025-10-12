using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SM_MentalHealthApp.Server.Services
{
    public class AgoraTokenService
    {
        private readonly string _appId = "efa11b3a7d05409ca979fb25a5b489ae";
        private readonly string _appCertificate = "89ab54068fae46aeaf930ffd493e977b";

        public string GenerateToken(string channelName, uint uid, uint expirationTimeInSeconds = 3600)
        {
            var currentTs = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var privilegeExpireTs = currentTs + expirationTimeInSeconds;

            var token = BuildRtcTokenWithUid(_appId, _appCertificate, channelName, uid, privilegeExpireTs);
            return token;
        }

        private static string BuildRtcTokenWithUid(string appId, string appCertificate, string channelName, uint uid, uint expireTs)
        {
            const string VERSION = "006";
            var issueTs = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var salt = RandomNumberGenerator.GetInt32(int.MaxValue);

            // Create signing message according to Agora spec
            var message = $"{appId}{channelName}{uid}{issueTs}{expireTs}{salt}";
            var signature = GenerateHmacSha256(appCertificate, message);

            // Serialize token body
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            WriteString(bw, BitConverter.ToString(signature).Replace("-", "").ToLower());
            bw.Write(issueTs);
            bw.Write(salt);

            // Privileges
            bw.Write((short)1); // 1 privilege (JoinChannel)
            bw.Write((short)1); // Privilege type: JoinChannel
            bw.Write(expireTs);

            var body = ms.ToArray();
            var encodedBody = Convert.ToBase64String(body);
            return $"{VERSION}{appId}{encodedBody}";
        }

        private static byte[] GenerateHmacSha256(string key, string message)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        }

        private static void WriteString(BinaryWriter bw, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            bw.Write((short)bytes.Length);
            bw.Write(bytes);
        }
    }
}