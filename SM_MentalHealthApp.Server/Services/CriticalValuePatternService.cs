using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface ICriticalValuePatternService
    {
        Task<List<CriticalValuePattern>> GetActivePatternsAsync();
        Task<List<CriticalValuePattern>> GetPatternsByCategoryAsync(string categoryName);
        Task<bool> MatchesAnyPatternAsync(string text, string? categoryName = null);
    }

    public class CriticalValuePatternService : ICriticalValuePatternService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<CriticalValuePatternService> _logger;
        private static List<CriticalValuePattern>? _cachedPatterns;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public CriticalValuePatternService(JournalDbContext context, ILogger<CriticalValuePatternService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CriticalValuePattern>> GetActivePatternsAsync()
        {
            // Use caching to avoid hitting the database on every request
            if (_cachedPatterns != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedPatterns;
            }

            try
            {
                var patterns = await _context.CriticalValuePatterns
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && (p.Category == null || p.Category.IsActive))
                    .ToListAsync();

                _cachedPatterns = patterns;
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                return patterns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading critical value patterns from database");
                return new List<CriticalValuePattern>();
            }
        }

        public async Task<List<CriticalValuePattern>> GetPatternsByCategoryAsync(string categoryName)
        {
            var allPatterns = await GetActivePatternsAsync();
            return allPatterns
                .Where(p => p.Category != null && 
                           p.Category.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<bool> MatchesAnyPatternAsync(string text, string? categoryName = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var patterns = categoryName != null
                ? await GetPatternsByCategoryAsync(categoryName)
                : await GetActivePatternsAsync();

            foreach (var pattern in patterns)
            {
                try
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(
                        text,
                        pattern.Pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid regex pattern {PatternId}: {Pattern}", pattern.Id, pattern.Pattern);
                }
            }

            return false;
        }
    }
}

