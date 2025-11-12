using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAIResponseTemplateService
    {
        Task<AIResponseTemplate?> GetTemplateByKeyAsync(string templateKey);
        Task<string> FormatTemplateAsync(string templateKey, Dictionary<string, string> placeholders);
        Task<List<AIResponseTemplate>> GetAllActiveTemplatesAsync();
        Task<AIResponseTemplate> CreateTemplateAsync(AIResponseTemplate template);
        Task<AIResponseTemplate> UpdateTemplateAsync(AIResponseTemplate template);
        Task<bool> DeleteTemplateAsync(int id);
    }
}

