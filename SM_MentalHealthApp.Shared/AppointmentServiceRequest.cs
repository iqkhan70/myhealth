using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Junction table for many-to-many relationship between Appointments and ServiceRequests
    /// Allows an appointment to be associated with one or multiple Service Requests
    /// </summary>
    public class AppointmentServiceRequest
    {
        public int Id { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        /// <summary>
        /// Timestamp when the SR was linked to this appointment
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional notes about why this SR is linked to this appointment
        /// </summary>
        public string? Notes { get; set; }

        // Navigation properties
        public Appointment Appointment { get; set; } = null!;
        public ServiceRequest ServiceRequest { get; set; } = null!;
    }
}

