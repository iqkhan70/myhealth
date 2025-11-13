using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface ICriticalValueKeywordService
    {
        Task<List<CriticalValueKeyword>> GetActiveKeywordsAsync();
        Task<List<CriticalValueKeyword>> GetKeywordsByCategoryAsync(string categoryName);
        Task<bool> ContainsAnyKeywordAsync(string text, string? categoryName = null);
        Task<int> CountKeywordsInTextAsync(string text, string categoryName);
        Task<List<string>> GetKeywordsListByCategoryAsync(string categoryName);
    }

    public class CriticalValueKeywordService : ICriticalValueKeywordService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<CriticalValueKeywordService> _logger;
        private static List<CriticalValueKeyword>? _cachedKeywords;
        private static Dictionary<string, List<string>>? _cachedLowercaseKeywordsByCategory; // Category -> List of lowercase keywords
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public CriticalValueKeywordService(JournalDbContext context, ILogger<CriticalValueKeywordService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CriticalValueKeyword>> GetActiveKeywordsAsync()
        {
            // Use caching to avoid hitting the database on every request
            if (_cachedKeywords != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedKeywords;
            }

            try
            {
                var keywords = await _context.CriticalValueKeywords
                    .Include(k => k.Category)
                    .Where(k => k.IsActive && (k.Category == null || k.Category.IsActive))
                    .ToListAsync();

                _cachedKeywords = keywords;
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                
                // Pre-compute lowercase keywords by category for faster matching
                _cachedLowercaseKeywordsByCategory = keywords
                    .GroupBy(k => k.Category?.Name ?? "Uncategorized")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(k => k.Keyword.ToLowerInvariant()).ToList()
                    );
                
                _logger.LogInformation("Loaded {Count} active keywords from database. Cache expires at {Expiry}", 
                    keywords.Count, _cacheExpiry);

                return keywords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading critical value keywords from database");
                return new List<CriticalValueKeyword>();
            }
        }
        
        /// <summary>
        /// Clears the keyword cache - useful after seeding new keywords
        /// </summary>
        public static void ClearCache()
        {
            _cachedKeywords = null;
            _cachedLowercaseKeywordsByCategory = null;
            _cacheExpiry = DateTime.MinValue;
        }

        public async Task<List<CriticalValueKeyword>> GetKeywordsByCategoryAsync(string categoryName)
        {
            var allKeywords = await GetActiveKeywordsAsync();
            var categoryKeywords = allKeywords
                .Where(k => k.Category != null && 
                           k.Category.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            _logger.LogDebug("Found {Count} keywords for category '{CategoryName}'", categoryKeywords.Count, categoryName);
            
            return categoryKeywords;
        }

        public async Task<bool> ContainsAnyKeywordAsync(string text, string? categoryName = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Ensure cache is loaded
            await GetActiveKeywordsAsync();
            
            // Use pre-computed lowercase keywords if available for better performance
            List<string>? lowercaseKeywords = null;
            if (_cachedLowercaseKeywordsByCategory != null)
            {
                if (categoryName != null)
                {
                    _cachedLowercaseKeywordsByCategory.TryGetValue(categoryName, out lowercaseKeywords);
                }
                else
                {
                    // If no category specified, check all categories
                    lowercaseKeywords = _cachedLowercaseKeywordsByCategory.Values.SelectMany(v => v).ToList();
                }
            }

            // Fallback to original method if cache not available
            if (lowercaseKeywords == null || !lowercaseKeywords.Any())
            {
                var keywords = categoryName != null
                    ? await GetKeywordsByCategoryAsync(categoryName)
                    : await GetActiveKeywordsAsync();

                if (keywords == null || !keywords.Any())
                {
                    _logger.LogWarning("No keywords found for category: {CategoryName}", categoryName ?? "ALL");
                    return false;
                }

                lowercaseKeywords = keywords.Select(k => k.Keyword.ToLowerInvariant()).ToList();
            }

            var lowerText = text.ToLowerInvariant();
            
            // Use pre-computed lowercase keywords for faster matching
            foreach (var keywordLower in lowercaseKeywords)
            {
                // Case-insensitive contains check
                if (lowerText.Contains(keywordLower))
                {
                    _logger.LogDebug("Keyword match found: '{Keyword}' in category '{Category}'", keywordLower, categoryName ?? "ALL");
                    return true;
                }
            }

            return false;
        }

        public async Task<int> CountKeywordsInTextAsync(string text, string categoryName)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var keywords = await GetKeywordsByCategoryAsync(categoryName);
            var lowerText = text.ToLowerInvariant();
            
            return keywords.Count(k => lowerText.Contains(k.Keyword.ToLowerInvariant()));
        }

        public async Task<List<string>> GetKeywordsListByCategoryAsync(string categoryName)
        {
            var keywords = await GetKeywordsByCategoryAsync(categoryName);
            return keywords.Select(k => k.Keyword).ToList();
        }
    }
}

