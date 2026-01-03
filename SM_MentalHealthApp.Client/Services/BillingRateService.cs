using System.Net.Http.Json;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services
{
    public class BillingRateService : BaseService, IBillingRateService
    {
        public BillingRateService(HttpClient httpClient, IAuthService authService) : base(httpClient, authService)
        {
        }

        public async Task<List<BillingRate>> GetBillingRatesAsync(long? billingAccountId = null, bool? isActive = null)
        {
            try
            {
                AddAuthorizationHeader();
                var queryParams = new List<string>();
                if (billingAccountId.HasValue)
                    queryParams.Add($"billingAccountId={billingAccountId.Value}");
                if (isActive.HasValue)
                    queryParams.Add($"isActive={isActive.Value}");

                var url = "api/BillingRate";
                if (queryParams.Any())
                    url += "?" + string.Join("&", queryParams);

                var response = await _http.GetFromJsonAsync<List<BillingRate>>(url);
                return response ?? new List<BillingRate>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting billing rates: {ex.Message}");
                return new List<BillingRate>();
            }
        }

        public async Task<BillingRate?> GetBillingRateByIdAsync(long id)
        {
            try
            {
                AddAuthorizationHeader();
                return await _http.GetFromJsonAsync<BillingRate>($"api/BillingRate/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting billing rate by ID: {ex.Message}");
                return null;
            }
        }

        public async Task<BillingRate?> GetBillingRateAsync(long billingAccountId, int expertiseId)
        {
            try
            {
                AddAuthorizationHeader();
                return await _http.GetFromJsonAsync<BillingRate>(
                    $"api/BillingRate/lookup?billingAccountId={billingAccountId}&expertiseId={expertiseId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting billing rate: {ex.Message}");
                return null;
            }
        }

        public async Task<BillingRate> CreateBillingRateAsync(CreateBillingRateRequest request)
        {
            AddAuthorizationHeader();
            var response = await _http.PostAsJsonAsync("api/BillingRate", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BillingRate>() 
                ?? throw new Exception("Failed to create billing rate");
        }

        public async Task<BillingRate?> UpdateBillingRateAsync(long id, UpdateBillingRateRequest request)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _http.PutAsJsonAsync($"api/BillingRate/{id}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BillingRate>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating billing rate: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteBillingRateAsync(long id)
        {
            try
            {
                AddAuthorizationHeader();
                var response = await _http.DeleteAsync($"api/BillingRate/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting billing rate: {ex.Message}");
                return false;
            }
        }
    }
}

