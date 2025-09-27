using SM_MentalHealthApp.Server.Models;

namespace SM_MentalHealthApp.Server.Services
{
    public interface INotificationService
    {
        Task SendEmergencyAlert(int doctorId, SM_MentalHealthApp.Shared.EmergencyAlert alert);
        Task SendPushNotification(int userId, string title, string message);
        Task SendEmailNotification(int userId, string subject, string body);
        Task SendSmsNotification(int userId, string message);
    }
}
