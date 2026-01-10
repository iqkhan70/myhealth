using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Billing status for service request assignments
    /// </summary>
    public enum BillingStatus
    {
        NotBillable = 0,  // Rejected, cancelled, never started
        Ready = 1,        // Work started/completed, ready to be invoiced
        Invoiced = 2,     // Included in an invoice
        Paid = 3,         // Invoice has been paid
        Voided = 4        // Was invoiced, then voided/credited
    }

    /// <summary>
    /// Invoice status
    /// </summary>
    public enum InvoiceStatus
    {
        Draft = 0,   // Invoice is being prepared
        Sent = 1,    // Invoice has been sent to SME
        Paid = 2,    // Invoice has been paid
        Voided = 3   // Invoice was voided/cancelled
    }

    /// <summary>
    /// SME Invoice entity
    /// </summary>
    public class SmeInvoice
    {
        public long Id { get; set; }
        
        [Required]
        public int SmeUserId { get; set; }
        
        /// <summary>
        /// Billing Account ID (references BillingAccounts table)
        /// </summary>
        [Required]
        public long BillingAccountId { get; set; }
        
        /// <summary>
        /// Type of billing account: "Company" or "Individual"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string BillingAccountType { get; set; } = "Individual";
        
        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime PeriodStart { get; set; }
        
        [Required]
        public DateTime PeriodEnd { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Sent, Paid, Voided
        
        [Required]
        public decimal SubTotal { get; set; } = 0.00m;
        
        [Required]
        public decimal TaxAmount { get; set; } = 0.00m;
        
        [Required]
        public decimal TotalAmount { get; set; } = 0.00m;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? SentAt { get; set; }
        
        public DateTime? PaidAt { get; set; }
        
        public DateTime? VoidedAt { get; set; }
        
        public int? CreatedByUserId { get; set; }
        
        public string? Notes { get; set; }
        
        // Navigation properties
        public User SmeUser { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public List<SmeInvoiceLine> InvoiceLines { get; set; } = new();
    }

    /// <summary>
    /// SME Invoice Line Item
    /// </summary>
    public class SmeInvoiceLine
    {
        public long Id { get; set; }
        
        [Required]
        public long InvoiceId { get; set; }
        
        [Required]
        public int AssignmentId { get; set; } // Unique constraint prevents duplicate billing
        
        /// <summary>
        /// Reference to ServiceRequestCharge (if billing by charge instead of assignment)
        /// </summary>
        public long? ChargeId { get; set; }
        
        [Required]
        public int ServiceRequestId { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public decimal Amount { get; set; } = 0.00m;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public SmeInvoice Invoice { get; set; } = null!;
        public ServiceRequestAssignment Assignment { get; set; } = null!;
        public ServiceRequestCharge? Charge { get; set; }
        public ServiceRequest ServiceRequest { get; set; } = null!;
    }

    /// <summary>
    /// Request to generate an invoice for an SME
    /// </summary>
    public class GenerateInvoiceRequest
    {
        [Required]
        public int SmeUserId { get; set; }
        
        [Required]
        public DateTime PeriodStart { get; set; }
        
        [Required]
        public DateTime PeriodEnd { get; set; }
        
        public decimal? TaxRate { get; set; } // Optional tax rate (e.g., 0.08 for 8%)
        
        public string? Notes { get; set; }
        
        public List<int>? AssignmentIds { get; set; } // Optional: specific assignments to include (if null, includes all Ready assignments)
    }

    /// <summary>
    /// Request to mark invoice as paid
    /// </summary>
    public class MarkInvoicePaidRequest
    {
        [Required]
        public long InvoiceId { get; set; }
        
        public DateTime? PaidDate { get; set; } // If null, uses current date
        
        public string? PaymentNotes { get; set; }
    }

    /// <summary>
    /// Request to void an invoice
    /// </summary>
    public class VoidInvoiceRequest
    {
        [Required]
        public long InvoiceId { get; set; }
        
        [Required]
        public string Reason { get; set; } = string.Empty;
        
        public bool ResetAssignmentsToReady { get; set; } = true; // Reset assignments back to Ready status
    }

    /// <summary>
    /// DTO for invoice display
    /// </summary>
    public class SmeInvoiceDto
    {
        public long Id { get; set; }
        public int SmeUserId { get; set; }
        public string SmeUserName { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? VoidedAt { get; set; }
        public string? Notes { get; set; }
        public int LineItemCount { get; set; }
        public List<SmeInvoiceLineDto> InvoiceLines { get; set; } = new();
        
        // Company billing fields
        public long? BillingAccountId { get; set; }
        public string? BillingAccountType { get; set; }
        public string? CompanyName { get; set; }
        public List<string> SmeNames { get; set; } = new(); // All SME names for company invoices
    }

    /// <summary>
    /// DTO for invoice line display
    /// </summary>
    public class SmeInvoiceLineDto
    {
        public long Id { get; set; }
        public long InvoiceId { get; set; }
        public int AssignmentId { get; set; }
        public int ServiceRequestId { get; set; }
        public string ServiceRequestTitle { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

