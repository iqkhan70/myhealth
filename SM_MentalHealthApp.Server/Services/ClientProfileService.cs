using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public class ClientProfileService : IClientProfileService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ClientProfileService> _logger;

        public ClientProfileService(JournalDbContext context, ILogger<ClientProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ClientProfile?> GetProfileAsync(int clientId)
        {
            try
            {
                return await _context.ClientProfiles
                    .Include(p => p.InteractionPatterns)
                    .Include(p => p.KeywordReactions)
                    .Include(p => p.ServicePreferences)
                    .FirstOrDefaultAsync(p => p.ClientId == clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client profile for client {ClientId}", clientId);
                return null;
            }
        }

        public async Task<ClientProfile> GetOrCreateProfileAsync(int clientId)
        {
            var profile = await GetProfileAsync(clientId);
            
            if (profile == null)
            {
                profile = new ClientProfile
                {
                    ClientId = clientId,
                    CommunicationStyle = "Balanced",
                    InformationTolerance = 0.5m,
                    EmotionalSensitivity = 0.5m,
                    PreferredTone = "Supportive",
                    TotalInteractions = 0,
                    SuccessfulResolutions = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ClientProfiles.Add(profile);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created new client profile for client {ClientId}", clientId);
            }

            return profile;
        }

        public async Task<ClientProfile> UpdateProfileAsync(ClientProfile profile)
        {
            try
            {
                profile.LastUpdated = DateTime.UtcNow;
                _context.ClientProfiles.Update(profile);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated client profile for client {ClientId}", profile.ClientId);
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client profile for client {ClientId}", profile.ClientId);
                throw;
            }
        }

        public async Task<List<ClientInteractionPattern>> GetInteractionPatternsAsync(int clientId, string? patternType = null)
        {
            try
            {
                var query = _context.ClientInteractionPatterns
                    .Where(p => p.ClientId == clientId);

                if (!string.IsNullOrEmpty(patternType))
                {
                    query = query.Where(p => p.PatternType == patternType);
                }

                return await query
                    .OrderByDescending(p => p.LastObserved)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interaction patterns for client {ClientId}", clientId);
                return new List<ClientInteractionPattern>();
            }
        }

        public async Task<ClientInteractionPattern> AddOrUpdatePatternAsync(int clientId, string patternType, string? patternData, decimal confidence)
        {
            try
            {
                var existing = await _context.ClientInteractionPatterns
                    .FirstOrDefaultAsync(p => p.ClientId == clientId && p.PatternType == patternType);

                if (existing != null)
                {
                    // Update existing pattern
                    existing.PatternData = patternData;
                    existing.Confidence = (existing.Confidence + confidence) / 2; // Average confidence
                    existing.OccurrenceCount++;
                    existing.LastObserved = DateTime.UtcNow;
                    
                    _context.ClientInteractionPatterns.Update(existing);
                    await _context.SaveChangesAsync();
                    return existing;
                }
                else
                {
                    // Create new pattern
                    var newPattern = new ClientInteractionPattern
                    {
                        ClientId = clientId,
                        PatternType = patternType,
                        PatternData = patternData,
                        Confidence = confidence,
                        OccurrenceCount = 1,
                        LastObserved = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ClientInteractionPatterns.Add(newPattern);
                    await _context.SaveChangesAsync();
                    return newPattern;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating interaction pattern for client {ClientId}", clientId);
                throw;
            }
        }

        public async Task<List<ClientKeywordReaction>> GetKeywordReactionsAsync(int clientId)
        {
            try
            {
                return await _context.ClientKeywordReactions
                    .Where(k => k.ClientId == clientId)
                    .OrderByDescending(k => k.ReactionScore)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting keyword reactions for client {ClientId}", clientId);
                return new List<ClientKeywordReaction>();
            }
        }

        public async Task<ClientKeywordReaction> AddOrUpdateKeywordReactionAsync(int clientId, string keyword, int scoreDelta)
        {
            try
            {
                var normalizedKeyword = keyword.ToLowerInvariant().Trim();
                var existing = await _context.ClientKeywordReactions
                    .FirstOrDefaultAsync(k => k.ClientId == clientId && k.Keyword == normalizedKeyword);

                if (existing != null)
                {
                    existing.ReactionScore += scoreDelta;
                    existing.OccurrenceCount++;
                    existing.LastSeen = DateTime.UtcNow;
                    
                    _context.ClientKeywordReactions.Update(existing);
                    await _context.SaveChangesAsync();
                    return existing;
                }
                else
                {
                    var newReaction = new ClientKeywordReaction
                    {
                        ClientId = clientId,
                        Keyword = normalizedKeyword,
                        ReactionScore = scoreDelta,
                        OccurrenceCount = 1,
                        LastSeen = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ClientKeywordReactions.Add(newReaction);
                    await _context.SaveChangesAsync();
                    return newReaction;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating keyword reaction for client {ClientId}", clientId);
                throw;
            }
        }

        public async Task<List<ClientServicePreference>> GetServicePreferencesAsync(int clientId)
        {
            try
            {
                return await _context.ClientServicePreferences
                    .Where(s => s.ClientId == clientId)
                    .OrderByDescending(s => s.PreferenceScore)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service preferences for client {ClientId}", clientId);
                return new List<ClientServicePreference>();
            }
        }

        public async Task<ClientServicePreference> AddOrUpdateServicePreferenceAsync(int clientId, string serviceType, decimal? successRate = null)
        {
            try
            {
                var normalizedServiceType = serviceType.Trim();
                var existing = await _context.ClientServicePreferences
                    .FirstOrDefaultAsync(s => s.ClientId == clientId && s.ServiceType == normalizedServiceType);

                if (existing != null)
                {
                    existing.RequestCount++;
                    existing.LastRequestDate = DateTime.UtcNow;
                    
                    if (successRate.HasValue)
                    {
                        // Update success rate (weighted average)
                        if (existing.SuccessRate.HasValue)
                        {
                            var totalRequests = existing.RequestCount - 1; // Before increment
                            existing.SuccessRate = ((existing.SuccessRate.Value * totalRequests) + successRate.Value) / existing.RequestCount;
                        }
                        else
                        {
                            existing.SuccessRate = successRate.Value;
                        }
                    }
                    
                    // Increase preference score slightly with each successful request
                    if (successRate.HasValue && successRate.Value > 0.7m)
                    {
                        existing.PreferenceScore = Math.Min(1.0m, existing.PreferenceScore + 0.05m);
                    }
                    
                    existing.UpdatedAt = DateTime.UtcNow;
                    
                    _context.ClientServicePreferences.Update(existing);
                    await _context.SaveChangesAsync();
                    return existing;
                }
                else
                {
                    var newPreference = new ClientServicePreference
                    {
                        ClientId = clientId,
                        ServiceType = normalizedServiceType,
                        PreferenceScore = 0.5m,
                        RequestCount = 1,
                        SuccessRate = successRate,
                        LastRequestDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.ClientServicePreferences.Add(newPreference);
                    await _context.SaveChangesAsync();
                    return newPreference;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/updating service preference for client {ClientId}", clientId);
                throw;
            }
        }

        public async Task<ClientInteractionHistory> AddInteractionHistoryAsync(ClientInteractionHistory history)
        {
            try
            {
                history.CreatedAt = DateTime.UtcNow;
                _context.ClientInteractionHistories.Add(history);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Added interaction history for client {ClientId}", history.ClientId);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding interaction history for client {ClientId}", history.ClientId);
                throw;
            }
        }

        public async Task<List<ClientInteractionHistory>> GetRecentInteractionHistoryAsync(int clientId, int limit = 50)
        {
            try
            {
                return await _context.ClientInteractionHistories
                    .Where(h => h.ClientId == clientId)
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interaction history for client {ClientId}", clientId);
                return new List<ClientInteractionHistory>();
            }
        }
    }
}

