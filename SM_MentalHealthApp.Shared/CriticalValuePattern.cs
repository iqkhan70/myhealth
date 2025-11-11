namespace SM_MentalHealthApp.Shared
{
    public class CriticalValuePattern
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual CriticalValueCategory? Category { get; set; }
    }
}

