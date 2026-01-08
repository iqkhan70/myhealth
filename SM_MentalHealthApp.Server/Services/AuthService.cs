using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RegisterAsync(RegisterRequest request);
        Task<bool> ValidateTokenAsync(string token);
        Task<AuthUser?> GetUserFromTokenAsync(string token);
        Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly JournalDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPiiEncryptionService _encryptionService;
        private readonly INotificationService? _notificationService;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public AuthService(JournalDbContext context, IConfiguration configuration, IPiiEncryptionService encryptionService, INotificationService? notificationService = null, IHttpContextAccessor? httpContextAccessor = null)
        {
            _context = context;
            _configuration = configuration;
            _encryptionService = encryptionService;
            _notificationService = notificationService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find user in Users table
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (user != null)
                {
                    if (!VerifyPassword(request.Password, user.PasswordHash))
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Message = "Invalid email or password"
                        };
                    }

                    // Update last login and first login status
                    user.LastLoginAt = DateTime.UtcNow;
                    if (user.IsFirstLogin)
                    {
                        user.IsFirstLogin = false;
                        user.MustChangePassword = true; // Force password change on first login
                    }
                    await _context.SaveChangesAsync();

                    // Decrypt PII data before returning
                    UserEncryptionHelper.DecryptUserData(user, _encryptionService);

                    var token = GenerateJwtToken(user.Id, user.Email, user.FirstName, user.LastName, user.RoleId, user.Role?.Name ?? "User", user.IsFirstLogin, user.MustChangePassword);

                    return new LoginResponse
                    {
                        Success = true,
                        Token = token,
                        Message = user.MustChangePassword ? "Login successful. Please change your password." : "Login successful",
                        User = user
                    };
                }

                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    DateOfBirth = request.DateOfBirth, // Will be encrypted before save
                    Gender = request.Gender,
                    RoleId = 1, // Default to Patient role
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Encrypt PII data before saving
                UserEncryptionHelper.EncryptUserData(user, _encryptionService);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Decrypt PII data after saving for response
                UserEncryptionHelper.DecryptUserData(user, _encryptionService);

                var token = GenerateJwtToken(user.Id, user.Email, user.FirstName, user.LastName, user.RoleId, user.Role?.Name ?? "Patient", user.IsFirstLogin, user.MustChangePassword);

                return new LoginResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Registration successful",
                    User = user
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!");

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "MentalHealthApp",
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"] ?? "MentalHealthAppUsers",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<AuthUser?> GetUserFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "userId");
                var roleIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "roleId");
                var roleNameClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "roleName")?.Value ?? "Patient";

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return null;
                }

                if (roleIdClaim == null || !int.TryParse(roleIdClaim.Value, out int roleId))
                {
                    return null;
                }

                // Check if user is a patient
                if (roleId == Shared.Constants.Roles.Patient)
                {
                    var user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                    if (user == null)
                        return null;

                    return new AuthUser
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        RoleId = user.RoleId,
                        RoleName = user.Role?.Name ?? "Patient",
                        IsFirstLogin = user.IsFirstLogin,
                        MustChangePassword = user.MustChangePassword
                    };
                }
                // Check if user is a doctor, admin, coordinator, attorney, or SME
                else if (roleId == Shared.Constants.Roles.Doctor || roleId == Shared.Constants.Roles.Admin || roleId == Shared.Constants.Roles.Coordinator || roleId == Shared.Constants.Roles.Attorney || roleId == Shared.Constants.Roles.Sme)
                {
                    var user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                    if (user == null)
                        return null;

                    return new AuthUser
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        RoleId = user.RoleId,
                        RoleName = user.Role?.Name ?? "User",
                        IsFirstLogin = user.IsFirstLogin,
                        MustChangePassword = user.MustChangePassword
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            try
            {
                // Security: userId parameter comes from authenticated JWT token, not from request body
                // This ensures users can only change their own password

                // Validate input
                if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "New password must be at least 6 characters long"
                    };
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "New password and confirmation do not match"
                    };
                }

                // Get the user by ID from token (not from request) - security enforced
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Verify current password
                if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    };
                }

                // Update password
                user.PasswordHash = HashPassword(request.NewPassword);
                user.MustChangePassword = false; // Clear the flag after successful change
                await _context.SaveChangesAsync();

                return new ChangePasswordResponse
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                return new ChangePasswordResponse
                {
                    Success = false,
                    Message = $"Password change failed: {ex.Message}"
                };
            }
        }

        private string HashPassword(string password)
        {
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

        private bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                var hashBytes = Convert.FromBase64String(passwordHash);
                var salt = new byte[32];
                Array.Copy(hashBytes, 0, salt, 0, 32);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(32);

                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 32] != hash[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                // Always return success to prevent email enumeration
                if (user == null)
                {
                    return new ForgotPasswordResponse
                    {
                        Success = true,
                        Message = "If an account with that email exists, a password reset link has been sent."
                    };
                }

                // Generate reset token
                var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour
                await _context.SaveChangesAsync();

                // Send email with reset link
                // Try to get base URL from request origin first, then config, then environment variable
                string baseUrl = "https://localhost:5263"; // Default fallback

                // First, try to get from HTTP request (Origin header or Request URL)
                try
                {
                    if (_httpContextAccessor?.HttpContext != null)
                    {
                        var httpRequest = _httpContextAccessor.HttpContext.Request;
                        var origin = httpRequest.Headers["Origin"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(origin))
                        {
                            baseUrl = origin;
                        }
                        else
                        {
                            var scheme = httpRequest.Scheme;
                            var host = httpRequest.Host.Value;
                            if (!string.IsNullOrEmpty(host))
                            {
                                baseUrl = $"{scheme}://{host}";
                            }
                        }
                    }

                }
                catch
                {
                    // If accessing HttpContext fails, fall through to config-based approach
                }

                // If we still have localhost, try config/environment variables
                if (baseUrl.Contains("localhost"))
                {
                    baseUrl = _configuration["AppSettings:BaseUrl"]
                        ?? Environment.GetEnvironmentVariable("BASE_URL")
                        ?? _configuration["BASE_URL"]
                        ?? baseUrl;
                }

                // If still localhost and we're in production/staging, use environment-specific domains
                if (baseUrl.Contains("localhost") && !string.IsNullOrEmpty(_configuration["ASPNETCORE_ENVIRONMENT"]))
                {
                    var env = _configuration["ASPNETCORE_ENVIRONMENT"];
                    if (env == "Staging")
                    {
                        baseUrl = "https://caseflowstage.store";
                    }
                    else if (env == "Production")
                    {
                        baseUrl = "https://caseflow.store";
                    }
                }

                var resetLink = $"{baseUrl}/reset-password?token={resetToken}&email={Uri.EscapeDataString(user.Email)}";

                var emailSubject = "Password Reset Request";
                var emailBody = $@"
<html>
<body>
    <h2>Password Reset Request</h2>
    <p>Hello {user.FirstName},</p>
    <p>You requested to reset your password. Click the link below to reset your password:</p>
    <p><a href=""{resetLink}"">{resetLink}</a></p>
    <p>This link will expire in 1 hour.</p>
    <p>If you did not request this password reset, please ignore this email.</p>
    <p>Thank you,<br/>Health App Team</p>
</body>
</html>";

                if (_notificationService != null)
                {
                    await _notificationService.SendEmailNotification(user.Id, emailSubject, emailBody);
                }

                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "If an account with that email exists, a password reset link has been sent."
                };
            }
            catch (Exception ex)
            {
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your request. Please try again later."
                };
            }
        }

        public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                if (request.NewPassword != request.ConfirmPassword)
                {
                    return new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "New password and confirm password do not match."
                    };
                }

                if (request.NewPassword.Length < 6)
                {
                    return new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Password must be at least 6 characters long."
                    };
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email &&
                                             u.PasswordResetToken == request.Token &&
                                             u.IsActive);

                if (user == null || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
                {
                    return new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset token. Please request a new password reset."
                    };
                }

                // Update password
                user.PasswordHash = HashPassword(request.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.MustChangePassword = false;
                await _context.SaveChangesAsync();

                return new ResetPasswordResponse
                {
                    Success = true,
                    Message = "Password has been reset successfully. You can now login with your new password."
                };
            }
            catch (Exception ex)
            {
                return new ResetPasswordResponse
                {
                    Success = false,
                    Message = $"An error occurred while resetting your password: {ex.Message}"
                };
            }
        }

        private string GenerateJwtToken(int id, string email, string firstName, string lastName, int roleId, string roleName, bool isFirstLogin, bool mustChangePassword)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", id.ToString()),
                    new Claim("email", email),
                    new Claim("firstName", firstName),
                    new Claim("lastName", lastName),
                    new Claim("roleId", roleId.ToString()),
                    new Claim("roleName", roleName),
                    new Claim(ClaimTypes.Name, $"{firstName} {lastName}"),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, roleName),
                    new Claim("isFirstLogin", isFirstLogin.ToString()),
                    new Claim("mustChangePassword", mustChangePassword.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(30), // ✅ 30 minute expiration to match session timeout
                Issuer = _configuration["Jwt:Issuer"] ?? "MentalHealthApp",
                Audience = _configuration["Jwt:Audience"] ?? "MentalHealthAppUsers",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
