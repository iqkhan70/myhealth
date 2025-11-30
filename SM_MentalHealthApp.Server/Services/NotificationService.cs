using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Models;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Shared;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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

                // Get SMTP configuration
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
                var smtpUsername = _configuration["Email:SmtpUsername"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                var smtpFromEmail = _configuration["Email:FromEmail"] ?? "noreply@healthapp.com";
                var smtpFromName = _configuration["Email:FromName"] ?? "Health App";
                var smtpEnableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);
                var emailEnabled = _configuration.GetValue<bool>("Email:Enabled", false);

                if (!emailEnabled)
                {
                    _logger.LogInformation("Email service is disabled. Email notification for user {UserId} ({Email}) would have been sent with subject: {Subject}",
                        userId, user.Email, subject);
                    _logger.LogInformation("Email body: {Body}", body);
                    return;
                }

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP configuration is incomplete. Email notification for user {UserId} ({Email}) not sent. Subject: {Subject}",
                        userId, user.Email, subject);
                    _logger.LogInformation("Email body: {Body}", body);
                    return;
                }

                // Create mail message
                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpFromEmail, smtpFromName);
                mailMessage.To.Add(new MailAddress(user.Email, $"{user.FirstName} {user.LastName}"));
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
                
                // For Gmail, configure TLS properly
                if (smtpHost.Contains("gmail.com"))
                {
                    // Gmail requires TLS 1.2 or higher
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                    ServicePointManager.ServerCertificateValidationCallback = 
                        delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
                        { return true; };
                    
                    smtpClient.EnableSsl = true;
                    smtpClient.UseDefaultCredentials = false;
                    
                    // Gmail requires the username to be the full email address
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Timeout = 30000; // 30 second timeout
                }
                else
                {
                    smtpClient.EnableSsl = smtpEnableSsl;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.Timeout = 30000;
                }

                // Log configuration (without password)
                _logger.LogInformation("Attempting to send email via SMTP: Host={Host}, Port={Port}, Username={Username}, SSL={Ssl}", 
                    smtpHost, smtpPort, smtpUsername, smtpClient.EnableSsl);

                // Send email
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email sent successfully to {Email} (user {UserId}): {Subject}",
                    user.Email, userId, subject);
            }
            catch (SmtpException smtpEx)
            {
                var errorMessage = smtpEx.Message;
                if (errorMessage.Contains("Authentication Required") || errorMessage.Contains("5.7.0"))
                {
                    _logger.LogError(smtpEx, 
                        "Gmail authentication failed for user {UserId}. This usually means you need to use a Gmail App Password instead of your regular password. " +
                        "Go to https://myaccount.google.com/apppasswords to generate one. Error: {Message}", 
                        userId, smtpEx.Message);
                }
                else
                {
                    _logger.LogError(smtpEx, "SMTP error sending email notification to user {UserId}: {Message}", 
                        userId, smtpEx.Message);
                }
                throw; // Re-throw to allow caller to handle
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification to user {UserId}: {Message}", 
                    userId, ex.Message);
                throw; // Re-throw to allow caller to handle
            }
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
