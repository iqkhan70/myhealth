using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    /// <summary>
    /// Service to manage ServiceRequestCharges
    /// 
    /// CRITICAL SAFETY: This service ensures only ONE charge per SR per company/individual
    /// by using database unique constraint (ServiceRequestId, BillingAccountId)
    /// </summary>
    public interface IServiceRequestChargeService
    {
        /// <summary>
        /// Creates charges for a Service Request based on billable assignments
        /// Groups by BillingAccount (CompanyId or SmeUserId) to prevent double billing
        /// </summary>
        Task CreateChargesForServiceRequestAsync(int serviceRequestId, decimal? amountPerCharge = null);

        /// <summary>
        /// Gets all Ready charges for a billing account (company or individual)
        /// </summary>
        Task<List<ServiceRequestCharge>> GetReadyChargesForBillingAccountAsync(
            int billingAccountId, 
            string billingAccountType,
            DateTime? startDate = null, 
            DateTime? endDate = null);
    }

    public class ServiceRequestChargeService : IServiceRequestChargeService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ServiceRequestChargeService> _logger;

        public ServiceRequestChargeService(
            JournalDbContext context,
            ILogger<ServiceRequestChargeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates charges for a Service Request based on billable assignments
        /// 
        /// Algorithm:
        /// 1. Get all billable assignments for the SR
        /// 2. Group by BillingAccountId (CompanyId if exists, else SmeUserId)
        /// 3. Create ONE charge per BillingAccountId
        /// 4. Database unique constraint prevents duplicates
        /// </summary>
        public async Task CreateChargesForServiceRequestAsync(int serviceRequestId, decimal? amountPerCharge = null)
        {
            try
            {
                // Get all billable assignments for this SR
                var billableAssignments = await _context.ServiceRequestAssignments
                    .Include(a => a.SmeUser)
                    .Where(a => a.ServiceRequestId == serviceRequestId &&
                                a.IsActive &&
                                a.IsBillable &&
                                (a.Status == "InProgress" ||
                                 a.Status == "Completed"))
                    .ToListAsync();

                if (!billableAssignments.Any())
                {
                    _logger.LogInformation(
                        "No billable assignments found for ServiceRequest {ServiceRequestId}",
                        serviceRequestId);
                    return;
                }

                // Group by BillingAccountId (CompanyId if exists, else SmeUserId)
                var billingGroups = billableAssignments
                    .GroupBy(a => new
                    {
                        BillingAccountId = a.SmeUser.CompanyId ?? a.SmeUserId,
                        BillingAccountType = a.SmeUser.CompanyId.HasValue ? "Company" : "Individual"
                    })
                    .ToList();

                var defaultAmount = amountPerCharge ?? 100.00m; // Default $100 per charge
                var chargesCreated = 0;

                foreach (var group in billingGroups)
                {
                    try
                    {
                        // Check if charge already exists (unique constraint will prevent duplicates)
                        var existingCharge = await _context.ServiceRequestCharges
                            .FirstOrDefaultAsync(c =>
                                c.ServiceRequestId == serviceRequestId &&
                                c.BillingAccountId == group.Key.BillingAccountId);

                        if (existingCharge != null)
                        {
                            _logger.LogInformation(
                                "Charge already exists for ServiceRequest {ServiceRequestId}, BillingAccount {BillingAccountId} ({Type})",
                                serviceRequestId, group.Key.BillingAccountId, group.Key.BillingAccountType);
                            continue;
                        }

                        // Create ONE charge per BillingAccount
                        var charge = new ServiceRequestCharge
                        {
                            ServiceRequestId = serviceRequestId,
                            BillingAccountId = group.Key.BillingAccountId,
                            BillingAccountType = group.Key.BillingAccountType,
                            Amount = defaultAmount,
                            Status = ChargeStatus.Ready.ToString(),
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ServiceRequestCharges.Add(charge);
                        chargesCreated++;

                        _logger.LogInformation(
                            "Created charge for ServiceRequest {ServiceRequestId}, BillingAccount {BillingAccountId} ({Type}) with {AssignmentCount} assignments",
                            serviceRequestId, group.Key.BillingAccountId, group.Key.BillingAccountType, group.Count());
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UQ_SR_BillingAccount") == true)
                    {
                        // Unique constraint violation - charge already exists (race condition)
                        _logger.LogWarning(
                            "Charge already exists for ServiceRequest {ServiceRequestId}, BillingAccount {BillingAccountId} (unique constraint violation)",
                            serviceRequestId, group.Key.BillingAccountId);
                    }
                }

                if (chargesCreated > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "Created {ChargeCount} charges for ServiceRequest {ServiceRequestId}",
                        chargesCreated, serviceRequestId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating charges for ServiceRequest {ServiceRequestId}",
                    serviceRequestId);
                throw;
            }
        }

        /// <summary>
        /// Gets all Ready charges for a billing account (company or individual)
        /// </summary>
        public async Task<List<ServiceRequestCharge>> GetReadyChargesForBillingAccountAsync(
            int billingAccountId,
            string billingAccountType,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.ServiceRequestCharges
                .Include(c => c.ServiceRequest)
                    .ThenInclude(sr => sr.Client)
                .Where(c => c.BillingAccountId == billingAccountId &&
                           c.BillingAccountType == billingAccountType &&
                           c.Status == ChargeStatus.Ready.ToString() &&
                           c.InvoiceId == null);

            // Date filtering (if provided)
            if (startDate.HasValue || endDate.HasValue)
            {
                // Filter by ServiceRequest CreatedAt or charge CreatedAt
                if (startDate.HasValue && endDate.HasValue)
                {
                    query = query.Where(c =>
                        (c.ServiceRequest.CreatedAt >= startDate.Value && c.ServiceRequest.CreatedAt <= endDate.Value) ||
                        (c.CreatedAt >= startDate.Value && c.CreatedAt <= endDate.Value));
                }
                else if (startDate.HasValue)
                {
                    query = query.Where(c =>
                        c.ServiceRequest.CreatedAt >= startDate.Value ||
                        c.CreatedAt >= startDate.Value);
                }
                else if (endDate.HasValue)
                {
                    query = query.Where(c =>
                        c.ServiceRequest.CreatedAt <= endDate.Value ||
                        c.CreatedAt <= endDate.Value);
                }
            }

            return await query.ToListAsync();
        }
    }
}

