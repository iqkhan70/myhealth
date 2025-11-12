using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public class GenericQuestionPatternService : IGenericQuestionPatternService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<GenericQuestionPatternService> _logger;
        private static List<GenericQuestionPattern>? _cachedPatterns;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public GenericQuestionPatternService(JournalDbContext context, ILogger<GenericQuestionPatternService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<GenericQuestionPattern>> GetActivePatternsAsync()
        {
            // Use caching to avoid hitting the database on every request
            if (_cachedPatterns != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedPatterns;
            }

            try
            {
                var patterns = await _context.GenericQuestionPatterns
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.Priority)
                    .ThenBy(p => p.Pattern)
                    .ToListAsync();

                _cachedPatterns = patterns;
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                return patterns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading generic question patterns from database");
                return new List<GenericQuestionPattern>();
            }
        }

        public async Task<GenericQuestionPattern?> GetPatternByIdAsync(int id)
        {
            try
            {
                return await _context.GenericQuestionPatterns.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting generic question pattern by ID {PatternId}", id);
                return null;
            }
        }

        public async Task<GenericQuestionPattern> CreatePatternAsync(GenericQuestionPattern pattern)
        {
            _context.GenericQuestionPatterns.Add(pattern);
            await _context.SaveChangesAsync();
            _cachedPatterns = null; // Invalidate cache
            return pattern;
        }

        public async Task<GenericQuestionPattern> UpdatePatternAsync(GenericQuestionPattern pattern)
        {
            _context.Entry(pattern).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _cachedPatterns = null; // Invalidate cache
            return pattern;
        }

        public async Task DeletePatternAsync(int id)
        {
            var pattern = await _context.GenericQuestionPatterns.FindAsync(id);
            if (pattern != null)
            {
                _context.GenericQuestionPatterns.Remove(pattern);
                await _context.SaveChangesAsync();
                _cachedPatterns = null; // Invalidate cache
            }
        }

        public async Task<bool> IsGenericKnowledgeQuestionAsync(string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
                return false;

            var lowerContent = messageContent.ToLower();

            // Get patterns from database
            var patterns = await GetActivePatternsAsync();
            if (!patterns.Any())
            {
                // Fallback to hardcoded patterns if database is empty
                _logger.LogWarning("No generic question patterns found in database, using fallback");
                return IsGenericKnowledgeQuestionFallback(messageContent);
            }

            // Check if it's a question (contains ?)
            bool isQuestion = lowerContent.Contains("?");

            // Check if it matches any pattern
            bool matchesGenericPattern = patterns.Any(pattern => lowerContent.Contains(pattern.Pattern.ToLower()));

            // Also check if it's asking about general information (not patient-specific)
            bool isGeneralInfo = lowerContent.Contains("in general") ||
                                lowerContent.Contains("generally") ||
                                (isQuestion && matchesGenericPattern &&
                                 !lowerContent.Contains("my") &&
                                 !lowerContent.Contains("patient") &&
                                 !lowerContent.Contains("i have") &&
                                 !lowerContent.Contains("i am") &&
                                 !lowerContent.Contains("i feel"));

            return isQuestion && (matchesGenericPattern || isGeneralInfo);
        }

        // Fallback method with hardcoded patterns (for when database is empty)
        private bool IsGenericKnowledgeQuestionFallback(string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
                return false;

            var lowerContent = messageContent.ToLower();

            var genericQuestionPatterns = new[]
            {
                "what are normal",
                "what are the normal",
                "what is normal",
                "what are critical",
                "what are serious",
                "what is a normal",
                "what are typical",
                "what is typical",
                "normal values of",
                "normal range of",
                "normal levels of",
                "what does",
                "how does",
                "explain",
                "tell me about"
            };

            bool isQuestion = lowerContent.Contains("?");
            bool matchesGenericPattern = genericQuestionPatterns.Any(pattern => lowerContent.Contains(pattern));

            bool isGeneralInfo = lowerContent.Contains("in general") ||
                                lowerContent.Contains("generally") ||
                                (isQuestion && matchesGenericPattern &&
                                 !lowerContent.Contains("my") &&
                                 !lowerContent.Contains("patient") &&
                                 !lowerContent.Contains("i have") &&
                                 !lowerContent.Contains("i am") &&
                                 !lowerContent.Contains("i feel"));

            return isQuestion && (matchesGenericPattern || isGeneralInfo);
        }
    }
}

