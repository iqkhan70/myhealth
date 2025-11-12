using Microsoft.Extensions.Caching.Memory;
using SM_MentalHealthApp.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Services
{
    /// <summary>
    /// Service to classify questions using database-driven keywords
    /// Replaces hardcoded keyword arrays in IntelligentContextService
    /// </summary>
    public interface IQuestionClassificationService
    {
        Task<QuestionType> ClassifyQuestionAsync(string question);
        Task<bool> IsNonPatientQuestionAsync(string question);
        Task<bool> IsMedicalResourceQuestionAsync(string question);
        Task<bool> IsMedicalStatusQuestionAsync(string question);
        Task<bool> IsMedicalRecommendationQuestionAsync(string question);
        Task<bool> IsGeneralMedicalQuestionAsync(string question);
    }

    public class QuestionClassificationService : IQuestionClassificationService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<QuestionClassificationService> _logger;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "QuestionKeywords_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public QuestionClassificationService(
            JournalDbContext context,
            ILogger<QuestionClassificationService> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<QuestionType> ClassifyQuestionAsync(string question)
        {
            var questionLower = question.ToLower();

            // Check in order of specificity
            if (await IsNonPatientQuestionAsync(questionLower))
                return QuestionType.NonPatientRelated;

            if (await IsMedicalResourceQuestionAsync(questionLower))
                return QuestionType.PatientMedicalResources;

            if (await IsMedicalStatusQuestionAsync(questionLower))
                return QuestionType.PatientMedicalStatus;

            if (await IsMedicalRecommendationQuestionAsync(questionLower))
                return QuestionType.PatientMedicalRecommendations;

            if (await IsGeneralMedicalQuestionAsync(questionLower))
                return QuestionType.GeneralMedical;

            // Default to patient medical status
            return QuestionType.PatientMedicalStatus;
        }

        public async Task<bool> IsNonPatientQuestionAsync(string question)
        {
            var keywords = await GetKeywordsAsync("NonPatient");
            return keywords.Any(keyword => question.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> IsMedicalResourceQuestionAsync(string question)
        {
            var keywords = await GetKeywordsAsync("MedicalResource");
            
            // Check for hospital + recommendation combination
            if ((question.Contains("hospital", StringComparison.OrdinalIgnoreCase)) && 
                (question.Contains("recommend", StringComparison.OrdinalIgnoreCase) || question.Contains("suggest", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check for zip code + medical facility combination
            if (question.Contains("zip code", StringComparison.OrdinalIgnoreCase) && 
                (question.Contains("hospital", StringComparison.OrdinalIgnoreCase) || 
                 question.Contains("clinic", StringComparison.OrdinalIgnoreCase) || 
                 question.Contains("facility", StringComparison.OrdinalIgnoreCase) || 
                 question.Contains("emergency", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check for emergency + location combination
            if (question.Contains("emergency", StringComparison.OrdinalIgnoreCase) && 
                (question.Contains("near", StringComparison.OrdinalIgnoreCase) || 
                 question.Contains("zip", StringComparison.OrdinalIgnoreCase) || 
                 question.Contains("location", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check for hospital + location combination
            if (question.Contains("hospital", StringComparison.OrdinalIgnoreCase) && 
                (question.Contains("near", StringComparison.OrdinalIgnoreCase) || 
                 question.Contains("zip", StringComparison.OrdinalIgnoreCase) || 
                 question.Contains("location", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return keywords.Any(keyword => question.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> IsMedicalStatusQuestionAsync(string question)
        {
            var keywords = await GetKeywordsAsync("MedicalStatus");
            return keywords.Any(keyword => question.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> IsMedicalRecommendationQuestionAsync(string question)
        {
            var keywords = await GetKeywordsAsync("MedicalRecommendation");
            return keywords.Any(keyword => question.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> IsGeneralMedicalQuestionAsync(string question)
        {
            var keywords = await GetKeywordsAsync("GeneralMedical");
            return keywords.Any(keyword => question.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<string>> GetKeywordsAsync(string category)
        {
            var cacheKey = CacheKeyPrefix + category;
            if (_cache.TryGetValue(cacheKey, out List<string>? cachedKeywords) && cachedKeywords != null)
            {
                return cachedKeywords;
            }

            try
            {
                // TODO: Create QuestionClassificationKeywords table in database
                // For now, return hardcoded fallback
                var keywords = GetHardcodedKeywords(category);
                _cache.Set(cacheKey, keywords, CacheDuration);
                return keywords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading question classification keywords for category {Category}. Using hardcoded fallback.", category);
                return GetHardcodedKeywords(category);
            }
        }

        private List<string> GetHardcodedKeywords(string category)
        {
            return category switch
            {
                "NonPatient" => new List<string>
                {
                    "bollywood", "movie", "actor", "actress", "celebrity", "star", "film",
                    "salam khan", "salman khan", "shah rukh", "amir khan", "hritik",
                    "weather", "sports", "cricket", "football", "politics", "news",
                    "recipe", "cooking", "travel", "vacation", "hotel", "restaurant"
                },
                "MedicalResource" => new List<string>
                {
                    "hospital", "clinic", "doctor", "specialist", "facility", "medical center",
                    "zip code", "near", "location", "address", "phone", "contact",
                    "emergency", "urgent care", "pharmacy", "lab", "imaging",
                    "recommended", "recommendation", "recommend", "suggest", "suggestion"
                },
                "MedicalStatus" => new List<string>
                {
                    "how is", "status", "doing", "condition", "health", "wellbeing",
                    "progress", "improvement", "worse", "better", "stable", "critical",
                    "hospitalization", "admitted", "discharge", "recovery", "treatment"
                },
                "MedicalRecommendation" => new List<string>
                {
                    "suggest", "recommend", "advice", "approach", "strategy", "plan",
                    "treatment", "therapy", "medication", "intervention", "next steps",
                    "what should", "how to", "attacking", "reduce", "prevent"
                },
                "GeneralMedical" => new List<string>
                {
                    "what is", "explain", "define", "meaning", "symptoms", "causes",
                    "diagnosis", "treatment", "side effects", "complications"
                },
                _ => new List<string>()
            };
        }
    }
}

