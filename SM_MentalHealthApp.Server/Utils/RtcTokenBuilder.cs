using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SM_MentalHealthApp.Server.Utils
{
    public static class RtcTokenBuilder
    {
        private static readonly string VERSION = "006";

        public static string BuildTokenWithUid(string appId, string appCertificate, string channelName, uint uid, int role, int privilegeExpireTs)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appCertificate))
                throw new ArgumentException("App ID and App Certificate must not be empty");

            var token = new AccessToken(appId, appCertificate, channelName, uid.ToString());
            token.AddPrivilege(Privileges.JoinChannel, privilegeExpireTs);
            return token.Build();
        }

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
                _salt = new Random().Next();
            }

            public void AddPrivilege(Privileges privilege, int expireTs)
            {
                _privileges[privilege] = expireTs;
            }

            public string Build()
            {
                var message = $"{_appId}{_channelName}{_uid}{_issueTs}{_salt}";
                var signature = GenerateHmacSha256(_appCertificate, message);

                var content = $"{_issueTs}:{_salt}:{signature}";
                var encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

                return $"{VERSION}{_appId}{encodedContent}";
            }

            private static string GenerateHmacSha256(string key, string message)
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private enum Privileges
        {
            JoinChannel = 1
        }
    }
}
