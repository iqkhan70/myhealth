using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    public class SmsMessage
    {
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTime SentAt { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        public User? Sender { get; set; }
        public User? Receiver { get; set; }
    }
}
