using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgoraController : ControllerBase
    {
        private readonly AgoraTokenService _agoraTokenService;
        private readonly ILogger<AgoraController> _logger;
        private readonly IConfiguration _configuration;

        public AgoraController(AgoraTokenService agoraTokenService, ILogger<AgoraController> logger, IConfiguration configuration)
        {
            _agoraTokenService = agoraTokenService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("token")]
        public IActionResult GenerateToken([FromBody] AgoraTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ChannelName))
                {
                    return BadRequest("Channel name is required");
                }

                // Generate a unique UID for the user (you can use user ID from JWT)
                var userId = GetUserIdFromToken();
                var uid = (uint)(userId % 1000000); // Ensure UID is within valid range

                var token = _agoraTokenService.GenerateToken(request.ChannelName, uid, request.ExpirationTimeInSeconds ?? 3600);

                var response = new AgoraTokenResponse
                {
                    Token = token,
                    AppId = GetAppIdFromConfig(),
                    ChannelName = request.ChannelName,
                    Uid = uid,
                    ExpirationTime = DateTime.UtcNow.AddSeconds(request.ExpirationTimeInSeconds ?? 3600)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Agora token");
                return StatusCode(500, "Error generating token");
            }
        }

        private int GetUserIdFromToken()
        {
            // Extract user ID from JWT token
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return 0; // Default fallback
        }

        private string GetAppIdFromConfig()
        {
            return _configuration["Agora:AppId"] ?? "efa11b3a7d05409ca979fb25a5b489ae";
        }
    }

    public class AgoraTokenRequest
    {
        public string ChannelName { get; set; } = string.Empty;
        public uint? ExpirationTimeInSeconds { get; set; } = 3600;
    }

    public class AgoraTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public uint Uid { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}
