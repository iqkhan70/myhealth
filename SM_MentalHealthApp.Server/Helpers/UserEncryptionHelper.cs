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
        /// Encrypts DateOfBirth and stores it in DateOfBirthEncrypted before saving to database
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
        }

        /// <summary>
        /// Decrypts DateOfBirthEncrypted and populates DateOfBirth after loading from database
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
        /// Encrypts DateOfBirth and stores it in DateOfBirthEncrypted before saving to database
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
        }

        /// <summary>
        /// Decrypts DateOfBirthEncrypted and populates DateOfBirth after loading from database
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

