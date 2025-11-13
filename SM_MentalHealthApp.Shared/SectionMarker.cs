using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Represents a section marker used for parsing context text
    /// Section markers identify different sections within input text (e.g., "=== MEDICAL DATA SUMMARY ===")
    /// </summary>
    public class SectionMarker
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Marker { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; } // e.g., "Patient Data", "Instructions", "Emergency"

        public int Priority { get; set; } = 0; // Higher priority markers are checked first
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

