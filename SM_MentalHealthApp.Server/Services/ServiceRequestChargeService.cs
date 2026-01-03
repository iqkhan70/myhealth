using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    /// <summary>
    /// Service to manage ServiceRequestCharges
    /// 
    /// CRITICAL SAFETY: This service ensures only ONE charge per SR per BillingAccount
    /// by using database unique constraint (ServiceRequestId, BillingAccountId)
    /// 
    /// Pricing Logic:
    /// 1. Determine PrimaryExpertiseId from ServiceRequest
    /// 2. Look up BillingRate for (BillingAccountId, PrimaryExpertiseId)
    /// 3. If rate exists and IsActive → use it
    /// 4. Else fallback to SystemDefaultAmount ($100)
    /// </summary>
    public interface IServiceRequestChargeService
    {
        /// <summary>
        /// Creates charges for a Service Request based on billable assignments
        /// Groups by BillingAccountId to prevent double billing
        /// Uses BillingRates to determine pricing per (BillingAccount, Expertise)
        /// </summary>
        Task CreateChargesForServiceRequestAsync(int serviceRequestId, decimal? systemDefaultAmount = null);

        /// <summary>
        /// Gets all Ready charges for a billing account
        /// </summary>
        Task<List<ServiceRequestCharge>> GetReadyChargesForBillingAccountAsync(
            long billingAccountId,
            DateTime? startDate = null, 
            DateTime? endDate = null);
    }

    public class ServiceRequestChargeService : IServiceRequestChargeService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ServiceRequestChargeService> _logger;
        private const decimal DEFAULT_SYSTEM_AMOUNT = 100.00m;

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
        /// 1. Get ServiceRequest with PrimaryExpertiseId
        /// 2. Get all billable assignments for the SR
        /// 3. Group by BillingAccountId (from User.BillingAccountId)
        /// 4. For each BillingAccount:
        ///    a. Determine PrimaryExpertiseId (from SR or auto-detect)
        ///    b. Look up BillingRate for (BillingAccountId, PrimaryExpertiseId)
        ///    c. Create charge with determined amount
        /// 5. Database unique constraint prevents duplicates
        /// </summary>
        public async Task CreateChargesForServiceRequestAsync(int serviceRequestId, decimal? systemDefaultAmount = null)
        {
            try
            {
                // Get ServiceRequest with expertise information
                var serviceRequest = await _context.ServiceRequests
                    .Include(sr => sr.Expertises)
                        .ThenInclude(sre => sre.Expertise)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

                if (serviceRequest == null)
                {
                    _logger.LogWarning("ServiceRequest {ServiceRequestId} not found", serviceRequestId);
                    return;
                }

                // Determine PrimaryExpertiseId
                int? primaryExpertiseId = serviceRequest.PrimaryExpertiseId;
                
                if (!primaryExpertiseId.HasValue)
                {
                    // Auto-detect: if SR has exactly 1 expertise, use it
                    var expertiseCount = serviceRequest.Expertises?.Count ?? 0;
                    if (expertiseCount == 1)
                    {
                        primaryExpertiseId = serviceRequest.Expertises!.First().ExpertiseId;
                        _logger.LogInformation(
                            "Auto-detected PrimaryExpertiseId {ExpertiseId} for ServiceRequest {ServiceRequestId} (single expertise)",
                            primaryExpertiseId, serviceRequestId);
                    }
                    else if (expertiseCount > 1)
                    {
                        _logger.LogWarning(
                            "ServiceRequest {ServiceRequestId} has {Count} expertise tags but no PrimaryExpertiseId. Coordinator must select primary expertise before billing.",
                            serviceRequestId, expertiseCount);
                        // Don't create charges if we can't determine pricing category
                        return;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "ServiceRequest {ServiceRequestId} has no expertise tags and no PrimaryExpertiseId. Cannot determine pricing.",
                            serviceRequestId);
                        return;
                    }
                }

                // Get all billable assignments for this SR
                var billableAssignments = await _context.ServiceRequestAssignments
                    .Include(a => a.SmeUser)
                    .Where(a => a.ServiceRequestId == serviceRequestId &&
                                a.IsActive &&
                                a.IsBillable &&
                                (a.Status == "Accepted" ||
                                 a.Status == "InProgress" ||
                                 a.Status == "Completed"))
                    .ToListAsync();

                if (!billableAssignments.Any())
                {
                    _logger.LogInformation(
                        "No billable assignments found for ServiceRequest {ServiceRequestId}",
                        serviceRequestId);
                    return;
                }

                // Group by BillingAccountId (from User.BillingAccountId)
                var billingGroups = billableAssignments
                    .Where(a => a.SmeUser.BillingAccountId.HasValue) // Only process SMEs with billing accounts
                    .GroupBy(a => a.SmeUser.BillingAccountId!.Value)
                    .ToList();

                if (!billingGroups.Any())
                {
                    _logger.LogWarning(
                        "No billable assignments with BillingAccountId found for ServiceRequest {ServiceRequestId}",
                        serviceRequestId);
                    return;
                }

                var defaultAmount = systemDefaultAmount ?? DEFAULT_SYSTEM_AMOUNT;
                var chargesCreated = 0;

                foreach (var group in billingGroups)
                {
                    try
                    {
                        var billingAccountId = group.Key;

                        // Check if charge already exists (unique constraint will prevent duplicates)
                        var existingCharge = await _context.ServiceRequestCharges
                            .FirstOrDefaultAsync(c =>
                                c.ServiceRequestId == serviceRequestId &&
                                c.BillingAccountId == billingAccountId);

                        if (existingCharge != null)
                        {
                            _logger.LogInformation(
                                "Charge already exists for ServiceRequest {ServiceRequestId}, BillingAccount {BillingAccountId}",
                                serviceRequestId, billingAccountId);
                            continue;
                        }

                        // Look up BillingRate for (BillingAccountId, PrimaryExpertiseId)
                        var billingRate = await _context.BillingRates
                            .FirstOrDefaultAsync(br =>
                                br.BillingAccountId == billingAccountId &&
                                br.ExpertiseId == primaryExpertiseId.Value &&
                                br.IsActive);

                        decimal chargeAmount;
                        string rateSource;

                        if (billingRate != null)
                        {
                            chargeAmount = billingRate.Amount;
                            rateSource = "BillingRate";
                            _logger.LogInformation(
                                "Using BillingRate {Amount} for BillingAccount {BillingAccountId}, Expertise {ExpertiseId}",
                                chargeAmount, billingAccountId, primaryExpertiseId.Value);
                        }
                        else
                        {
                            chargeAmount = defaultAmount;
                            rateSource = "Default";
                            _logger.LogInformation(
                                "No BillingRate found for BillingAccount {BillingAccountId}, Expertise {ExpertiseId}. Using default {Amount}",
                                billingAccountId, primaryExpertiseId.Value, defaultAmount);
                        }

                        // Create ONE charge per BillingAccount
                        var charge = new ServiceRequestCharge
                        {
                            ServiceRequestId = serviceRequestId,
                            BillingAccountId = billingAccountId,
                            ExpertiseId = primaryExpertiseId.Value,
                            RateSource = rateSource,
                            Amount = chargeAmount,
                            Status = ChargeStatus.Ready.ToString(),
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ServiceRequestCharges.Add(charge);
                        chargesCreated++;

                        _logger.LogInformation(
                            "Created charge for ServiceRequest {ServiceRequestId}, BillingAccount {BillingAccountId}, Expertise {ExpertiseId}, Amount {Amount}, Source {Source} with {AssignmentCount} assignments",
                            serviceRequestId, billingAccountId, primaryExpertiseId.Value, chargeAmount, rateSource, group.Count());
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UQ_Charge_SR_BillingAccount") == true ||
                                                       ex.InnerException?.Message?.Contains("UQ_SR_BillingAccount") == true)
                    {
                        // Unique constraint violation - charge already exists (race condition)
                        _logger.LogWarning(
                            "Charge already exists for ServiceRequest {ServiceRequestId}, BillingAccount {BillingAccountId} (unique constraint violation)",
                            serviceRequestId, group.Key);
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
        /// Gets all Ready charges for a billing account
        /// </summary>
        public async Task<List<ServiceRequestCharge>> GetReadyChargesForBillingAccountAsync(
            long billingAccountId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.ServiceRequestCharges
                .Include(c => c.ServiceRequest)
                    .ThenInclude(sr => sr.Client)
                .Include(c => c.Expertise)
                .Where(c => c.BillingAccountId == billingAccountId &&
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
