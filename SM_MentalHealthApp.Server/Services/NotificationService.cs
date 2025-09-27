using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Models;
using SM_MentalHealthApp.Shared;
using System.Collections.Concurrent;

namespace SM_MentalHealthApp.Server.Services
{
    public class NotificationService : INotificationService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly ConcurrentDictionary<int, List<SM_MentalHealthApp.Shared.EmergencyAlert>> _pendingAlerts = new();

        public NotificationService(JournalDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
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

                // Log the alert
                _logger.LogInformation("Emergency alert queued for doctor {DoctorId}: {AlertType}",
                    doctorId, alert.EmergencyType);

                // In a real implementation, this would trigger WebSocket notifications
                // For now, we'll just log it
                await Task.CompletedTask;
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
                if (user == null) return;

                _logger.LogInformation("Email notification sent to user {UserId}: {Subject}", userId, subject);

                // In a real implementation, this would send actual emails
                // using services like SendGrid, AWS SES, or SMTP
                // For now, we'll just log it
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification to user {UserId}", userId);
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
    }
}
