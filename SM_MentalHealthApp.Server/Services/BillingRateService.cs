using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    /// <summary>
    /// Service for managing BillingRates
    /// </summary>
    public interface IBillingRateService
    {
        Task<List<BillingRate>> GetBillingRatesAsync(long? billingAccountId = null, bool? isActive = null);
        Task<BillingRate?> GetBillingRateByIdAsync(long id);
        Task<BillingRate> CreateBillingRateAsync(CreateBillingRateRequest request);
        Task<BillingRate?> UpdateBillingRateAsync(long id, UpdateBillingRateRequest request);
        Task<bool> DeleteBillingRateAsync(long id);
        Task<BillingRate?> GetBillingRateAsync(long billingAccountId, int expertiseId);
    }

    public class BillingRateService : IBillingRateService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<BillingRateService> _logger;

        public BillingRateService(
            JournalDbContext context,
            ILogger<BillingRateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get billing rates, optionally filtered by billing account and active status
        /// </summary>
        public async Task<List<BillingRate>> GetBillingRatesAsync(long? billingAccountId = null, bool? isActive = null)
        {
            try
            {
                var query = _context.BillingRates
                    .Include(br => br.BillingAccount)
                    .Include(br => br.Expertise)
                    .AsQueryable();

                if (billingAccountId.HasValue)
                {
                    query = query.Where(br => br.BillingAccountId == billingAccountId.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(br => br.IsActive == isActive.Value);
                }

                return await query
                    .OrderBy(br => br.BillingAccountId)
                    .ThenBy(br => br.Expertise.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing rates");
                throw;
            }
        }

        /// <summary>
        /// Get a billing rate by ID
        /// </summary>
        public async Task<BillingRate?> GetBillingRateByIdAsync(long id)
        {
            try
            {
                return await _context.BillingRates
                    .Include(br => br.BillingAccount)
                    .Include(br => br.Expertise)
                    .FirstOrDefaultAsync(br => br.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing rate by ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Get a billing rate by BillingAccountId and ExpertiseId
        /// </summary>
        public async Task<BillingRate?> GetBillingRateAsync(long billingAccountId, int expertiseId)
        {
            try
            {
                return await _context.BillingRates
                    .Include(br => br.BillingAccount)
                    .Include(br => br.Expertise)
                    .FirstOrDefaultAsync(br => 
                        br.BillingAccountId == billingAccountId && 
                        br.ExpertiseId == expertiseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing rate for BillingAccount {BillingAccountId}, Expertise {ExpertiseId}", 
                    billingAccountId, expertiseId);
                throw;
            }
        }

        /// <summary>
        /// Create a new billing rate
        /// </summary>
        public async Task<BillingRate> CreateBillingRateAsync(CreateBillingRateRequest request)
        {
            try
            {
                // Verify billing account exists
                var billingAccount = await _context.BillingAccounts
                    .FirstOrDefaultAsync(ba => ba.Id == request.BillingAccountId && ba.IsActive);

                if (billingAccount == null)
                {
                    throw new ArgumentException($"BillingAccount with ID {request.BillingAccountId} not found or inactive");
                }

                // Verify expertise exists
                var expertise = await _context.Expertises
                    .FirstOrDefaultAsync(e => e.Id == request.ExpertiseId && e.IsActive);

                if (expertise == null)
                {
                    throw new ArgumentException($"Expertise with ID {request.ExpertiseId} not found or inactive");
                }

                // Check if rate already exists for this (BillingAccount, Expertise) combination
                var existingRate = await _context.BillingRates
                    .FirstOrDefaultAsync(br => 
                        br.BillingAccountId == request.BillingAccountId && 
                        br.ExpertiseId == request.ExpertiseId);

                if (existingRate != null)
                {
                    throw new InvalidOperationException(
                        $"A billing rate already exists for BillingAccount {request.BillingAccountId} and Expertise {request.ExpertiseId}. " +
                        "Use Update instead.");
                }

                var billingRate = new BillingRate
                {
                    BillingAccountId = request.BillingAccountId,
                    ExpertiseId = request.ExpertiseId,
                    Amount = request.Amount,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BillingRates.Add(billingRate);
                await _context.SaveChangesAsync();

                // Reload with includes
                return await GetBillingRateByIdAsync(billingRate.Id) ?? billingRate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating billing rate");
                throw;
            }
        }

        /// <summary>
        /// Update an existing billing rate
        /// </summary>
        public async Task<BillingRate?> UpdateBillingRateAsync(long id, UpdateBillingRateRequest request)
        {
            try
            {
                var billingRate = await _context.BillingRates
                    .FirstOrDefaultAsync(br => br.Id == id);

                if (billingRate == null)
                {
                    return null;
                }

                // If ExpertiseId is being changed, check for conflicts
                if (request.ExpertiseId.HasValue && request.ExpertiseId.Value != billingRate.ExpertiseId)
                {
                    var existingRate = await _context.BillingRates
                        .FirstOrDefaultAsync(br => 
                            br.BillingAccountId == billingRate.BillingAccountId && 
                            br.ExpertiseId == request.ExpertiseId.Value &&
                            br.Id != id);

                    if (existingRate != null)
                    {
                        throw new InvalidOperationException(
                            $"A billing rate already exists for BillingAccount {billingRate.BillingAccountId} and Expertise {request.ExpertiseId.Value}");
                    }

                    // Verify new expertise exists
                    var expertise = await _context.Expertises
                        .FirstOrDefaultAsync(e => e.Id == request.ExpertiseId.Value && e.IsActive);

                    if (expertise == null)
                    {
                        throw new ArgumentException($"Expertise with ID {request.ExpertiseId.Value} not found or inactive");
                    }

                    billingRate.ExpertiseId = request.ExpertiseId.Value;
                }

                if (request.Amount.HasValue)
                {
                    billingRate.Amount = request.Amount.Value;
                }

                if (request.IsActive.HasValue)
                {
                    billingRate.IsActive = request.IsActive.Value;
                }

                await _context.SaveChangesAsync();

                // Reload with includes
                return await GetBillingRateByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating billing rate: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Delete (deactivate) a billing rate
        /// </summary>
        public async Task<bool> DeleteBillingRateAsync(long id)
        {
            try
            {
                var billingRate = await _context.BillingRates
                    .FirstOrDefaultAsync(br => br.Id == id);

                if (billingRate == null)
                {
                    return false;
                }

                // Soft delete by deactivating
                billingRate.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("BillingRate {Id} deactivated", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting billing rate: {Id}", id);
                throw;
            }
        }
    }
}

