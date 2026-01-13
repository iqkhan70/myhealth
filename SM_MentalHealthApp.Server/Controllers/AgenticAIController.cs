using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;

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

        /// <summary>
        /// Set the active Service Request for the current client's agent session
        /// </summary>
        [HttpPost("set-active-sr")]
        [Authorize(Roles = "Patient")]
        public async Task<ActionResult> SetActiveServiceRequest([FromBody] SetActiveServiceRequestRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                // Verify client access
                if (request.ClientId != currentUserId.Value)
                {
                    return BadRequest("You can only set active SR for yourself.");
                }

                var sessionService = HttpContext.RequestServices.GetRequiredService<IClientAgentSessionService>();
                var success = await sessionService.SetActiveServiceRequestAsync(request.ClientId, request.ServiceRequestId);

                if (success)
                {
                    return Ok(new { success = true, message = "Active service request set successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to set active service request" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting active service request");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Clear the active Service Request for the current client's agent session
        /// </summary>
        [HttpPost("clear-active-sr")]
        [Authorize(Roles = "Patient")]
        public async Task<ActionResult> ClearActiveServiceRequest()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                var sessionService = HttpContext.RequestServices.GetRequiredService<IClientAgentSessionService>();
                var success = await sessionService.ClearActiveServiceRequestAsync(currentUserId.Value);

                if (success)
                {
                    return Ok(new { success = true, message = "Active service request cleared successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to clear active service request" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing active service request");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }
    }

    public class SetActiveServiceRequestRequest
    {
        public int ClientId { get; set; }
        public int ServiceRequestId { get; set; }
    }
}

