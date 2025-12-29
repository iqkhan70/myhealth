using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    public class ChatSession
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string SessionId { get; set; } = string.Empty;

        public int? PatientId { get; set; } // For doctor-patient conversations

        public int? ServiceRequestId { get; set; } // Optional: links to ServiceRequest for data isolation

        [StringLength(2000)]
        public string? Summary { get; set; } // AI-generated conversation summary

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public PrivacyLevel PrivacyLevel { get; set; } = PrivacyLevel.Full;

        public int MessageCount { get; set; } = 0;

        // Doctor ignore functionality - allows doctors to mark historical data as ignored for AI analysis
        public bool IsIgnoredByDoctor { get; set; } = false;
        public int? IgnoredByDoctorId { get; set; } // Which doctor marked this as ignored
        public DateTime? IgnoredAt { get; set; } // When it was ignored

        // Navigation properties
        public User? User { get; set; }
        public User? Patient { get; set; }
        public User? IgnoredByDoctor { get; set; } // Navigation to the doctor who ignored it
        public ServiceRequest? ServiceRequest { get; set; } // Navigation to service request
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        // Computed properties for search/filtering
        public string PatientDisplayName => PatientId.HasValue
            ? (Patient?.FullName ?? $"Patient {PatientId}")
            : "General Chat";

        public string UserDisplayName => User?.FullName ?? $"User {UserId}";
    }

    public enum PrivacyLevel
    {
        None,        // No chat history stored
        Summary,     // Only AI-generated summaries
        Full,        // Recent messages + summaries
        Private      // Private chat sessions
    }
}
