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

        [StringLength(2000)]
        public string? Summary { get; set; } // AI-generated conversation summary

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public PrivacyLevel PrivacyLevel { get; set; } = PrivacyLevel.Full;

        public int MessageCount { get; set; } = 0;

        // Navigation properties
        public User? User { get; set; }
        public User? Patient { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public enum PrivacyLevel
    {
        None,        // No chat history stored
        Summary,     // Only AI-generated summaries
        Full        // Recent messages + summaries
    }
}
