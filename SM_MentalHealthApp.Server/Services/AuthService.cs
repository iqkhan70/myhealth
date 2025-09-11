using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RegisterAsync(RegisterRequest request);
        Task<bool> ValidateTokenAsync(string token);
        Task<AuthUser?> GetUserFromTokenAsync(string token);
        Task<ChangePasswordResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    }

    public class AuthService : IAuthService
    {
        private readonly JournalDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(JournalDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Try to find user in Patients table first
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Email == request.Email && p.IsActive);

                if (patient != null)
                {
                    if (!VerifyPassword(request.Password, patient.PasswordHash))
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Message = "Invalid email or password"
                        };
                    }

                    // Update last login and first login status
                    patient.LastLoginAt = DateTime.UtcNow;
                    if (patient.IsFirstLogin)
                    {
                        patient.IsFirstLogin = false;
                        patient.MustChangePassword = true; // Force password change on first login
                    }
                    await _context.SaveChangesAsync();

                    var token = GenerateJwtToken(patient.Id, patient.Email, patient.FirstName, patient.LastName, patient.RoleId, patient.Role?.Name ?? "Patient", patient.IsFirstLogin, patient.MustChangePassword);

                    return new LoginResponse
                    {
                        Success = true,
                        Token = token,
                        Message = patient.MustChangePassword ? "Login successful. Please change your password." : "Login successful",
                        Patient = patient
                    };
                }

                // Try to find user in Doctors table
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.Email == request.Email && d.IsActive);

                if (doctor != null)
                {
                    if (!VerifyPassword(request.Password, doctor.PasswordHash))
                    {
                        return new LoginResponse
                        {
                            Success = false,
                            Message = "Invalid email or password"
                        };
                    }

                    // Update last login and first login status
                    doctor.LastLoginAt = DateTime.UtcNow;
                    if (doctor.IsFirstLogin)
                    {
                        doctor.IsFirstLogin = false;
                        doctor.MustChangePassword = true; // Force password change on first login
                    }
                    await _context.SaveChangesAsync();

                    var token = GenerateJwtToken(doctor.Id, doctor.Email, doctor.FirstName, doctor.LastName, doctor.RoleId, doctor.Role?.Name ?? "Doctor", doctor.IsFirstLogin, doctor.MustChangePassword);

                    // Create a Patient object for the response (for compatibility)
                    var patientForResponse = new Patient
                    {
                        Id = doctor.Id,
                        FirstName = doctor.FirstName,
                        LastName = doctor.LastName,
                        Email = doctor.Email,
                        RoleId = doctor.RoleId,
                        Role = doctor.Role,
                        IsFirstLogin = doctor.IsFirstLogin,
                        MustChangePassword = doctor.MustChangePassword
                    };

                    return new LoginResponse
                    {
                        Success = true,
                        Token = token,
                        Message = doctor.MustChangePassword ? "Login successful. Please change your password." : "Login successful",
                        Patient = patientForResponse
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
                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Email == request.Email);

                if (existingPatient != null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                var patient = new Patient
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(patient.Id, patient.Email, patient.FirstName, patient.LastName, patient.RoleId, patient.Role?.Name ?? "Patient", patient.IsFirstLogin, patient.MustChangePassword);

                return new LoginResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Registration successful",
                    Patient = patient
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

                // Check if user is a patient (roleId = 1)
                if (roleId == 1)
                {
                    var patient = await _context.Patients
                        .Include(p => p.Role)
                        .FirstOrDefaultAsync(p => p.Id == userId && p.IsActive);

                    if (patient == null)
                        return null;

                    return new AuthUser
                    {
                        Id = patient.Id,
                        Email = patient.Email,
                        FirstName = patient.FirstName,
                        LastName = patient.LastName,
                        RoleId = patient.RoleId,
                        RoleName = patient.Role?.Name ?? "Patient",
                        IsFirstLogin = patient.IsFirstLogin,
                        MustChangePassword = patient.MustChangePassword
                    };
                }
                // Check if user is a doctor (roleId = 2)
                else if (roleId == 2)
                {
                    var doctor = await _context.Doctors
                        .Include(d => d.Role)
                        .FirstOrDefaultAsync(d => d.Id == userId && d.IsActive);

                    if (doctor == null)
                        return null;

                    return new AuthUser
                    {
                        Id = doctor.Id,
                        Email = doctor.Email,
                        FirstName = doctor.FirstName,
                        LastName = doctor.LastName,
                        RoleId = doctor.RoleId,
                        RoleName = doctor.Role?.Name ?? "Doctor",
                        IsFirstLogin = doctor.IsFirstLogin,
                        MustChangePassword = doctor.MustChangePassword
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

                // Get the patient
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == userId && p.IsActive);

                if (patient == null)
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Verify current password
                if (!VerifyPassword(request.CurrentPassword, patient.PasswordHash))
                {
                    return new ChangePasswordResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    };
                }

                // Update password
                patient.PasswordHash = HashPassword(request.NewPassword);
                patient.MustChangePassword = false; // Clear the flag after successful change
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
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"] ?? "MentalHealthApp",
                Audience = _configuration["Jwt:Audience"] ?? "MentalHealthAppUsers",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
