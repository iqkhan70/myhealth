using System.ComponentModel.DataAnnotations;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Models
{
    // Emergency message received from device
    public class EmergencyMessage
    {
        [Required]
        public string DeviceToken { get; set; } = string.Empty;

        [Required]
        public string EmergencyType { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }

        public string? Message { get; set; }

        [Required]
        public string Severity { get; set; } = string.Empty;

        public VitalSigns? VitalSigns { get; set; }

        public LocationData? Location { get; set; }

        public string? DeviceId { get; set; }

        public string? Signature { get; set; } // For message integrity verification
    }

    // Device registration request
    public class DeviceRegistrationRequest
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public string DeviceName { get; set; } = string.Empty;

        [Required]
        public string DeviceType { get; set; } = string.Empty; // "smartwatch", "phone", etc.

        public string? DeviceModel { get; set; }

        public string? OperatingSystem { get; set; }
    }

    // Device registration response
    public class DeviceRegistrationResponse
    {
        public bool Success { get; set; }
        public string? DeviceToken { get; set; }
        public string? Message { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    // Emergency response sent to doctor
    public class EmergencyAlert
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string EmergencyType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public VitalSigns? VitalSigns { get; set; }
        public LocationData? Location { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public int? AcknowledgedByDoctorId { get; set; }
    }

    // Vital signs data
    public class VitalSigns
    {
        public int? HeartRate { get; set; }
        public string? BloodPressure { get; set; }
        public double? Temperature { get; set; }
        public int? OxygenSaturation { get; set; }
        public int? Steps { get; set; }
        public double? Calories { get; set; }
        public string? ActivityLevel { get; set; }
    }

    // Location data
    public class LocationData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public string? Address { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Emergency types enum
    public enum EmergencyType
    {
        Fall = 1,
        Cardiac = 2,
        PanicAttack = 3,
        Seizure = 4,
        Overdose = 5,
        SelfHarm = 6,
        Unconscious = 7,
        Other = 8
    }

    // Severity levels
    public enum EmergencySeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    // Device information stored in database
    public class RegisteredDevice
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string DeviceToken { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty; // For signature verification
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string? LastKnownLocation { get; set; }

        // Navigation properties
        public User Patient { get; set; } = null!;
    }

    // Emergency incident log
    public class EmergencyIncident
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public string EmergencyType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceToken { get; set; } = string.Empty;
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? DoctorResponse { get; set; }
        public string? ActionTaken { get; set; }
        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? VitalSignsJson { get; set; }
        public string? LocationJson { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Navigation properties
        public User Patient { get; set; } = null!;
        public User? Doctor { get; set; }
    }

    // Test emergency request for development/testing
    public class TestEmergencyRequest
    {
        [Required]
        public string DeviceToken { get; set; } = string.Empty;

        [Required]
        public string EmergencyType { get; set; } = string.Empty;

        [Required]
        public string Severity { get; set; } = string.Empty;

        public string? Message { get; set; }

        public string? DeviceId { get; set; }

        // Vital signs for testing
        public int? HeartRate { get; set; }
        public string? BloodPressure { get; set; }
        public double? Temperature { get; set; }
        public int? OxygenSaturation { get; set; }

        // Location for testing
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
