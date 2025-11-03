using System.Text.Json.Serialization;

namespace SM_MentalHealthApp.Shared
{
    public enum AppointmentType
    {
        Regular = 1,
        UrgentCare = 2
    }

    public enum AppointmentStatus
    {
        Scheduled = 1,
        Confirmed = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5,
        NoShow = 6
    }

    public class Appointment
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30); // Default 30 minutes
        public AppointmentType AppointmentType { get; set; } = AppointmentType.Regular;
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; } // Admin who created the appointment
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [JsonIgnore]
        public User Doctor { get; set; } = null!;
        [JsonIgnore]
        public User Patient { get; set; } = null!;
        [JsonIgnore]
        public User CreatedByUser { get; set; } = null!;

        // Computed properties
        public DateTime EndDateTime => AppointmentDateTime.Add(Duration);
        public bool IsUrgentCare => AppointmentType == AppointmentType.UrgentCare;
        public bool IsBusinessHours { get; set; } // Will be calculated based on business hours
    }

    public class DoctorAvailability
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public DateTime Date { get; set; }
        public bool IsOutOfOffice { get; set; } = false;
        public string? Reason { get; set; } // Reason for OOO
        public TimeSpan? StartTime { get; set; } // Available start time (nullable for full day OOO)
        public TimeSpan? EndTime { get; set; } // Available end time (nullable for full day OOO)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [JsonIgnore]
        public User Doctor { get; set; } = null!;
    }

    // Business hours configuration (can be stored in appsettings or database)
    public class BusinessHours
    {
        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0); // 9:00 AM
        public TimeSpan EndTime { get; set; } = new TimeSpan(17, 0, 0); // 5:00 PM
        public List<DayOfWeek> WorkingDays { get; set; } = new()
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };
    }

    // DTOs for API requests/responses
    public class CreateAppointmentRequest
    {
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);
        public AppointmentType AppointmentType { get; set; } = AppointmentType.Regular;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateAppointmentRequest
    {
        public int Id { get; set; }
        public DateTime? AppointmentDateTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public AppointmentType? AppointmentType { get; set; }
        public AppointmentStatus? Status { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }

    public class AppointmentDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorEmail { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public DateTime AppointmentDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public TimeSpan Duration { get; set; }
        public AppointmentType AppointmentType { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public bool IsUrgentCare { get; set; }
        public bool IsBusinessHours { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class DoctorAvailabilityRequest
    {
        public int DoctorId { get; set; }
        public DateTime Date { get; set; }
        public bool IsOutOfOffice { get; set; }
        public string? Reason { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }

    public class DoctorAvailabilityDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool IsOutOfOffice { get; set; }
        public string? Reason { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }

    public class AppointmentValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class AppointmentConflictCheck
    {
        public bool HasConflict { get; set; }
        public List<AppointmentDto> ConflictingAppointments { get; set; } = new();
        public string? ConflictMessage { get; set; }
    }
}
