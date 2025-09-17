using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatHistoryController : ControllerBase
    {
        private readonly IChatHistoryService _chatHistoryService;
        private readonly ILogger<ChatHistoryController> _logger;

        public ChatHistoryController(IChatHistoryService chatHistoryService, ILogger<ChatHistoryController> logger)
        {
            _chatHistoryService = chatHistoryService;
            _logger = logger;
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<List<ChatSession>>> GetUserSessions([FromQuery] int? patientId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized("User not authenticated");
                }

                var sessions = await _chatHistoryService.GetUserSessionsAsync(userId, patientId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sessions");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("sessions/{sessionId}")]
        public async Task<ActionResult<ChatSession>> GetSession(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized("User not authenticated");
                }

                var session = await _chatHistoryService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound("Session not found");
                }

                // Get user's role to determine access permissions
                var userRole = GetCurrentUserRole();

                // Role-based access control
                if (userRole == 1) // Patient
                {
                    // Patients can see their own direct conversations and when doctors chat about them
                    if (session.UserId != userId && session.PatientId != userId)
                    {
                        return StatusCode(403, "Access denied to this session");
                    }
                }
                else if (userRole == 2) // Doctor
                {
                    // Doctors can only see their own conversations
                    if (session.UserId != userId)
                    {
                        return StatusCode(403, "Access denied to this session");
                    }
                }
                else if (userRole == 3) // Admin
                {
                    // Admins can see all sessions
                    // No additional checks needed
                }
                else
                {
                    return StatusCode(403, "Access denied to this session");
                }

                return Ok(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpDelete("sessions/{sessionId}")]
        public async Task<ActionResult> DeleteSession(int sessionId)
        {
            try
            {
                _logger.LogInformation("DeleteSession called for sessionId: {SessionId}", sessionId);

                var userId = GetCurrentUserId();
                _logger.LogInformation("Current userId: {UserId}", userId);

                if (userId == 0)
                {
                    _logger.LogWarning("User not authenticated for delete request");
                    return Unauthorized("User not authenticated");
                }

                var session = await _chatHistoryService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound("Session not found");
                }

                // Get user's role to determine access permissions
                var userRole = GetCurrentUserRole();

                // Role-based access control for deletion
                if (userRole == 1) // Patient
                {
                    // Patients can only delete their own direct conversations
                    // They cannot delete doctor's chats about them (even if they are the PatientId)
                    if (session.UserId != userId)
                    {
                        return StatusCode(403, "Access denied: You can only delete your own conversations, not doctor's chats about you");
                    }
                }
                else if (userRole == 2) // Doctor
                {
                    // Doctors can only delete their own sessions
                    if (session.UserId != userId)
                    {
                        return StatusCode(403, "Access denied to this session");
                    }
                }
                else if (userRole == 3) // Admin
                {
                    // Admins can delete any session
                    // No additional checks needed
                }
                else
                {
                    return StatusCode(403, "Access denied to this session");
                }

                await _chatHistoryService.DeleteSessionAsync(sessionId);
                _logger.LogInformation("Session {SessionId} deleted successfully by user {UserId}", sessionId, userId);
                return Ok(new { message = "Session deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("cleanup")]
        public async Task<ActionResult> CleanupExpiredData()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized("User not authenticated");
                }

                // Only allow admins to trigger cleanup
                var userRole = GetCurrentUserRole();
                if (userRole != 3) // Admin role
                {
                    return StatusCode(403, "Only administrators can trigger cleanup");
                }

                await _chatHistoryService.CleanupExpiredDataAsync();
                return Ok(new { message = "Cleanup completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return StatusCode(500, "Internal server error");
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private int GetCurrentUserRole()
        {
            var roleIdClaim = User.FindFirst("roleId")?.Value;
            return int.TryParse(roleIdClaim, out var roleId) ? roleId : 0;
        }
    }
}
