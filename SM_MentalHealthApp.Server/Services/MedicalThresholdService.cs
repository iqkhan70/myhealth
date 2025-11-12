using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public class MedicalThresholdService : IMedicalThresholdService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<MedicalThresholdService> _logger;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public MedicalThresholdService(JournalDbContext context, ILogger<MedicalThresholdService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<List<MedicalThreshold>> GetActiveThresholdsAsync(string parameterName)
        {
            var cacheKey = $"thresholds_{parameterName}";
            
            if (_cache.TryGetValue(cacheKey, out List<MedicalThreshold>? cachedThresholds) && cachedThresholds != null)
            {
                return cachedThresholds;
            }

            try
            {
                var thresholds = await _context.MedicalThresholds
                    .Where(t => t.IsActive && 
                               (t.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase) ||
                                (t.SecondaryParameterName != null && t.SecondaryParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase))))
                    .OrderByDescending(t => t.Priority)
                    .ThenByDescending(t => t.ThresholdValue ?? t.MinValue ?? 0)
                    .ToListAsync();

                _cache.Set(cacheKey, thresholds, CacheDuration);
                return thresholds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading medical thresholds for parameter: {ParameterName}", parameterName);
                return GetHardcodedThresholds(parameterName);
            }
        }

        public async Task<bool> IsValueCriticalAsync(string parameterName, double value, double? secondaryValue = null)
        {
            var threshold = await GetMatchingThresholdAsync(parameterName, value, secondaryValue);
            return threshold != null && threshold.SeverityLevel?.Equals("Critical", StringComparison.OrdinalIgnoreCase) == true;
        }

        public async Task<MedicalThreshold?> GetMatchingThresholdAsync(string parameterName, double value, double? secondaryValue = null)
        {
            var thresholds = await GetActiveThresholdsAsync(parameterName);

            foreach (var threshold in thresholds)
            {
                if (MatchesThreshold(threshold, parameterName, value, secondaryValue))
                {
                    return threshold;
                }
            }

            return null;
        }

        public async Task<string?> GetSeverityLevelAsync(string parameterName, double value, double? secondaryValue = null)
        {
            var threshold = await GetMatchingThresholdAsync(parameterName, value, secondaryValue);
            return threshold?.SeverityLevel;
        }

        private bool MatchesThreshold(MedicalThreshold threshold, string parameterName, double value, double? secondaryValue)
        {
            // Check if this threshold applies to the primary parameter
            bool isPrimaryMatch = threshold.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase);
            
            // For blood pressure, check both systolic and diastolic
            if (isPrimaryMatch && threshold.SecondaryParameterName != null && secondaryValue.HasValue)
            {
                // Check primary value (systolic)
                bool primaryMatches = CheckValue(threshold.ComparisonOperator, threshold.ThresholdValue ?? threshold.MinValue ?? 0, value);
                
                // Check secondary value (diastolic)
                bool secondaryMatches = CheckValue(threshold.SecondaryComparisonOperator, threshold.SecondaryThresholdValue ?? 0, secondaryValue.Value);
                
                // For BP, typically we use OR logic (either value exceeds threshold)
                return primaryMatches || secondaryMatches;
            }
            else if (isPrimaryMatch)
            {
                // Single parameter check
                return CheckValue(threshold.ComparisonOperator, threshold.ThresholdValue ?? threshold.MinValue ?? 0, value);
            }

            return false;
        }

        private bool CheckValue(string? operatorStr, double threshold, double value)
        {
            if (string.IsNullOrEmpty(operatorStr))
            {
                // Default to >= for backward compatibility
                operatorStr = ">=";
            }

            return operatorStr switch
            {
                ">=" => value >= threshold,
                "<=" => value <= threshold,
                ">" => value > threshold,
                "<" => value < threshold,
                "==" => Math.Abs(value - threshold) < 0.001, // Floating point comparison
                _ => value >= threshold // Default
            };
        }

        private List<MedicalThreshold> GetHardcodedThresholds(string parameterName)
        {
            _logger.LogWarning("Using hardcoded thresholds for parameter: {ParameterName}", parameterName);
            
            return parameterName.ToLowerInvariant() switch
            {
                "blood pressure" or "bp" => new List<MedicalThreshold>
                {
                    new MedicalThreshold
                    {
                        ParameterName = "Blood Pressure Systolic",
                        SecondaryParameterName = "Blood Pressure Diastolic",
                        SeverityLevel = "Critical",
                        ThresholdValue = 180,
                        SecondaryThresholdValue = 110,
                        ComparisonOperator = ">=",
                        SecondaryComparisonOperator = ">=",
                        Priority = 10,
                        IsActive = true,
                        Description = "Hypertensive crisis - immediate medical intervention required"
                    },
                    new MedicalThreshold
                    {
                        ParameterName = "Blood Pressure Systolic",
                        SecondaryParameterName = "Blood Pressure Diastolic",
                        SeverityLevel = "High",
                        ThresholdValue = 140,
                        SecondaryThresholdValue = 90,
                        ComparisonOperator = ">=",
                        SecondaryComparisonOperator = ">=",
                        Priority = 8,
                        IsActive = true,
                        Description = "High blood pressure - requires immediate attention"
                    }
                },
                "hemoglobin" or "hgb" or "hb" => new List<MedicalThreshold>
                {
                    new MedicalThreshold
                    {
                        ParameterName = "Hemoglobin",
                        SeverityLevel = "Critical",
                        ThresholdValue = 7.0,
                        ComparisonOperator = "<",
                        Priority = 10,
                        IsActive = true,
                        Description = "Severe anemia - blood transfusion may be required"
                    },
                    new MedicalThreshold
                    {
                        ParameterName = "Hemoglobin",
                        SeverityLevel = "Low",
                        ThresholdValue = 10.0,
                        ComparisonOperator = "<",
                        Priority = 8,
                        IsActive = true,
                        Description = "Moderate anemia - requires monitoring"
                    }
                },
                "triglycerides" or "trig" => new List<MedicalThreshold>
                {
                    new MedicalThreshold
                    {
                        ParameterName = "Triglycerides",
                        SeverityLevel = "Critical",
                        ThresholdValue = 500,
                        ComparisonOperator = ">=",
                        Priority = 10,
                        IsActive = true,
                        Description = "Extremely high - risk of pancreatitis"
                    },
                    new MedicalThreshold
                    {
                        ParameterName = "Triglycerides",
                        SeverityLevel = "High",
                        ThresholdValue = 200,
                        ComparisonOperator = ">=",
                        Priority = 8,
                        IsActive = true,
                        Description = "High - requires dietary intervention"
                    }
                },
                _ => new List<MedicalThreshold>()
            };
        }
    }
}

