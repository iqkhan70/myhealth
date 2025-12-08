using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;

namespace CheckAdmin
{
    // Simple password hasher
    public static class PasswordHasher
    {
        public static string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 10000, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(32));
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Check admin password
            var connectionString = "Server=localhost;Database=mentalhealth;User=root;Password=root;";
            var options = new DbContextOptionsBuilder<JournalDbContext>()
                .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)))
                .Options;

            using var context = new JournalDbContext(options);

            // Get admin user
            var adminUser = await context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.RoleId == 3); // Admin role

            if (adminUser == null)
            {
                Console.WriteLine("No admin user found!");
                return;
            }

            // Generate new password hash
            var salt = "mentalhealth_salt_2024";
            var newPasswordHash = PasswordHasher.HashPassword("Password123!", salt);

            // Update password
            adminUser.PasswordHash = newPasswordHash;
            await context.SaveChangesAsync();

        }
    }
}
