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

            var lowerContent = messageContent.ToLower().Trim();

            // Get patterns from database
            var patterns = await GetActivePatternsAsync();
            if (!patterns.Any())
            {
                // Fallback to hardcoded patterns if database is empty
                _logger.LogWarning("No generic question patterns found in database, using fallback");
                return IsGenericKnowledgeQuestionFallback(messageContent);
            }

            // Check if it's a question - either has question mark OR starts with question words
            bool hasQuestionMark = lowerContent.Contains("?");
            bool startsWithQuestionWord = lowerContent.StartsWith("what ") ||
                                         lowerContent.StartsWith("how ") ||
                                         lowerContent.StartsWith("why ") ||
                                         lowerContent.StartsWith("when ") ||
                                         lowerContent.StartsWith("where ") ||
                                         lowerContent.StartsWith("who ") ||
                                         lowerContent.StartsWith("which ") ||
                                         lowerContent.StartsWith("explain ") ||
                                         lowerContent.StartsWith("tell me ") ||
                                         lowerContent.StartsWith("describe ");
            
            bool isQuestion = hasQuestionMark || startsWithQuestionWord;

            // Check if it matches any pattern (prioritize higher priority patterns first)
            // Sort patterns by priority descending to check most specific patterns first
            var sortedPatterns = patterns.OrderByDescending(p => p.Priority).ThenBy(p => p.Pattern);
            bool matchesGenericPattern = sortedPatterns.Any(pattern => 
            {
                var patternLower = pattern.Pattern.ToLower();
                // Check if pattern appears at the start (most common case)
                if (lowerContent.StartsWith(patternLower))
                    return true;
                
                // Check if pattern appears as a complete phrase (with word boundaries)
                // This handles cases like "what are normal blood pressure values"
                var patternWithSpace = " " + patternLower;
                if (lowerContent.Contains(patternWithSpace))
                    return true;
                
                // Check if pattern appears at the end
                if (lowerContent.EndsWith(" " + patternLower) || lowerContent.EndsWith(patternLower))
                    return true;
                
                return false;
            });

            // Check if it's asking about general information (not patient-specific)
            bool isGeneralInfo = lowerContent.Contains("in general") ||
                                lowerContent.Contains("generally") ||
                                (matchesGenericPattern &&
                                 !lowerContent.Contains("my ") &&
                                 !lowerContent.Contains(" my") &&
                                 !lowerContent.Contains("patient") &&
                                 !lowerContent.Contains("i have") &&
                                 !lowerContent.Contains("i am") &&
                                 !lowerContent.Contains("i feel") &&
                                 !lowerContent.Contains("i'm ") &&
                                 !lowerContent.Contains("i've "));

            // Return true if it's a question AND matches pattern OR is general info
            // Also allow if it matches pattern even without explicit question mark (for chat interfaces)
            return (isQuestion && (matchesGenericPattern || isGeneralInfo)) || 
                   (matchesGenericPattern && isGeneralInfo);
        }

        // Fallback method with hardcoded patterns (for when database is empty)
        private bool IsGenericKnowledgeQuestionFallback(string messageContent)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
                return false;

            var lowerContent = messageContent.ToLower().Trim();

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

            // Check if it's a question - either has question mark OR starts with question words
            bool hasQuestionMark = lowerContent.Contains("?");
            bool startsWithQuestionWord = lowerContent.StartsWith("what ") ||
                                         lowerContent.StartsWith("how ") ||
                                         lowerContent.StartsWith("why ") ||
                                         lowerContent.StartsWith("when ") ||
                                         lowerContent.StartsWith("where ") ||
                                         lowerContent.StartsWith("who ") ||
                                         lowerContent.StartsWith("which ") ||
                                         lowerContent.StartsWith("explain ") ||
                                         lowerContent.StartsWith("tell me ") ||
                                         lowerContent.StartsWith("describe ");
            
            bool isQuestion = hasQuestionMark || startsWithQuestionWord;
            
            // Check if it matches any pattern
            bool matchesGenericPattern = genericQuestionPatterns.Any(pattern => 
            {
                // Check if pattern appears at the start (most common case)
                if (lowerContent.StartsWith(pattern))
                    return true;
                
                // Check if pattern appears as a complete phrase (with word boundaries)
                var patternWithSpace = " " + pattern;
                if (lowerContent.Contains(patternWithSpace))
                    return true;
                
                // Check if pattern appears at the end
                if (lowerContent.EndsWith(" " + pattern) || lowerContent.EndsWith(pattern))
                    return true;
                
                return false;
            });

            bool isGeneralInfo = lowerContent.Contains("in general") ||
                                lowerContent.Contains("generally") ||
                                (matchesGenericPattern &&
                                 !lowerContent.Contains("my ") &&
                                 !lowerContent.Contains(" my") &&
                                 !lowerContent.Contains("patient") &&
                                 !lowerContent.Contains("i have") &&
                                 !lowerContent.Contains("i am") &&
                                 !lowerContent.Contains("i feel") &&
                                 !lowerContent.Contains("i'm ") &&
                                 !lowerContent.Contains("i've "));

            // Return true if it's a question AND matches pattern OR is general info
            // Also allow if it matches pattern even without explicit question mark
            return (isQuestion && (matchesGenericPattern || isGeneralInfo)) || 
                   (matchesGenericPattern && isGeneralInfo);
        }
    }
}

