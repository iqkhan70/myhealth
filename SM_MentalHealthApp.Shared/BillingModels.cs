using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Represents a billing account - "who gets billed" (Company or Individual SME)
    /// Every SME has exactly one BillingAccountId:
    /// - If they belong to a company → SME points to that company billing account
    /// - Else → SME points to their own individual billing account
    /// </summary>
    public class BillingAccount
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // "Company" or "Individual"

        /// <summary>
        /// If Type=Company, this is the Company ID
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// If Type=Individual, this is the User ID
        /// </summary>
        public int? UserId { get; set; }

        [MaxLength(255)]
        public string? Name { get; set; } // Convenience field for display

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Company? Company { get; set; }
        public User? User { get; set; }
        public List<BillingRate> BillingRates { get; set; } = new();
    }

    /// <summary>
    /// Stores pricing per (BillingAccount, Expertise) combination
    /// Allows different prices for different expertise types per billing account
    /// </summary>
    public class BillingRate
    {
        public long Id { get; set; }

        [Required]
        public long BillingAccountId { get; set; }

        [Required]
        public int ExpertiseId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public BillingAccount BillingAccount { get; set; } = null!;
        public Expertise Expertise { get; set; } = null!;
    }

    /// <summary>
    /// Request to create a billing rate
    /// </summary>
    public class CreateBillingRateRequest
    {
        [Required]
        public long BillingAccountId { get; set; }

        [Required]
        public int ExpertiseId { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Amount { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Request to update a billing rate
    /// </summary>
    public class UpdateBillingRateRequest
    {
        public int? ExpertiseId { get; set; }

        public decimal? Amount { get; set; }

        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// DTO for billable assignment information
    /// </summary>
    public class BillableAssignmentDto
    {
        public int AssignmentId { get; set; }
        public int ServiceRequestId { get; set; }
        public string ServiceRequestTitle { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int SmeUserId { get; set; }
        public string SmeUserName { get; set; } = string.Empty;
        public string? SmeCompany { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool IsBillable { get; set; }
        public string BillingStatus { get; set; } = "NotBillable";
        public long? InvoiceId { get; set; }
        public DateTime? BilledAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public int? DaysToComplete { get; set; }
    }

    /// <summary>
    /// DTO for billing summary grouped by SME or Company
    /// </summary>
    public class BillingSummaryDto
    {
        public int? SmeUserId { get; set; }
        public string SmeUserName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public int TotalBillableAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int InProgressAssignments { get; set; }
        public DateTime? FirstAssignmentDate { get; set; }
        public DateTime? LastAssignmentDate { get; set; }
        public double AverageDaysToComplete { get; set; }
        public List<BillableAssignmentDto> Assignments { get; set; } = new();
    }
}
