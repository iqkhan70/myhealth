using System.Net.Http.Json;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services
{
    public class ExpertiseService : IExpertiseService
    {
        private readonly HttpClient _httpClient;

        public ExpertiseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Expertise>> GetAllExpertisesAsync(bool activeOnly = true)
        {
            try
            {
                var url = $"api/Expertise?activeOnly={activeOnly}";
                Console.WriteLine($"Fetching expertises from: {url}");
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response: {response.StatusCode} - {errorContent}");
                    throw new Exception($"Failed to get expertises: {response.StatusCode} - {errorContent}");
                }
                
                var result = await response.Content.ReadFromJsonAsync<List<Expertise>>();
                Console.WriteLine($"Successfully loaded {result?.Count ?? 0} expertises");
                return result ?? new List<Expertise>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting expertises: {ex}");
                throw; // Re-throw to let the UI handle it
            }
        }

        public async Task<Expertise?> GetExpertiseByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Expertise/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Expertise>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting expertise {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<Expertise> CreateExpertiseAsync(string name, string? description = null)
        {
            var request = new { Name = name, Description = description };
            var response = await _httpClient.PostAsJsonAsync("api/Expertise", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Expertise>() ?? throw new Exception("Failed to create expertise");
        }

        public async Task<Expertise?> UpdateExpertiseAsync(int id, string name, string? description = null, bool? isActive = null)
        {
            var request = new { Name = name, Description = description, IsActive = isActive };
            var response = await _httpClient.PutAsJsonAsync($"api/Expertise/{id}", request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Expertise>();
        }

        public async Task<bool> DeleteExpertiseAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Expertise/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<int>> GetSmeExpertisesAsync(int smeUserId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Expertise/sme/{smeUserId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<int>>() ?? new List<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting SME expertises: {ex.Message}");
                return new List<int>();
            }
        }

        public async Task<bool> SetSmeExpertisesAsync(int smeUserId, List<int> expertiseIds)
        {
            try
            {
                var request = new { ExpertiseIds = expertiseIds };
                var response = await _httpClient.PostAsJsonAsync($"api/Expertise/sme/{smeUserId}", request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<int>> GetServiceRequestExpertisesAsync(int serviceRequestId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Expertise/service-request/{serviceRequestId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<int>>() ?? new List<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting ServiceRequest expertises: {ex.Message}");
                return new List<int>();
            }
        }
    }
}

