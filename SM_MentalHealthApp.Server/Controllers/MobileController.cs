using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MobileController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<MobileController> _logger;

        public MobileController(JournalDbContext context, ILogger<MobileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get assigned patients for a doctor (mobile app)
        /// </summary>
        [HttpGet("doctor/patients")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult<List<User>>> GetDoctorPatients()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int doctorId))
                {
                    return Unauthorized("Invalid user token");
                }

                _logger.LogInformation("Getting patients for doctor {DoctorId}", doctorId);

                var patients = await _context.UserAssignments
                    .Where(ua => ua.AssignerId == doctorId)
                    .Include(ua => ua.Assignee)
                    .Select(ua => ua.Assignee)
                    .Where(p => p.IsActive && p.RoleId == 1) // Active patients only
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToListAsync();

                _logger.LogInformation("Found {PatientCount} patients for doctor {DoctorId}", patients.Count, doctorId);

                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patients for doctor");
                return StatusCode(500, "Error retrieving patients");
            }
        }

        /// <summary>
        /// Get assigned doctors for a patient (mobile app)
        /// </summary>
        [HttpGet("patient/doctors")]
        [Authorize(Roles = "Patient")]
        public async Task<ActionResult<List<User>>> GetPatientDoctors()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int patientId))
                {
                    return Unauthorized("Invalid user token");
                }

                _logger.LogInformation("Getting doctors for patient {PatientId}", patientId);

                var doctors = await _context.UserAssignments
                    .Where(ua => ua.AssigneeId == patientId)
                    .Include(ua => ua.Assigner)
                    .Select(ua => ua.Assigner)
                    .Where(d => d.IsActive && d.RoleId == 2) // Active doctors only
                    .OrderBy(d => d.LastName)
                    .ThenBy(d => d.FirstName)
                    .ToListAsync();

                _logger.LogInformation("Found {DoctorCount} doctors for patient {PatientId}", doctors.Count, patientId);

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctors for patient");
                return StatusCode(500, "Error retrieving doctors");
            }
        }

        /// <summary>
        /// Get chat history between two users
        /// </summary>
        [HttpGet("chat/{targetUserId}")]
        public async Task<ActionResult<List<ChatMessage>>> GetChatHistory(int targetUserId, [FromQuery] int limit = 50)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int currentUserId))
                {
                    return Unauthorized("Invalid user token");
                }

                _logger.LogInformation("Getting chat history between {CurrentUserId} and {TargetUserId}", currentUserId, targetUserId);

                // TODO: Implement chat message storage and retrieval
                // For now, return empty list as chat messages are handled in real-time via WebSocket
                var messages = new List<ChatMessage>();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat history between {CurrentUserId} and {TargetUserId}", User.FindFirst("userId")?.Value, targetUserId);
                return StatusCode(500, "Error retrieving chat history");
            }
        }

        /// <summary>
        /// Send a chat message
        /// </summary>
        [HttpPost("chat/send")]
        public async Task<ActionResult> SendChatMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int senderId))
                {
                    return Unauthorized("Invalid user token");
                }

                _logger.LogInformation("Sending message from {SenderId} to {TargetUserId}", senderId, request.TargetUserId);

                // Verify target user exists and is accessible
                var targetUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && u.IsActive);

                if (targetUser == null)
                {
                    return NotFound("Target user not found");
                }

                // Verify relationship exists (doctor-patient)
                var hasRelationship = await _context.UserAssignments
                    .AnyAsync(ua =>
                        (ua.AssignerId == senderId && ua.AssigneeId == request.TargetUserId) ||
                        (ua.AssignerId == request.TargetUserId && ua.AssigneeId == senderId));

                if (!hasRelationship)
                {
                    return Forbid("No relationship exists with target user");
                }

                // TODO: Store message in database
                // TODO: Send message via WebSocket to target user

                return Ok(new { success = true, message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending chat message");
                return StatusCode(500, "Error sending message");
            }
        }

        /// <summary>
        /// Initiate a call (video/audio)
        /// </summary>
        [HttpPost("call/initiate")]
        public async Task<ActionResult> InitiateCall([FromBody] InitiateCallRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int callerId))
                {
                    return Unauthorized("Invalid user token");
                }

                _logger.LogInformation("Initiating {CallType} call from {CallerId} to {TargetUserId}", request.CallType, callerId, request.TargetUserId);

                // Verify target user exists and is accessible
                var targetUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && u.IsActive);

                if (targetUser == null)
                {
                    return NotFound("Target user not found");
                }

                // Verify relationship exists (doctor-patient)
                var hasRelationship = await _context.UserAssignments
                    .AnyAsync(ua =>
                        (ua.AssignerId == callerId && ua.AssigneeId == request.TargetUserId) ||
                        (ua.AssignerId == request.TargetUserId && ua.AssigneeId == callerId));

                if (!hasRelationship)
                {
                    return Forbid("No relationship exists with target user");
                }

                // TODO: Send call invitation via WebSocket to target user
                // TODO: Store call record in database

                return Ok(new { success = true, message = "Call initiated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                return StatusCode(500, "Error initiating call");
            }
        }

        /// <summary>
        /// Get user profile for mobile app
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<User>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Remove sensitive information
                user.PasswordHash = null;

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, "Error retrieving profile");
            }
        }
    }

    // Request DTOs
    public class SendMessageRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int TargetUserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class InitiateCallRequest
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int TargetUserId { get; set; }
        public string CallType { get; set; } = string.Empty; // "video" or "audio"
    }

    // Response DTOs
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int TargetUserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string SenderName { get; set; } = string.Empty;
    }
}
