using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Database model for content types - replaces the ContentType enum
    /// </summary>
    public class ContentTypeModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Icon { get; set; } // For UI display (e.g., "📄", "🖼️", "🎥")

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ContentItem> Contents { get; set; } = new List<ContentItem>();
    }
}
