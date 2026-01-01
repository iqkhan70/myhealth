using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Represents an expertise category (e.g., Roofing, Plumbing, Medical, Legal)
    /// </summary>
    public class Expertise
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public List<SmeExpertise> SmeExpertises { get; set; } = new();
        public List<ServiceRequestExpertise> ServiceRequestExpertises { get; set; } = new();
    }

    /// <summary>
    /// Junction table: SME ↔ Expertise (many-to-many)
    /// </summary>
    public class SmeExpertise
    {
        public long Id { get; set; }

        [Required]
        public int SmeUserId { get; set; }

        [Required]
        public int ExpertiseId { get; set; }

        public bool IsPrimary { get; set; } = false; // Primary expertise for this SME

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User SmeUser { get; set; } = null!;
        public Expertise Expertise { get; set; } = null!;
    }

    /// <summary>
    /// Junction table: ServiceRequest ↔ Expertise (many-to-many)
    /// </summary>
    public class ServiceRequestExpertise
    {
        public long Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public int ExpertiseId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ServiceRequest ServiceRequest { get; set; } = null!;
        public Expertise Expertise { get; set; } = null!;
    }
}

