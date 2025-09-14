using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Text;
using System.Text.RegularExpressions;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IContentAnalysisService
    {
        Task<string> ExtractTextFromContentAsync(ContentItem content);
        Task<ContentAnalysis> AnalyzeContentAsync(ContentItem content);
        Task<List<ContentAnalysis>> GetContentAnalysisForPatientAsync(int patientId);
        Task<string> BuildEnhancedContextAsync(int patientId, string originalPrompt);
        Task<List<ContentAlert>> GenerateContentAlertsAsync(int patientId);
    }

    public class ContentAnalysisService : IContentAnalysisService
    {
        private readonly JournalDbContext _context;
        private readonly S3Service _s3Service;
        private readonly ILogger<ContentAnalysisService> _logger;

        public ContentAnalysisService(JournalDbContext context, S3Service s3Service, ILogger<ContentAnalysisService> logger)
        {
            _context = context;
            _s3Service = s3Service;
            _logger = logger;
        }

        public async Task<string> ExtractTextFromContentAsync(ContentItem content)
        {
            try
            {
                // Get file from S3
                var fileStream = await _s3Service.GetFileStreamAsync(content.S3Key);
                if (fileStream == null)
                {
                    _logger.LogWarning("Could not retrieve file for content {ContentId}", content.Id);
                    return string.Empty;
                }

                // Extract text based on content type
                return content.Type switch
                {
                    ContentType.Document => await ExtractTextFromDocumentAsync(fileStream, content.OriginalFileName),
                    ContentType.Image => await ExtractTextFromImageAsync(fileStream, content.OriginalFileName),
                    _ => await ExtractTextFromGenericFileAsync(fileStream, content.OriginalFileName)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from content {ContentId}", content.Id);
                return string.Empty;
            }
        }

        public async Task<ContentAnalysis> AnalyzeContentAsync(ContentItem content)
        {
            try
            {
                var extractedText = await ExtractTextFromContentAsync(content);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    return new ContentAnalysis
                    {
                        ContentId = content.Id,
                        ContentType = content.Type.ToString(),
                        ExtractedText = string.Empty,
                        AnalysisResults = new Dictionary<string, object>(),
                        Alerts = new List<string>(),
                        ProcessedAt = DateTime.UtcNow
                    };
                }

                var analysis = new ContentAnalysis
                {
                    ContentId = content.Id,
                    ContentType = content.Type.ToString(),
                    ExtractedText = extractedText,
                    AnalysisResults = new Dictionary<string, object>(),
                    Alerts = new List<string>(),
                    ProcessedAt = DateTime.UtcNow
                };

                // Analyze content based on type
                await AnalyzeDocumentContent(analysis, extractedText);
                await AnalyzeTestResults(analysis, extractedText);
                await AnalyzeMedicalReports(analysis, extractedText);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing content {ContentId}", content.Id);
                return new ContentAnalysis
                {
                    ContentId = content.Id,
                    ContentType = content.Type.ToString(),
                    ExtractedText = string.Empty,
                    AnalysisResults = new Dictionary<string, object>(),
                    Alerts = new List<string> { $"Analysis failed: {ex.Message}" },
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<List<ContentAnalysis>> GetContentAnalysisForPatientAsync(int patientId)
        {
            _logger.LogInformation("Getting content analysis for patient {PatientId}", patientId);

            var contents = await _context.Contents
                .Where(c => c.PatientId == patientId && c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            _logger.LogInformation("Found {ContentCount} content items for patient {PatientId}", contents.Count, patientId);

            var analyses = new List<ContentAnalysis>();
            foreach (var content in contents)
            {
                _logger.LogInformation("Analyzing content {ContentId} for patient {PatientId}", content.Id, patientId);
                var analysis = await AnalyzeContentAsync(content);
                analyses.Add(analysis);
                _logger.LogInformation("Content analysis completed for {ContentId}, alerts: {AlertCount}", content.Id, analysis.Alerts.Count);
            }

            return analyses;
        }

        public async Task<string> BuildEnhancedContextAsync(int patientId, string originalPrompt)
        {
            try
            {
                _logger.LogInformation("=== BUILDING ENHANCED CONTEXT FOR PATIENT {PatientId} ===", patientId);
                _logger.LogInformation("Original prompt: {OriginalPrompt}", originalPrompt);

                // If no patient is selected (patientId = 0), return empty context
                if (patientId <= 0)
                {
                    _logger.LogInformation("No patient selected (patientId = {PatientId}), returning empty context", patientId);
                    return originalPrompt;
                }

                _logger.LogInformation("Patient selected (patientId = {PatientId}), proceeding with context building", patientId);

                var context = new StringBuilder();

                // Get journal entries (existing functionality)
                _logger.LogInformation("=== JOURNAL ENTRIES QUERY DEBUG ===");
                _logger.LogInformation("Querying journal entries for patientId: {PatientId}", patientId);

                var recentEntries = await _context.JournalEntries
                    .Where(e => e.UserId == patientId)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("Found {JournalCount} journal entries for patient {PatientId}", recentEntries.Count, patientId);

                // Debug: Log each journal entry found
                foreach (var entry in recentEntries)
                {
                    _logger.LogInformation("Journal Entry - ID: {EntryId}, UserId: {UserId}, CreatedAt: {CreatedAt}, Mood: {Mood}",
                        entry.Id, entry.UserId, entry.CreatedAt, entry.Mood);
                }

                // Debug: Check total journal entries in database for this patient
                var totalEntriesForPatient = await _context.JournalEntries
                    .Where(e => e.UserId == patientId)
                    .CountAsync();
                _logger.LogInformation("Total journal entries in database for patient {PatientId}: {TotalCount}", patientId, totalEntriesForPatient);

                // Debug: Check total journal entries in database (all patients)
                var totalEntriesAll = await _context.JournalEntries.CountAsync();
                _logger.LogInformation("Total journal entries in database (all patients): {TotalCount}", totalEntriesAll);

                if (recentEntries.Any())
                {
                    context.AppendLine("=== RECENT JOURNAL ENTRIES ===");
                    foreach (var entry in recentEntries)
                    {
                        context.AppendLine($"[{entry.CreatedAt:MM/dd/yyyy}] Mood: {entry.Mood}");
                        context.AppendLine($"Entry: {entry.Text.Substring(0, Math.Min(200, entry.Text.Length))}...");
                        context.AppendLine();
                    }
                }

                // Get content analysis (NEW functionality)
                var contentAnalyses = await GetContentAnalysisForPatientAsync(patientId);
                _logger.LogInformation("Found {ContentCount} content analyses for patient {PatientId}", contentAnalyses.Count, patientId);

                if (contentAnalyses.Any())
                {
                    context.AppendLine("=== PATIENT CONTENT ANALYSIS ===");
                    foreach (var analysis in contentAnalyses.Take(3)) // Limit to 3 most recent
                    {
                        context.AppendLine($"Content Type: {analysis.ContentType}");
                        if (!string.IsNullOrEmpty(analysis.ExtractedText))
                        {
                            context.AppendLine($"Content Summary: {analysis.ExtractedText.Substring(0, Math.Min(300, analysis.ExtractedText.Length))}...");
                        }

                        if (analysis.Alerts.Any())
                        {
                            context.AppendLine($"⚠️ ALERTS: {string.Join(", ", analysis.Alerts)}");
                        }
                        context.AppendLine();
                    }
                }
                else
                {
                    _logger.LogInformation("No content analysis found for patient {PatientId}", patientId);
                }

                context.AppendLine($"=== USER QUESTION ===");
                context.AppendLine(originalPrompt);
                context.AppendLine();
                context.AppendLine("Please provide a comprehensive response considering both journal entries and medical content. Highlight any concerning patterns or important medical information that should be brought to the doctor's attention.");

                var result = context.ToString();
                _logger.LogInformation("Enhanced context built for patient {PatientId}, length: {Length}", patientId, result.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building enhanced context for patient {PatientId}", patientId);
                return originalPrompt; // Fallback to original prompt
            }
        }

        public async Task<List<ContentAlert>> GenerateContentAlertsAsync(int patientId)
        {
            var alerts = new List<ContentAlert>();
            var analyses = await GetContentAnalysisForPatientAsync(patientId);

            foreach (var analysis in analyses)
            {
                // Check for concerning patterns
                if (analysis.Alerts.Any())
                {
                    alerts.Add(new ContentAlert
                    {
                        ContentId = analysis.ContentId,
                        AlertType = "Content Analysis",
                        Message = string.Join(", ", analysis.Alerts),
                        Severity = "Warning",
                        CreatedAt = analysis.ProcessedAt
                    });
                }

                // Check for test result alerts
                if (analysis.AnalysisResults.ContainsKey("TestResults"))
                {
                    var testResults = analysis.AnalysisResults["TestResults"] as Dictionary<string, object>;
                    if (testResults != null && testResults.ContainsKey("AbnormalValues"))
                    {
                        alerts.Add(new ContentAlert
                        {
                            ContentId = analysis.ContentId,
                            AlertType = "Test Results",
                            Message = "Abnormal test values detected",
                            Severity = "High",
                            CreatedAt = analysis.ProcessedAt
                        });
                    }
                }
            }

            return alerts;
        }

        private async Task<string> ExtractTextFromDocumentAsync(Stream fileStream, string fileName)
        {
            // For now, return a placeholder. In production, you'd use libraries like:
            // - iTextSharp for PDFs
            // - DocumentFormat.OpenXml for Word docs
            // - EPPlus for Excel files

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "PDF content extraction not implemented yet",
                ".doc" or ".docx" => "Word document content extraction not implemented yet",
                ".txt" => await new StreamReader(fileStream).ReadToEndAsync(),
                _ => "Document content extraction not implemented yet"
            };
        }

        private async Task<string> ExtractTextFromImageAsync(Stream fileStream, string fileName)
        {
            // For OCR, you'd use libraries like:
            // - Tesseract.NET
            // - Azure Computer Vision
            // - Google Cloud Vision API

            return "Image OCR not implemented yet";
        }

        private async Task<string> ExtractTextFromGenericFileAsync(Stream fileStream, string fileName)
        {
            try
            {
                using var reader = new StreamReader(fileStream);
                return await reader.ReadToEndAsync();
            }
            catch
            {
                return "Could not extract text from file";
            }
        }

        private async Task AnalyzeDocumentContent(ContentAnalysis analysis, string text)
        {
            // Basic keyword analysis
            var keywords = new Dictionary<string, int>
            {
                ["medication"] = 0,
                ["symptom"] = 0,
                ["pain"] = 0,
                ["anxiety"] = 0,
                ["depression"] = 0,
                ["sleep"] = 0,
                ["mood"] = 0
            };

            var lowerText = text.ToLowerInvariant();
            foreach (var keyword in keywords.Keys.ToList())
            {
                keywords[keyword] = Regex.Matches(lowerText, keyword).Count;
            }

            analysis.AnalysisResults["Keywords"] = keywords;
        }

        private async Task AnalyzeTestResults(ContentAnalysis analysis, string text)
        {
            // Look for test result patterns
            var testPatterns = new Dictionary<string, string>
            {
                [@"blood pressure[:\s]+(\d+)/(\d+)"] = "Blood Pressure",
                [@"heart rate[:\s]+(\d+)"] = "Heart Rate",
                [@"temperature[:\s]+(\d+\.?\d*)"] = "Temperature",
                [@"glucose[:\s]+(\d+\.?\d*)"] = "Glucose",
                [@"cholesterol[:\s]+(\d+\.?\d*)"] = "Cholesterol"
            };

            var testResults = new Dictionary<string, object>();
            var abnormalValues = new List<string>();

            foreach (var pattern in testPatterns)
            {
                var matches = Regex.Matches(text, pattern.Key, RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    var testName = pattern.Value;
                    var values = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                    testResults[testName] = values;

                    // Basic abnormal value detection (simplified)
                    if (testName == "Blood Pressure" && values.Count >= 2)
                    {
                        if (int.TryParse(values[0], out int systolic) && int.TryParse(values[1], out int diastolic))
                        {
                            if (systolic > 140 || diastolic > 90)
                            {
                                abnormalValues.Add($"High blood pressure: {systolic}/{diastolic}");
                            }
                        }
                    }
                }
            }

            if (testResults.Any())
            {
                analysis.AnalysisResults["TestResults"] = testResults;
            }

            if (abnormalValues.Any())
            {
                analysis.AnalysisResults["AbnormalValues"] = abnormalValues;
                analysis.Alerts.AddRange(abnormalValues);
            }
        }

        private async Task AnalyzeMedicalReports(ContentAnalysis analysis, string text)
        {
            // Look for concerning medical terms
            var concerningTerms = new[]
            {
                "abnormal", "elevated", "high", "low", "critical", "urgent", "severe",
                "worsening", "deteriorating", "complications", "side effects"
            };

            var lowerText = text.ToLowerInvariant();
            var foundConcerns = concerningTerms.Where(term => lowerText.Contains(term)).ToList();

            if (foundConcerns.Any())
            {
                analysis.Alerts.Add($"Concerning terms found: {string.Join(", ", foundConcerns)}");
            }
        }
    }

    public class ContentAnalysis
    {
        public int ContentId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public Dictionary<string, object> AnalysisResults { get; set; } = new();
        public List<string> Alerts { get; set; } = new();
        public DateTime ProcessedAt { get; set; }
    }

    public class ContentAlert
    {
        public int ContentId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
