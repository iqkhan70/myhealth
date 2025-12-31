using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using BillingStatus = SM_MentalHealthApp.Shared.BillingStatus;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IInvoicingService
    {
        Task<SmeInvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequest request, int? createdByUserId = null);
        Task<bool> MarkInvoicePaidAsync(long invoiceId, DateTime? paidDate = null, string? paymentNotes = null);
        Task<bool> VoidInvoiceAsync(long invoiceId, string reason, bool resetAssignmentsToReady = true);
        Task<List<SmeInvoiceDto>> GetInvoicesAsync(int? smeUserId = null, InvoiceStatus? status = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<SmeInvoiceDto?> GetInvoiceByIdAsync(long invoiceId);
        Task<List<BillableAssignmentDto>> GetReadyToBillAssignmentsAsync(int smeUserId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class InvoicingService : IInvoicingService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<InvoicingService> _logger;

        public InvoicingService(JournalDbContext context, ILogger<InvoicingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generate an invoice for an SME with all Ready assignments in the period
        /// </summary>
        public async Task<SmeInvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequest request, int? createdByUserId = null)
        {
            try
            {
                // Get all Ready assignments for this SME in the period
                var readyAssignments = await _context.ServiceRequestAssignments
                    .Include(a => a.ServiceRequest)
                        .ThenInclude(sr => sr.Client)
                    .Include(a => a.SmeUser)
                    .Where(a => a.IsActive &&
                        a.SmeUserId == request.SmeUserId &&
                        a.BillingStatus == BillingStatus.Ready.ToString() &&
                        a.InvoiceId == null &&
                        (request.AssignmentIds == null || request.AssignmentIds.Contains(a.Id)) &&
                        // Date filter: use StartedAt if available, otherwise AssignedAt
                        ((a.StartedAt.HasValue && a.StartedAt >= request.PeriodStart && a.StartedAt <= request.PeriodEnd) ||
                         (!a.StartedAt.HasValue && a.AssignedAt >= request.PeriodStart && a.AssignedAt <= request.PeriodEnd)))
                    .ToListAsync();

                if (!readyAssignments.Any())
                {
                    throw new InvalidOperationException("No ready-to-bill assignments found for this SME in the specified period");
                }

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumberAsync(request.SmeUserId);

                // Calculate totals (assuming flat rate per assignment - you can customize this)
                var subTotal = readyAssignments.Count * 100.00m; // Example: $100 per assignment
                var taxRate = request.TaxRate ?? 0.00m;
                var taxAmount = subTotal * taxRate;
                var totalAmount = subTotal + taxAmount;

                // Create invoice
                var invoice = new SmeInvoice
                {
                    SmeUserId = request.SmeUserId,
                    InvoiceNumber = invoiceNumber,
                    PeriodStart = request.PeriodStart,
                    PeriodEnd = request.PeriodEnd,
                    Status = InvoiceStatus.Draft.ToString(),
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    CreatedByUserId = createdByUserId,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SmeInvoices.Add(invoice);
                await _context.SaveChangesAsync(); // Save to get invoice ID

                // Create invoice lines and update assignments
                var invoiceLines = new List<SmeInvoiceLine>();
                foreach (var assignment in readyAssignments)
                {
                    var line = new SmeInvoiceLine
                    {
                        InvoiceId = invoice.Id,
                        AssignmentId = assignment.Id,
                        ServiceRequestId = assignment.ServiceRequestId,
                        Description = $"Service Request: {assignment.ServiceRequest.Title}",
                        Amount = 100.00m, // Example: $100 per assignment
                        CreatedAt = DateTime.UtcNow
                    };

                    invoiceLines.Add(line);

                    // Update assignment to Invoiced
                    assignment.BillingStatus = BillingStatus.Invoiced.ToString();
                    assignment.InvoiceId = invoice.Id;
                    assignment.BilledAt = DateTime.UtcNow;
                }

                _context.SmeInvoiceLines.AddRange(invoiceLines);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Generated invoice {InvoiceNumber} for SME {SmeUserId} with {LineCount} line items. Total: {Total}",
                    invoiceNumber, request.SmeUserId, invoiceLines.Count, totalAmount);

                return await GetInvoiceByIdAsync(invoice.Id) ?? throw new Exception("Failed to retrieve generated invoice");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice for SME {SmeUserId}", request.SmeUserId);
                throw;
            }
        }

        /// <summary>
        /// Mark an invoice as paid and update all related assignments
        /// </summary>
        public async Task<bool> MarkInvoicePaidAsync(long invoiceId, DateTime? paidDate = null, string? paymentNotes = null)
        {
            try
            {
                var invoice = await _context.SmeInvoices
                    .Include(i => i.InvoiceLines)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return false;

                if (invoice.Status == InvoiceStatus.Paid.ToString())
                {
                    _logger.LogWarning("Invoice {InvoiceId} is already marked as paid", invoiceId);
                    return false;
                }

                var paidDateTime = paidDate ?? DateTime.UtcNow;

                // Update invoice
                invoice.Status = InvoiceStatus.Paid.ToString();
                invoice.PaidAt = paidDateTime;
                if (!string.IsNullOrEmpty(paymentNotes))
                {
                    invoice.Notes = string.IsNullOrEmpty(invoice.Notes) 
                        ? paymentNotes 
                        : $"{invoice.Notes}\n\nPayment: {paymentNotes}";
                }

                // Update all assignments to Paid
                var assignmentIds = invoice.InvoiceLines.Select(l => l.AssignmentId).ToList();
                var assignments = await _context.ServiceRequestAssignments
                    .Where(a => assignmentIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var assignment in assignments)
                {
                    assignment.BillingStatus = BillingStatus.Paid.ToString();
                    assignment.PaidAt = paidDateTime;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Invoice {InvoiceId} ({InvoiceNumber}) marked as paid. Updated {AssignmentCount} assignments to Paid status.",
                    invoiceId, invoice.InvoiceNumber, assignments.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice {InvoiceId} as paid", invoiceId);
                return false;
            }
        }

        /// <summary>
        /// Void an invoice and optionally reset assignments back to Ready
        /// </summary>
        public async Task<bool> VoidInvoiceAsync(long invoiceId, string reason, bool resetAssignmentsToReady = true)
        {
            try
            {
                var invoice = await _context.SmeInvoices
                    .Include(i => i.InvoiceLines)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                if (invoice == null)
                    return false;

                // Update invoice
                invoice.Status = InvoiceStatus.Voided.ToString();
                invoice.VoidedAt = DateTime.UtcNow;
                invoice.Notes = string.IsNullOrEmpty(invoice.Notes) 
                    ? $"VOIDED: {reason}" 
                    : $"{invoice.Notes}\n\nVOIDED: {reason}";

                // Update assignments
                var assignmentIds = invoice.InvoiceLines.Select(l => l.AssignmentId).ToList();
                var assignments = await _context.ServiceRequestAssignments
                    .Where(a => assignmentIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var assignment in assignments)
                {
                    if (resetAssignmentsToReady)
                    {
                        // Reset to Ready so they can be re-invoiced
                        assignment.BillingStatus = BillingStatus.Ready.ToString();
                        assignment.InvoiceId = null;
                        assignment.BilledAt = null;
                    }
                    else
                    {
                        // Mark as Voided to prevent re-billing
                        assignment.BillingStatus = BillingStatus.Voided.ToString();
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Invoice {InvoiceId} ({InvoiceNumber}) voided. Reason: {Reason}. Reset {AssignmentCount} assignments.",
                    invoiceId, invoice.InvoiceNumber, reason, assignments.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voiding invoice {InvoiceId}", invoiceId);
                return false;
            }
        }

        /// <summary>
        /// Get invoices with optional filters
        /// </summary>
        public async Task<List<SmeInvoiceDto>> GetInvoicesAsync(int? smeUserId = null, InvoiceStatus? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.SmeInvoices
                    .Include(i => i.SmeUser)
                    .Include(i => i.InvoiceLines)
                    .AsQueryable();

                if (smeUserId.HasValue)
                    query = query.Where(i => i.SmeUserId == smeUserId.Value);

                if (status.HasValue)
                    query = query.Where(i => i.Status == status.Value.ToString());

                // Date filtering: match invoices where the period overlaps with the requested date range
                // If only startDate is provided, show invoices that end on or after that date
                // If only endDate is provided, show invoices that start on or before that date
                // If both are provided, show invoices that overlap with the range
                if (startDate.HasValue && endDate.HasValue)
                {
                    // Normalize dates to start of day for comparison
                    var startDateNormalized = startDate.Value.Date;
                    var endDateNormalized = endDate.Value.Date;
                    
                    // Invoice period overlaps with requested range if:
                    // Invoice starts before or on the end date AND invoice ends on or after the start date
                    query = query.Where(i => i.PeriodStart.Date <= endDateNormalized && i.PeriodEnd.Date >= startDateNormalized);
                }
                else if (startDate.HasValue)
                {
                    var startDateNormalized = startDate.Value.Date;
                    query = query.Where(i => i.PeriodEnd.Date >= startDateNormalized);
                }
                else if (endDate.HasValue)
                {
                    var endDateNormalized = endDate.Value.Date;
                    query = query.Where(i => i.PeriodStart.Date <= endDateNormalized);
                }

                var invoices = await query
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return invoices.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoices");
                return new List<SmeInvoiceDto>();
            }
        }

        /// <summary>
        /// Get invoice by ID
        /// </summary>
        public async Task<SmeInvoiceDto?> GetInvoiceByIdAsync(long invoiceId)
        {
            try
            {
                var invoice = await _context.SmeInvoices
                    .Include(i => i.SmeUser)
                    .Include(i => i.InvoiceLines)
                        .ThenInclude(l => l.Assignment)
                            .ThenInclude(a => a.ServiceRequest)
                                .ThenInclude(sr => sr.Client)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId);

                return invoice != null ? MapToDto(invoice) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
                return null;
            }
        }

        /// <summary>
        /// Get assignments that are ready to be billed (for invoice generation preview)
        /// </summary>
        public async Task<List<BillableAssignmentDto>> GetReadyToBillAssignmentsAsync(int smeUserId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.ServiceRequestAssignments
                    .Include(a => a.ServiceRequest)
                        .ThenInclude(sr => sr.Client)
                    .Include(a => a.SmeUser)
                    .Where(a => a.IsActive &&
                        a.SmeUserId == smeUserId &&
                        a.BillingStatus == BillingStatus.Ready.ToString() &&
                        a.InvoiceId == null);

                // Date filter
                if (startDate.HasValue)
                {
                    query = query.Where(a => 
                        (a.StartedAt.HasValue && a.StartedAt >= startDate.Value) ||
                        (!a.StartedAt.HasValue && a.AssignedAt >= startDate.Value));
                }

                if (endDate.HasValue)
                {
                    var endDateTime = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(a => 
                        (a.StartedAt.HasValue && a.StartedAt <= endDateTime) ||
                        (!a.StartedAt.HasValue && a.AssignedAt <= endDateTime));
                }

                var assignments = await query
                    .OrderByDescending(a => a.StartedAt ?? a.AssignedAt)
                    .ToListAsync();

                return assignments.Select(a => new BillableAssignmentDto
                {
                    AssignmentId = a.Id,
                    ServiceRequestId = a.ServiceRequestId,
                    ServiceRequestTitle = a.ServiceRequest.Title,
                    ClientId = a.ServiceRequest.ClientId,
                    ClientName = $"{a.ServiceRequest.Client.FirstName} {a.ServiceRequest.Client.LastName}",
                    SmeUserId = a.SmeUserId,
                    SmeUserName = $"{a.SmeUser.FirstName} {a.SmeUser.LastName}",
                    SmeCompany = a.SmeUser.Specialization,
                    Status = a.Status,
                    StartedAt = a.StartedAt,
                    CompletedAt = a.CompletedAt,
                    AssignedAt = a.AssignedAt,
                    IsBillable = a.IsBillable,
                    DaysToComplete = a.CompletedAt.HasValue && a.StartedAt.HasValue
                        ? (int?)(a.CompletedAt.Value - a.StartedAt.Value).TotalDays
                        : null
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ready-to-bill assignments for SME {SmeUserId}", smeUserId);
                return new List<BillableAssignmentDto>();
            }
        }

        /// <summary>
        /// Generate unique invoice number
        /// </summary>
        private async Task<string> GenerateInvoiceNumberAsync(int smeUserId)
        {
            var sme = await _context.Users.FindAsync(smeUserId);
            var smeInitials = sme != null ? $"{sme.FirstName[0]}{sme.LastName[0]}" : "SME";
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var invoiceNumber = $"INV-{smeInitials}-{timestamp}";
            
            // Ensure uniqueness
            var exists = await _context.SmeInvoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber);
            if (exists)
            {
                invoiceNumber = $"{invoiceNumber}-{Guid.NewGuid().ToString("N").Substring(0, 4)}";
            }
            
            return invoiceNumber;
        }

        /// <summary>
        /// Map SmeInvoice to DTO
        /// </summary>
        private SmeInvoiceDto MapToDto(SmeInvoice invoice)
        {
            return new SmeInvoiceDto
            {
                Id = invoice.Id,
                SmeUserId = invoice.SmeUserId,
                SmeUserName = $"{invoice.SmeUser.FirstName} {invoice.SmeUser.LastName}",
                InvoiceNumber = invoice.InvoiceNumber,
                PeriodStart = invoice.PeriodStart,
                PeriodEnd = invoice.PeriodEnd,
                Status = invoice.Status,
                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                CreatedAt = invoice.CreatedAt,
                SentAt = invoice.SentAt,
                PaidAt = invoice.PaidAt,
                VoidedAt = invoice.VoidedAt,
                Notes = invoice.Notes,
                LineItemCount = invoice.InvoiceLines.Count,
                InvoiceLines = invoice.InvoiceLines.Select(l => new SmeInvoiceLineDto
                {
                    Id = l.Id,
                    InvoiceId = l.InvoiceId,
                    AssignmentId = l.AssignmentId,
                    ServiceRequestId = l.ServiceRequestId,
                    ServiceRequestTitle = l.ServiceRequest?.Title ?? "Unknown",
                    ClientName = l.ServiceRequest?.Client != null 
                        ? $"{l.ServiceRequest.Client.FirstName} {l.ServiceRequest.Client.LastName}" 
                        : "Unknown",
                    Description = l.Description,
                    Amount = l.Amount,
                    CreatedAt = l.CreatedAt
                }).ToList()
            };
        }
    }
}

