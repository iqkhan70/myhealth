using System.ComponentModel.DataAnnotations;

namespace SM_MentalHealthApp.Shared
{
    /// <summary>
    /// Represents a charge for a Service Request to a Billing Account (Company or Individual SME)
    /// 
    /// CRITICAL SAFETY FEATURE:
    /// - Unique constraint on (ServiceRequestId, BillingAccountId) ensures only ONE charge
    ///   per SR per company/individual, preventing double billing even if multiple SMEs worked.
    /// 
    /// Billing Logic:
    /// - If multiple SMEs from the same company work on the same SR → ONE charge to the company
    /// - If SMEs from different companies work on the same SR → ONE charge per company
    /// - If independent SMEs work on the same SR → ONE charge per individual SME
    /// </summary>
    public class ServiceRequestCharge
    {
        public long Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        /// <summary>
        /// Billing Account ID (references BillingAccounts table)
        /// </summary>
        [Required]
        public long BillingAccountId { get; set; }

        /// <summary>
        /// Primary expertise used for pricing this charge
        /// </summary>
        public int? ExpertiseId { get; set; }

        /// <summary>
        /// Source of the rate: "BillingRate" or "Default" (system default $100)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RateSource { get; set; } = "Default";

        /// <summary>
        /// Charge amount for this SR to this billing account
        /// </summary>
        [Required]
        public decimal Amount { get; set; } = 0.00m;

        /// <summary>
        /// Charge status: Ready, Invoiced, Paid, Voided
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Ready";

        /// <summary>
        /// Reference to invoice if this charge has been invoiced
        /// </summary>
        public long? InvoiceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? InvoicedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime? VoidedAt { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public ServiceRequest ServiceRequest { get; set; } = null!;
        public Expertise? Expertise { get; set; }
        public SmeInvoice? Invoice { get; set; }
    }

    /// <summary>
    /// Charge status enum
    /// </summary>
    public enum ChargeStatus
    {
        Ready = 0,      // Ready to be invoiced
        Invoiced = 1,   // Included in an invoice
        Paid = 2,       // Invoice has been paid
        Voided = 3      // Charge was voided/cancelled
    }
}

