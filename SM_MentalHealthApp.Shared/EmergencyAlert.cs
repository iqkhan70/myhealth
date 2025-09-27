using System.Text.Json.Serialization;

namespace SM_MentalHealthApp.Shared
{
    public class EmergencyAlert
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string EmergencyType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public int? AcknowledgedByDoctorId { get; set; }
        public string? DoctorResponse { get; set; }
        public string? ActionTaken { get; set; }
        public string? Resolution { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public VitalSigns? VitalSigns { get; set; }
        public LocationData? Location { get; set; }
    }

    public class VitalSigns
    {
        public int? HeartRate { get; set; }
        public string? BloodPressure { get; set; }
        public double? Temperature { get; set; }
        public int? OxygenSaturation { get; set; }
    }

    public class LocationData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public string? Address { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
