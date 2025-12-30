using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;
using SM_MentalHealthApp.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatHistoryController : BaseController
    {
        private readonly IChatHistoryService _chatHistoryService;
        private readonly IServiceRequestService _serviceRequestService;
        private readonly ILogger<ChatHistoryController> _logger;
        private readonly JournalDbContext _context;

        public ChatHistoryController(IChatHistoryService chatHistoryService, IServiceRequestService serviceRequestService, ILogger<ChatHistoryController> logger, JournalDbContext context)
        {
            _chatHistoryService = chatHistoryService;
            _serviceRequestService = serviceRequestService;
            _logger = logger;
            _context = context;
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<List<ChatSession>>> GetUserSessions([FromQuery] int? patientId = null, [FromQuery] int? serviceRequestId = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();
                
                if (!userId.HasValue)
                {
                    _logger.LogWarning("GetUserSessions: User not authenticated - GetCurrentUserId returned null. User.Identity: {IsAuthenticated}, Claims: {Claims}",
                        User.Identity?.IsAuthenticated,
                        string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                    return Unauthorized("User not authenticated");
                }

                // For doctors and attorneys, filter by assigned ServiceRequests
                if ((currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney) && userId.HasValue)
                {
                    // Get assigned ServiceRequest IDs for this SME
                    var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(userId.Value);

                    if (!serviceRequestIds.Any())
                    {
                        return Ok(new List<ChatSession>());
                    }

                    // If specific SR requested, verify access
                    if (serviceRequestId.HasValue)
                    {
                        if (!serviceRequestIds.Contains(serviceRequestId.Value))
                            return Forbid("You are not assigned to this service request");
                        
                        serviceRequestIds = new List<int> { serviceRequestId.Value };
                    }

                    // Filter sessions by ServiceRequestId
                    var query = _context.ChatSessions
                        .Where(cs => cs.IsActive && 
                            cs.ServiceRequestId.HasValue && 
                            serviceRequestIds.Contains(cs.ServiceRequestId.Value));

                    if (patientId.HasValue)
                        query = query.Where(cs => cs.PatientId == patientId.Value);
                    else if (userId.HasValue)
                        query = query.Where(cs => cs.UserId == userId.Value);

                    var sessions = await query
                        .OrderByDescending(cs => cs.LastActivityAt)
                        .ToListAsync();

                    return Ok(sessions);
                }

                // For patients and admins, use existing service
                var allSessions = await _chatHistoryService.GetUserSessionsAsync(userId.Value, patientId);
                
                if (serviceRequestId.HasValue)
                    allSessions = allSessions?.Where(s => s.ServiceRequestId == serviceRequestId.Value).ToList() ?? new List<ChatSession>();
                
                // Always return OK with list (empty list if no sessions) - never error on empty
                return Ok(allSessions ?? new List<ChatSession>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sessions - returning empty list. Exception: {Exception}", ex);
                // Return empty list instead of error - allows UI to show empty grid
                return Ok(new List<ChatSession>());
            }
        }

        [HttpGet("sessions/{sessionId}")]
        public async Task<ActionResult<ChatSession>> GetSession(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                var session = await _chatHistoryService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound("Session not found");
                }

                // Get user's role to determine access permissions
                var userRole = GetCurrentRoleId();

                // Role-based access control
                if (userRole == Shared.Constants.Roles.Patient)
                {
                    // Patients can see their own direct conversations and when doctors chat about them
                    if (session.UserId != userId.Value && session.PatientId != userId.Value)
                    {
                        return StatusCode(403, "Access denied to this session");
                    }
                }
                else if (userRole == Shared.Constants.Roles.Doctor || userRole == Shared.Constants.Roles.Attorney)
                {
                    // Doctors and attorneys can see sessions they created OR sessions in their assigned ServiceRequests
                    bool hasAccess = session.UserId == userId.Value;
                    
                    if (!hasAccess && session.ServiceRequestId.HasValue)
                    {
                        hasAccess = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(
                            session.ServiceRequestId.Value, userId.Value);
                    }
                    
                    if (!hasAccess)
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
                var userRole = GetCurrentRoleId();

                // Role-based access control for deletion
                if (userRole == Shared.Constants.Roles.Patient)
                {
                    // Patients can only delete their own direct conversations
                    // They cannot delete doctor's chats about them (even if they are the PatientId)
                    if (session.UserId != userId.Value)
                    {
                        return StatusCode(403, "Access denied: You can only delete your own conversations, not doctor's chats about you");
                    }
                }
                else if (userRole == 2) // Doctor
                {
                    // Doctors can only delete their own sessions
                    if (session.UserId != userId.Value)
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
                if (!userId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                // Only allow admins to trigger cleanup
                var userRole = GetCurrentRoleId();
                if (userRole != Shared.Constants.Roles.Admin)
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

        [HttpPost("sessions/{sessionId}/generate-summary")]
        public async Task<ActionResult> GenerateSummary(int sessionId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                // Check if user has access to this session
                var session = await _chatHistoryService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound("Session not found");
                }

                // Check access permissions
                var userRole = GetCurrentRoleId();
                if (userRole == Shared.Constants.Roles.Patient && session.UserId != userId.Value && session.PatientId != userId.Value)
                {
                    return Forbid("Access denied to this session");
                }
                else if (userRole == Shared.Constants.Roles.Doctor && session.UserId != userId.Value)
                {
                    return Forbid("Access denied to this session");
                }

                await _chatHistoryService.GenerateSessionSummaryAsync(sessionId);
                return Ok(new { message = "Summary generation initiated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary for session {SessionId}", sessionId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Toggle ignore status for a chat session (doctors only)
        /// </summary>
        [HttpPost("sessions/{sessionId}/toggle-ignore")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult> ToggleIgnoreSession(int sessionId)
        {
            try
            {
                var doctorId = GetCurrentUserId();
                if (!doctorId.HasValue)
                {
                    return Unauthorized("Doctor not authenticated");
                }

                var session = await _chatHistoryService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound("Session not found");
                }

                // Verify doctor has access to this session
                if (session.UserId != doctorId.Value)
                {
                    return Forbid("You can only ignore your own chat sessions");
                }

                await _chatHistoryService.ToggleIgnoreAsync(sessionId, doctorId.Value);
                
                // Reload session to get updated status
                var updatedSession = await _chatHistoryService.GetSessionAsync(sessionId);
                return Ok(new { message = updatedSession?.IsIgnoredByDoctor == true ? "Session ignored" : "Session unignored", isIgnored = updatedSession?.IsIgnoredByDoctor ?? false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling ignore status for session {SessionId}", sessionId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
