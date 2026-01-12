using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IClientProfileService
    {
        Task<ClientProfile?> GetProfileAsync(int clientId);
        Task<ClientProfile> GetOrCreateProfileAsync(int clientId);
        Task<ClientProfile> UpdateProfileAsync(ClientProfile profile);
        Task<List<ClientInteractionPattern>> GetInteractionPatternsAsync(int clientId, string? patternType = null);
        Task<ClientInteractionPattern> AddOrUpdatePatternAsync(int clientId, string patternType, string? patternData, decimal confidence);
        Task<List<ClientKeywordReaction>> GetKeywordReactionsAsync(int clientId);
        Task<ClientKeywordReaction> AddOrUpdateKeywordReactionAsync(int clientId, string keyword, int scoreDelta);
        Task<List<ClientServicePreference>> GetServicePreferencesAsync(int clientId);
        Task<ClientServicePreference> AddOrUpdateServicePreferenceAsync(int clientId, string serviceType, decimal? successRate = null);
        Task<ClientInteractionHistory> AddInteractionHistoryAsync(ClientInteractionHistory history);
        Task<List<ClientInteractionHistory>> GetRecentInteractionHistoryAsync(int clientId, int limit = 50);
    }
}

