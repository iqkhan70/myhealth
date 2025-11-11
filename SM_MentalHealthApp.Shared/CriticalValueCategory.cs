namespace SM_MentalHealthApp.Shared
{
    public class CriticalValueCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<CriticalValuePattern> Patterns { get; set; } = new List<CriticalValuePattern>();
    }
}

