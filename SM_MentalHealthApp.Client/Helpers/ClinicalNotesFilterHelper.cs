using System.Text.RegularExpressions;

namespace SM_MentalHealthApp.Client.Helpers
{
    /// <summary>
    /// Helper class to parse RadzenDataGrid filter strings for ClinicalNotes
    /// </summary>
    public static class ClinicalNotesFilterHelper
    {
        /// <summary>
        /// Parses a RadzenDataGrid filter string and extracts filter values for ClinicalNotes
        /// </summary>
        public static (string? searchTerm, string? noteType, string? priority, bool? isIgnoredByDoctor, DateTime? createdDateFrom, DateTime? createdDateTo) ParseFilter(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return (null, null, null, null, null, null);

            // Decode URL-encoded filter from Radzen
            var decodedFilter = System.Net.WebUtility.UrlDecode(filter);
            
            string? searchTerm = null;
            string? noteType = null;
            string? priority = null;
            bool? isIgnoredByDoctor = null;
            DateTime? createdDateFrom = null;
            DateTime? createdDateTo = null;

            // Properties that map to searchTerm (text search)
            var textSearchProperties = new[] { "Title", "Content", "PatientName", "DoctorName" };
            
            // Properties that map to noteType
            var noteTypeProperty = "NoteType";
            
            // Properties that map to priority
            var priorityProperty = "Priority";

            // === PARSE TEXT SEARCH FILTERS ===
            // Handle complex Radzen patterns like:
            // ((Title == null ? "" : Title).ToLower()).Contains("value".ToLower())
            // (Title == null ? "" : Title).ToLower().Contains("value".ToLower())
            // Title.Contains('value')
            // Title == 'value'
            
            foreach (var prop in textSearchProperties)
            {
                // Pattern 1: ((Property == null ? "" : Property).ToLower()).Contains("value".ToLower())
                var pattern1 = $@"\(\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\)\.Contains\((""([^""]*)""|'([^']*)')\.ToLower\(\)\)";
                var match1 = Regex.Match(decodedFilter, pattern1, RegexOptions.IgnoreCase);
                if (match1.Success)
                {
                    searchTerm = match1.Groups[2].Success ? match1.Groups[2].Value : match1.Groups[3].Value;
                    break;
                }

                // Pattern 2: (Property == null ? "" : Property).ToLower().Contains("value".ToLower())
                var pattern2 = $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\.Contains\((""([^""]*)""|'([^']*)')\.ToLower\(\)\)";
                var match2 = Regex.Match(decodedFilter, pattern2, RegexOptions.IgnoreCase);
                if (match2.Success)
                {
                    searchTerm = match2.Groups[2].Success ? match2.Groups[2].Value : match2.Groups[3].Value;
                    break;
                }

                // Pattern 3: Property.Contains('value')
                var pattern3 = $@"\b{prop}\s*\.Contains\((""([^""]*)""|'([^']*)')\)";
                var match3 = Regex.Match(decodedFilter, pattern3, RegexOptions.IgnoreCase);
                if (match3.Success)
                {
                    searchTerm = match3.Groups[2].Success ? match3.Groups[2].Value : match3.Groups[3].Value;
                    break;
                }

                // Pattern 4: Property == 'value'
                var pattern4 = $@"\b{prop}\s*==\s*(""([^""]*)""|'([^']*)')";
                var match4 = Regex.Match(decodedFilter, pattern4, RegexOptions.IgnoreCase);
                if (match4.Success)
                {
                    searchTerm = match4.Groups[2].Success ? match4.Groups[2].Value : match4.Groups[3].Value;
                    break;
                }
            }

            // === PARSE NOTE TYPE FILTER ===
            // Pattern 1: ((NoteType == null ? "" : NoteType).ToLower()) == "value".ToLower()
            var noteTypePattern1 = $@"\(\({noteTypeProperty}\s*==\s*null\s*\?\s*""""\s*:\s*{noteTypeProperty}\)\.ToLower\(\)\)\s*==\s*(""([^""]*)""|'([^']*)')\.ToLower\(\)";
            var noteTypeMatch1 = Regex.Match(decodedFilter, noteTypePattern1, RegexOptions.IgnoreCase);
            if (noteTypeMatch1.Success)
            {
                noteType = noteTypeMatch1.Groups[2].Success ? noteTypeMatch1.Groups[2].Value : noteTypeMatch1.Groups[3].Value;
            }
            else
            {
                // Pattern 2: (NoteType == null ? "" : NoteType).ToLower() == "value".ToLower()
                var noteTypePattern2a = $@"\({noteTypeProperty}\s*==\s*null\s*\?\s*""""\s*:\s*{noteTypeProperty}\)\.ToLower\(\)\s*==\s*(""([^""]*)""|'([^']*)')\.ToLower\(\)";
                var noteTypeMatch2a = Regex.Match(decodedFilter, noteTypePattern2a, RegexOptions.IgnoreCase);
                if (noteTypeMatch2a.Success)
                {
                    noteType = noteTypeMatch2a.Groups[2].Success ? noteTypeMatch2a.Groups[2].Value : noteTypeMatch2a.Groups[3].Value;
                }
                else
                {
                    // Pattern 3: NoteType == 'value'
                    var noteTypePattern3 = $@"\b{noteTypeProperty}\s*==\s*(""([^""]*)""|'([^']*)')";
                    var noteTypeMatch3 = Regex.Match(decodedFilter, noteTypePattern3, RegexOptions.IgnoreCase);
                    if (noteTypeMatch3.Success)
                    {
                        noteType = noteTypeMatch3.Groups[2].Success ? noteTypeMatch3.Groups[2].Value : noteTypeMatch3.Groups[3].Value;
                    }
                }
            }

            // === PARSE PRIORITY FILTER ===
            // Pattern 1: ((Priority == null ? "" : Priority).ToLower()) == "value".ToLower()
            var priorityPattern1 = $@"\(\({priorityProperty}\s*==\s*null\s*\?\s*""""\s*:\s*{priorityProperty}\)\.ToLower\(\)\)\s*==\s*(""([^""]*)""|'([^']*)')\.ToLower\(\)";
            var priorityMatch1 = Regex.Match(decodedFilter, priorityPattern1, RegexOptions.IgnoreCase);
            if (priorityMatch1.Success)
            {
                priority = priorityMatch1.Groups[2].Success ? priorityMatch1.Groups[2].Value : priorityMatch1.Groups[3].Value;
            }
            else
            {
                // Pattern 2: (Priority == null ? "" : Priority).ToLower() == "value".ToLower()
                var priorityPattern2a = $@"\({priorityProperty}\s*==\s*null\s*\?\s*""""\s*:\s*{priorityProperty}\)\.ToLower\(\)\s*==\s*(""([^""]*)""|'([^']*)')\.ToLower\(\)";
                var priorityMatch2a = Regex.Match(decodedFilter, priorityPattern2a, RegexOptions.IgnoreCase);
                if (priorityMatch2a.Success)
                {
                    priority = priorityMatch2a.Groups[2].Success ? priorityMatch2a.Groups[2].Value : priorityMatch2a.Groups[3].Value;
                }
                else
                {
                    // Pattern 3: Priority == 'value'
                    var priorityPattern3 = $@"\b{priorityProperty}\s*==\s*(""([^""]*)""|'([^']*)')";
                    var priorityMatch3 = Regex.Match(decodedFilter, priorityPattern3, RegexOptions.IgnoreCase);
                    if (priorityMatch3.Success)
                    {
                        priority = priorityMatch3.Groups[2].Success ? priorityMatch3.Groups[2].Value : priorityMatch3.Groups[3].Value;
                    }
                }
            }

            // === PARSE AI STATUS (IsIgnoredByDoctor) BOOLEAN FILTER ===
            // Pattern 1: IsIgnoredByDoctor == true/false
            var aiStatusPattern1 = @"IsIgnoredByDoctor\s*==\s*(true|false)";
            var aiStatusMatch1 = Regex.Match(decodedFilter, aiStatusPattern1, RegexOptions.IgnoreCase);
            if (aiStatusMatch1.Success)
            {
                isIgnoredByDoctor = bool.Parse(aiStatusMatch1.Groups[1].Value);
            }
            else
            {
                // Pattern 2: IsIgnoredByDoctor == True/False (capitalized)
                var aiStatusPattern2 = @"IsIgnoredByDoctor\s*==\s*(True|False)";
                var aiStatusMatch2 = Regex.Match(decodedFilter, aiStatusPattern2, RegexOptions.IgnoreCase);
                if (aiStatusMatch2.Success)
                {
                    isIgnoredByDoctor = bool.Parse(aiStatusMatch2.Groups[1].Value);
                }
            }

            // === PARSE DATE FILTER (CreatedAt) ===
            // Pattern: CreatedAt > DateTime("2001-12-07") or CreatedAt >= DateTime("2001-12-07")
            // Pattern: CreatedAt < DateTime("2001-12-07") or CreatedAt <= DateTime("2001-12-07")
            // Pattern: CreatedAt == DateTime("2001-12-07")
            var dateProperty = "CreatedAt";
            
            // Match date comparisons
            var datePattern = $@"{dateProperty}\s*(==|>=|<=|>|<)\s*DateTime\(""([^""]+)""\)";
            var dateMatches = Regex.Matches(decodedFilter, datePattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in dateMatches)
            {
                var op = match.Groups[1].Value;
                var dateStr = match.Groups[2].Value;
                
                // Parse date string (could be yyyy-MM-dd or yyyy-MM-ddTHH:mm:ss)
                if (DateTime.TryParse(dateStr, out var dateValue))
                {
                    // Extract just the date portion (ignore time)
                    var dateOnly = dateValue.Date;
                    
                    if (op == "==" || op == ">=")
                    {
                        // For == or >=, set the from date
                        if (!createdDateFrom.HasValue || dateOnly < createdDateFrom.Value)
                            createdDateFrom = dateOnly;
                    }
                    if (op == "==" || op == "<=")
                    {
                        // For == or <=, set the to date (end of day)
                        var endOfDay = dateOnly.AddDays(1).AddTicks(-1);
                        if (!createdDateTo.HasValue || endOfDay > createdDateTo.Value)
                            createdDateTo = endOfDay;
                    }
                    if (op == ">")
                    {
                        // For >, set from date to next day
                        createdDateFrom = dateOnly.AddDays(1);
                    }
                    if (op == "<")
                    {
                        // For <, set to date to previous day end
                        createdDateTo = dateOnly.AddTicks(-1);
                    }
                }
            }

            return (searchTerm, noteType, priority, isIgnoredByDoctor, createdDateFrom, createdDateTo);
        }
    }
}

