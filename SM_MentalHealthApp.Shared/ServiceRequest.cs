using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Represents a service request (SR1, SR2, etc.) for a client
    /// Each client can have multiple service requests, each assigned to different SMEs
    /// </summary>
    public class ServiceRequest
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; } // The patient/client this SR belongs to

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty; // e.g., "Medical Consultation", "Legal Case Review"

        [MaxLength(100)]
        public string? Type { get; set; } // e.g., "Medical", "Legal", "General"

        [MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Completed, Cancelled, OnHold

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int? CreatedByUserId { get; set; } // Who created this SR (admin, coordinator, etc.)

        public bool IsActive { get; set; } = true;

        [MaxLength(1000)]
        public string? Description { get; set; } // Optional description of the service request
        
        // Location fields for location-based matching
        [MaxLength(10)]
        public string? ServiceZipCode { get; set; } // Service location ZIP (defaults to client ZIP)
        public int MaxDistanceMiles { get; set; } = 50; // Maximum distance to search for SMEs

        /// <summary>
        /// Primary expertise used for billing/pricing.
        /// ServiceRequests can have multiple expertise tags, but pricing needs a single "billing category".
        /// If NULL and SR has exactly 1 expertise, use that. Otherwise coordinator must select.
        /// </summary>
        public int? PrimaryExpertiseId { get; set; }

        // Navigation properties
        public User Client { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public Expertise? PrimaryExpertise { get; set; }
        public List<ServiceRequestAssignment> Assignments { get; set; } = new();
        public List<ServiceRequestExpertise> Expertises { get; set; } = new();
    }

    /// <summary>
    /// Represents the assignment of an SME (doctor/attorney) to a ServiceRequest
    /// Supports multiple SMEs per SR in the future if needed
    /// </summary>
    public class ServiceRequestAssignment
    {
        public int Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public int SmeUserId { get; set; } // The doctor/attorney assigned to this SR

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UnassignedAt { get; set; } // When assignment was removed

        public bool IsActive { get; set; } = true;

        public int? AssignedByUserId { get; set; } // Who made this assignment

        // Assignment lifecycle fields
        [MaxLength(30)]
        public string Status { get; set; } = "Assigned"; // Assigned, Accepted, Rejected, InProgress, Completed, Abandoned

        [MaxLength(50)]
        public string? OutcomeReason { get; set; } // Reason for outcome (SME_NoResponse, Client_Cancelled, etc.)

        [MaxLength(30)]
        public string? ResponsibilityParty { get; set; } // SME, Client, System, Coordinator, Unknown

        public DateTime? AcceptedAt { get; set; } // When SME accepted

        public DateTime? StartedAt { get; set; } // When SME started working

        public DateTime? CompletedAt { get; set; } // When assignment completed

        public bool IsBillable { get; set; } = false; // Whether this assignment is billable

        // Billing status tracking (prevents re-billing)
        [MaxLength(20)]
        public string BillingStatus { get; set; } = "NotBillable"; // NotBillable, Ready, Invoiced, Paid, Voided

        public long? InvoiceId { get; set; } // Reference to SmeInvoice if invoiced

        public DateTime? BilledAt { get; set; } // When this assignment was included in an invoice

        public DateTime? PaidAt { get; set; } // When payment was received for this assignment

        // Navigation properties
        public ServiceRequest ServiceRequest { get; set; } = null!;
        public User SmeUser { get; set; } = null!;
        public User? AssignedByUser { get; set; }
    }

    /// <summary>
    /// Request model for creating a service request
    /// </summary>
    public class CreateServiceRequestRequest
    {
        [Required]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Type { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? SmeUserId { get; set; } // Optional: assign SME immediately on creation
        public List<int>? ExpertiseIds { get; set; } // Optional: expertise categories for this SR
        public int? PrimaryExpertiseId { get; set; } // Primary expertise for billing/pricing
        [MaxLength(10)]
        public string? ServiceZipCode { get; set; } // Optional: service location ZIP (defaults to client ZIP)
        public int? MaxDistanceMiles { get; set; } // Optional: max distance in miles (defaults to 50)
    }

    /// <summary>
    /// Request model for updating a service request
    /// </summary>
    public class UpdateServiceRequestRequest
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(100)]
        public string? Type { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
        public List<int>? ExpertiseIds { get; set; } // Optional: expertise categories for this SR
        public int? PrimaryExpertiseId { get; set; } // Primary expertise for billing/pricing
        [MaxLength(10)]
        public string? ServiceZipCode { get; set; } // Optional: service location ZIP
        public int? MaxDistanceMiles { get; set; } // Optional: max distance in miles
    }

    /// <summary>
    /// Request model for assigning an SME to a service request
    /// </summary>
    public class AssignSmeToServiceRequestRequest
    {
        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public int SmeUserId { get; set; }
    }

    /// <summary>
    /// Request model for unassigning an SME from a service request
    /// </summary>
    public class UnassignSmeFromServiceRequestRequest
    {
        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public int SmeUserId { get; set; }
    }

    /// <summary>
    /// DTO for service request responses
    /// </summary>
    public class ServiceRequestDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Description { get; set; }
        public string? ServiceZipCode { get; set; } // Service location ZIP (defaults to client ZIP)
        public int MaxDistanceMiles { get; set; } = 50; // Maximum distance to search for SMEs
        public int? PrimaryExpertiseId { get; set; } // Primary expertise for billing/pricing
        public string? PrimaryExpertiseName { get; set; } // Primary expertise name for display
        public List<ServiceRequestAssignmentDto> Assignments { get; set; } = new();
        public List<int> ExpertiseIds { get; set; } = new();
        public List<string> ExpertiseNames { get; set; } = new();
        
        /// <summary>
        /// Computed property for filtering by SME names (comma-separated)
        /// </summary>
        public string AssignedSmeNames => 
            Assignments != null && Assignments.Any(a => a.IsActive)
                ? string.Join(", ", Assignments.Where(a => a.IsActive).Select(a => a.SmeUserName))
                : string.Empty;
    }
}

