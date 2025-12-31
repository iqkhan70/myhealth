using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Represents a company that SMEs can belong to for billing purposes
    /// </summary>
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public List<User> Users { get; set; } = new();
    }

    /// <summary>
    /// Request model for creating a company
    /// </summary>
    public class CreateCompanyRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request model for updating a company
    /// </summary>
    public class UpdateCompanyRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }
}

