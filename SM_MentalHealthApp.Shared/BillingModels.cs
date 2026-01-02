using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
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
        public string? SmeCompany { get; set; } // If SMEs are tied to companies
        public string Status { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime AssignedAt { get; set; }
        public bool IsBillable { get; set; }
        public string BillingStatus { get; set; } = "NotBillable"; // NotBillable, Ready, Invoiced, Paid, Voided
        public long? InvoiceId { get; set; }
        public DateTime? BilledAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public int? DaysToComplete { get; set; } // Calculated field
    }

    /// <summary>
    /// Billing summary grouped by SME or Company
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

    /// <summary>
    /// Request for billing report
    /// </summary>
    public class BillingReportRequest
    {
        public int? SmeUserId { get; set; }
        public string? CompanyName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool GroupByCompany { get; set; } = false;
    }
}

