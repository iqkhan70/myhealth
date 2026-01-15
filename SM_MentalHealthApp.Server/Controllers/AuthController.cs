using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRedisCacheService _redisCache;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IRedisCacheService redisCache, ILogger<AuthController> logger)
        {
            _authService = authService;
            _redisCache = redisCache;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Email and password are required"
                });
            }

            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            // ✅ Store session in Redis (30 minute expiration to match JWT token expiration)
            if (result.Success && !string.IsNullOrEmpty(result.Token) && result.User != null)
            {
                var sessionKey = $"session:{result.User.Id}:{result.Token}";
                var sessionData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    userId = result.User.Id,
                    email = result.User.Email,
                    token = result.Token,
                    expiresAt = DateTime.UtcNow.AddMinutes(30)
                });
                await _redisCache.SetAsync(sessionKey, sessionData, TimeSpan.FromMinutes(30));

                // ✅ Clear ClientAgentSession on login to ensure fresh start
                // This ensures users don't inherit SR context from previous sessions
                // (Even if logout wasn't called, we start fresh on login)
                try
                {
                    var agentSessionService = HttpContext.RequestServices.GetRequiredService<IClientAgentSessionService>();
                    await agentSessionService.ClearActiveServiceRequestAsync(result.User.Id);
                    _logger.LogInformation("Cleared ClientAgentSession for user {UserId} on login to ensure fresh start", result.User.Id);
                }
                catch (Exception ex)
                {
                    // Log but don't fail login if clearing agent session fails
                    _logger.LogWarning(ex, "Failed to clear ClientAgentSession for user {UserId} on login", result.User.Id);
                }
            }

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Email and password are required"
                });
            }

            if (string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "First name and last name are required"
                });
            }

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            // ✅ Store session in Redis (30 minute expiration to match JWT token expiration)
            if (result.Success && !string.IsNullOrEmpty(result.Token) && result.User != null)
            {
                var sessionKey = $"session:{result.User.Id}:{result.Token}";
                var sessionData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    userId = result.User.Id,
                    email = result.User.Email,
                    token = result.Token,
                    expiresAt = DateTime.UtcNow.AddMinutes(30)
                });
                await _redisCache.SetAsync(sessionKey, sessionData, TimeSpan.FromMinutes(30));
            }

            return Ok(result);
        }

        [HttpPost("validate")]
        public async Task<ActionResult<bool>> ValidateToken([FromBody] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(false);
            }

            // ✅ First validate JWT token structure
            var isValidJwt = await _authService.ValidateTokenAsync(token);
            if (!isValidJwt)
            {
                return Ok(false);
            }

            // ✅ Then check if session exists in Redis (session management)
            var user = await _authService.GetUserFromTokenAsync(token);
            if (user == null)
            {
                return Ok(false);
            }

            var sessionKey = $"session:{user.Id}:{token}";
            var sessionExists = await _redisCache.ExistsAsync(sessionKey);

            return Ok(sessionExists);
        }

        [HttpGet("user")]
        public async Task<ActionResult<AuthUser?>> GetCurrentUser()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var user = await _authService.GetUserFromTokenAsync(token);

            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(user);
        }

        [HttpPost("change-password")]
        [Authorize] // Require authentication
        public async Task<ActionResult<ChangePasswordResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Security: User ID comes from JWT token, NOT from request body
            // This prevents users from changing other users' passwords
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var user = await _authService.GetUserFromTokenAsync(token);

            if (user == null)
            {
                return Unauthorized();
            }

            // Use authenticated user's ID - ignore any email/userId in request body for security
            var result = await _authService.ChangePasswordAsync(user.Id, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Email is required"
                });
            }

            var result = await _authService.ForgotPasswordAsync(request);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new ResetPasswordResponse
                {
                    Success = false,
                    Message = "Token, email, and new password are required"
                });
            }

            var result = await _authService.ResetPasswordAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Logout - invalidates session in Redis and clears ClientAgentSession
        /// </summary>
        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var user = await _authService.GetUserFromTokenAsync(token);

            if (user != null)
            {
                // ✅ Remove all sessions for this user from Redis
                var sessionKey = $"session:{user.Id}:{token}";
                await _redisCache.RemoveAsync(sessionKey);

                // ✅ Clear ClientAgentSession to reset SR context on logout
                // This ensures users start fresh when they log back in
                try
                {
                    var agentSessionService = HttpContext.RequestServices.GetRequiredService<IClientAgentSessionService>();
                    await agentSessionService.ClearActiveServiceRequestAsync(user.Id);
                    _logger.LogInformation("Cleared ClientAgentSession for user {UserId} on logout", user.Id);
                }
                catch (Exception ex)
                {
                    // Log but don't fail logout if clearing agent session fails
                    _logger.LogWarning(ex, "Failed to clear ClientAgentSession for user {UserId} on logout", user.Id);
                }

                // Also remove any other sessions for this user (optional - for security)
                // This would require scanning Redis keys, which is expensive, so we'll just remove the current session
            }

            return Ok(new { success = true, message = "Logged out successfully" });
        }

        /// <summary>
        /// Store user preference in Redis
        /// </summary>
        [HttpPost("cache/set")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> SetCacheValue([FromBody] CacheRequest request)
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var user = await _authService.GetUserFromTokenAsync(token);

            if (user == null)
            {
                return Unauthorized();
            }

            var cacheKey = $"user:{user.Id}:{request.Key}";
            var expiration = request.ExpirationMinutes > 0
                ? TimeSpan.FromMinutes(request.ExpirationMinutes)
                : TimeSpan.FromDays(30); // Default 30 days

            await _redisCache.SetAsync(cacheKey, request.Value, expiration);

            return Ok(new { success = true });
        }

        /// <summary>
        /// Get user preference from Redis
        /// </summary>
        [HttpGet("cache/get/{key}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<string?>> GetCacheValue(string key)
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var user = await _authService.GetUserFromTokenAsync(token);

            if (user == null)
            {
                return Unauthorized();
            }

            var cacheKey = $"user:{user.Id}:{key}";
            var value = await _redisCache.GetAsync(cacheKey);

            return Ok(new { success = true, value = value });
        }

        /// <summary>
        /// Remove user preference from Redis
        /// </summary>
        [HttpDelete("cache/remove/{key}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> RemoveCacheValue(string key)
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var user = await _authService.GetUserFromTokenAsync(token);

            if (user == null)
            {
                return Unauthorized();
            }

            var cacheKey = $"user:{user.Id}:{key}";
            await _redisCache.RemoveAsync(cacheKey);

            return Ok(new { success = true });
        }
    }

    public class CacheRequest
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; } = 0; // 0 = use default
    }
}
