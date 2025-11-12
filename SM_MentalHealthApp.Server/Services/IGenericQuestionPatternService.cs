using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IGenericQuestionPatternService
    {
        Task<List<GenericQuestionPattern>> GetActivePatternsAsync();
        Task<GenericQuestionPattern?> GetPatternByIdAsync(int id);
        Task<GenericQuestionPattern> CreatePatternAsync(GenericQuestionPattern pattern);
        Task<GenericQuestionPattern> UpdatePatternAsync(GenericQuestionPattern pattern);
        Task DeletePatternAsync(int id);
        Task<bool> IsGenericKnowledgeQuestionAsync(string messageContent);
    }
}

