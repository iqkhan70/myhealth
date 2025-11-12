namespace SM_MentalHealthApp.Shared
{
    public class KnowledgeBaseCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public ICollection<KnowledgeBaseEntry> Entries { get; set; } = new List<KnowledgeBaseEntry>();
    }
}

