using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [Required]
        public MessageRole Role { get; set; }

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true; // Soft delete flag

        public bool IsMedicalData { get; set; } = false;

        public MessageType MessageType { get; set; } = MessageType.Question;

        [StringLength(1000)]
        public string? Metadata { get; set; } // JSON for additional context

        // Navigation properties
        public ChatSession? Session { get; set; }
    }

    public enum MessageRole
    {
        User,
        Assistant,
        System
    }

    public enum MessageType
    {
        Question,
        Response,
        Context,
        Summary
    }
}
