using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAppointmentReminderService
    {
        Task SendRemindersAsync();
    }

    public class AppointmentReminderService : BackgroundService, IAppointmentReminderService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppointmentReminderService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

        public AppointmentReminderService(IServiceProvider serviceProvider, ILogger<AppointmentReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment Reminder Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Appointment Reminder Service");
                }

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Appointment Reminder Service stopped");
        }

        public async Task SendRemindersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<JournalDbContext>();
            var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

            var now = DateTime.UtcNow;
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            // Find appointments that need day-before reminders (tomorrow)
            var dayBeforeAppointments = await context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.IsActive
                    && a.Status != AppointmentStatus.Cancelled
                    && a.Status != AppointmentStatus.NoShow
                    && a.Status != AppointmentStatus.Completed
                    && a.AppointmentDateTime.Date == tomorrow
                    && !a.DayBeforeReminderSent
                    && a.AppointmentDateTime > now)
                .ToListAsync();

            // Find appointments that need day-of reminders (today)
            var dayOfAppointments = await context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.IsActive
                    && a.Status != AppointmentStatus.Cancelled
                    && a.Status != AppointmentStatus.NoShow
                    && a.Status != AppointmentStatus.Completed
                    && a.AppointmentDateTime.Date == today
                    && !a.DayOfReminderSent
                    && a.AppointmentDateTime > now)
                .ToListAsync();

            _logger.LogInformation("Found {DayBeforeCount} appointments needing day-before reminders and {DayOfCount} needing day-of reminders",
                dayBeforeAppointments.Count, dayOfAppointments.Count);

            // Send day-before reminders
            foreach (var appointment in dayBeforeAppointments)
            {
                await SendDayBeforeReminderAsync(appointment, smsService, context);
            }

            // Send day-of reminders
            foreach (var appointment in dayOfAppointments)
            {
                await SendDayOfReminderAsync(appointment, smsService, context);
            }
        }

        private async Task SendDayBeforeReminderAsync(Appointment appointment, ISmsService smsService, JournalDbContext context)
        {
            try
            {
                if (appointment.Patient == null || string.IsNullOrEmpty(appointment.Patient.MobilePhone))
                {
                    _logger.LogWarning("Patient {PatientId} does not have a mobile phone. Skipping reminder for appointment {AppointmentId}",
                        appointment.PatientId, appointment.Id);
                    // Mark as sent to avoid retrying
                    appointment.DayBeforeReminderSent = true;
                    await context.SaveChangesAsync();
                    return;
                }

                var doctor = appointment.Doctor;
                var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "your doctor";

                var message = $"📅 Appointment Reminder\n\n" +
                    $"You have an appointment tomorrow with Dr. {doctorName} on {appointment.AppointmentDateTime:MM/dd/yyyy} at {appointment.AppointmentDateTime:hh:mm tt}.\n\n" +
                    $"Duration: {(int)appointment.Duration.TotalMinutes} minutes\n";

                if (!string.IsNullOrEmpty(appointment.Reason))
                {
                    message += $"Reason: {appointment.Reason}\n\n";
                }

                message += "Please arrive on time. We look forward to seeing you!\n\n" +
                    "Reply STOP to opt out of appointment reminders.";

                var success = await smsService.SendSmsAsync(appointment.Patient.MobilePhone, message);

                if (success)
                {
                    appointment.DayBeforeReminderSent = true;
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Day-before reminder sent for appointment {AppointmentId} to patient {PatientId} ({PhoneNumber})",
                        appointment.Id, appointment.PatientId, appointment.Patient.MobilePhone);
                }
                else
                {
                    _logger.LogWarning("Failed to send day-before reminder for appointment {AppointmentId} to patient {PatientId} ({PhoneNumber})",
                        appointment.Id, appointment.PatientId, appointment.Patient.MobilePhone);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending day-before reminder for appointment {AppointmentId}", appointment.Id);
            }
        }

        private async Task SendDayOfReminderAsync(Appointment appointment, ISmsService smsService, JournalDbContext context)
        {
            try
            {
                if (appointment.Patient == null || string.IsNullOrEmpty(appointment.Patient.MobilePhone))
                {
                    _logger.LogWarning("Patient {PatientId} does not have a mobile phone. Skipping reminder for appointment {AppointmentId}",
                        appointment.PatientId, appointment.Id);
                    // Mark as sent to avoid retrying
                    appointment.DayOfReminderSent = true;
                    await context.SaveChangesAsync();
                    return;
                }

                var doctor = appointment.Doctor;
                var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "your doctor";
                var timeUntilAppointment = appointment.AppointmentDateTime - DateTime.UtcNow;
                var hoursUntil = (int)timeUntilAppointment.TotalHours;
                var minutesUntil = (int)timeUntilAppointment.TotalMinutes % 60;

                var timeText = hoursUntil > 0
                    ? $"{hoursUntil} hour{(hoursUntil > 1 ? "s" : "")} and {minutesUntil} minute{(minutesUntil != 1 ? "s" : "")}"
                    : $"{minutesUntil} minute{(minutesUntil != 1 ? "s" : "")}";

                var message = $"📅 Appointment Today\n\n" +
                    $"You have an appointment with Dr. {doctorName} today at {appointment.AppointmentDateTime:hh:mm tt} ({timeText} from now).\n\n" +
                    $"Duration: {(int)appointment.Duration.TotalMinutes} minutes\n";

                if (!string.IsNullOrEmpty(appointment.Reason))
                {
                    message += $"Reason: {appointment.Reason}\n\n";
                }

                message += "Please arrive on time. See you soon!\n\n" +
                    "Reply STOP to opt out of appointment reminders.";

                var success = await smsService.SendSmsAsync(appointment.Patient.MobilePhone, message);

                if (success)
                {
                    appointment.DayOfReminderSent = true;
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Day-of reminder sent for appointment {AppointmentId} to patient {PatientId} ({PhoneNumber})",
                        appointment.Id, appointment.PatientId, appointment.Patient.MobilePhone);
                }
                else
                {
                    _logger.LogWarning("Failed to send day-of reminder for appointment {AppointmentId} to patient {PatientId} ({PhoneNumber})",
                        appointment.Id, appointment.PatientId, appointment.Patient.MobilePhone);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending day-of reminder for appointment {AppointmentId}", appointment.Id);
            }
        }
    }
}
