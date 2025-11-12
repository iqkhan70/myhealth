using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<KnowledgeBaseService> _logger;
        private static List<KnowledgeBaseEntry>? _cachedEntries;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public KnowledgeBaseService(JournalDbContext context, ILogger<KnowledgeBaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<KnowledgeBaseEntry?> FindMatchingEntryAsync(string question)
        {
            try
            {
                var entries = await GetActiveEntriesAsync();
                if (!entries.Any())
                    return null;

                var questionLower = question.ToLowerInvariant().Trim();

                // Find entries where keywords match the question
                // Keywords can be comma-separated or JSON array
                var bestMatch = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Keywords))
                    .Select(e => new
                    {
                        Entry = e,
                        Keywords = ParseKeywords(e.Keywords)
                    })
                    .Where(x => x.Keywords.Any())
                    .Select(x => new
                    {
                        x.Entry,
                        x.Keywords,
                        MatchCount = x.Keywords.Count(k => questionLower.Contains(k.ToLowerInvariant()))
                    })
                    .Where(x => x.MatchCount > 0)
                    .OrderByDescending(x => x.Entry.Priority)
                    .ThenByDescending(x => x.MatchCount)
                    .FirstOrDefault();

                if (bestMatch != null)
                {
                    _logger.LogInformation("Found knowledge base entry match: {EntryId} - {Title} (Priority: {Priority}, Matches: {MatchCount})",
                        bestMatch.Entry.Id, bestMatch.Entry.Title, bestMatch.Entry.Priority, bestMatch.MatchCount);
                    return bestMatch.Entry;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding matching knowledge base entry");
                return null;
            }
        }

        private List<string> ParseKeywords(string keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords))
                return new List<string>();

            // Try parsing as JSON array first
            try
            {
                var jsonArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(keywords);
                if (jsonArray != null && jsonArray.Length > 0)
                    return jsonArray.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
            }
            catch
            {
                // Not JSON, try comma-separated
            }

            // Parse as comma-separated
            return keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();
        }

        public async Task<List<KnowledgeBaseEntry>> GetActiveEntriesAsync(int? categoryId = null)
        {
            // Check cache
            if (_cachedEntries != null && DateTime.UtcNow < _cacheExpiry)
            {
                if (categoryId.HasValue)
                {
                    return _cachedEntries.Where(e => e.CategoryId == categoryId.Value).ToList();
                }
                return _cachedEntries;
            }

            try
            {
                var query = _context.KnowledgeBaseEntries
                    .Include(e => e.Category)
                    .Where(e => e.IsActive && (e.Category == null || e.Category.IsActive));

                if (categoryId.HasValue)
                {
                    query = query.Where(e => e.CategoryId == categoryId.Value);
                }

                var entries = await query
                    .OrderByDescending(e => e.Priority)
                    .ThenBy(e => e.Title)
                    .ToListAsync();

                _cachedEntries = entries;
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading knowledge base entries");
                return new List<KnowledgeBaseEntry>();
            }
        }

        public async Task<List<KnowledgeBaseCategory>> GetActiveCategoriesAsync()
        {
            try
            {
                return await _context.KnowledgeBaseCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading knowledge base categories");
                return new List<KnowledgeBaseCategory>();
            }
        }

        public async Task<KnowledgeBaseEntry?> GetEntryByIdAsync(int id)
        {
            try
            {
                return await _context.KnowledgeBaseEntries
                    .Include(e => e.Category)
                    .FirstOrDefaultAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge base entry {Id}", id);
                return null;
            }
        }

        public async Task<KnowledgeBaseCategory?> GetCategoryByIdAsync(int id)
        {
            try
            {
                return await _context.KnowledgeBaseCategories.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge base category {Id}", id);
                return null;
            }
        }

        public async Task<KnowledgeBaseEntry> CreateEntryAsync(KnowledgeBaseEntry entry)
        {
            entry.CreatedAt = DateTime.UtcNow;
            _context.KnowledgeBaseEntries.Add(entry);
            await _context.SaveChangesAsync();
            
            // Invalidate cache
            _cachedEntries = null;
            _cacheExpiry = DateTime.MinValue;
            
            return entry;
        }

        public async Task<KnowledgeBaseEntry> UpdateEntryAsync(KnowledgeBaseEntry entry)
        {
            entry.UpdatedAt = DateTime.UtcNow;
            _context.KnowledgeBaseEntries.Update(entry);
            await _context.SaveChangesAsync();
            
            // Invalidate cache
            _cachedEntries = null;
            _cacheExpiry = DateTime.MinValue;
            
            return entry;
        }

        public async Task<bool> DeleteEntryAsync(int id)
        {
            try
            {
                var entry = await _context.KnowledgeBaseEntries.FindAsync(id);
                if (entry == null)
                    return false;

                // Soft delete
                entry.IsActive = false;
                entry.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                // Invalidate cache
                _cachedEntries = null;
                _cacheExpiry = DateTime.MinValue;
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting knowledge base entry {Id}", id);
                return false;
            }
        }

        public async Task<KnowledgeBaseCategory> CreateCategoryAsync(KnowledgeBaseCategory category)
        {
            category.CreatedAt = DateTime.UtcNow;
            _context.KnowledgeBaseCategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<KnowledgeBaseCategory> UpdateCategoryAsync(KnowledgeBaseCategory category)
        {
            category.UpdatedAt = DateTime.UtcNow;
            _context.KnowledgeBaseCategories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.KnowledgeBaseCategories.FindAsync(id);
                if (category == null)
                    return false;

                // Soft delete
                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting knowledge base category {Id}", id);
                return false;
            }
        }
    }
}

