using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SM_MentalHealthApp.Server.Scripts
{
    /// <summary>
    /// Script to encrypt existing DateOfBirth data in the database.
    /// Run this after the EncryptDateOfBirth migration has been applied.
    /// 
    /// Usage: Create a console app or run this from Program.cs temporarily
    /// </summary>
    public class EncryptExistingDateOfBirthData
    {
        public static async Task RunAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<JournalDbContext>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IPiiEncryptionService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EncryptExistingDateOfBirthData>>();

            logger.LogInformation("Starting encryption of existing DateOfBirth data...");

            // Encrypt Users
            var users = await context.Users
                .Where(u => !string.IsNullOrEmpty(u.DateOfBirthEncrypted))
                .ToListAsync();

            logger.LogInformation("Found {UserCount} users to encrypt...", users.Count);

            int encryptedUsers = 0;
            int skippedUsers = 0;
            int reEncryptedUsers = 0;
            foreach (var user in users)
            {
                try
                {
                    // Try to decrypt first to see if it's already encrypted with current key
                    var testDecrypt = encryptionService.DecryptDateTime(user.DateOfBirthEncrypted);
                    
                    if (testDecrypt == DateTime.MinValue)
                    {
                        // Decryption failed - might be plain text or encrypted with different key
                        // Try parsing as plain text date
                        if (DateTime.TryParse(user.DateOfBirthEncrypted, out var dateValue))
                        {
                            // This is plain text, encrypt it
                            user.DateOfBirthEncrypted = encryptionService.EncryptDateTime(dateValue);
                            encryptedUsers++;
                        }
                        else
                        {
                            // Might be encrypted with old key - try to decrypt with old key logic
                            // For now, re-encrypt by trying to decrypt and re-encrypt
                            // This will fail if we can't decrypt, but we'll log it
                            logger.LogWarning("User {UserId} has encrypted data that cannot be decrypted with current key. May need manual intervention.", user.Id);
                            skippedUsers++;
                        }
                    }
                    else
                    {
                        // Successfully decrypted - data is encrypted with current key
                        // Re-encrypt to ensure consistency
                        user.DateOfBirthEncrypted = encryptionService.EncryptDateTime(testDecrypt);
                        reEncryptedUsers++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing user {UserId}", user.Id);
                }
            }

            // Encrypt UserRequests
            var userRequests = await context.UserRequests
                .Where(ur => !string.IsNullOrEmpty(ur.DateOfBirthEncrypted))
                .ToListAsync();

            logger.LogInformation("Found {RequestCount} user requests to encrypt...", userRequests.Count);

            int encryptedRequests = 0;
            int skippedRequests = 0;
            foreach (var userRequest in userRequests)
            {
                try
                {
                    // Check if already encrypted (encrypted strings are base64 and won't parse as DateTime)
                    if (DateTime.TryParse(userRequest.DateOfBirthEncrypted, out var dateValue))
                    {
                        // This is plain text, encrypt it
                        userRequest.DateOfBirthEncrypted = encryptionService.EncryptDateTime(dateValue);
                        encryptedRequests++;
                    }
                    else
                    {
                        // Already encrypted, skip
                        skippedRequests++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error encrypting user request {RequestId}", userRequest.Id);
                }
            }

            await context.SaveChangesAsync();
            
            logger.LogInformation("Encryption complete!");
            logger.LogInformation("Users: {Encrypted} newly encrypted, {ReEncrypted} re-encrypted, {Skipped} skipped", encryptedUsers, reEncryptedUsers, skippedUsers);
            logger.LogInformation("UserRequests: {Encrypted} newly encrypted, {ReEncrypted} re-encrypted, {Skipped} skipped", encryptedRequests, 0, skippedRequests);
            
            Console.WriteLine($"✅ Successfully encrypted {encryptedUsers} user DateOfBirth records.");
            Console.WriteLine($"✅ Successfully re-encrypted {reEncryptedUsers} user DateOfBirth records.");
            Console.WriteLine($"✅ Successfully encrypted {encryptedRequests} user request DateOfBirth records.");
            Console.WriteLine($"ℹ️  Skipped {skippedUsers} users and {skippedRequests} user requests.");
        }
    }
}

