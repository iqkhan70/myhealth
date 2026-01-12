using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Shared.Constants;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class AgenticAIController : ControllerBase
    {
        private readonly IServiceRequestAgenticAIService _agenticAIService;
        private readonly ILogger<AgenticAIController> _logger;

        public AgenticAIController(
            IServiceRequestAgenticAIService agenticAIService,
            ILogger<AgenticAIController> logger)
        {
            _agenticAIService = agenticAIService;
            _logger = logger;
        }

        /// <summary>
        /// Process a service request message using agentic AI
        /// </summary>
        [HttpPost("process-service-request")]
        public async Task<ActionResult<ProcessServiceRequestResponse>> ProcessServiceRequest([FromBody] ProcessServiceRequestRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                // Verify client access
                if (currentRoleId == Roles.Patient && request.ClientId != currentUserId)
                {
                    _logger.LogWarning("Patient {UserId} attempted to process SR for different client {ClientId}", 
                        currentUserId, request.ClientId);
                    return BadRequest("You can only process service requests for yourself.");
                }

                var response = await _agenticAIService.ProcessServiceRequestAsync(
                    request.ClientId,
                    request.ClientMessage,
                    request.ServiceRequestId);

                return Ok(new ProcessServiceRequestResponse
                {
                    Success = true,
                    Response = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing service request with agentic AI");
                return StatusCode(500, new ProcessServiceRequestResponse
                {
                    Success = false,
                    ErrorMessage = "An error occurred while processing your request."
                });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
        }

        private int? GetCurrentRoleId()
        {
            var roleIdClaim = User.FindFirst("roleId");
            return roleIdClaim != null && int.TryParse(roleIdClaim.Value, out var roleId) ? roleId : null;
        }
    }
}

