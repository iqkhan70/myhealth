using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Shared.Constants;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
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
        private readonly IPiiEncryptionService _encryptionService;

        public UserRequestService(JournalDbContext context, ILogger<UserRequestService> logger, IPiiEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;
            
            // Remove all non-digit characters except + at the start
            var normalized = phone.Trim();
            if (normalized.StartsWith("+"))
            {
                normalized = "+" + new string(normalized.Substring(1).Where(char.IsDigit).ToArray());
            }
            else
            {
                normalized = new string(normalized.Where(char.IsDigit).ToArray());
            }
            
            return normalized;
        }

        public async Task<bool> ValidateEmailAndPhoneAsync(string email, string mobilePhone)
        {
            // Check if email already exists (email is not encrypted, so direct comparison is fine)
            var existingUserByEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (existingUserByEmail != null)
            {
                _logger.LogWarning("ValidateEmailAndPhoneAsync: Email already exists - {Email}", email);
                return false; // Email already exists
            }

            // Check if phone number already exists
            // Since MobilePhone is encrypted, we need to decrypt all users' phone numbers and compare
            if (!string.IsNullOrEmpty(mobilePhone))
            {
                var normalizedInputPhone = NormalizePhoneNumber(mobilePhone);
                _logger.LogInformation("ValidateEmailAndPhoneAsync: Checking phone - Input: {InputPhone}, Normalized: {NormalizedPhone}", mobilePhone, normalizedInputPhone);
                
                var allUsers = await _context.Users
                    .Where(u => u.MobilePhoneEncrypted != null && u.MobilePhoneEncrypted != string.Empty)
                    .ToListAsync();

                _logger.LogInformation("ValidateEmailAndPhoneAsync: Found {Count} users with phone numbers", allUsers.Count);

                // Decrypt all users' phone numbers for comparison
                UserEncryptionHelper.DecryptUserData(allUsers, _encryptionService);

                var existingUserByPhone = allUsers
                    .FirstOrDefault(u => 
                    {
                        if (string.IsNullOrEmpty(u.MobilePhone))
                            return false;
                        
                        var normalizedUserPhone = NormalizePhoneNumber(u.MobilePhone);
                        var matches = normalizedUserPhone == normalizedInputPhone;
                        
                        if (matches)
                        {
                            _logger.LogWarning("ValidateEmailAndPhoneAsync: Phone match found - UserId: {UserId}, UserPhone: {UserPhone}, NormalizedUserPhone: {NormalizedUserPhone}, InputPhone: {InputPhone}, NormalizedInputPhone: {NormalizedInputPhone}", 
                                u.Id, u.MobilePhone, normalizedUserPhone, mobilePhone, normalizedInputPhone);
                        }
                        
                        return matches;
                    });

                if (existingUserByPhone != null)
                {
                    _logger.LogWarning("ValidateEmailAndPhoneAsync: Phone number already exists - UserId: {UserId}, Phone: {Phone}", existingUserByPhone.Id, existingUserByPhone.MobilePhone);
                    return false; // Phone number already exists
                }
            }

            _logger.LogInformation("ValidateEmailAndPhoneAsync: Email and phone are available - Email: {Email}, Phone: {Phone}", email, mobilePhone);
            return true; // Email and phone are available
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
            // Load all pending requests and decrypt them for comparison
            var allPendingRequests = await _context.UserRequests
                .Where(ur => ur.Status == UserRequestStatus.Pending)
                .ToListAsync();
            
            _logger.LogInformation("CreateUserRequestAsync: Found {Count} pending requests to check", allPendingRequests.Count);
            
            // Decrypt phone numbers for comparison
            UserEncryptionHelper.DecryptUserRequestData(allPendingRequests, _encryptionService);
            
            var normalizedInputPhone = NormalizePhoneNumber(request.MobilePhone);
            _logger.LogInformation("CreateUserRequestAsync: Checking for existing request - Email: {Email}, Phone: {Phone}, NormalizedPhone: {NormalizedPhone}", 
                request.Email, request.MobilePhone, normalizedInputPhone);
            
            var existingRequest = allPendingRequests
                .FirstOrDefault(ur => 
                {
                    var emailMatch = ur.Email.ToLower() == request.Email.ToLower();
                    if (emailMatch)
                    {
                        _logger.LogWarning("CreateUserRequestAsync: Found pending request with matching email - RequestId: {RequestId}, Email: {Email}", ur.Id, ur.Email);
                        return true;
                    }
                    
                    if (!string.IsNullOrEmpty(ur.MobilePhone) && !string.IsNullOrEmpty(request.MobilePhone))
                    {
                        var normalizedRequestPhone = NormalizePhoneNumber(ur.MobilePhone);
                        var phoneMatch = normalizedRequestPhone == normalizedInputPhone;
                        
                        if (phoneMatch)
                        {
                            _logger.LogWarning("CreateUserRequestAsync: Found pending request with matching phone - RequestId: {RequestId}, RequestPhone: {RequestPhone}, NormalizedRequestPhone: {NormalizedRequestPhone}, InputPhone: {InputPhone}, NormalizedInputPhone: {NormalizedInputPhone}", 
                                ur.Id, ur.MobilePhone, normalizedRequestPhone, request.MobilePhone, normalizedInputPhone);
                        }
                        
                        return phoneMatch;
                    }
                    
                    return false;
                });

            if (existingRequest != null)
            {
                throw new InvalidOperationException("A pending request with this email or phone number already exists.");
            }

            // Normalize DateOfBirth to date-only (midnight, no timezone) to avoid timezone conversion issues
            var dateOfBirthDateOnly = request.DateOfBirth.Date; // Extract date part only, discarding time
            
            var userRequest = new UserRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                DateOfBirth = dateOfBirthDateOnly, // Use date-only to avoid timezone issues
                Gender = request.Gender,
                MobilePhone = request.MobilePhone,
                Reason = request.Reason,
                Status = UserRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Store the original DateOfBirth before encryption (for return value)
            var originalDateOfBirth = userRequest.DateOfBirth;

            // Encrypt DateOfBirth before saving
            UserEncryptionHelper.EncryptUserRequestData(userRequest, _encryptionService);

            _context.UserRequests.Add(userRequest);
            await _context.SaveChangesAsync();

            // After SaveChanges, EF Core might reset the computed DateOfBirth property
            // Reload from database to get the encrypted value, then decrypt
            await _context.Entry(userRequest).ReloadAsync();
            
            // Decrypt DateOfBirth after saving for return value
            UserEncryptionHelper.DecryptUserRequestData(userRequest, _encryptionService);
            
            // If decryption failed or returned MinValue, use the original date we stored
            if (userRequest.DateOfBirth == DateTime.MinValue && originalDateOfBirth != DateTime.MinValue)
            {
                userRequest.DateOfBirth = originalDateOfBirth;
            }

            _logger.LogInformation("User request created: ID={Id}, Email={Email}, Phone={Phone}, DateOfBirth={DateOfBirth}", 
                userRequest.Id, userRequest.Email, userRequest.MobilePhone, userRequest.DateOfBirth);

            return userRequest;
        }

        public async Task<List<UserRequest>> GetAllUserRequestsAsync()
        {
            // Get all user requests
            var allRequests = await _context.UserRequests
                .Include(ur => ur.ReviewedByUser)
                .OrderByDescending(ur => ur.RequestedAt)
                .ToListAsync();

            // Decrypt all requests first (needed for phone comparison)
            UserEncryptionHelper.DecryptUserRequestData(allRequests, _encryptionService);

            // Get all user emails that exist in the Users table (email is not encrypted)
            var existingUserEmails = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => u.Email.ToLower())
                .ToListAsync();

            // Get all users with phone numbers and decrypt them for comparison
            var allUsersWithPhones = await _context.Users
                .Where(u => u.IsActive && u.MobilePhoneEncrypted != null)
                .ToListAsync();
            
            // Decrypt phone numbers for comparison
            UserEncryptionHelper.DecryptUserData(allUsersWithPhones, _encryptionService);
            
            var existingUserPhones = allUsersWithPhones
                .Where(u => !string.IsNullOrEmpty(u.MobilePhone))
                .Select(u => NormalizePhoneNumber(u.MobilePhone!))
                .ToList();

            // Filter out requests where a user already exists with the same email or phone
            var filteredRequests = allRequests
                .Where(ur => 
                {
                    // If request is approved, check if user exists
                    if (ur.Status == UserRequestStatus.Approved)
                    {
                        var emailExists = existingUserEmails.Contains(ur.Email.ToLower());
                        var phoneExists = !string.IsNullOrEmpty(ur.MobilePhone) && existingUserPhones.Contains(NormalizePhoneNumber(ur.MobilePhone));
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
            var userRequest = await _context.UserRequests
                .Include(ur => ur.ReviewedByUser)
                .FirstOrDefaultAsync(ur => ur.Id == id);
            
            if (userRequest != null)
            {
                // Decrypt DateOfBirth after loading
                UserEncryptionHelper.DecryptUserRequestData(userRequest, _encryptionService);
            }
            
            return userRequest;
        }

        public async Task<UserRequest> ApproveUserRequestAsync(int id, int reviewerUserId, string notes, ISmsService smsService, INotificationService notificationService)
        {
            var userRequest = await _context.UserRequests.FindAsync(id);
            if (userRequest == null)
            {
                throw new InvalidOperationException("User request not found.");
            }
            
            // Decrypt DateOfBirth before using it
            UserEncryptionHelper.DecryptUserRequestData(userRequest, _encryptionService);

            // If already approved, check if user exists (might have been approved before)
            if (userRequest.Status == UserRequestStatus.Approved)
            {
                // Check email first (not encrypted)
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == userRequest.Email.ToLower());
                
                if (existingUserByEmail != null)
                {
                    throw new InvalidOperationException("This request was already approved and a user account already exists.");
                }
                
                // Check phone number (need to decrypt all users' phones for comparison)
                if (!string.IsNullOrEmpty(userRequest.MobilePhone))
                {
                    var normalizedRequestPhone = NormalizePhoneNumber(userRequest.MobilePhone);
                    
                    var allUsersWithPhones = await _context.Users
                        .Where(u => u.MobilePhoneEncrypted != null && u.MobilePhoneEncrypted != string.Empty)
                        .ToListAsync();
                    
                    UserEncryptionHelper.DecryptUserData(allUsersWithPhones, _encryptionService);
                    
                    var existingUserByPhone = allUsersWithPhones
                        .FirstOrDefault(u => 
                        {
                            if (string.IsNullOrEmpty(u.MobilePhone))
                                return false;
                            
                            var normalizedUserPhone = NormalizePhoneNumber(u.MobilePhone);
                            return normalizedUserPhone == normalizedRequestPhone;
                        });
                    
                    if (existingUserByPhone != null)
                    {
                        throw new InvalidOperationException("This request was already approved and a user account already exists.");
                    }
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
                MobilePhone = userRequest.MobilePhone, // Will be encrypted before save
                RoleId = Roles.Patient, // Create as Patient
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsFirstLogin = true,
                MustChangePassword = true // Force password change on first login
            };

            // Encrypt PII data (DateOfBirth and MobilePhone) before saving
            UserEncryptionHelper.EncryptUserData(user, _encryptionService);

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

            // Save changes first to get the user ID
            await _context.SaveChangesAsync();

            // If reviewer is a Coordinator, automatically assign the new patient to the Coordinator
            if (reviewer != null && reviewer.RoleId == Roles.Coordinator)
            {
                // Check if assignment already exists (shouldn't, but be safe)
                var existingAssignment = await _context.UserAssignments
                    .FirstOrDefaultAsync(ua => ua.AssignerId == reviewerUserId && ua.AssigneeId == user.Id);
                
                if (existingAssignment == null)
                {
                    var assignment = new UserAssignment
                    {
                        AssignerId = reviewerUserId,
                        AssigneeId = user.Id,
                        AssignedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    
                    _context.UserAssignments.Add(assignment);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Patient {PatientId} automatically assigned to Coordinator {CoordinatorId} upon approval", 
                        user.Id, reviewerUserId);
                }
            }

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

