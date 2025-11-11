using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IAIInstructionService
    {
        Task<string> BuildInstructionsAsync(string context = "HealthCheck");
        Task<List<AIInstructionCategory>> GetCategoriesAsync(string context = "HealthCheck");
        Task<List<AIInstruction>> GetInstructionsByCategoryAsync(int categoryId);
    }

    public class AIInstructionService : IAIInstructionService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<AIInstructionService> _logger;
        private static Dictionary<string, string> _cachedInstructions = new();
        private static Dictionary<string, DateTime> _cacheExpiry = new();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public AIInstructionService(JournalDbContext context, ILogger<AIInstructionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> BuildInstructionsAsync(string context = "HealthCheck")
        {
            try
            {
                // Check cache first
                if (_cachedInstructions.ContainsKey(context) && 
                    _cacheExpiry.ContainsKey(context) && 
                    DateTime.UtcNow < _cacheExpiry[context])
                {
                    return _cachedInstructions[context];
                }

                // Load categories and instructions from database
                var categories = await _context.AIInstructionCategories
                    .Include(c => c.Instructions)
                    .Where(c => c.Context == context && c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                var instructionsBuilder = new System.Text.StringBuilder();

                foreach (var category in categories)
                {
                    var activeInstructions = category.Instructions
                        .Where(i => i.IsActive)
                        .OrderBy(i => i.DisplayOrder)
                        .ThenBy(i => i.Id)
                        .ToList();

                    if (activeInstructions.Any())
                    {
                        // Add category header if it has a name
                        if (!string.IsNullOrWhiteSpace(category.Name))
                        {
                            instructionsBuilder.AppendLine($"**{category.Name}**");
                        }

                        // Add category description if available
                        if (!string.IsNullOrWhiteSpace(category.Description))
                        {
                            instructionsBuilder.AppendLine(category.Description);
                        }

                        // Add instructions
                        foreach (var instruction in activeInstructions)
                        {
                            if (!string.IsNullOrWhiteSpace(instruction.Title))
                            {
                                instructionsBuilder.AppendLine($"- {instruction.Title}: {instruction.Content}");
                            }
                            else
                            {
                                instructionsBuilder.AppendLine($"- {instruction.Content}");
                            }
                        }

                        instructionsBuilder.AppendLine();
                    }
                }

                var result = instructionsBuilder.ToString().TrimEnd();
                
                // Cache the result
                _cachedInstructions[context] = result;
                _cacheExpiry[context] = DateTime.UtcNow.Add(CacheDuration);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building AI instructions for context: {Context}", context);
                // Return fallback instructions if database fails
                return GetFallbackInstructions(context);
            }
        }

        public async Task<List<AIInstructionCategory>> GetCategoriesAsync(string context = "HealthCheck")
        {
            try
            {
                return await _context.AIInstructionCategories
                    .Where(c => c.Context == context && c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI instruction categories for context: {Context}", context);
                return new List<AIInstructionCategory>();
            }
        }

        public async Task<List<AIInstruction>> GetInstructionsByCategoryAsync(int categoryId)
        {
            try
            {
                return await _context.AIInstructions
                    .Where(i => i.CategoryId == categoryId && i.IsActive)
                    .OrderBy(i => i.DisplayOrder)
                    .ThenBy(i => i.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI instructions for category: {CategoryId}", categoryId);
                return new List<AIInstruction>();
            }
        }

        private string GetFallbackInstructions(string context)
        {
            // Fallback instructions if database is unavailable
            if (context == "HealthCheck")
            {
                return @"**Patient Medical Overview:**
- Review all available patient data
- Provide comprehensive health assessment
- Reference specific values from medical data";
            }
            return string.Empty;
        }
    }
}

