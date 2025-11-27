using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Controllers;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class UserRequestController : BaseController
    {
        private readonly IUserRequestService _userRequestService;
        private readonly ILogger<UserRequestController> _logger;

        public UserRequestController(
            IUserRequestService userRequestService,
            ILogger<UserRequestController> logger)
        {
            _userRequestService = userRequestService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new user request (guest registration) - No authentication required
        /// </summary>
        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<ActionResult<UserRequest>> CreateUserRequest([FromBody] CreateUserRequestRequest request)
        {
            try
            {
                // Validate email and phone don't exist
                var isValid = await _userRequestService.ValidateEmailAndPhoneAsync(request.Email, request.MobilePhone);
                if (!isValid)
                {
                    return BadRequest(new { message = "A user with this email or phone number already exists in the system. Please use different credentials." });
                }

                var userRequest = await _userRequestService.CreateUserRequestAsync(request);
                return Ok(userRequest);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user request");
                return StatusCode(500, new { message = "An error occurred while creating the user request." });
            }
        }

        /// <summary>
        /// Get all user requests - Admin and Coordinator only
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<List<UserRequest>>> GetAllUserRequests()
        {
            try
            {
                var requests = await _userRequestService.GetAllUserRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user requests");
                return StatusCode(500, new { message = "An error occurred while retrieving user requests." });
            }
        }

        /// <summary>
        /// Get a specific user request by ID - Admin and Coordinator only
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<UserRequest>> GetUserRequest(int id)
        {
            try
            {
                var request = await _userRequestService.GetUserRequestByIdAsync(id);
                if (request == null)
                {
                    return NotFound(new { message = "User request not found." });
                }
                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user request {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the user request." });
            }
        }

        /// <summary>
        /// Approve a user request - Admin and Coordinator only
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<UserRequest>> ApproveUserRequest(
            int id,
            [FromBody] ApproveUserRequestRequest request,
            [FromServices] ISmsService smsService)
        {
            try
            {
                var reviewerUserId = GetCurrentUserId();
                if (!reviewerUserId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }

                var userRequest = await _userRequestService.ApproveUserRequestAsync(
                    id, reviewerUserId.Value, request.Notes, smsService);

                return Ok(new { message = "User request approved successfully. User account created and SMS sent.", userRequest });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user request {Id}", id);
                return StatusCode(500, new { message = "An error occurred while approving the user request." });
            }
        }

        /// <summary>
        /// Reject a user request - Admin and Coordinator only
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<UserRequest>> RejectUserRequest(
            int id,
            [FromBody] RejectUserRequestRequest request)
        {
            try
            {
                var reviewerUserId = GetCurrentUserId();
                if (!reviewerUserId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }

                var userRequest = await _userRequestService.RejectUserRequestAsync(
                    id, reviewerUserId.Value, request.Notes);

                return Ok(new { message = "User request rejected successfully.", userRequest });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user request {Id}", id);
                return StatusCode(500, new { message = "An error occurred while rejecting the user request." });
            }
        }

        /// <summary>
        /// Mark a user request as pending (for further review) - Admin and Coordinator only
        /// </summary>
        [HttpPost("{id}/pending")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<UserRequest>> MarkPendingUserRequest(
            int id,
            [FromBody] MarkPendingUserRequestRequest request)
        {
            try
            {
                var reviewerUserId = GetCurrentUserId();
                if (!reviewerUserId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid user token." });
                }

                var userRequest = await _userRequestService.MarkPendingUserRequestAsync(
                    id, reviewerUserId.Value, request.Notes);

                return Ok(new { message = "User request marked as pending for further review.", userRequest });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking user request as pending {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the user request." });
            }
        }
    }
}

