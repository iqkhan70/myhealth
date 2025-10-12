using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SM_MentalHealthApp.Server.Utils
{
    public static class RtcTokenBuilder
    {
        private const string VERSION = "006";

        /// <summary>
        /// Builds an Agora RTC token using a numeric UID.
        /// </summary>
        public static string BuildTokenWithUid(string appId, string appCertificate, string channelName, uint uid, int role, int privilegeExpireTs)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appCertificate))
                throw new ArgumentException("App ID and App Certificate must not be empty");

            var uidStr = uid == 0 ? "" : uid.ToString();
            var token = new AccessToken(appId, appCertificate, channelName, uidStr);
            token.AddPrivilege(Privileges.JoinChannel, privilegeExpireTs);

            return token.Build();
        }

        // -------------------------------------------------------------
        // Inner AccessToken class
        // -------------------------------------------------------------
        private class AccessToken
        {
            private readonly string _appId;
            private readonly string _appCertificate;
            private readonly string _channelName;
            private readonly string _uid;
            private readonly int _issueTs;
            private readonly int _salt;
            private readonly Dictionary<Privileges, int> _privileges = new();

            public AccessToken(string appId, string appCertificate, string channelName, string uid)
            {
                _appId = appId;
                _appCertificate = appCertificate;
                _channelName = channelName;
                _uid = uid;
                _issueTs = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _salt = RandomNumberGenerator.GetInt32(int.MaxValue); // cryptographically secure salt
            }

            public void AddPrivilege(Privileges privilege, int expireTs)
            {
                _privileges[privilege] = expireTs;
            }

            public string Build()
            {
                // --- Step 1: Build message for signature ---
                // According to Agora spec: sign(appCertificate, appId + channelName + uid + issueTs + expireTs + salt)
                var expireTs = GetMaxExpire();
                var message = Encoding.UTF8.GetBytes($"{_appId}{_channelName}{_uid}{_issueTs}{expireTs}{_salt}");
                var signature = GenerateHmacSha256(_appCertificate, message);

                // --- Step 2: Serialize body ---
                var body = PackContent(signature, _issueTs, _salt, _privileges);

                // --- Step 3: Encode as base64 ---
                var encodedBody = Convert.ToBase64String(body);

                // --- Step 4: Final token format ---
                return $"{VERSION}{_appId}{encodedBody}";
            }

            private int GetMaxExpire()
            {
                // Return the max privilege expiration timestamp
                int max = 0;
                foreach (var kvp in _privileges)
                    if (kvp.Value > max)
                        max = kvp.Value;
                return max;
            }

            private static byte[] PackContent(byte[] signature, int issueTs, int salt, Dictionary<Privileges, int> privileges)
            {
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);

                // Signature
                WriteString(bw, BitConverter.ToString(signature).Replace("-", "").ToLower());

                // Issue timestamp
                bw.Write(issueTs);

                // Salt
                bw.Write(salt);

                // Privileges
                bw.Write((short)privileges.Count);
                foreach (var kv in privileges)
                {
                    bw.Write((short)kv.Key);
                    bw.Write(kv.Value);
                }

                return ms.ToArray();
            }

            private static void WriteString(BinaryWriter bw, string value)
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                bw.Write((short)bytes.Length);
                bw.Write(bytes);
            }

            private static byte[] GenerateHmacSha256(string key, byte[] message)
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
                return hmac.ComputeHash(message);
            }
        }

        // -------------------------------------------------------------
        // Privileges Enum
        // -------------------------------------------------------------
        private enum Privileges : short
        {
            JoinChannel = 1
        }
    }
}
