using Microsoft.Extensions.Caching.Memory;
using SM_MentalHealthApp.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Services
{
    /// <summary>
    /// Service to manage section markers used for parsing context text
    /// This makes section markers data-driven instead of hardcoded
    /// </summary>
    public interface ISectionMarkerService
    {
        Task<List<string>> GetSectionMarkersAsync();
        Task<bool> ContainsSectionMarkerAsync(string text, string markerType);
        Task<string?> ExtractSectionAsync(string text, string markerType);
    }

    public class SectionMarkerService : ISectionMarkerService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<SectionMarkerService> _logger;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "SectionMarkersCache";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public SectionMarkerService(JournalDbContext context, ILogger<SectionMarkerService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<List<string>> GetSectionMarkersAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<string>? cachedMarkers) && cachedMarkers != null)
            {
                return cachedMarkers;
            }

            try
            {
                // TODO: Create SectionMarkers table in database
                // For now, return hardcoded fallback
                var markers = GetHardcodedSectionMarkers();
                _cache.Set(CacheKey, markers, CacheDuration);
                return markers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading section markers from database. Using hardcoded fallback.");
                return GetHardcodedSectionMarkers();
            }
        }

        public async Task<bool> ContainsSectionMarkerAsync(string text, string markerType)
        {
            var markers = await GetSectionMarkersAsync();
            var marker = markers.FirstOrDefault(m => m.Contains(markerType, StringComparison.OrdinalIgnoreCase));
            return marker != null && text.Contains(marker, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string?> ExtractSectionAsync(string text, string markerType)
        {
            var markers = await GetSectionMarkersAsync();
            var marker = markers.FirstOrDefault(m => m.Contains(markerType, StringComparison.OrdinalIgnoreCase));
            
            if (marker == null) return null;

            var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            // Find the end of this section (next section marker or end of text)
            // For clinical notes, we want to include all content until the next major section
            var nextIndex = text.Length;
            var searchStart = index + marker.Length;
            
            foreach (var nextMarker in markers)
            {
                if (nextMarker != marker && !nextMarker.Contains(markerType, StringComparison.OrdinalIgnoreCase))
                {
                    // Only look for markers that come after this one
                    var nextPos = text.IndexOf(nextMarker, searchStart, StringComparison.OrdinalIgnoreCase);
                    if (nextPos > index && nextPos < nextIndex)
                    {
                        // For clinical notes, make sure we don't cut off too early
                        // If the next marker is very close (within 50 chars), it might be a false match
                        if (markerType.Contains("CLINICAL NOTES") && (nextPos - searchStart) < 50)
                        {
                            continue; // Skip this marker, it's too close
                        }
                        nextIndex = nextPos;
                    }
                }
            }

            var extracted = text.Substring(index, nextIndex - index);
            _logger.LogDebug("Extracted section '{MarkerType}': {Length} chars (from index {Start} to {End})", 
                markerType, extracted.Length, index, nextIndex);
            
            return extracted;
        }

        private List<string> GetHardcodedSectionMarkers()
        {
            return new List<string>
            {
                "=== RECENT JOURNAL ENTRIES ===",
                "=== MEDICAL DATA SUMMARY ===",
                "=== CURRENT MEDICAL STATUS ===",
                "=== HISTORICAL MEDICAL CONCERNS ===",
                "=== HEALTH TREND ANALYSIS ===",
                "=== RECENT CLINICAL NOTES ===",
                "=== RECENT CHAT HISTORY ===",
                "=== RECENT EMERGENCY INCIDENTS ===",
                "=== USER QUESTION ===",
                "=== PROGRESSION ANALYSIS ===",
                "=== INSTRUCTIONS FOR AI HEALTH CHECK ANALYSIS ===",
                "Recent Patient Activity:",
                "Current Test Results",
                "Latest Update:",
                "**Medical Resource Information",
                "**Medical Facilities Search",
                "Doctor asks:",
                "Patient asks:",
                // Additional markers for emergency and data detection
                "Fall",
                "Session:",
                "Summary:",
                "Clinical Notes",
                "Journal Entries",
                "Chat History",
                "MOOD PATTERNS (Last 30 days):",
                "RECENT JOURNAL ENTRIES (Last 14 days):",
                "AI Health Check for Patient"
            };
        }
    }
}

