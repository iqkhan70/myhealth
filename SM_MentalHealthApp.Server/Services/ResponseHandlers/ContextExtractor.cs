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
        private readonly ILogger<ContextExtractor> _logger;

        public ContextExtractor(
            ICriticalValuePatternService patternService,
            ICriticalValueKeywordService keywordService,
            ILogger<ContextExtractor> logger)
        {
            _patternService = patternService;
            _keywordService = keywordService;
            _logger = logger;
        }

        public async Task<ResponseContext> ExtractContextAsync(string text)
        {
            var context = new ResponseContext
            {
                FullText = text,
                IsAiHealthCheck = text.Contains("AI Health Check for Patient") ||
                                 text.Contains("=== INSTRUCTIONS FOR AI HEALTH CHECK ANALYSIS ===")
            };

            // Extract patient data sections (exclude AI instructions)
            context.PatientDataText = ExtractPatientDataSections(text);

            // Check for critical values
            context.HasCriticalValues = await _patternService.MatchesAnyPatternAsync(context.PatientDataText) ||
                                       await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "Critical");

            // Check for abnormal values
            context.HasAbnormalValues = await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "Abnormal");
            var hasHighConcern = await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "High Concern");
            var hasDistress = await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "Distress");
            context.HasAnyConcerns = context.HasAbnormalValues || hasHighConcern || hasDistress;

            // Check for normal values
            context.HasNormalValues = await _keywordService.ContainsAnyKeywordAsync(context.PatientDataText, "Normal");

            // Check for medical data and journal entries
            context.HasMedicalData = text.Contains("=== MEDICAL DATA SUMMARY ===");
            context.HasJournalEntries = text.Contains("=== RECENT JOURNAL ENTRIES ===");

            // Extract lists
            context.CriticalAlerts = ExtractCriticalAlerts(text);
            context.NormalValues = ExtractNormalValues(text);
            context.AbnormalValues = ExtractAbnormalValues(text);
            context.JournalEntries = ExtractJournalEntries(text);

            return context;
        }

        private string ExtractPatientDataSections(string text)
        {
            try
            {
                var sections = new List<string>();

                // Extract specific patient data sections
                var sectionMarkers = new[]
                {
                    "=== RECENT JOURNAL ENTRIES ===",
                    "=== MEDICAL DATA SUMMARY ===",
                    "=== CURRENT MEDICAL STATUS ===",
                    "=== HISTORICAL MEDICAL CONCERNS ===",
                    "=== HEALTH TREND ANALYSIS ===",
                    "=== RECENT CLINICAL NOTES ===",
                    "=== RECENT CHAT HISTORY ===",
                    "=== RECENT EMERGENCY INCIDENTS ===",
                    "Recent Patient Activity:",
                    "Current Test Results",
                    "Latest Update:"
                };

                foreach (var marker in sectionMarkers)
                {
                    var index = text.IndexOf(marker);
                    if (index >= 0)
                    {
                        // Find the end of this section (next section marker or end of text)
                        var nextIndex = text.Length;
                        foreach (var nextMarker in sectionMarkers)
                        {
                            if (nextMarker != marker)
                            {
                                var nextPos = text.IndexOf(nextMarker, index + marker.Length);
                                if (nextPos > index && nextPos < nextIndex)
                                {
                                    nextIndex = nextPos;
                                }
                            }
                        }

                        // Also check for instruction sections that should be excluded
                        var instructionStart = text.IndexOf("=== INSTRUCTIONS", index);
                        if (instructionStart > index && instructionStart < nextIndex)
                        {
                            nextIndex = instructionStart;
                        }

                        var sectionLength = nextIndex - index;
                        if (sectionLength > 0)
                        {
                            var section = text.Substring(index, sectionLength);

                            // For "=== CURRENT MEDICAL STATUS ===" section, exclude status summary text and header lines
                            if (marker == "=== CURRENT MEDICAL STATUS ===")
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
                            if (marker == "=== RECENT CLINICAL NOTES ===")
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

        private List<string> ExtractJournalEntries(string text)
        {
            var entries = new List<string>();
            var journalStart = text.IndexOf("=== RECENT JOURNAL ENTRIES ===");
            if (journalStart >= 0)
            {
                var journalEnd = text.IndexOf("===", journalStart + 30);
                if (journalEnd < 0) journalEnd = text.Length;

                var section = text.Substring(journalStart, journalEnd - journalStart);
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

