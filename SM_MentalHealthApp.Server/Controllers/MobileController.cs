using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using SM_MentalHealthApp.Server.Hubs;
using System.Collections.Concurrent;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
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

        // Rate limiting: track last request time per user
        private static readonly Dictionary<int, DateTime> _lastRequestTime = new();
        private static readonly object _lockObject = new();

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

                // Rate limiting: prevent requests more than once per 5 seconds
                lock (_lockObject)
                {
                    if (_lastRequestTime.TryGetValue(patientId, out var lastTime))
                    {
                        var timeSinceLastRequest = DateTime.UtcNow - lastTime;
                        if (timeSinceLastRequest.TotalSeconds < 5)
                        {
                            // Silently return empty - don't log every blocked request to reduce spam
                            return Ok(new List<User>());
                        }
                    }
                    _lastRequestTime[patientId] = DateTime.UtcNow;
                }

                _logger.LogInformation("Getting doctors and coordinators for patient {PatientId}", patientId);

                // Get all assigners (doctors and coordinators) for this patient
                var assigners = await _context.UserAssignments
                    .Where(ua => ua.AssigneeId == patientId)
                    .Include(ua => ua.Assigner)
                        .ThenInclude(a => a.Role)
                    .Where(ua => ua.Assigner != null && 
                                 ua.Assigner.IsActive && 
                                 (ua.Assigner.RoleId == 2 || ua.Assigner.RoleId == 4)) // Active doctors and coordinators
                    .Select(ua => new
                    {
                        ua.Assigner.Id,
                        ua.Assigner.FirstName,
                        ua.Assigner.LastName,
                        ua.Assigner.Email,
                        ua.Assigner.MobilePhone,
                        ua.Assigner.Specialization,
                        ua.Assigner.RoleId,
                        RoleName = ua.Assigner.Role != null ? ua.Assigner.Role.Name : "Unknown",
                        ua.Assigner.IsActive
                    })
                    .OrderBy(d => d.LastName)
                    .ThenBy(d => d.FirstName)
                    .ToListAsync();

                _logger.LogInformation("Found {AssignerCount} assigners (doctors/coordinators) for patient {PatientId}", assigners.Count, patientId);

                // Create response objects with roleName included
                // ASP.NET Core will serialize these with camelCase by default
                var response = assigners.Select(a => new
                {
                    Id = a.Id,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    Email = a.Email,
                    MobilePhone = a.MobilePhone,
                    Specialization = a.Specialization,
                    RoleId = a.RoleId,
                    RoleName = a.RoleName,
                    IsActive = a.IsActive
                }).ToList();

                return Ok(response);
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

                // Get sender info for message data
                var sender = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == senderId);

                if (sender == null)
                {
                    return BadRequest("Sender not found");
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

                // Notify SignalR clients about the new message
                var messageData = new
                {
                    id = smsMessage.Id.ToString(),
                    senderId = senderId,
                    targetUserId = request.TargetUserId,
                    message = request.Message,
                    senderName = $"{sender.FirstName} {sender.LastName}",
                    timestamp = smsMessage.SentAt.ToString("O")
                };

                // Send to target user if they're connected via SignalR
                // Use the same approach as MobileHub.SendMessage - check UserConnections directly
                if (MobileHub.UserConnections.TryGetValue(request.TargetUserId, out string? targetConnectionId))
                {
                    await _hubContext.Clients.Client(targetConnectionId).SendAsync("new-message", messageData);
                    _logger.LogInformation("SignalR notification sent to connection {ConnectionId} for message {MessageId} from {SenderId} to {ReceiverId}",
                        targetConnectionId, smsMessage.Id, senderId, request.TargetUserId);
                }
                else
                {
                    _logger.LogWarning("Target user {TargetUserId} is not connected via SignalR, message saved but not delivered in real-time",
                        request.TargetUserId);
                }
                
                // Also send confirmation to sender if they're connected
                if (MobileHub.UserConnections.TryGetValue(senderId, out string? senderConnectionId))
                {
                    await _hubContext.Clients.Client(senderConnectionId).SendAsync("message-sent", messageData);
                }

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

        /// <summary>
        /// Send SMS to doctor's phone number
        /// </summary>
        [HttpPost("send-sms")]
        [Authorize]
        public async Task<ActionResult> SendSmsToDoctor([FromBody] SendMobileMessageRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out int senderId))
                {
                    return Unauthorized("Invalid user token");
                }

                _logger.LogInformation("SMS request from {SenderId} to {TargetUserId}: {Message}",
                    senderId, request.TargetUserId, request.Message);

                // Verify target user exists and is a doctor
                var targetUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && u.IsActive);

                if (targetUser == null)
                {
                    return NotFound("Target user not found");
                }

                if (targetUser.RoleId != 2) // RoleId 2 = Doctor
                {
                    return BadRequest("Target user is not a doctor");
                }

                if (string.IsNullOrEmpty(targetUser.MobilePhone))
                {
                    return BadRequest("Doctor does not have a phone number configured");
                }

                // Verify relationship exists (doctor-patient)
                var hasRelationship = await _context.UserAssignments
                    .AnyAsync(ua =>
                        (ua.AssignerId == senderId && ua.AssigneeId == request.TargetUserId) ||
                        (ua.AssignerId == request.TargetUserId && ua.AssigneeId == senderId));

                if (!hasRelationship)
                {
                    return Forbid("No relationship exists with target doctor");
                }

                // Get sender info for SMS message
                var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId);
                if (sender == null)
                {
                    return BadRequest("Sender not found");
                }

                // Format SMS message
                var smsMessage = $"From {sender.FirstName} {sender.LastName} (Patient): {request.Message}";

                // Send SMS via Vonage service
                var smsService = HttpContext.RequestServices.GetRequiredService<ISmsService>();
                var smsSent = await smsService.SendSmsAsync(targetUser.MobilePhone, smsMessage);

                if (smsSent)
                {
                    // Save message to database
                    var smsRecord = new SmsMessage
                    {
                        SenderId = senderId,
                        ReceiverId = request.TargetUserId,
                        Message = request.Message,
                        SentAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _context.SmsMessages.Add(smsRecord);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("SMS sent successfully to doctor {DoctorId} ({DoctorName}) at {PhoneNumber}",
                        targetUser.Id, targetUser.FullName, targetUser.MobilePhone);

                    return Ok(new
                    {
                        success = true,
                        message = "SMS sent successfully",
                        messageId = smsRecord.Id,
                        doctorName = targetUser.FullName,
                        phoneNumber = targetUser.MobilePhone
                    });
                }
                else
                {
                    _logger.LogError("Failed to send SMS to doctor {DoctorId} ({DoctorName}) at {PhoneNumber}",
                        targetUser.Id, targetUser.FullName, targetUser.MobilePhone);

                    return StatusCode(500, "Failed to send SMS");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS");
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
