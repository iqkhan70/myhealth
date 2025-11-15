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
            var salt = (uint)RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);

            // Create signing message according to Agora spec
            // IMPORTANT: Message format is: appCertificate + appId + channelName + uid + issueTs + expireTs + salt
            var message = $"{appCertificate}{appId}{channelName}{uid}{issueTs}{expireTs}{salt}";
            var signature = GenerateHmacSha256(appCertificate, message);
            var signatureHex = BitConverter.ToString(signature).Replace("-", "").ToLower();

            // Serialize token body according to Agora 006 token format
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            // Write signature as hex string
            WriteString(bw, signatureHex);
            
            // Write timestamps and salt as uint (4 bytes each)
            WriteUint(bw, issueTs);
            WriteUint(bw, expireTs);
            WriteUint(bw, salt);

            // Privileges - need JoinChannel + PublishAudioStream + PublishVideoStream for full functionality
            bw.Write((short)3); // 3 privileges (JoinChannel, PublishAudioStream, PublishVideoStream)
            
            // Privilege 1: JoinChannel
            bw.Write((short)1); // Privilege type: JoinChannel
            WriteUint(bw, expireTs);
            
            // Privilege 2: PublishAudioStream
            bw.Write((short)2); // Privilege type: PublishAudioStream
            WriteUint(bw, expireTs);
            
            // Privilege 3: PublishVideoStream
            bw.Write((short)3); // Privilege type: PublishVideoStream
            WriteUint(bw, expireTs);

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

        private static void WriteUint(BinaryWriter bw, uint value)
        {
            bw.Write(value);
        }
    }
}
