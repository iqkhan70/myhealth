using System.Text.RegularExpressions;

namespace SM_MentalHealthApp.Client.Helpers
{
    /// <summary>
    /// Helper class to transform filter strings from RadzenDataGrid (C# syntax) to OData syntax
    /// </summary>
    public static class ODataFilterHelper
    {
        /// <summary>
        /// Transforms a RadzenDataGrid filter (C# syntax) to OData filter syntax with case-insensitive comparisons
        /// </summary>
        public static string TransformToOData(string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return filter ?? string.Empty;

            System.Diagnostics.Debug.WriteLine($"[ODataFilterHelper] Input filter: {filter}");
            var result = filter;

            // List of string properties that should be case-insensitive
            var stringProperties = new[]
            {
                "FirstName", "LastName", "Email", "MobilePhone", "Gender",
                "Specialization", "LicenseNumber", "Race", "AccidentAddress",
                "VehicleDetails", "PoliceCaseNumber", "AccidentDetails",
                "RoadConditions", "DoctorsInformation", "LawyersInformation",
                "AdditionalNotes",
                // Appointment properties (Note: DoctorName and PatientName are DTO-only, handled separately)
                "Reason", "Notes", "CreatedBy",
                // Content properties
                "Title", "Description", "OriginalFileName", "MimeType"
            };

            // === STRING FILTER TRANSFORMS (your existing code) ===

            foreach (var prop in stringProperties)
            {
                // contains(...)
                // Pattern 1: ((Property == null ? "" : Property).ToLower()).Contains("value".ToLower())
                result = Regex.Replace(result,
                    $@"\(\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\)\.Contains\((""([^""]*)""|'([^']*)')\.ToLower\(\)\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        value = value.Replace("'", "''");
                        System.Diagnostics.Debug.WriteLine($"[ODataFilterHelper] Matched pattern 1 for {prop}: {value}");
                        return $"contains({prop}, '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                // Pattern 2: (Property == null ? "" : Property).ToLower().Contains("value".ToLower())
                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\.Contains\((""([^""]*)""|'([^']*)')\.ToLower\(\)\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        value = value.Replace("'", "''");
                        System.Diagnostics.Debug.WriteLine($"[ODataFilterHelper] Matched pattern 2 for {prop}: {value}");
                        return $"contains({prop}, '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                // Pattern 3: (Property == null ? "" : Property).ToLower.Contains("value".ToLower) - WITHOUT parentheses around ToLower/Contains
                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\.Contains\((""([^""]*)""|'([^']*)')\.ToLower\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        value = value.Replace("'", "''");
                        System.Diagnostics.Debug.WriteLine($"[ODataFilterHelper] Matched pattern 3 for {prop}: {value}");
                        return $"contains({prop}, '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                // startswith(...)
                result = Regex.Replace(result,
                    $@"\(\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\)\.StartsWith\((""([^""]*)""|'([^']*)')\.ToLower\(\)\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"startswith(tolower({prop}), '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\.StartsWith\((""([^""]*)""|'([^']*)')\.ToLower\(\)\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"startswith(tolower({prop}), '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                // Pattern without parentheses: (Property == null ? "" : Property).ToLower.StartsWith("value".ToLower)
                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\.StartsWith\((""([^""]*)""|'([^']*)')\.ToLower\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"startswith(tolower({prop}), '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                // endswith(...)
                result = Regex.Replace(result,
                    $@"\(\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\)\.EndsWith\((""([^""]*)""|'([^']*)')\.ToLower\(\)\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"endswith(tolower({prop}), '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\.EndsWith\((""([^""]*)""|'([^']*)')\.ToLower\(\)\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"endswith(tolower({prop}), '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                // Pattern without parentheses: (Property == null ? "" : Property).ToLower.EndsWith("value".ToLower)
                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\.EndsWith\((""([^""]*)""|'([^']*)')\.ToLower\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"endswith(tolower({prop}), '{value}')";
                    },
                    RegexOptions.IgnoreCase);

                // equals
                result = Regex.Replace(result,
                    $@"\(\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\)\s*==\s*(""([^""]*)""|'([^']*)')\.ToLower\(\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"tolower({prop}) eq '{value}'";
                    },
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\s*==\s*(""([^""]*)""|'([^']*)')\.ToLower\(\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"tolower({prop}) eq '{value}'";
                    },
                    RegexOptions.IgnoreCase);

                // Pattern without parentheses: (Property == null ? "" : Property).ToLower == "value".ToLower
                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\s*==\s*(""([^""]*)""|'([^']*)')\.ToLower",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"tolower({prop}) eq '{value}'";
                    },
                    RegexOptions.IgnoreCase);

                // not equals
                result = Regex.Replace(result,
                    $@"\(\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\)\s*!=\s*(""([^""]*)""|'([^']*)')\.ToLower\(\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"tolower({prop}) ne '{value}'";
                    },
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\(\)\s*!=\s*(""([^""]*)""|'([^']*)')\.ToLower\(\)",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"tolower({prop}) ne '{value}'";
                    },
                    RegexOptions.IgnoreCase);

                // Pattern without parentheses: (Property == null ? "" : Property).ToLower != "value".ToLower
                result = Regex.Replace(result,
                    $@"\({prop}\s*==\s*null\s*\?\s*""""\s*:\s*{prop}\)\.ToLower\s*!=\s*(""([^""]*)""|'([^']*)')\.ToLower",
                    match =>
                    {
                        var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
                        return $"tolower({prop}) ne '{value}'";
                    },
                    RegexOptions.IgnoreCase);
            }

            // Also handle simple OData-style filters (if Radzen generates them directly)
            foreach (var prop in stringProperties)
            {
                result = Regex.Replace(result,
                    $@"\b{prop}\s+(eq|ne)\s+'([^']*)'",
                    $"tolower({prop}) $1 tolower('$2')",
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(result,
                    $@"\b{prop}\s+(contains|startswith|endswith)\s+'([^']*)'",
                    match =>
                    {
                        var func = match.Groups[1].Value;
                        var value = match.Groups[2].Value;

                        if (func == "contains")
                            return $"contains({prop}, '{value}')";
                        else if (func == "startswith")
                            return $"startswith({prop}, '{value}')";
                        else
                            return $"endswith({prop}, '{value}')";
                    },
                    RegexOptions.IgnoreCase);
            }

            // === NEW: DATE FILTER TRANSFORMS ===
            // Note: DateOfBirth is handled specially on the server (encrypted PII)
            // The filter syntax is transformed here, but the server will handle it after decryption
            var dateProperties = new[]
            {
                "DateOfBirth", // Handled specially on server after decryption
                "CreatedAt",
                "LastLoginAt",
                "AccidentDate",
                "DateReported",
                // Appointment properties
                "AppointmentDateTime",
                // Content properties
                "LastAccessedAt", "IgnoredAt"
            };

            foreach (var prop in dateProperties)
            {
                // Match patterns like:
                // DateOfBirth > DateTime("2001-12-07")
                // DateOfBirth >= DateTime("2001-12-07")
                // DateOfBirth == DateTime("2001-12-07")
                result = Regex.Replace(result,
                    $@"\b{prop}\s*(==|!=|>=|<=|>|<)\s*DateTime\(""(.*?)""\)",
                    match =>
                    {
                        var op = match.Groups[1].Value;      // >, <, >=, <=, ==, !=
                        var dateStr = match.Groups[2].Value; // 2001-12-07 or 2001-12-07T10:30:00

                        var odataOp = op switch
                        {
                            "==" => "eq",
                            "!=" => "ne",
                            ">=" => "ge",
                            "<=" => "le",
                            ">" => "gt",
                            "<" => "lt",
                            _ => "eq"
                        };

                        string iso;
                        if (dateStr.Contains("T"))
                        {
                            // Full datetime provided - extract just the date portion
                            // Remove time and timezone to use date-only format
                            var dateOnly = dateStr.Substring(0, 10); // Extract yyyy-MM-dd
                            iso = dateOnly;
                        }
                        else
                        {
                            // Only date provided - use as-is (date-only format)
                            iso = dateStr;
                        }

                        // For date-only filters, always use date-only format (yyyy-MM-dd) without time
                        // OData will handle the date comparison correctly
                        if (op == "==")
                        {
                            // For equality, use date-only format
                            return $"{prop} eq {iso}";
                        }
                        else if (op == "!=")
                        {
                            // For not equal, use date-only format
                            return $"{prop} ne {iso}";
                        }
                        else
                        {
                            // For comparison operators, use date-only format
                            // OData will compare dates correctly
                            return $"{prop} {odataOp} {iso}";
                        }
                    },
                    RegexOptions.IgnoreCase);
            }

            System.Diagnostics.Debug.WriteLine($"[ODataFilterHelper] Output: {result}");

            // Debug: Check if Title was transformed
            if (filter.Contains("Title", StringComparison.OrdinalIgnoreCase) && !result.Contains("Title", StringComparison.OrdinalIgnoreCase) && !result.Contains("contains", StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"[ODataFilterHelper] WARNING: Title filter may not have been transformed correctly!");
                System.Diagnostics.Debug.WriteLine($"[ODataFilterHelper] Original: {filter}");
                System.Diagnostics.Debug.WriteLine($"[ODataFilterHelper] Result: {result}");
            }

            return result;
        }
    }
}
