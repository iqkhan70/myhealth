using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace SM_MentalHealthApp.Server.Scripts
{
    /// <summary>
    /// One-time script to encrypt existing plain text MobilePhone data
    /// Run this after the database migration has been applied
    /// </summary>
    public static class EncryptExistingMobilePhoneData
    {
        public static async Task RunAsync()
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Build service collection
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.AddConsole());
            
            // Add database context
            // Try "MySQL" first (as in appsettings.json), then "DefaultConnection" as fallback
            var connectionString = configuration.GetConnectionString("MySQL") 
                ?? configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("❌ Error: Connection string not found in appsettings.json");
                Console.WriteLine("   Looking for 'MySQL' or 'DefaultConnection' connection string");
                return;
            }
            
            Console.WriteLine($"📡 Connecting to database...");
            
            // Use hardcoded server version instead of AutoDetect to avoid connection during registration
            // This matches the version used in DependencyInjection.cs
            services.AddDbContext<JournalDbContext>(options =>
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

            // Add encryption service
            services.AddSingleton<IPiiEncryptionService, PiiEncryptionService>();

            var serviceProvider = services.BuildServiceProvider();

            var context = serviceProvider.GetRequiredService<JournalDbContext>();
            var encryptionService = serviceProvider.GetRequiredService<IPiiEncryptionService>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("EncryptExistingMobilePhoneData");

            try
            {
                logger.LogInformation("Starting MobilePhone encryption process...");

                // Encrypt Users table
                var usersWithPlainTextPhones = await context.Users
                    .Where(u => u.MobilePhoneEncrypted != null && 
                                u.MobilePhoneEncrypted != "" &&
                                // Check if it looks like plain text (not encrypted)
                                (!u.MobilePhoneEncrypted.Contains("=") || u.MobilePhoneEncrypted.Length < 30))
                    .ToListAsync();

                logger.LogInformation($"Found {usersWithPlainTextPhones.Count} users with potentially plain text phone numbers");

                int encryptedCount = 0;
                int skippedCount = 0;

                foreach (var user in usersWithPlainTextPhones)
                {
                    try
                    {
                        // Check if already encrypted (try to decrypt, if it fails or returns same value, it's plain text)
                        var decrypted = encryptionService.Decrypt(user.MobilePhoneEncrypted);
                        
                        // If decryption returns the same string or doesn't look like a phone number, it's likely plain text
                        if (decrypted == user.MobilePhoneEncrypted || 
                            (!decrypted.Any(char.IsDigit) && decrypted.Length < 10))
                        {
                            // It's plain text, encrypt it
                            var encrypted = encryptionService.Encrypt(user.MobilePhoneEncrypted);
                            user.MobilePhoneEncrypted = encrypted;
                            encryptedCount++;
                        }
                        else
                        {
                            // Already encrypted, skip
                            skippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // If decryption fails, assume it's plain text and encrypt it
                        try
                        {
                            var encrypted = encryptionService.Encrypt(user.MobilePhoneEncrypted);
                            user.MobilePhoneEncrypted = encrypted;
                            encryptedCount++;
                            logger.LogWarning($"Encrypted phone for user {user.Id} after decryption failure: {ex.Message}");
                        }
                        catch (Exception encryptEx)
                        {
                            logger.LogError(encryptEx, $"Failed to encrypt phone for user {user.Id}: {encryptEx.Message}");
                        }
                    }
                }

                // Encrypt UserRequests table
                var requestsWithPlainTextPhones = await context.UserRequests
                    .Where(ur => ur.MobilePhoneEncrypted != null && 
                                 ur.MobilePhoneEncrypted != "" &&
                                 // Check if it looks like plain text (not encrypted)
                                 (!ur.MobilePhoneEncrypted.Contains("=") || ur.MobilePhoneEncrypted.Length < 30))
                    .ToListAsync();

                logger.LogInformation($"Found {requestsWithPlainTextPhones.Count} user requests with potentially plain text phone numbers");

                int encryptedRequestCount = 0;
                int skippedRequestCount = 0;

                foreach (var request in requestsWithPlainTextPhones)
                {
                    try
                    {
                        // Check if already encrypted
                        var decrypted = encryptionService.Decrypt(request.MobilePhoneEncrypted);
                        
                        if (decrypted == request.MobilePhoneEncrypted || 
                            (!decrypted.Any(char.IsDigit) && decrypted.Length < 10))
                        {
                            // It's plain text, encrypt it
                            var encrypted = encryptionService.Encrypt(request.MobilePhoneEncrypted);
                            request.MobilePhoneEncrypted = encrypted;
                            encryptedRequestCount++;
                        }
                        else
                        {
                            // Already encrypted, skip
                            skippedRequestCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // If decryption fails, assume it's plain text and encrypt it
                        try
                        {
                            var encrypted = encryptionService.Encrypt(request.MobilePhoneEncrypted);
                            request.MobilePhoneEncrypted = encrypted;
                            encryptedRequestCount++;
                            logger.LogWarning($"Encrypted phone for request {request.Id} after decryption failure: {ex.Message}");
                        }
                        catch (Exception encryptEx)
                        {
                            logger.LogError(encryptEx, $"Failed to encrypt phone for request {request.Id}: {encryptEx.Message}");
                        }
                    }
                }

                // Save all changes
                await context.SaveChangesAsync();

                logger.LogInformation("MobilePhone encryption completed successfully!");
                logger.LogInformation($"Users: {encryptedCount} encrypted, {skippedCount} skipped (already encrypted)");
                logger.LogInformation($"UserRequests: {encryptedRequestCount} encrypted, {skippedRequestCount} skipped (already encrypted)");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during MobilePhone encryption process");
                throw;
            }
            finally
            {
                await context.DisposeAsync();
                serviceProvider.Dispose();
            }
        }
    }
}

