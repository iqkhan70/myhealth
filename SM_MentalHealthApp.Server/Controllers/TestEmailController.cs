using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestEmailController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly JournalDbContext _context;
        private readonly ILogger<TestEmailController> _logger;

        public TestEmailController(
            INotificationService notificationService,
            JournalDbContext context,
            ILogger<TestEmailController> logger)
        {
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("send-test-email")]
        [AllowAnonymous]
        public async Task<ActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                // Find user by email or use current user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (user == null)
                {
                    return NotFound(new { message = $"User with email {request.Email} not found." });
                }

                var subject = request.Subject ?? "Test Email from Health App";
                var body = request.Body ?? $@"
<html>
<body>
    <h2>Test Email</h2>
    <p>This is a test email from the Health App server.</p>
    <p>If you received this, the email service is working correctly!</p>
    <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}</p>
</body>
</html>";

                await _notificationService.SendEmailNotification(user.Id, subject, body);

                return Ok(new 
                { 
                    message = "Test email sent successfully", 
                    recipient = user.Email,
                    userId = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email to {Email}", request.Email);
                return StatusCode(500, new 
                { 
                    message = "Failed to send test email", 
                    error = ex.Message,
                    details = ex.ToString()
                });
            }
        }

        [HttpGet("test-config")]
        [AllowAnonymous]
        public ActionResult TestEmailConfig()
        {
            try
            {
                var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                
                var emailEnabled = config.GetValue<bool>("Email:Enabled", false);
                var smtpHost = config["Email:SmtpHost"];
                var smtpPort = config.GetValue<int>("Email:SmtpPort", 587);
                var smtpUsername = config["Email:SmtpUsername"];
                var smtpFromEmail = config["Email:FromEmail"];
                var smtpFromName = config["Email:FromName"];
                var enableSsl = config.GetValue<bool>("Email:EnableSsl", true);
                
                var hasPassword = !string.IsNullOrEmpty(config["Email:SmtpPassword"]);

                return Ok(new
                {
                    emailEnabled,
                    smtpHost,
                    smtpPort,
                    smtpUsername,
                    smtpFromEmail,
                    smtpFromName,
                    enableSsl,
                    hasPassword,
                    passwordConfigured = hasPassword,
                    configurationComplete = emailEnabled && 
                                           !string.IsNullOrEmpty(smtpHost) && 
                                           !string.IsNullOrEmpty(smtpUsername) && 
                                           hasPassword
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error reading email configuration", error = ex.Message });
            }
        }
    }

    public class TestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}

