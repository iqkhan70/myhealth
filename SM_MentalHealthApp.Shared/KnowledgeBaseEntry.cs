namespace SM_MentalHealthApp.Shared
{
    public class KnowledgeBaseEntry
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        
        // Keywords/topics that trigger this entry (comma-separated or JSON array)
        // e.g., "anxiety, anxious, panic attack" or ["anxiety", "anxious", "panic attack"]
        public string Keywords { get; set; } = string.Empty;
        
        // Priority: Higher priority entries are checked first
        public int Priority { get; set; } = 0;
        
        // Whether to use this as a direct response or as context for AI
        public bool UseAsDirectResponse { get; set; } = true;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }

        // Navigation properties
        public KnowledgeBaseCategory? Category { get; set; }
    }
}

