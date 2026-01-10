using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Models;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Shared;
using System.Collections.Concurrent;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SM_MentalHealthApp.Server.Services
{
    public class NotificationService : INotificationService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISmsService _smsService;
        private readonly IConfiguration _configuration;
        private readonly IPiiEncryptionService _encryptionService;
        private readonly ConcurrentDictionary<int, List<SM_MentalHealthApp.Shared.EmergencyAlert>> _pendingAlerts = new();

        public NotificationService(JournalDbContext context, ILogger<NotificationService> logger, ISmsService smsService, IConfiguration configuration, IPiiEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _smsService = smsService;
            _configuration = configuration;
            _encryptionService = encryptionService;
        }

        public async Task SendEmergencyAlert(int doctorId, SM_MentalHealthApp.Shared.EmergencyAlert alert)
        {
            try
            {
                // Store alert for real-time delivery
                if (!_pendingAlerts.ContainsKey(doctorId))
                {
                    _pendingAlerts[doctorId] = new List<SM_MentalHealthApp.Shared.EmergencyAlert>();
                }
                _pendingAlerts[doctorId].Add(alert);

                // Send SMS notification to doctor
                await SendSmsNotificationToDoctor(doctorId, alert);

                // Log the alert
                _logger.LogInformation("Emergency alert queued for doctor {DoctorId}: {AlertType}",
                    doctorId, alert.EmergencyType);

                // In a real implementation, this would trigger WebSocket notifications
                // For now, we'll just log it
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending emergency alert to doctor {DoctorId}", doctorId);
            }
        }

        public async Task SendPushNotification(int userId, string title, string message)
        {
            try
            {
                // Get user's push notification tokens
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return;

                _logger.LogInformation("Push notification sent to user {UserId}: {Title}", userId, title);

                // In a real implementation, this would send actual push notifications
                // using services like Firebase Cloud Messaging (FCM) or Apple Push Notification Service (APNS)
                // For now, we'll just log it
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
            }
        }

        public async Task SendEmailNotification(int userId, string subject, string body)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for email notification", userId);
                    return;
                }

                if (string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogWarning("User {UserId} has no email address", userId);
                    return;
                }

                // Get email configuration - support Mailgun API and SMTP (fallback)
                var emailProvider = _configuration["Email:Provider"] ?? "Mailgun"; // "Mailgun" or "SMTP"
                var emailEnabled = _configuration.GetValue<bool>("Email:Enabled", false);
                var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@healthapp.com";
                var fromName = _configuration["Email:FromName"] ?? "Health App";

                if (!emailEnabled)
                {
                    _logger.LogInformation("Email service is disabled. Email notification for user {UserId} ({Email}) would have been sent with subject: {Subject}",
                        userId, user.Email, subject);
                    _logger.LogInformation("Email body: {Body}", body);
                    return;
                }

                // Use Mailgun API (recommended - no port issues)
                if (emailProvider.Equals("Mailgun", StringComparison.OrdinalIgnoreCase))
                {
                    var mailgunApiKey = _configuration["Email:MailgunApiKey"];
                    var mailgunDomain = _configuration["Email:MailgunDomain"];

                    // Check if API key is missing or is a placeholder
                    if (string.IsNullOrEmpty(mailgunApiKey) ||
                        mailgunApiKey.Equals("USE_ENV_VARIABLE", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrEmpty(mailgunDomain))
                    {
                        _logger.LogWarning("Mailgun API key or domain is not configured. Falling back to SMTP. " +
                            "To use Mailgun, set Email:MailgunApiKey and Email:MailgunDomain in config or environment variables.");

                        // Fallback to SMTP if Mailgun is not properly configured
                        await SendEmailViaSmtp(userId, user.Email, user.FirstName, user.LastName, subject, body, fromEmail, fromName);
                        return;
                    }

                    try
                    {
                        // For Mailgun, the From email must use the Mailgun domain
                        // Extract the local part (before @) from fromEmail, or use "noreply"
                        string mailgunFromEmail;
                        if (fromEmail.Contains("@"))
                        {
                            var localPart = fromEmail.Split('@')[0];
                            mailgunFromEmail = $"{localPart}@{mailgunDomain}";
                        }
                        else
                        {
                            mailgunFromEmail = $"noreply@{mailgunDomain}";
                        }

                        _logger.LogInformation("Using Mailgun From address: {FromEmail} (original: {OriginalFromEmail})",
                            mailgunFromEmail, fromEmail);

                        await SendEmailViaMailgun(userId, user.Email, user.FirstName, user.LastName, subject, body, mailgunFromEmail, fromName, mailgunApiKey, mailgunDomain);
                    }
                    catch (Exception mailgunEx)
                    {
                        _logger.LogWarning(mailgunEx, "Mailgun error: {Message}. Falling back to SMTP.", mailgunEx.Message);
                        // Fallback to SMTP on exceptions
                        await SendEmailViaSmtp(userId, user.Email, user.FirstName, user.LastName, subject, body, fromEmail, fromName);
                    }
                }
                else
                {
                    // Use SMTP directly
                    await SendEmailViaSmtp(userId, user.Email, user.FirstName, user.LastName, subject, body, fromEmail, fromName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification to user {UserId}: {Message}",
                    userId, ex.Message);
                throw; // Re-throw to allow caller to handle
            }
        }

        private async Task SendEmailViaMailgun(int userId, string toEmail, string firstName, string lastName,
            string subject, string body, string fromEmail, string fromName, string apiKey, string domain)
        {
            try
            {
                // Mailgun API endpoint
                var apiUrl = $"https://api.mailgun.net/v3/{domain}/messages";

                // Create HTTP client with basic auth
                using var httpClient = new HttpClient();
                var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{apiKey}"));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

                // Check if body is HTML
                var isHtml = body.Contains("<html>") || body.Contains("<body>") || body.Contains("<p>");

                // Prepare form data
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("from", $"{fromName} <{fromEmail}>"),
                    new KeyValuePair<string, string>("to", $"{firstName} {lastName} <{toEmail}>"),
                    new KeyValuePair<string, string>("subject", subject)
                };

                if (isHtml)
                {
                    formData.Add(new KeyValuePair<string, string>("html", body));
                }
                else
                {
                    formData.Add(new KeyValuePair<string, string>("text", body));
                }

                var content = new FormUrlEncodedContent(formData);

                _logger.LogInformation("Attempting to send email via Mailgun: Domain={Domain}, To={To}",
                    domain, toEmail);

                var response = await httpClient.PostAsync(apiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully via Mailgun to {Email} (user {UserId}): {Subject}",
                        toEmail, userId, subject);
                }
                else
                {
                    _logger.LogError("Mailgun API error: Status={Status}, Body={Body}",
                        response.StatusCode, responseBody);
                    throw new Exception($"Mailgun API error: {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email via Mailgun to user {UserId}: {Message}",
                    userId, ex.Message);
                throw;
            }
        }

        private async Task SendEmailViaSmtp(int userId, string toEmail, string firstName, string lastName,
            string subject, string body, string fromEmail, string fromName)
        {
            // Get SMTP configuration
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var smtpEnableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("SMTP configuration is incomplete. Email notification for user {UserId} ({Email}) not sent. Subject: {Subject}",
                    userId, toEmail, subject);
                return;
            }

            // Create mail message
            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail, fromName);
            mailMessage.To.Add(new MailAddress(toEmail, $"{firstName} {lastName}"));
            mailMessage.Subject = subject;

            // Check if body is HTML
            if (body.Contains("<html>") || body.Contains("<body>") || body.Contains("<p>"))
            {
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;
            }
            else
            {
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = false;
            }

            // Create SMTP client
            using var smtpClient = new SmtpClient(smtpHost, smtpPort);
            smtpClient.EnableSsl = smtpEnableSsl;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.Timeout = 30000;

            _logger.LogInformation("Attempting to send email via SMTP: Host={Host}, Port={Port}, Username={Username}",
                smtpHost, smtpPort, smtpUsername);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully via SMTP to {Email} (user {UserId}): {Subject}",
                toEmail, userId, subject);
        }

        public async Task SendSmsNotification(int userId, string message)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return;

                _logger.LogInformation("SMS notification sent to user {UserId}: {Message}", userId, message);

                // In a real implementation, this would send actual SMS
                // using services like Twilio, AWS SNS, or similar
                // For now, we'll just log it
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS notification to user {UserId}", userId);
            }
        }

        // Method to get pending alerts for a doctor (for real-time updates)
        public List<SM_MentalHealthApp.Shared.EmergencyAlert> GetPendingAlerts(int doctorId)
        {
            if (_pendingAlerts.TryGetValue(doctorId, out var alerts))
            {
                var result = alerts.ToList();
                alerts.Clear(); // Remove after retrieving
                return result;
            }
            return new List<SM_MentalHealthApp.Shared.EmergencyAlert>();
        }

        private async Task SendSmsNotificationToDoctor(int doctorId, SM_MentalHealthApp.Shared.EmergencyAlert alert)
        {
            try
            {
                // Get doctor's phone number from database
                var doctor = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == doctorId && u.RoleId == 2); // RoleId 2 = Doctor

                if (doctor == null)
                {
                    _logger.LogWarning("Doctor {DoctorId} not found for SMS notification", doctorId);
                    return;
                }

                // Decrypt PII data (including MobilePhone) before using for SMS
                UserEncryptionHelper.DecryptUserData(doctor, _encryptionService);

                if (string.IsNullOrEmpty(doctor.MobilePhone))
                {
                    _logger.LogWarning("Doctor {DoctorId} ({DoctorName}) has no mobile phone number configured",
                        doctorId, doctor.FullName);
                    return;
                }

                // Send SMS via Vonage
                var smsSent = await _smsService.SendEmergencyAlertAsync(doctor.MobilePhone, alert);

                if (smsSent)
                {
                    _logger.LogInformation("Emergency SMS sent to doctor {DoctorId} ({DoctorName}) at {PhoneNumber}",
                        doctorId, doctor.FullName, doctor.MobilePhone);
                }
                else
                {
                    _logger.LogError("Failed to send emergency SMS to doctor {DoctorId} ({DoctorName}) at {PhoneNumber}",
                        doctorId, doctor.FullName, doctor.MobilePhone);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS notification to doctor {DoctorId}", doctorId);
            }
        }
    }
}
