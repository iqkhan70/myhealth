using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services
{
    public interface IBillingRateService
    {
        Task<List<BillingRate>> GetBillingRatesAsync(long? billingAccountId = null, bool? isActive = null);
        Task<BillingRate?> GetBillingRateByIdAsync(long id);
        Task<BillingRate?> GetBillingRateAsync(long billingAccountId, int expertiseId);
        Task<BillingRate> CreateBillingRateAsync(CreateBillingRateRequest request);
        Task<BillingRate?> UpdateBillingRateAsync(long id, UpdateBillingRateRequest request);
        Task<bool> DeleteBillingRateAsync(long id);
    }
}

