using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using SM_MentalHealthApp.Server.Hubs;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MobileController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<MobileController> _logger;
        private readonly IHubContext<MobileHub> _hubContext;

        public MobileController(JournalDbContext context, ILogger<MobileController> logger, IHubContext<MobileHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
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
        public async Task<ActionResult> SendChatMessage([FromBody] SendMobileMessageRequest request)
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
        public async Task<ActionResult> InitiateCall([FromBody] MobileInitiateCallRequest request)
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
                    return BadRequest("No relationship exists with target user");
                }

                // Get caller info for the notification
                var caller = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == callerId);

                if (caller == null)
                {
                    return BadRequest("Caller not found");
                }

                // Create call invitation data
                var callId = Guid.NewGuid().ToString();
                var callData = new
                {
                    CallId = callId,
                    CallerId = callerId,
                    CallerName = $"{caller.FirstName} {caller.LastName}",
                    CallerRole = caller.RoleId == 2 ? "Doctor" : "Patient",
                    CallType = request.CallType,
                    Timestamp = DateTime.UtcNow.ToString("O")
                };

                // Send call invitation via SignalR to all connected clients
                // The clients will filter based on their user ID
                var callDataWithTarget = new
                {
                    CallId = callId,
                    CallerId = callerId,
                    CallerName = $"{caller.FirstName} {caller.LastName}",
                    CallerRole = caller.RoleId == 2 ? "Doctor" : "Patient",
                    CallType = request.CallType,
                    TargetUserId = request.TargetUserId, // Add target user ID for client-side filtering
                    Timestamp = DateTime.UtcNow.ToString("O")
                };

                await _hubContext.Clients.All.SendAsync("incoming-call", callDataWithTarget);

                _logger.LogInformation("Call invitation sent via SignalR from {CallerId} to {TargetUserId}", callerId, request.TargetUserId);

                return Ok(new { success = true, message = "Call initiated successfully", callId = callId });
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

        [HttpPost("send-message")]
        [Authorize]
        public async Task<ActionResult> SendMessage([FromBody] SendMobileMessageRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int senderId))
                {
                    return Unauthorized("Invalid user token");
                }

                _logger.LogInformation("Sending message from {SenderId} to {TargetUserId}", senderId, request.TargetUserId);

                // Verify target user exists and relationship exists
                var hasRelationship = await _context.UserAssignments
                    .AnyAsync(ua =>
                        (ua.AssignerId == senderId && ua.AssigneeId == request.TargetUserId) ||
                        (ua.AssignerId == request.TargetUserId && ua.AssigneeId == senderId));

                if (!hasRelationship)
                {
                    return Forbid("No relationship exists with target user");
                }

                // Save message to database
                var smsMessage = new SmsMessage
                {
                    SenderId = senderId,
                    ReceiverId = request.TargetUserId,
                    Message = request.Message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.SmsMessages.Add(smsMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Message saved: '{Message}' from {SenderId} to {ReceiverId}",
                    request.Message, senderId, request.TargetUserId);

                return Ok(new { message = "Message sent successfully", messageId = smsMessage.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("messages/{targetUserId}")]
        [Authorize]
        public async Task<ActionResult> GetMessages(int targetUserId, [FromQuery] int limit = 50)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                // Get messages between the two users
                var messages = await _context.SmsMessages
                    .Where(m =>
                        (m.SenderId == userId && m.ReceiverId == targetUserId) ||
                        (m.SenderId == targetUserId && m.ReceiverId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .Take(limit)
                    .Select(m => new
                    {
                        id = m.Id,
                        senderId = m.SenderId,
                        receiverId = m.ReceiverId,
                        message = m.Message,
                        sentAt = m.SentAt,
                        isRead = m.IsRead,
                        isMe = m.SenderId == userId
                    })
                    .ToListAsync();

                return Ok(messages.OrderBy(m => m.sentAt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // Request DTOs
    public class MobileInitiateCallRequest
    {
        public int TargetUserId { get; set; }
        public string CallType { get; set; } = string.Empty; // "Video" or "Audio"
    }

    public class SendMobileMessageRequest
    {
        public int TargetUserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

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
