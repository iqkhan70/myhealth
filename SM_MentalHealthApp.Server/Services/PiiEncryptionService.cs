using System.Security.Cryptography;
using System.Text;

namespace SM_MentalHealthApp.Server.Services
{
    /// <summary>
    /// Service for encrypting and decrypting PII (Personally Identifiable Information) data
    /// Uses AES-256 encryption with a key derived from configuration
    /// </summary>
    public interface IPiiEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        string EncryptDateTime(DateTime dateTime);
        DateTime DecryptDateTime(string encryptedDateTime);
    }

    public class PiiEncryptionService : IPiiEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public PiiEncryptionService(IConfiguration configuration)
        {
            // Get encryption key from configuration or use a default (should be set in production)
            // Try both PiiEncryption:Key and Encryption:Key for backward compatibility
            var encryptionKey = configuration["PiiEncryption:Key"] 
                ?? configuration["Encryption:Key"] 
                ?? "DefaultEncryptionKey32BytesLong!!"; // 32 bytes for AES-256
            
            // Derive a consistent key and IV from the encryption key
            using (var sha256 = SHA256.Create())
            {
                var keyHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
                _key = keyHash; // 32 bytes for AES-256
                
                // Derive IV from key (first 16 bytes of a hash of the key + "IV")
                var ivHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey + "IV"));
                _iv = new byte[16];
                Array.Copy(ivHash, _iv, 16);
            }
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                // Check if it looks like a base64 encrypted string (contains = and is longer)
                if (!cipherText.Contains("=") || cipherText.Length < 20)
                {
                    // Might be plain text, return as-is
                    return cipherText;
                }

                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        var decrypted = srDecrypt.ReadToEnd();
                        // If decryption returns garbage or same as input, it failed
                        if (decrypted == cipherText || string.IsNullOrWhiteSpace(decrypted))
                        {
                            return cipherText; // Return original, might be plain text
                        }
                        return decrypted;
                    }
                }
            }
            catch (Exception ex)
            {
                // If decryption fails, might be plain text or wrong key
                System.Diagnostics.Debug.WriteLine($"Decryption failed: {ex.Message}. Returning original.");
                return cipherText;
            }
        }

        public string EncryptDateTime(DateTime dateTime)
        {
            // Use date-only format (YYYY-MM-DD) to avoid timezone issues
            // Extract date part only to ensure consistent storage
            var dateOnly = dateTime.Date;
            var dateString = dateOnly.ToString("yyyy-MM-dd"); // Date-only format, no time/timezone
            return Encrypt(dateString);
        }

        public DateTime DecryptDateTime(string encryptedDateTime)
        {
            if (string.IsNullOrEmpty(encryptedDateTime))
                return DateTime.MinValue;

            try
            {
                // First, try to parse as plain text ISO 8601 (in case it's unencrypted from migration)
                if (DateTime.TryParse(encryptedDateTime, out var plainTextResult))
                {
                    // If it parses as a date, it might be plain text - but check if it looks like ISO format
                    if (encryptedDateTime.Contains("T") || encryptedDateTime.Contains("-") && encryptedDateTime.Length < 30)
                    {
                        // Looks like plain text date, return it
                        return plainTextResult;
                    }
                }

                // Try to decrypt
                var decryptedString = Decrypt(encryptedDateTime);
                
                // Check if decryption actually worked (if it returns the same string, decryption failed)
                if (decryptedString == encryptedDateTime)
                {
                    // Decryption failed - try parsing as plain text
                    if (DateTime.TryParse(encryptedDateTime, out var fallbackResult))
                    {
                        return fallbackResult;
                    }
                    System.Diagnostics.Debug.WriteLine($"Decryption failed: returned same string. Encrypted: {encryptedDateTime.Substring(0, Math.Min(50, encryptedDateTime.Length))}...");
                    return DateTime.MinValue;
                }
                
                // Try parsing as date-only first (YYYY-MM-DD)
                if (DateTime.TryParseExact(decryptedString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var dateOnlyResult))
                {
                    return dateOnlyResult.Date; // Return as date-only (midnight, no timezone)
                }
                
                // Fallback to standard DateTime parsing
                if (DateTime.TryParse(decryptedString, out var result))
                    return result.Date; // Return as date-only to avoid timezone issues
                
                System.Diagnostics.Debug.WriteLine($"Failed to parse decrypted date string: {decryptedString}");
                
                // Final fallback: try parsing original as plain text
                if (DateTime.TryParse(encryptedDateTime, out var legacyResult))
                    return legacyResult;
                
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception during decryption: {ex.Message}");
                // Fallback: try parsing as legacy unencrypted DateTime
                if (DateTime.TryParse(encryptedDateTime, out var legacyResult))
                    return legacyResult;
                
                return DateTime.MinValue;
            }
        }
    }
}

