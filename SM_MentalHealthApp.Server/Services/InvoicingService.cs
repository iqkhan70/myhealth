using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using BillingStatus = SM_MentalHealthApp.Shared.BillingStatus;
using ChargeStatus = SM_MentalHealthApp.Shared.ChargeStatus;

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
        private readonly IServiceRequestChargeService _chargeService;

        public InvoicingService(
            JournalDbContext context,
            ILogger<InvoicingService> logger,
            IServiceRequestChargeService chargeService)
        {
            _context = context;
            _logger = logger;
            _chargeService = chargeService;
        }

        /// <summary>
        /// Generate an invoice for an SME with all Ready assignments in the period
        /// Uses ServiceRequestCharges to prevent double-billing (one charge per SR per company)
        /// </summary>
        public async Task<SmeInvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequest request, int? createdByUserId = null)
        {
            try
            {
                // Get the SME user to determine billing account
                var smeUser = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == request.SmeUserId);

                if (smeUser == null)
                {
                    throw new InvalidOperationException($"SME user {request.SmeUserId} not found");
                }

                // Determine billing account (CompanyId if exists, else SmeUserId)
                var billingAccountId = smeUser.CompanyId ?? request.SmeUserId;
                var billingAccountType = smeUser.CompanyId.HasValue ? "Company" : "Individual";

                // If company billing, get ALL Ready assignments for ALL SMEs in the company
                // If individual billing, get assignments for this SME only
                var readyAssignmentsQuery = _context.ServiceRequestAssignments
                    .Include(a => a.ServiceRequest)
                        .ThenInclude(sr => sr.Client)
                    .Include(a => a.SmeUser)
                        .ThenInclude(u => u.Company)
                    .Where(a => a.IsActive &&
                        a.BillingStatus == BillingStatus.Ready.ToString() &&
                        a.InvoiceId == null &&
                        a.Status == AssignmentStatus.Completed.ToString() && // Only completed assignments are ready to bill
                        a.IsBillable && // Must be marked as billable
                                        // Date filter: use StartedAt if available, otherwise AssignedAt
                        ((a.StartedAt.HasValue && a.StartedAt >= request.PeriodStart && a.StartedAt <= request.PeriodEnd) ||
                         (!a.StartedAt.HasValue && a.AssignedAt >= request.PeriodStart && a.AssignedAt <= request.PeriodEnd)));

                // Filter by billing account: if company, get all SMEs in company; if individual, get this SME only
                if (billingAccountType == "Company")
                {
                    readyAssignmentsQuery = readyAssignmentsQuery.Where(a =>
                        a.SmeUser.CompanyId == billingAccountId);
                }
                else
                {
                    readyAssignmentsQuery = readyAssignmentsQuery.Where(a =>
                        a.SmeUserId == request.SmeUserId);
                }

                // Apply assignment filter if specified
                if (request.AssignmentIds != null && request.AssignmentIds.Any())
                {
                    readyAssignmentsQuery = readyAssignmentsQuery.Where(a => request.AssignmentIds.Contains(a.Id));
                }

                var readyAssignments = await readyAssignmentsQuery.ToListAsync();

                if (!readyAssignments.Any())
                {
                    throw new InvalidOperationException("No ready-to-bill assignments found for this billing account in the specified period");
                }

                // Create charges for all ServiceRequests that have ready assignments
                // This ensures we use the charge-based system to prevent double-billing
                var serviceRequestIds = readyAssignments.Select(a => a.ServiceRequestId).Distinct().ToList();
                foreach (var srId in serviceRequestIds)
                {
                    try
                    {
                        await _chargeService.CreateChargesForServiceRequestAsync(srId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create charges for ServiceRequest {ServiceRequestId}, continuing...", srId);
                    }
                }

                // Get ALL Ready charges for this billing account in the period
                var readyCharges = await _chargeService.GetReadyChargesForBillingAccountAsync(
                    billingAccountId,
                    billingAccountType,
                    request.PeriodStart,
                    request.PeriodEnd);

                // Filter to only charges for ServiceRequests that have assignments in our readyAssignments list
                var relevantCharges = readyCharges
                    .Where(c => serviceRequestIds.Contains(c.ServiceRequestId) && c.InvoiceId == null)
                    .ToList();

                if (!relevantCharges.Any())
                {
                    throw new InvalidOperationException("No ready-to-bill charges found for this billing account in the specified period. Charges may have already been invoiced.");
                }

                // Group by: Service Request → Company (or Individual SME if no company)
                // If multiple SMEs from the same company work on the same SR → ONE invoice
                // If individual SME (no company) works on SR → Separate invoice per SME
                var assignmentsByGroup = readyAssignments
                    .GroupBy(a => new
                    {
                        ServiceRequestId = a.ServiceRequestId,
                        BillingAccountId = a.SmeUser.CompanyId ?? a.SmeUserId, // Use CompanyId if exists, else SmeUserId for individual
                        BillingAccountType = a.SmeUser.CompanyId.HasValue ? "Company" : "Individual"
                    })
                    .ToList();

                if (!assignmentsByGroup.Any())
                {
                    throw new InvalidOperationException("No assignments found to invoice");
                }

                // Generate invoices - one per group (SR + SME + Company)
                var generatedInvoices = new List<SmeInvoiceDto>();

                foreach (var group in assignmentsByGroup)
                {
                    var groupAssignments = group.ToList();
                    var serviceRequestId = group.Key.ServiceRequestId;
                    var groupBillingAccountId = group.Key.BillingAccountId;
                    var groupBillingAccountType = group.Key.BillingAccountType;
                    var serviceRequest = groupAssignments.First().ServiceRequest;
                    
                    // Get the first SME user for invoice generation (for invoice number)
                    var firstSmeUser = groupAssignments.First().SmeUser;
                    var smeUserId = firstSmeUser.Id;

                    // Get charge for this SR and billing account
                    var charge = relevantCharges.FirstOrDefault(c =>
                        c.ServiceRequestId == serviceRequestId &&
                        c.BillingAccountId == groupBillingAccountId);

                    if (charge == null)
                    {
                        _logger.LogWarning("No charge found for ServiceRequest {ServiceRequestId}, BillingAccount {BillingAccountId} ({Type}), skipping...", 
                            serviceRequestId, groupBillingAccountId, groupBillingAccountType);
                        continue;
                    }

                    // Generate invoice number
                    var invoiceNumber = await GenerateInvoiceNumberAsync(smeUserId);

                    // Calculate totals
                    var subTotal = charge.Amount;
                    var taxRate = request.TaxRate ?? 0.00m;
                    var taxAmount = subTotal * taxRate;
                    var totalAmount = subTotal + taxAmount;

                    // Create ONE invoice for this group (SR + SME + Company)
                    var invoice = new SmeInvoice
                    {
                        SmeUserId = smeUserId,
                        BillingAccountId = groupBillingAccountId,
                        BillingAccountType = groupBillingAccountType,
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

                    // Create invoice line for this group
                    var line = new SmeInvoiceLine
                    {
                        InvoiceId = invoice.Id,
                        ChargeId = charge.Id,
                        ServiceRequestId = serviceRequestId,
                        AssignmentId = groupAssignments.First().Id, // Use first assignment ID
                        Description = $"Service Request: {serviceRequest.Title}",
                        Amount = charge.Amount,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.SmeInvoiceLines.Add(line);

                    // Update charge to Invoiced
                    charge.Status = ChargeStatus.Invoiced.ToString();
                    charge.InvoiceId = invoice.Id;
                    charge.InvoicedAt = DateTime.UtcNow;

                    // Update all assignments for this group to Invoiced
                    foreach (var assignment in groupAssignments)
                    {
                        assignment.BillingStatus = BillingStatus.Invoiced.ToString();
                        assignment.InvoiceId = invoice.Id;
                        assignment.BilledAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Generated invoice {InvoiceNumber} for ServiceRequest {ServiceRequestId}, BillingAccount {BillingAccountId} ({Type}) with {AssignmentCount} assignments. Total: {Total}",
                        invoiceNumber, serviceRequestId, groupBillingAccountId, groupBillingAccountType, groupAssignments.Count, totalAmount);

                    // Get the generated invoice DTO
                    var invoiceDto = await GetInvoiceByIdAsync(invoice.Id);
                    if (invoiceDto != null)
                    {
                        generatedInvoices.Add(invoiceDto);
                    }
                }

                if (!generatedInvoices.Any())
                {
                    throw new Exception("Failed to generate any invoices");
                }

                // Return the first invoice
                return generatedInvoices.First();
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

                var result = new List<SmeInvoiceDto>();
                foreach (var invoice in invoices)
                {
                    result.Add(await MapToDtoAsync(invoice));
                }
                return result;
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

                return invoice != null ? await MapToDtoAsync(invoice) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
                return null;
            }
        }

        /// <summary>
        /// Get assignments that are ready to be billed (for invoice generation preview)
        /// If SME belongs to a company, returns all assignments for all SMEs in that company
        /// If SME is individual, returns only that SME's assignments
        /// </summary>
        public async Task<List<BillableAssignmentDto>> GetReadyToBillAssignmentsAsync(int smeUserId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Get the SME user to determine if they belong to a company
                var smeUser = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == smeUserId);

                if (smeUser == null)
                {
                    return new List<BillableAssignmentDto>();
                }

                // Determine billing account (CompanyId if exists, else SmeUserId)
                var billingAccountId = smeUser.CompanyId ?? smeUserId;
                var billingAccountType = smeUser.CompanyId.HasValue ? "Company" : "Individual";

                var query = _context.ServiceRequestAssignments
                    .Include(a => a.ServiceRequest)
                        .ThenInclude(sr => sr.Client)
                    .Include(a => a.SmeUser)
                        .ThenInclude(u => u.Company)
                    .Where(a => a.IsActive &&
                        a.BillingStatus == BillingStatus.Ready.ToString() &&
                        a.InvoiceId == null &&
                        a.Status == AssignmentStatus.Completed.ToString() && // Only completed assignments are ready to bill
                        a.IsBillable); // Must be marked as billable

                // Filter by billing account: if company, get all SMEs in company; if individual, get this SME only
                if (billingAccountType == "Company")
                {
                    query = query.Where(a => a.SmeUser.CompanyId == billingAccountId);
                }
                else
                {
                    query = query.Where(a => a.SmeUserId == smeUserId);
                }

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
                    SmeCompany = a.SmeUser.Company != null ? a.SmeUser.Company.Name : null,
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
        private async Task<SmeInvoiceDto> MapToDtoAsync(SmeInvoice invoice)
        {
            // Get all SME names for company invoices
            var smeNames = new List<string>();
            string? companyName = null;

            if (invoice.BillingAccountType == "Company")
            {
                // Get company name
                var company = await _context.Companies.FindAsync(invoice.BillingAccountId);
                companyName = company?.Name;

                // Get all SMEs in this company that have assignments in this invoice
                var invoiceLineAssignmentIds = invoice.InvoiceLines
                    .Where(l => l.AssignmentId > 0)
                    .Select(l => l.AssignmentId)
                    .ToList();

                // Load assignments first, then format names in memory (EF Core can't translate string.Format)
                var assignments = await _context.ServiceRequestAssignments
                    .Include(a => a.SmeUser)
                    .Where(a => invoiceLineAssignmentIds.Contains(a.Id) &&
                               a.SmeUser.CompanyId == invoice.BillingAccountId)
                    .ToListAsync();

                smeNames = assignments
                    .Select(a => $"{a.SmeUser.FirstName} {a.SmeUser.LastName}")
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
            }
            else
            {
                // Individual invoice - just the one SME
                smeNames.Add($"{invoice.SmeUser.FirstName} {invoice.SmeUser.LastName}");
            }

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
                BillingAccountId = invoice.BillingAccountId,
                BillingAccountType = invoice.BillingAccountType,
                CompanyName = companyName,
                SmeNames = smeNames,
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

        /// <summary>
        /// Map SmeInvoice to DTO (synchronous version for backward compatibility)
        /// </summary>
        private SmeInvoiceDto MapToDto(SmeInvoice invoice)
        {
            // For synchronous calls, we'll load SME names if needed
            // This is a simplified version - async version is preferred
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
                BillingAccountId = invoice.BillingAccountId,
                BillingAccountType = invoice.BillingAccountType,
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

