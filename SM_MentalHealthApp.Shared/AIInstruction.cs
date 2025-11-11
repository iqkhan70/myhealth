using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Categories for organizing AI instructions (e.g., "Critical Priority", "Patient Medical Overview")
    /// </summary>
    public class AIInstructionCategory
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "Critical Priority", "Patient Medical Overview"
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string Context { get; set; } = "HealthCheck"; // HealthCheck, Chat, DecisionSupport, etc.
        
        public int DisplayOrder { get; set; } = 0; // Order in which categories should appear
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property
        public ICollection<AIInstruction> Instructions { get; set; } = new List<AIInstruction>();
    }
    
    /// <summary>
    /// Individual AI instructions that can be managed in the database
    /// </summary>
    public class AIInstruction
    {
        public int Id { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty; // The actual instruction text
        
        [MaxLength(200)]
        public string? Title { get; set; } // Optional title/header for the instruction
        
        public int DisplayOrder { get; set; } = 0; // Order within the category
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property
        public AIInstructionCategory? Category { get; set; }
    }
}

