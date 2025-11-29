using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Services
{
    public class UserService
    {
        private readonly JournalDbContext _context;
        private readonly IPiiEncryptionService _encryptionService;

        public UserService(JournalDbContext context, IPiiEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Check if user with email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (existingUser != null)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            // Encrypt DateOfBirth before saving
            UserEncryptionHelper.EncryptUserData(user, _encryptionService);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Decrypt DateOfBirth after saving for return value
            UserEncryptionHelper.DecryptUserData(user, _encryptionService);
            
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.JournalEntries.OrderByDescending(j => j.CreatedAt))
                .Include(u => u.ChatSessions)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
            
            if (user != null)
            {
                UserEncryptionHelper.DecryptUserData(user, _encryptionService);
            }
            
            return user;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.JournalEntries.OrderByDescending(j => j.CreatedAt))
                .Include(u => u.ChatSessions)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            
            if (user != null)
            {
                UserEncryptionHelper.DecryptUserData(user, _encryptionService);
            }
            
            return user;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.JournalEntries)
                .Include(u => u.ChatSessions)
                .Where(u => u.IsActive)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();
            
            UserEncryptionHelper.DecryptUserData(users, _encryptionService);
            
            return users;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Email = user.Email;
            existingUser.DateOfBirth = user.DateOfBirth; // Set the date
            existingUser.Gender = user.Gender;
            existingUser.RoleId = user.RoleId;
            existingUser.IsActive = user.IsActive;
            existingUser.LastLoginAt = DateTime.UtcNow;

            // Update role-specific fields if they exist
            if (user.RoleId == Shared.Constants.Roles.Doctor)
            {
                existingUser.Specialization = user.Specialization;
                existingUser.LicenseNumber = user.LicenseNumber;
            }

            // Encrypt DateOfBirth before saving
            UserEncryptionHelper.EncryptUserData(existingUser, _encryptionService);

            await _context.SaveChangesAsync();
            
            // Decrypt DateOfBirth after saving for return value
            UserEncryptionHelper.DecryptUserData(existingUser, _encryptionService);
            
            return existingUser;
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User> GetOrCreateDemoUserAsync()
        {
            // For demo purposes, create a default user if none exists
            var demoUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "demo@mentalhealth.app");

            if (demoUser == null)
            {
                demoUser = new User
                {
                    FirstName = "Demo",
                    LastName = "Patient",
                    Email = "demo@mentalhealth.app",
                    DateOfBirth = DateTime.UtcNow.AddYears(-30),
                    Gender = "Other",
                    RoleId = Shared.Constants.Roles.Patient,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Encrypt DateOfBirth before saving
                UserEncryptionHelper.EncryptUserData(demoUser, _encryptionService);

                _context.Users.Add(demoUser);
                await _context.SaveChangesAsync();
            }
            else
            {
                demoUser.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return await GetUserByIdAsync(demoUser.Id);
        }

        public async Task<UserStats> GetUserStatsAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.JournalEntries)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }
            
            // Decrypt DateOfBirth for stats calculation
            UserEncryptionHelper.DecryptUserData(user, _encryptionService);

            var entries = user.JournalEntries;
            var totalEntries = entries.Count;
            var recentEntries = entries.Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-30)).ToList();

            var moodCounts = recentEntries
                .Where(e => !string.IsNullOrEmpty(e.Mood))
                .GroupBy(e => e.Mood)
                .ToDictionary(g => g.Key, g => g.Count());

            var averageMood = moodCounts.Any()
                ? moodCounts.OrderByDescending(kvp => kvp.Value).First().Key
                : "Unknown";

            return new UserStats
            {
                UserId = userId,
                TotalJournalEntries = totalEntries,
                EntriesLast30Days = recentEntries.Count,
                AverageMood = averageMood,
                MoodDistribution = moodCounts,
                LastEntryDate = entries.OrderByDescending(e => e.CreatedAt).FirstOrDefault()?.CreatedAt,
                MostCommonMood = moodCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "Unknown",
                TotalChatSessions = user.ChatSessions.Count
            };
        }
    }
}
