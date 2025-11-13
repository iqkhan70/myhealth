using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public class AIResponseTemplateService : IAIResponseTemplateService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<AIResponseTemplateService> _logger;
        private static Dictionary<string, AIResponseTemplate>? _cachedTemplates;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

        public AIResponseTemplateService(JournalDbContext context, ILogger<AIResponseTemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AIResponseTemplate?> GetTemplateByKeyAsync(string templateKey)
        {
            try
            {
                // Check cache first
                if (_cachedTemplates != null && DateTime.UtcNow < _cacheExpiry)
                {
                    if (_cachedTemplates.TryGetValue(templateKey, out var cached))
                    {
                        return cached;
                    }
                }

                // Load from database
                var template = await _context.AIResponseTemplates
                    .FirstOrDefaultAsync(t => t.TemplateKey == templateKey && t.IsActive);

                // Update cache
                if (_cachedTemplates == null || DateTime.UtcNow >= _cacheExpiry)
                {
                    await RefreshCacheAsync();
                }

                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting template by key {TemplateKey}", templateKey);
                return null;
            }
        }

        public async Task<string> FormatTemplateAsync(string templateKey, Dictionary<string, string>? placeholders)
        {
            try
            {
                var template = await GetTemplateByKeyAsync(templateKey);
                if (template == null)
                {
                    _logger.LogWarning("Template not found for key: {TemplateKey}", templateKey);
                    return string.Empty;
                }

                var result = template.Content;

                // Replace placeholders like {CRITICAL_VALUES}, {STATUS}, etc.
                if (placeholders != null)
                {
                    foreach (var placeholder in placeholders)
                    {
                        result = result.Replace($"{{{placeholder.Key}}}", placeholder.Value ?? string.Empty);
                    }
                }

                // Remove any unreplaced placeholders (optional - you might want to keep them for debugging)
                // result = System.Text.RegularExpressions.Regex.Replace(result, @"\{[A-Z_]+\}", string.Empty);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting template {TemplateKey}", templateKey);
                return string.Empty;
            }
        }

        public async Task<List<AIResponseTemplate>> GetAllActiveTemplatesAsync()
        {
            try
            {
                // Check cache
                if (_cachedTemplates != null && DateTime.UtcNow < _cacheExpiry)
                {
                    return _cachedTemplates.Values.ToList();
                }

                var templates = await _context.AIResponseTemplates
                    .Where(t => t.IsActive)
                    .OrderByDescending(t => t.Priority)
                    .ThenBy(t => t.TemplateName)
                    .ToListAsync();

                // Update cache
                await RefreshCacheAsync();

                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all active templates");
                return new List<AIResponseTemplate>();
            }
        }

        public async Task<AIResponseTemplate> CreateTemplateAsync(AIResponseTemplate template)
        {
            template.CreatedAt = DateTime.UtcNow;
            _context.AIResponseTemplates.Add(template);
            await _context.SaveChangesAsync();
            
            // Invalidate cache
            _cachedTemplates = null;
            _cacheExpiry = DateTime.MinValue;
            
            return template;
        }

        public async Task<AIResponseTemplate> UpdateTemplateAsync(AIResponseTemplate template)
        {
            template.UpdatedAt = DateTime.UtcNow;
            _context.AIResponseTemplates.Update(template);
            await _context.SaveChangesAsync();
            
            // Invalidate cache
            _cachedTemplates = null;
            _cacheExpiry = DateTime.MinValue;
            
            return template;
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            try
            {
                var template = await _context.AIResponseTemplates.FindAsync(id);
                if (template == null)
                    return false;

                // Soft delete
                template.IsActive = false;
                template.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                // Invalidate cache
                _cachedTemplates = null;
                _cacheExpiry = DateTime.MinValue;
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {Id}", id);
                return false;
            }
        }

        private async Task RefreshCacheAsync()
        {
            try
            {
                var templates = await _context.AIResponseTemplates
                    .Where(t => t.IsActive)
                    .ToListAsync();

                _cachedTemplates = templates.ToDictionary(t => t.TemplateKey, t => t);
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing template cache");
            }
        }
    }
}

