using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SM_MentalHealthApp.Server.Utils
{
    public static class RtcTokenBuilder
    {
        private const string VERSION = "006";

        // Privilege constants (Agora defined)
        private const short PrivilegeJoinChannel = 1;
        private const short PrivilegePublishAudioStream = 2;
        private const short PrivilegePublishVideoStream = 3;
        private const short PrivilegePublishDataStream = 4;

        /// <summary>
        /// Builds a proper Agora RTC token (fully compatible with Web/iOS/Android SDKs)
        /// </summary>
        public static string BuildRtcTokenWithUid(
            string appId,
            string appCertificate,
            string channelName,
            uint uid,
            uint expireSeconds,
            bool isPublisher = true)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(channelName))
                throw new ArgumentException("App ID and channel name must not be empty.");

            var issueTs = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var salt = (uint)RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
            var expireTs = issueTs + expireSeconds;

            // Build signature message (Agora spec)
            var message = $"{appCertificate}{appId}{channelName}{uid}{issueTs}{expireTs}{salt}";
            var signature = GenerateHmacSha256(appCertificate, message);
            var signatureHex = BitConverter.ToString(signature).Replace("-", "").ToLower();

            // Serialize body according to Agora 006 token format
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            WriteString(bw, signatureHex);
            WriteUint(bw, issueTs);
            WriteUint(bw, expireTs);
            WriteUint(bw, salt);

            // Privilege count (JoinChannel + optional publish privileges)
            var privilegeCount = (short)(isPublisher ? 4 : 1);
            WriteShort(bw, privilegeCount);

            // Privilege: JoinChannel
            WriteShort(bw, PrivilegeJoinChannel);
            WriteUint(bw, expireTs);

            if (isPublisher)
            {
                WriteShort(bw, PrivilegePublishAudioStream);
                WriteUint(bw, expireTs);

                WriteShort(bw, PrivilegePublishVideoStream);
                WriteUint(bw, expireTs);

                WriteShort(bw, PrivilegePublishDataStream);
                WriteUint(bw, expireTs);
            }

            var encodedBody = Convert.ToBase64String(ms.ToArray());
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
            WriteShort(bw, (short)bytes.Length);
            bw.Write(bytes);
        }

        private static void WriteShort(BinaryWriter bw, short value)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            bw.Write(bytes);
        }

        private static void WriteUint(BinaryWriter bw, uint value)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)value));
            bw.Write(bytes);
        }
    }
}
