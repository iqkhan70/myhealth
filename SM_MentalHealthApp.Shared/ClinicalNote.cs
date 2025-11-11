using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Clinical notes created by doctors for patients
    /// </summary>
    public class ClinicalNote
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string NoteType { get; set; } = "General"; // General, Assessment, Treatment, Follow-up, etc.

        [MaxLength(20)]
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical

        public bool IsConfidential { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsArchived { get; set; } = false;

        // Tags for better organization and search
        public string? Tags { get; set; } // Comma-separated tags like "diabetes,medication,urgent"

        // Doctor ignore functionality - allows doctors to mark clinical notes as ignored for AI analysis
        public bool IsIgnoredByDoctor { get; set; } = false;
        public int? IgnoredByDoctorId { get; set; } // Which doctor marked this as ignored
        public DateTime? IgnoredAt { get; set; } // When it was ignored

        // Navigation properties
        public User Patient { get; set; } = null!;
        public User Doctor { get; set; } = null!;
        public User? IgnoredByDoctor { get; set; } // Navigation to the doctor who ignored it
    }

    /// <summary>
    /// Request model for creating clinical notes
    /// </summary>
    public class CreateClinicalNoteRequest
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string NoteType { get; set; } = "General";

        [MaxLength(20)]
        public string Priority { get; set; } = "Normal";

        public bool IsConfidential { get; set; } = false;

        public string? Tags { get; set; }
    }

    /// <summary>
    /// Request model for updating clinical notes
    /// </summary>
    public class UpdateClinicalNoteRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string NoteType { get; set; } = "General";

        [MaxLength(20)]
        public string Priority { get; set; } = "Normal";

        public bool IsConfidential { get; set; } = false;

        public string? Tags { get; set; }
    }

    /// <summary>
    /// Response model for clinical notes (without sensitive data)
    /// </summary>
    public class ClinicalNoteDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string NoteType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool IsConfidential { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Tags { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public bool IsIgnoredByDoctor { get; set; }
        public int? IgnoredByDoctorId { get; set; }
        public DateTime? IgnoredAt { get; set; }
    }
}
