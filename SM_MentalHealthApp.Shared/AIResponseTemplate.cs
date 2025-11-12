namespace SM_MentalHealthApp.Shared
{
    public class AIResponseTemplate
    {
        public int Id { get; set; }
        public string TemplateKey { get; set; } = string.Empty; // e.g., "critical_alert", "stable_status", "concerns_detected"
        public string TemplateName { get; set; } = string.Empty; // Human-readable name
        public string Content { get; set; } = string.Empty; // Template with placeholders like {CRITICAL_VALUES}, {STATUS}
        public string? Description { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}

