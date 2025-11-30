using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Helpers
{
    /// <summary>
    /// Helper class for encrypting/decrypting User PII data
    /// </summary>
    public static class UserEncryptionHelper
    {
        /// <summary>
        /// Encrypts DateOfBirth and MobilePhone, storing them in encrypted fields before saving to database
        /// </summary>
        public static void EncryptUserData(User user, IPiiEncryptionService encryptionService)
        {
            if (user == null || encryptionService == null)
                return;

            // Encrypt DateOfBirth if it's been set
            if (user.DateOfBirth != DateTime.MinValue)
            {
                user.DateOfBirthEncrypted = encryptionService.EncryptDateTime(user.DateOfBirth);
            }

            // Encrypt MobilePhone if it's been set
            if (!string.IsNullOrEmpty(user.MobilePhone))
            {
                user.MobilePhoneEncrypted = encryptionService.Encrypt(user.MobilePhone);
            }
        }

        /// <summary>
        /// Decrypts DateOfBirthEncrypted and MobilePhoneEncrypted, populating computed properties after loading from database
        /// </summary>
        public static void DecryptUserData(User user, IPiiEncryptionService encryptionService)
        {
            if (user == null || encryptionService == null)
                return;

            // Decrypt DateOfBirthEncrypted if it exists
            if (!string.IsNullOrEmpty(user.DateOfBirthEncrypted))
            {
                try
                {
                    var decryptedDate = encryptionService.DecryptDateTime(user.DateOfBirthEncrypted);
                    if (decryptedDate != DateTime.MinValue && decryptedDate.Year > 1900) // Valid date check
                    {
                        user.DateOfBirth = decryptedDate;
                    }
                    else
                    {
                        // Decryption failed - might be encrypted with wrong key
                        // Try parsing as plain text (in case migration left it as plain text)
                        if (DateTime.TryParse(user.DateOfBirthEncrypted, out var plainTextDate) && plainTextDate.Year > 1900)
                        {
                            user.DateOfBirth = plainTextDate;
                            // Re-encrypt with current key
                            user.DateOfBirthEncrypted = encryptionService.EncryptDateTime(plainTextDate);
                        }
                        else
                        {
                            // Can't decrypt or parse - set to MinValue
                            user.DateOfBirth = DateTime.MinValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Try parsing as plain text as fallback
                    if (DateTime.TryParse(user.DateOfBirthEncrypted, out var fallbackDate) && fallbackDate.Year > 1900)
                    {
                        user.DateOfBirth = fallbackDate;
                    }
                    else
                    {
                        user.DateOfBirth = DateTime.MinValue;
                    }
                }
            }
            else
            {
                // Empty DateOfBirthEncrypted - user needs to set it
                user.DateOfBirth = DateTime.MinValue;
            }

            // Decrypt MobilePhoneEncrypted if it exists
            if (!string.IsNullOrEmpty(user.MobilePhoneEncrypted))
            {
                try
                {
                    var decryptedPhone = encryptionService.Decrypt(user.MobilePhoneEncrypted);
                    // Check if decryption actually worked (if it returns the same string, decryption failed)
                    if (decryptedPhone != user.MobilePhoneEncrypted && !string.IsNullOrWhiteSpace(decryptedPhone))
                    {
                        user.MobilePhone = decryptedPhone;
                    }
                    else
                    {
                        // Decryption failed - might be plain text or wrong key
                        // If it looks like a phone number (contains digits and + or -), treat as plain text
                        if (user.MobilePhoneEncrypted.Any(char.IsDigit) && 
                            (user.MobilePhoneEncrypted.Contains("+") || user.MobilePhoneEncrypted.Contains("-") || 
                             user.MobilePhoneEncrypted.Length >= 10))
                        {
                            user.MobilePhone = user.MobilePhoneEncrypted;
                            // Re-encrypt with current key
                            user.MobilePhoneEncrypted = encryptionService.Encrypt(user.MobilePhoneEncrypted);
                        }
                        else
                        {
                            user.MobilePhone = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Try treating as plain text as fallback
                    if (user.MobilePhoneEncrypted.Any(char.IsDigit) && 
                        (user.MobilePhoneEncrypted.Contains("+") || user.MobilePhoneEncrypted.Contains("-") || 
                         user.MobilePhoneEncrypted.Length >= 10))
                    {
                        user.MobilePhone = user.MobilePhoneEncrypted;
                    }
                    else
                    {
                        user.MobilePhone = null;
                    }
                }
            }
            else
            {
                // Empty MobilePhoneEncrypted - no phone number
                user.MobilePhone = null;
            }
        }

        /// <summary>
        /// Decrypts DateOfBirth for a collection of users
        /// </summary>
        public static void DecryptUserData(IEnumerable<User> users, IPiiEncryptionService encryptionService)
        {
            if (users == null || encryptionService == null)
                return;

            foreach (var user in users)
            {
                DecryptUserData(user, encryptionService);
            }
        }

        /// <summary>
        /// Encrypts DateOfBirth and MobilePhone, storing them in encrypted fields before saving to database
        /// </summary>
        public static void EncryptUserRequestData(UserRequest userRequest, IPiiEncryptionService encryptionService)
        {
            if (userRequest == null || encryptionService == null)
                return;

            // Encrypt DateOfBirth if it's been set
            if (userRequest.DateOfBirth != DateTime.MinValue)
            {
                userRequest.DateOfBirthEncrypted = encryptionService.EncryptDateTime(userRequest.DateOfBirth);
            }

            // Encrypt MobilePhone if it's been set
            if (!string.IsNullOrEmpty(userRequest.MobilePhone))
            {
                userRequest.MobilePhoneEncrypted = encryptionService.Encrypt(userRequest.MobilePhone);
            }
        }

        /// <summary>
        /// Decrypts DateOfBirthEncrypted and MobilePhoneEncrypted, populating computed properties after loading from database
        /// </summary>
        public static void DecryptUserRequestData(UserRequest userRequest, IPiiEncryptionService encryptionService)
        {
            if (userRequest == null || encryptionService == null)
                return;

            // Decrypt DateOfBirthEncrypted if it exists
            if (!string.IsNullOrEmpty(userRequest.DateOfBirthEncrypted))
            {
                try
                {
                    var decryptedDate = encryptionService.DecryptDateTime(userRequest.DateOfBirthEncrypted);
                    if (decryptedDate != DateTime.MinValue && decryptedDate.Year > 1900) // Valid date check
                    {
                        userRequest.DateOfBirth = decryptedDate;
                    }
                    else
                    {
                        // Decryption failed - might be encrypted with wrong key
                        // Try parsing as plain text (in case migration left it as plain text)
                        if (DateTime.TryParse(userRequest.DateOfBirthEncrypted, out var plainTextDate) && plainTextDate.Year > 1900)
                        {
                            userRequest.DateOfBirth = plainTextDate;
                            // Re-encrypt with current key
                            userRequest.DateOfBirthEncrypted = encryptionService.EncryptDateTime(plainTextDate);
                        }
                        else
                        {
                            // Can't decrypt or parse - set to MinValue
                            userRequest.DateOfBirth = DateTime.MinValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Try parsing as plain text as fallback
                    if (DateTime.TryParse(userRequest.DateOfBirthEncrypted, out var fallbackDate) && fallbackDate.Year > 1900)
                    {
                        userRequest.DateOfBirth = fallbackDate;
                    }
                    else
                    {
                        userRequest.DateOfBirth = DateTime.MinValue;
                    }
                }
            }
            else
            {
                // Empty DateOfBirthEncrypted - user needs to set it
                userRequest.DateOfBirth = DateTime.MinValue;
            }

            // Decrypt MobilePhoneEncrypted if it exists
            if (!string.IsNullOrEmpty(userRequest.MobilePhoneEncrypted))
            {
                try
                {
                    var decryptedPhone = encryptionService.Decrypt(userRequest.MobilePhoneEncrypted);
                    // Check if decryption actually worked (if it returns the same string, decryption failed)
                    if (decryptedPhone != userRequest.MobilePhoneEncrypted && !string.IsNullOrWhiteSpace(decryptedPhone))
                    {
                        userRequest.MobilePhone = decryptedPhone;
                    }
                    else
                    {
                        // Decryption failed - might be plain text or wrong key
                        // If it looks like a phone number (contains digits and + or -), treat as plain text
                        if (userRequest.MobilePhoneEncrypted.Any(char.IsDigit) && 
                            (userRequest.MobilePhoneEncrypted.Contains("+") || userRequest.MobilePhoneEncrypted.Contains("-") || 
                             userRequest.MobilePhoneEncrypted.Length >= 10))
                        {
                            userRequest.MobilePhone = userRequest.MobilePhoneEncrypted;
                            // Re-encrypt with current key
                            userRequest.MobilePhoneEncrypted = encryptionService.Encrypt(userRequest.MobilePhoneEncrypted);
                        }
                        else
                        {
                            userRequest.MobilePhone = string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Try treating as plain text as fallback
                    if (userRequest.MobilePhoneEncrypted.Any(char.IsDigit) && 
                        (userRequest.MobilePhoneEncrypted.Contains("+") || userRequest.MobilePhoneEncrypted.Contains("-") || 
                         userRequest.MobilePhoneEncrypted.Length >= 10))
                    {
                        userRequest.MobilePhone = userRequest.MobilePhoneEncrypted;
                    }
                    else
                    {
                        userRequest.MobilePhone = string.Empty;
                    }
                }
            }
            else
            {
                // Empty MobilePhoneEncrypted - no phone number
                userRequest.MobilePhone = string.Empty;
            }
        }

        /// <summary>
        /// Decrypts DateOfBirth for a collection of user requests
        /// </summary>
        public static void DecryptUserRequestData(IEnumerable<UserRequest> userRequests, IPiiEncryptionService encryptionService)
        {
            if (userRequests == null || encryptionService == null)
                return;

            foreach (var userRequest in userRequests)
            {
                DecryptUserRequestData(userRequest, encryptionService);
            }
        }
    }
}

