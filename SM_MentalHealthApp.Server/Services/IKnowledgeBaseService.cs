using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IKnowledgeBaseService
    {
        Task<KnowledgeBaseEntry?> FindMatchingEntryAsync(string question);
        Task<List<KnowledgeBaseCategory>> GetActiveCategoriesAsync();
        Task<List<KnowledgeBaseEntry>> GetActiveEntriesAsync(int? categoryId = null);
        Task<KnowledgeBaseEntry?> GetEntryByIdAsync(int id);
        Task<KnowledgeBaseCategory?> GetCategoryByIdAsync(int id);
        Task<KnowledgeBaseEntry> CreateEntryAsync(KnowledgeBaseEntry entry);
        Task<KnowledgeBaseEntry> UpdateEntryAsync(KnowledgeBaseEntry entry);
        Task<bool> DeleteEntryAsync(int id);
        Task<KnowledgeBaseCategory> CreateCategoryAsync(KnowledgeBaseCategory category);
        Task<KnowledgeBaseCategory> UpdateCategoryAsync(KnowledgeBaseCategory category);
        Task<bool> DeleteCategoryAsync(int id);
    }
}

