using Radzen;

namespace SM_MentalHealthApp.Client.Helpers
{
    public static class NotificationHelper
    {
        public static void ShowError(this NotificationService notificationService, string message, int duration = 6000)
        {
            notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = message,
                Duration = duration
            });
        }

        public static void ShowSuccess(this NotificationService notificationService, string message, int duration = 4000)
        {
            notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Success",
                Detail = message,
                Duration = duration
            });
        }

        public static void ShowWarning(this NotificationService notificationService, string message, int duration = 5000)
        {
            notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Warning",
                Detail = message,
                Duration = duration
            });
        }

        public static void ShowInfo(this NotificationService notificationService, string message, int duration = 4000)
        {
            notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info",
                Detail = message,
                Duration = duration
            });
        }

        public static void ShowError(this NotificationService notificationService, string summary, string detail, int duration = 6000)
        {
            notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = summary,
                Detail = detail,
                Duration = duration
            });
        }

        public static void ShowSuccess(this NotificationService notificationService, string summary, string detail, int duration = 4000)
        {
            notificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = summary,
                Detail = detail,
                Duration = duration
            });
        }
    }
}

