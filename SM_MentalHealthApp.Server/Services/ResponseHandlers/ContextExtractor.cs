using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Services.ResponseHandlers
{
    /// <summary>
    /// Extracts and analyzes context from the input text
    /// </summary>
    public class ContextExtractor
    {
        private readonly ICriticalValuePatternService _patternService;
        private readonly ICriticalValueKeywordService _keywordService;
        private readonly ISectionMarkerService _sectionMarkerService;
        private readonly ILogger<ContextExtractor> _logger;

        public ContextExtractor(
            ICriticalValuePatternService patternService,
            ICriticalValueKeywordService keywordService,
            ISectionMarkerService sectionMarkerService,
            ILogger<ContextExtractor> logger)
        {
            _patternService = patternService;
            _keywordService = keywordService;
            _sectionMarkerService = sectionMarkerService;
            _logger = logger;
        }

        public async Task<ResponseContext> ExtractContextAsync(string text)
        {
            var context = new ResponseContext
            {
                FullText = text,
                IsAiHealthCheck = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "AI Health Check") ||
                                 await _sectionMarkerService.ContainsSectionMarkerAsync(text, "INSTRUCTIONS FOR AI HEALTH CHECK")
            };

            // Extract patient data sections (exclude AI instructions)
            context.PatientDataText = await ExtractPatientDataSectionsAsync(text);

            _logger.LogInformation("Extracted patient data text length: {Length}", context.PatientDataText?.Length ?? 0);
            _logger.LogInformation("Patient data text preview: {Preview}",
                context.PatientDataText?.Length > 200 ? context.PatientDataText.Substring(0, 200) : context.PatientDataText);

            // Check for critical values
            context.HasCriticalValues = await _patternService.MatchesAnyPatternAsync(context.PatientDataText) ||
                                       await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "Critical");
            _logger.LogInformation("HasCriticalValues: {HasCritical}", context.HasCriticalValues);

            // Check for abnormal values
            context.HasAbnormalValues = await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "Abnormal");
            var hasHighConcern = await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "High Concern");
            var hasDistress = await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "Distress");
            context.HasAnyConcerns = context.HasAbnormalValues || hasHighConcern || hasDistress;

            _logger.LogInformation("HasAbnormalValues: {HasAbnormal}, HasHighConcern: {HasHighConcern}, HasDistress: {HasDistress}, HasAnyConcerns: {HasAnyConcerns}",
                context.HasAbnormalValues, hasHighConcern, hasDistress, context.HasAnyConcerns);

            // Debug: Check if clinical notes section exists and what keywords might match
            if (!string.IsNullOrEmpty(context.PatientDataText) && context.PatientDataText.Contains("RECENT CLINICAL NOTES", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Service notes detected in patient data. Checking for specific keywords...");
                var testKeywords = new[] { "serious symptoms", "anxiety", "high blood pressure", "heart problems", "risk of", "more test" };
                foreach (var testKeyword in testKeywords)
                {
                    var found = context.PatientDataText.ToLowerInvariant().Contains(testKeyword.ToLowerInvariant());
                    _logger.LogInformation("Keyword '{Keyword}' found in patient data: {Found}", testKeyword, found);
                }
            }

            // Check for normal values
            context.HasNormalValues = await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "Normal");

            // Check for medical data and journal entries using section markers
            context.HasMedicalData = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "MEDICAL DATA SUMMARY");
            context.HasJournalEntries = await _sectionMarkerService.ContainsSectionMarkerAsync(text, "RECENT JOURNAL ENTRIES");

            // Extract lists
            context.CriticalAlerts = ExtractCriticalAlerts(text);
            context.NormalValues = ExtractNormalValues(text);
            context.AbnormalValues = ExtractAbnormalValues(text);
            context.JournalEntries = await ExtractJournalEntriesAsync(text);

            return context;
        }

        private async Task<string> ExtractPatientDataSectionsAsync(string text)
        {
            try
            {
                var sections = new List<string>();
                var sectionMarkers = await _sectionMarkerService.GetSectionMarkersAsync();

                // Filter to only patient data sections (exclude instructions and questions)
                var patientDataMarkerTypes = new[]
                {
                    "RECENT JOURNAL ENTRIES",
                    "MEDICAL DATA SUMMARY",
                    "CURRENT MEDICAL STATUS",
                    "HISTORICAL MEDICAL CONCERNS",
                    "HEALTH TREND ANALYSIS",
                    "RECENT CLINICAL NOTES",
                    "RECENT CHAT HISTORY",
                    "RECENT EMERGENCY INCIDENTS",
                    "Recent Patient Activity",
                    "Current Test Results",
                    "Latest Update"
                };

                foreach (var markerType in patientDataMarkerTypes)
                {
                    var section = await _sectionMarkerService.ExtractSectionAsync(text, markerType);
                    if (!string.IsNullOrEmpty(section))
                    {
                        // For "=== CURRENT MEDICAL STATUS ===" section, exclude status summary text and header lines
                        if (markerType.Contains("CURRENT MEDICAL STATUS"))
                        {
                            var lines = section.Split('\n');
                            var filteredLines = new List<string>();
                            bool inStatusSummary = false;

                            foreach (var line in lines)
                            {
                                // Skip header lines that are just formatting
                                if (line.Contains("**") && (line.Contains("DETECTED") || line.Contains("VALUES") ||
                                    line.Contains("STATUS:") || line.Contains("CRITICAL VALUES") ||
                                    line.Contains("ABNORMAL VALUES") || line.Contains("NORMAL VALUES")))
                                {
                                    continue;
                                }

                                // Skip status summary lines
                                if (line.Contains("**STATUS:") || line.Contains("STATUS: CRITICAL") ||
                                    line.Contains("STATUS: CONCERNING") || line.Contains("STATUS: STABLE"))
                                {
                                    inStatusSummary = true;
                                    continue;
                                }

                                if (inStatusSummary && string.IsNullOrWhiteSpace(line))
                                {
                                    inStatusSummary = false;
                                    continue;
                                }

                                if (!inStatusSummary)
                                {
                                    filteredLines.Add(line);
                                }
                            }

                            section = string.Join("\n", filteredLines);
                        }

                        // For "=== RECENT CLINICAL NOTES ===" section, exclude instruction text
                        if (markerType.Contains("RECENT CLINICAL NOTES"))
                        {
                            var lines = section.Split('\n');
                            var filteredLines = new List<string>();
                            bool skipInstructionLines = false;

                            foreach (var line in lines)
                            {
                                if (line.Contains("⚠️ IMPORTANT:") || line.Contains("HIGH PRIORITY") ||
                                    line.Contains("should be given") || line.Contains("contain critical medical observations"))
                                {
                                    skipInstructionLines = true;
                                    continue;
                                }

                                if (skipInstructionLines && string.IsNullOrWhiteSpace(line))
                                {
                                    skipInstructionLines = false;
                                    continue;
                                }

                                if (!skipInstructionLines)
                                {
                                    filteredLines.Add(line);
                                }
                            }

                            section = string.Join("\n", filteredLines);
                        }

                        sections.Add(section);
                    }
                }

                return string.Join("\n", sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting patient data sections");
                return text;
            }
        }

        private List<string> ExtractCriticalAlerts(string text)
        {
            var alerts = new List<string>();
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("🚨 CRITICAL:") || line.Contains("CRITICAL VALUES:") || line.Contains("CRITICAL:"))
                {
                    alerts.Add(line.Trim());
                }
            }
            return alerts;
        }

        private List<string> ExtractNormalValues(string text)
        {
            var values = new List<string>();
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("✅ NORMAL:") || line.Contains("NORMAL VALUES:"))
                {
                    values.Add(line.Trim());
                }
            }
            return values;
        }

        private List<string> ExtractAbnormalValues(string text)
        {
            var values = new List<string>();
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("⚠️") || line.Contains("ABNORMAL VALUES:"))
                {
                    values.Add(line.Trim());
                }
            }
            return values;
        }

        private async Task<List<string>> ExtractJournalEntriesAsync(string text)
        {
            var entries = new List<string>();
            var section = await _sectionMarkerService.ExtractSectionAsync(text, "RECENT JOURNAL ENTRIES");
            if (!string.IsNullOrEmpty(section))
            {
                var lines = section.Split('\n')
                    .Where(l => l.Contains("[") && l.Contains("]") && (l.Contains("Mood:") || l.Contains("Entry:")))
                    .Take(3)
                    .ToList();

                entries.AddRange(lines);
            }
            return entries;
        }
    }
}

