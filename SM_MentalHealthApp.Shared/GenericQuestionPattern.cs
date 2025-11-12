using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    public class GenericQuestionPattern
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Pattern { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int Priority { get; set; } = 0; // Higher priority patterns are checked first
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

