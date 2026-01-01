using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services
{
    public interface IExpertiseService
    {
        Task<List<Expertise>> GetAllExpertisesAsync(bool activeOnly = true);
        Task<Expertise?> GetExpertiseByIdAsync(int id);
        Task<Expertise> CreateExpertiseAsync(string name, string? description = null);
        Task<Expertise?> UpdateExpertiseAsync(int id, string name, string? description = null, bool? isActive = null);
        Task<bool> DeleteExpertiseAsync(int id);
        Task<List<int>> GetSmeExpertisesAsync(int smeUserId);
        Task<bool> SetSmeExpertisesAsync(int smeUserId, List<int> expertiseIds);
        Task<List<int>> GetServiceRequestExpertisesAsync(int serviceRequestId);
    }
}

