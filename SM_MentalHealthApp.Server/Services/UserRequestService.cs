using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Shared.Constants;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IUserRequestService
    {
        Task<UserRequest> CreateUserRequestAsync(CreateUserRequestRequest request);
        Task<List<UserRequest>> GetAllUserRequestsAsync();
        Task<UserRequest?> GetUserRequestByIdAsync(int id);
        Task<UserRequest> ApproveUserRequestAsync(int id, int reviewerUserId, string notes, ISmsService smsService, INotificationService notificationService);
        Task<UserRequest> RejectUserRequestAsync(int id, int reviewerUserId, string notes);
        Task<UserRequest> MarkPendingUserRequestAsync(int id, int reviewerUserId, string notes);
        Task<bool> ValidateEmailAndPhoneAsync(string email, string mobilePhone);
    }

    public class UserRequestService : IUserRequestService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<UserRequestService> _logger;

        public UserRequestService(JournalDbContext context, ILogger<UserRequestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ValidateEmailAndPhoneAsync(string email, string mobilePhone)
        {
            // Check if email or phone already exists in Users table
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => 
                    (u.Email.ToLower() == email.ToLower()) || 
                    (u.MobilePhone != null && u.MobilePhone == mobilePhone));

            return existingUser == null; // Returns true if email/phone is available
        }

        public async Task<UserRequest> CreateUserRequestAsync(CreateUserRequestRequest request)
        {
            // Validate email and phone don't exist in Users table
            var isValid = await ValidateEmailAndPhoneAsync(request.Email, request.MobilePhone);
            if (!isValid)
            {
                throw new InvalidOperationException("A user with this email or phone number already exists in the system.");
            }

            // Check if there's already a pending request with same email or phone
            var existingRequest = await _context.UserRequests
                .FirstOrDefaultAsync(ur => 
                    (ur.Email.ToLower() == request.Email.ToLower() || ur.MobilePhone == request.MobilePhone) &&
                    ur.Status == UserRequestStatus.Pending);

            if (existingRequest != null)
            {
                throw new InvalidOperationException("A pending request with this email or phone number already exists.");
            }

            var userRequest = new UserRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                MobilePhone = request.MobilePhone,
                Reason = request.Reason,
                Status = UserRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserRequests.Add(userRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User request created: ID={Id}, Email={Email}, Phone={Phone}", 
                userRequest.Id, userRequest.Email, userRequest.MobilePhone);

            return userRequest;
        }

        public async Task<List<UserRequest>> GetAllUserRequestsAsync()
        {
            // Get all user requests
            var allRequests = await _context.UserRequests
                .Include(ur => ur.ReviewedByUser)
                .OrderByDescending(ur => ur.RequestedAt)
                .ToListAsync();

            // Get all user emails and phone numbers that exist in the Users table
            var existingUserEmails = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => u.Email.ToLower())
                .ToListAsync();

            var existingUserPhones = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => u.MobilePhone)
                .Where(phone => !string.IsNullOrEmpty(phone))
                .ToListAsync();

            // Filter out requests where a user already exists with the same email or phone
            var filteredRequests = allRequests
                .Where(ur => 
                {
                    // If request is approved, check if user exists
                    if (ur.Status == UserRequestStatus.Approved)
                    {
                        var emailExists = existingUserEmails.Contains(ur.Email.ToLower());
                        var phoneExists = existingUserPhones.Contains(ur.MobilePhone);
                        // Hide if user exists (approved and user created)
                        return !emailExists && !phoneExists;
                    }
                    // Show pending and rejected requests
                    return true;
                })
                .ToList();

            return filteredRequests;
        }

        public async Task<UserRequest?> GetUserRequestByIdAsync(int id)
        {
            return await _context.UserRequests
                .Include(ur => ur.ReviewedByUser)
                .FirstOrDefaultAsync(ur => ur.Id == id);
        }

        public async Task<UserRequest> ApproveUserRequestAsync(int id, int reviewerUserId, string notes, ISmsService smsService, INotificationService notificationService)
        {
            var userRequest = await _context.UserRequests.FindAsync(id);
            if (userRequest == null)
            {
                throw new InvalidOperationException("User request not found.");
            }

            // If already approved, check if user exists (might have been approved before)
            if (userRequest.Status == UserRequestStatus.Approved)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        (u.Email.ToLower() == userRequest.Email.ToLower()) || 
                        (u.MobilePhone != null && u.MobilePhone == userRequest.MobilePhone));
                
                if (existingUser != null)
                {
                    throw new InvalidOperationException("This request was already approved and a user account already exists.");
                }
                // If approved but user doesn't exist, allow re-approval (edge case)
            }

            // Validate email and phone still don't exist (in case user was created between request and approval)
            var isValid = await ValidateEmailAndPhoneAsync(userRequest.Email, userRequest.MobilePhone);
            if (!isValid)
            {
                throw new InvalidOperationException("A user with this email or phone number already exists. Cannot approve this request.");
            }

            // Generate temporary password
            var tempPassword = GenerateTemporaryPassword();

            // Create user as Patient
            var user = new User
            {
                FirstName = userRequest.FirstName,
                LastName = userRequest.LastName,
                Email = userRequest.Email,
                PasswordHash = HashPassword(tempPassword),
                DateOfBirth = userRequest.DateOfBirth,
                Gender = userRequest.Gender,
                MobilePhone = userRequest.MobilePhone,
                RoleId = Roles.Patient, // Create as Patient
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsFirstLogin = true,
                MustChangePassword = true // Force password change on first login
            };

            _context.Users.Add(user);
            
            // Get reviewer name for notes
            var reviewer = await _context.Users.FindAsync(reviewerUserId);
            var reviewerName = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}" : "System";
            
            // Append notes instead of overwriting
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var newNoteEntry = $"\n\n--- APPROVED on {timestamp} by {reviewerName} ---\n{notes}";
            userRequest.Notes = string.IsNullOrWhiteSpace(userRequest.Notes) 
                ? newNoteEntry.TrimStart() 
                : userRequest.Notes + newNoteEntry;
            
            // Update user request
            userRequest.Status = UserRequestStatus.Approved;
            userRequest.ReviewedByUserId = reviewerUserId;
            userRequest.ReviewedAt = DateTime.UtcNow;
            userRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Prepare credentials message
            var credentialsMessage = $"Welcome to Health App! Your account has been approved.\n\nUsername: {user.Email}\nTemporary Password: {tempPassword}\n\nPlease change your password on first login.\n\nYou will also receive these credentials via email.";
            var emailSubject = "Health App - Account Approved";
            var emailBody = $@"
<html>
<body>
    <h2>Welcome to Health App!</h2>
    <p>Your account has been approved and is ready to use.</p>
    <p><strong>Username:</strong> {user.Email}</p>
    <p><strong>Temporary Password:</strong> {tempPassword}</p>
    <p><strong>Important:</strong> Please change your password on first login for security.</p>
    <p>You will also receive these credentials via SMS. Both SMS and email have been sent to ensure you receive your login information.</p>
    <p>Thank you for using Health App!</p>
</body>
</html>";

            // Send SMS (don't fail approval if SMS fails, but log it)
            try
            {
                var smsSent = await smsService.SendSmsAsync(user.MobilePhone, credentialsMessage);
                if (smsSent)
                {
                    _logger.LogInformation("SMS sent successfully to {Phone} for user {UserId}", user.MobilePhone, user.Id);
                }
                else
                {
                    _logger.LogWarning("SMS sending failed for {Phone} (user {UserId}), email will still be sent", user.MobilePhone, user.Id);
                }
            }
            catch (Exception smsEx)
            {
                _logger.LogError(smsEx, "Error sending SMS to {Phone} (user {UserId}), email will still be sent", user.MobilePhone, user.Id);
            }

            // Send Email (don't fail approval if email fails, but log it)
            try
            {
                await notificationService.SendEmailNotification(user.Id, emailSubject, emailBody);
                _logger.LogInformation("Email sent successfully to {Email} for user {UserId}", user.Email, user.Id);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Error sending email to {Email} (user {UserId}), SMS was still sent", user.Email, user.Id);
            }

            _logger.LogInformation("User request approved: ID={Id}, User created: ID={UserId}, Email={Email}, SMS and Email notifications sent", 
                userRequest.Id, user.Id, user.Email);

            return userRequest;
        }

        public async Task<UserRequest> RejectUserRequestAsync(int id, int reviewerUserId, string notes)
        {
            var userRequest = await _context.UserRequests.FindAsync(id);
            if (userRequest == null)
            {
                throw new InvalidOperationException("User request not found.");
            }

            // Get reviewer name for notes
            var reviewer = await _context.Users.FindAsync(reviewerUserId);
            var reviewerName = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}" : "System";
            
            // Append notes instead of overwriting
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var newNoteEntry = $"\n\n--- REJECTED on {timestamp} by {reviewerName} ---\n{notes}";
            userRequest.Notes = string.IsNullOrWhiteSpace(userRequest.Notes) 
                ? newNoteEntry.TrimStart() 
                : userRequest.Notes + newNoteEntry;

            userRequest.Status = UserRequestStatus.Rejected;
            userRequest.ReviewedByUserId = reviewerUserId;
            userRequest.ReviewedAt = DateTime.UtcNow;
            userRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User request rejected: ID={Id}, Reviewer={ReviewerId}", 
                userRequest.Id, reviewerUserId);

            return userRequest;
        }

        public async Task<UserRequest> MarkPendingUserRequestAsync(int id, int reviewerUserId, string notes)
        {
            var userRequest = await _context.UserRequests.FindAsync(id);
            if (userRequest == null)
            {
                throw new InvalidOperationException("User request not found.");
            }

            // Get reviewer name for notes
            var reviewer = await _context.Users.FindAsync(reviewerUserId);
            var reviewerName = reviewer != null ? $"{reviewer.FirstName} {reviewer.LastName}" : "System";
            
            // Append notes instead of overwriting
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var newNoteEntry = $"\n\n--- MARKED AS PENDING on {timestamp} by {reviewerName} ---\n{notes}";
            userRequest.Notes = string.IsNullOrWhiteSpace(userRequest.Notes) 
                ? newNoteEntry.TrimStart() 
                : userRequest.Notes + newNoteEntry;

            // Update notes and reviewer info, but keep status as Pending
            userRequest.ReviewedByUserId = reviewerUserId;
            userRequest.ReviewedAt = DateTime.UtcNow;
            userRequest.UpdatedAt = DateTime.UtcNow;
            // Status remains Pending

            await _context.SaveChangesAsync();

            _logger.LogInformation("User request marked as pending: ID={Id}, Reviewer={ReviewerId}", 
                userRequest.Id, reviewerUserId);

            return userRequest;
        }

        private string HashPassword(string password)
        {
            // Use the same PBKDF2 hashing method as AuthService for consistency
            // This ensures passwords created here can be verified by AuthService
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[32];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            var hashBytes = new byte[64];
            Array.Copy(salt, 0, hashBytes, 0, 32);
            Array.Copy(hash, 0, hashBytes, 32, 32);

            return Convert.ToBase64String(hashBytes);
        }

        private string GenerateTemporaryPassword()
        {
            // Generate a random 8-character password
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

