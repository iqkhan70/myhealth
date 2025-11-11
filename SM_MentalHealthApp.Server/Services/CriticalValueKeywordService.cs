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

                return keywords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading critical value keywords from database");
                return new List<CriticalValueKeyword>();
            }
        }

        public async Task<List<CriticalValueKeyword>> GetKeywordsByCategoryAsync(string categoryName)
        {
            var allKeywords = await GetActiveKeywordsAsync();
            return allKeywords
                .Where(k => k.Category != null && 
                           k.Category.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<bool> ContainsAnyKeywordAsync(string text, string? categoryName = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var keywords = categoryName != null
                ? await GetKeywordsByCategoryAsync(categoryName)
                : await GetActiveKeywordsAsync();

            var lowerText = text.ToLowerInvariant();
            
            foreach (var keyword in keywords)
            {
                // Case-insensitive contains check
                if (lowerText.Contains(keyword.Keyword.ToLowerInvariant()))
                {
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

