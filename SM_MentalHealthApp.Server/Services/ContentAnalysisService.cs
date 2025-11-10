using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Tesseract;
using Azure.AI.Vision.ImageAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using Path = System.IO.Path;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IContentAnalysisService
    {
        Task<string> ExtractTextFromContentAsync(ContentItem content);
        Task<SM_MentalHealthApp.Shared.ContentAnalysis> AnalyzeContentAsync(ContentItem content);
        Task<List<SM_MentalHealthApp.Shared.ContentAnalysis>> GetContentAnalysisForPatientAsync(int patientId);
        Task<string> BuildEnhancedContextAsync(int patientId, string originalPrompt);
        Task<List<SM_MentalHealthApp.Shared.ContentAlert>> GenerateContentAlertsAsync(int patientId);
        Task ProcessAllUnanalyzedContentAsync();
        Task<List<ClinicalNoteDto>> SearchClinicalNotesWithAIAsync(string searchTerm, int? patientId = null, int? doctorId = null);
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
                _logger.LogInformation("Extracting text from content {ContentId}, S3Key: {S3Key}, Type: {Type}",
                    content.Id, content.S3Key, content.ContentTypeModel?.Name ?? "Unknown");

                // Get file from S3
                var fileStream = await _s3Service.GetFileStreamAsync(content.S3Key);
                if (fileStream == null)
                {
                    _logger.LogWarning("Could not retrieve file for content {ContentId} with S3Key: {S3Key}", content.Id, content.S3Key);
                    return string.Empty;
                }

                _logger.LogInformation("Successfully retrieved file stream for content {ContentId}, stream length: {StreamLength}",
                    content.Id, fileStream.Length);

                // Extract text based on content type
                var contentTypeName = content.ContentTypeModel?.Name ?? "Document";
                var extractedText = contentTypeName switch
                {
                    "Document" => await ExtractTextFromDocumentAsync(fileStream, content.OriginalFileName),
                    "Image" => await ExtractTextFromImageAsync(fileStream, content.OriginalFileName),
                    "Video" => await ExtractTextFromVideoAsync(fileStream, content.OriginalFileName),
                    "Audio" => await ExtractTextFromAudioAsync(fileStream, content.OriginalFileName),
                    _ => await ExtractTextFromGenericFileAsync(fileStream, content.OriginalFileName)
                };

                _logger.LogInformation("Text extraction completed for content {ContentId}, extracted length: {ExtractedLength}",
                    content.Id, extractedText?.Length ?? 0);

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from content {ContentId}", content.Id);
                return string.Empty;
            }
        }

        public async Task<SM_MentalHealthApp.Shared.ContentAnalysis> AnalyzeContentAsync(ContentItem content)
        {
            try
            {
                _logger.LogInformation("Starting content analysis for content {ContentId} of type {ContentType}", content.Id, content.ContentTypeModel?.Name ?? "Unknown");

                // Check if analysis already exists to prevent duplicates
                var existingAnalysis = await _context.ContentAnalyses
                    .FirstOrDefaultAsync(ca => ca.ContentId == content.Id);

                if (existingAnalysis != null)
                {
                    _logger.LogInformation("Content analysis already exists for content {ContentId}, returning existing analysis", content.Id);
                    return existingAnalysis;
                }

                var extractedText = await ExtractTextFromContentAsync(content);
                _logger.LogInformation("Extracted text length: {TextLength} for content {ContentId}", extractedText?.Length ?? 0, content.Id);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    var emptyAnalysis = new SM_MentalHealthApp.Shared.ContentAnalysis
                    {
                        ContentId = content.Id,
                        ContentTypeName = content.ContentTypeModel?.Name ?? "Unknown",
                        ExtractedText = string.Empty,
                        AnalysisResults = new Dictionary<string, object>(),
                        Alerts = new List<string>(),
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingStatus = "Completed"
                    };

                    // Save to database
                    _context.ContentAnalyses.Add(emptyAnalysis);
                    await _context.SaveChangesAsync();
                    return emptyAnalysis;
                }

                var analysis = new SM_MentalHealthApp.Shared.ContentAnalysis
                {
                    ContentId = content.Id,
                    ContentTypeName = content.ContentTypeModel?.Name ?? "Unknown",
                    ExtractedText = extractedText,
                    AnalysisResults = new Dictionary<string, object>(),
                    Alerts = new List<string>(),
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingStatus = "Processing"
                };

                // Analyze content based on type
                await AnalyzeDocumentContent(analysis, extractedText);
                await AnalyzeTestResults(analysis, extractedText);
                await AnalyzeMedicalReports(analysis, extractedText);

                analysis.ProcessingStatus = "Completed";

                // Save to database with retry logic for race conditions
                try
                {
                    _context.ContentAnalyses.Add(analysis);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Content analysis saved to database for content {ContentId} with {AlertCount} alerts",
                        content.Id, analysis.Alerts.Count);
                }
                catch (Exception dbEx) when (dbEx.Message.Contains("duplicate") || dbEx.Message.Contains("unique"))
                {
                    _logger.LogWarning("Duplicate analysis detected for content {ContentId}, retrieving existing analysis", content.Id);
                    // If there's a duplicate, get the existing one
                    var duplicateAnalysis = await _context.ContentAnalyses
                        .FirstOrDefaultAsync(ca => ca.ContentId == content.Id);
                    if (duplicateAnalysis != null)
                    {
                        return duplicateAnalysis;
                    }
                    throw; // Re-throw if we can't find the existing analysis
                }


                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing content {ContentId}", content.Id);
                var errorAnalysis = new SM_MentalHealthApp.Shared.ContentAnalysis
                {
                    ContentId = content.Id,
                    ContentTypeName = content.ContentTypeModel?.Name ?? "Unknown",
                    ExtractedText = string.Empty,
                    AnalysisResults = new Dictionary<string, object>(),
                    Alerts = new List<string> { $"Analysis failed: {ex.Message}" },
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingStatus = "Failed",
                    ErrorMessage = ex.Message
                };

                // Save error to database
                _context.ContentAnalyses.Add(errorAnalysis);
                await _context.SaveChangesAsync();

                return errorAnalysis;
            }
        }

        public async Task<List<SM_MentalHealthApp.Shared.ContentAnalysis>> GetContentAnalysisForPatientAsync(int patientId)
        {
            _logger.LogInformation("Getting content analysis for patient {PatientId}", patientId);

            // First, let's check what content exists for this patient
            var patientContents = await _context.Contents
                .Where(c => c.PatientId == patientId && c.IsActive)
                .ToListAsync();

            _logger.LogInformation("Found {ContentCount} active content items for patient {PatientId}", patientContents.Count, patientId);

            foreach (var content in patientContents)
            {
                _logger.LogInformation("Content {ContentId}: {Title} - {ContentType} - Created: {CreatedAt}",
                    content.Id, content.Title, content.ContentTypeModel?.Name ?? "Unknown", content.CreatedAt);
            }

            // Get already processed content analyses from database
            var contentIds = patientContents.Select(c => c.Id).ToList();
            var analyses = await _context.ContentAnalyses
                .Where(ca => contentIds.Contains(ca.ContentId))
                .OrderByDescending(ca => ca.ProcessedAt)
                .Take(10) // Limit to recent analyses
                .ToListAsync();

            _logger.LogInformation("Found {AnalysisCount} existing content analyses for patient {PatientId}", analyses.Count, patientId);

            foreach (var analysis in analyses)
            {
                _logger.LogInformation("Analysis {AnalysisId}: ContentId={ContentId}, Status={Status}, Alerts={AlertCount}",
                    analysis.Id, analysis.ContentId, analysis.ProcessingStatus, analysis.Alerts.Count);
            }

            // If no analyses found, try to process any unprocessed content
            if (!analyses.Any())
            {
                _logger.LogInformation("No existing analyses found, checking for unprocessed content for patient {PatientId}", patientId);

                var unprocessedContents = await _context.Contents
                    .Where(c => c.PatientId == patientId && c.IsActive)
                    .Where(c => !_context.ContentAnalyses.Any(ca => ca.ContentId == c.Id))
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("Found {UnprocessedCount} unprocessed content items for patient {PatientId}", unprocessedContents.Count, patientId);

                foreach (var content in unprocessedContents)
                {
                    try
                    {
                        _logger.LogInformation("Processing unprocessed content {ContentId} for patient {PatientId}", content.Id, patientId);
                        var analysis = await AnalyzeContentAsync(content);
                        analyses.Add(analysis);
                        _logger.LogInformation("Content analysis completed for {ContentId}, alerts: {AlertCount}", content.Id, analysis.Alerts.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing content {ContentId} for patient {PatientId}", content.Id, patientId);
                    }
                }
            }

            return analyses;
        }

        public async Task<string> BuildEnhancedContextAsync(int patientId, string originalPrompt)
        {
            try
            {
                _logger.LogInformation("=== CONTENT ANALYSIS SERVICE CALLED ===");
                _logger.LogInformation("Original prompt: {OriginalPrompt}", originalPrompt);
                _logger.LogInformation("Patient ID: {PatientId}", patientId);

                // If no patient is selected (patientId = 0), return empty context
                if (patientId <= 0)
                {
                    _logger.LogInformation("No patient selected (patientId = {PatientId}), returning empty context", patientId);
                    return originalPrompt;
                }

                _logger.LogInformation("Patient selected (patientId = {PatientId}), proceeding with context building", patientId);

                var context = new StringBuilder();

                // Get clinical notes for the patient
                var recentClinicalNotes = await _context.ClinicalNotes
                    .Where(cn => cn.PatientId == patientId && cn.IsActive)
                    .OrderByDescending(cn => cn.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("Found {ClinicalNotesCount} clinical notes for patient {PatientId}", recentClinicalNotes.Count, patientId);

                if (recentClinicalNotes.Any())
                {
                    context.AppendLine("=== RECENT CLINICAL NOTES ===");
                    foreach (var note in recentClinicalNotes)
                    {
                        context.AppendLine($"[{note.CreatedAt:MM/dd/yyyy}] {note.Title} ({note.NoteType})");
                        if (!string.IsNullOrEmpty(note.Content))
                        {
                            context.AppendLine($"Content: {note.Content.Substring(0, Math.Min(200, note.Content.Length))}...");
                        }
                        if (!string.IsNullOrEmpty(note.Tags))
                        {
                            context.AppendLine($"Tags: {note.Tags}");
                        }
                        context.AppendLine($"Priority: {note.Priority}");
                        context.AppendLine();
                    }
                }

                // Get journal entries (existing functionality)
                var recentEntries = await _context.JournalEntries
                    .Where(e => e.UserId == patientId)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("Found {JournalCount} journal entries for patient {PatientId}", recentEntries.Count, patientId);

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

                // Get emergency incidents for the patient
                var recentEmergencies = await _context.EmergencyIncidents
                    .Where(e => e.PatientId == patientId)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(3)
                    .ToListAsync();

                _logger.LogInformation("Found {EmergencyCount} emergency incidents for patient {PatientId}", recentEmergencies.Count, patientId);

                if (recentEmergencies.Any())
                {
                    context.AppendLine("=== RECENT EMERGENCY INCIDENTS ===");
                    foreach (var emergency in recentEmergencies)
                    {
                        context.AppendLine($"[{emergency.Timestamp:MM/dd/yyyy HH:mm}] {emergency.EmergencyType} - {emergency.Severity}");
                        if (!string.IsNullOrEmpty(emergency.Message))
                        {
                            context.AppendLine($"Message: {emergency.Message}");
                        }
                        if (!string.IsNullOrEmpty(emergency.VitalSignsJson))
                        {
                            try
                            {
                                var vitals = JsonSerializer.Deserialize<dynamic>(emergency.VitalSignsJson);
                                context.AppendLine($"Vital Signs: {emergency.VitalSignsJson}");
                            }
                            catch
                            {
                                context.AppendLine($"Vital Signs: Available but not parsed");
                            }
                        }
                        context.AppendLine($"Status: {(emergency.IsAcknowledged ? "Acknowledged" : "Pending")}");

                        // Add clinical significance for specific emergency types
                        switch (emergency.EmergencyType.ToLower())
                        {
                            case "fall":
                                context.AppendLine("CLINICAL SIGNIFICANCE: Fall incidents are critical for patient safety and may indicate:");
                                context.AppendLine("- Risk of injury, fractures, or head trauma");
                                context.AppendLine("- Potential medication side effects or dizziness");
                                context.AppendLine("- Need for mobility assessment and fall prevention measures");
                                context.AppendLine("- Possible underlying health conditions affecting balance");
                                break;
                            case "cardiac":
                                context.AppendLine("CLINICAL SIGNIFICANCE: Cardiac emergencies require immediate attention for:");
                                context.AppendLine("- Heart attack, arrhythmia, or cardiac arrest risk");
                                context.AppendLine("- Vital signs monitoring and emergency intervention");
                                context.AppendLine("- Medication review and cardiac assessment");
                                break;
                            case "panic attack":
                                context.AppendLine("CLINICAL SIGNIFICANCE: Panic attacks indicate:");
                                context.AppendLine("- Acute anxiety episode requiring immediate intervention");
                                context.AppendLine("- Need for breathing techniques and calming strategies");
                                context.AppendLine("- Possible medication adjustment or crisis intervention");
                                break;
                            case "seizure":
                                context.AppendLine("CLINICAL SIGNIFICANCE: Seizure incidents require:");
                                context.AppendLine("- Immediate medical attention and safety measures");
                                context.AppendLine("- Neurological assessment and medication review");
                                context.AppendLine("- Monitoring for post-seizure complications");
                                break;
                            case "overdose":
                                context.AppendLine("CLINICAL SIGNIFICANCE: Overdose incidents are life-threatening:");
                                context.AppendLine("- Immediate emergency medical intervention required");
                                context.AppendLine("- Risk of respiratory depression and organ damage");
                                context.AppendLine("- Need for substance abuse assessment and intervention");
                                break;
                            case "self harm":
                                context.AppendLine("CLINICAL SIGNIFICANCE: Self-harm incidents indicate:");
                                context.AppendLine("- Acute mental health crisis requiring immediate intervention");
                                context.AppendLine("- Risk of suicide or serious injury");
                                context.AppendLine("- Need for crisis counseling and safety planning");
                                break;
                        }
                        context.AppendLine();
                    }

                    // Add explicit instruction to prioritize emergency data
                    context.AppendLine("⚠️ CRITICAL: The above emergency incidents require IMMEDIATE ATTENTION and should be the PRIMARY FOCUS of your response!");
                    context.AppendLine();
                }

                // Get content analyses for medical data (excluding ignored content)
                var allContentAnalyses = await GetContentAnalysisForPatientAsync(patientId);
                // Filter out analyses for content items that have been ignored by doctors
                allContentAnalyses = allContentAnalyses
                    .Where(ca => !_context.Contents.Any(c => c.Id == ca.ContentId && c.IsIgnoredByDoctor))
                    .ToList();
                _logger.LogInformation("Found {ContentCount} content analyses for patient {PatientId} (after filtering ignored items)", allContentAnalyses.Count, patientId);

                if (allContentAnalyses.Any())
                {
                    // Sort by date - most recent first
                    var sortedAnalyses = allContentAnalyses.OrderByDescending(a => a.ProcessedAt).ToList();
                    var latestAnalysis = sortedAnalyses.First();

                    context.AppendLine("=== MEDICAL DATA SUMMARY ===");
                    context.AppendLine($"Latest Update: {latestAnalysis.ProcessedAt:MM/dd/yyyy HH:mm}");
                    context.AppendLine($"Content Type: {latestAnalysis.ContentTypeName}");

                    if (!string.IsNullOrEmpty(latestAnalysis.ExtractedText))
                    {
                        context.AppendLine($"Current Test Results: {latestAnalysis.ExtractedText}");
                        context.AppendLine();
                    }

                    // Show progression analysis - compare latest with previous results
                    if (sortedAnalyses.Count > 1)
                    {
                        var previousAnalysis = sortedAnalyses[1];
                        context.AppendLine("=== PROGRESSION ANALYSIS ===");
                        context.AppendLine($"Previous Results ({previousAnalysis.ProcessedAt:MM/dd/yyyy HH:mm}): {previousAnalysis.ExtractedText}");
                        context.AppendLine($"Current Results ({latestAnalysis.ProcessedAt:MM/dd/yyyy HH:mm}): {latestAnalysis.ExtractedText}");
                        context.AppendLine();

                        // Analyze improvement or deterioration
                        var currentHasCritical = latestAnalysis.AnalysisResults.ContainsKey("CriticalValues");
                        var previousHasCritical = previousAnalysis.AnalysisResults.ContainsKey("CriticalValues");
                        var currentHasNormal = latestAnalysis.AnalysisResults.ContainsKey("NormalValues");
                        var previousHasNormal = previousAnalysis.AnalysisResults.ContainsKey("NormalValues");

                        if (previousHasCritical && !currentHasCritical && currentHasNormal)
                        {
                            context.AppendLine("✅ **IMPROVEMENT NOTED:** Previous results showed critical values, but current results show normal values. This indicates positive progress, though continued monitoring is recommended.");
                        }
                        else if (!previousHasCritical && currentHasCritical)
                        {
                            context.AppendLine("⚠️ **DETERIORATION NOTED:** Current results show critical values where previous results were normal. Immediate attention may be required.");
                        }
                        else if (previousHasCritical && currentHasCritical)
                        {
                            context.AppendLine("🚨 **CRITICAL VALUES PERSIST:** Both previous and current results show critical values. Immediate medical attention is required.");
                        }
                        else
                        {
                            context.AppendLine("📊 **STABLE CONDITION:** Results remain consistent between previous and current tests.");
                        }
                        context.AppendLine();
                    }

                    // Show current status analysis - simplified and professional
                    var hasCriticalValues = latestAnalysis.AnalysisResults.ContainsKey("CriticalValues");
                    var hasAbnormalValues = latestAnalysis.AnalysisResults.ContainsKey("AbnormalValues");
                    var hasNormalValues = latestAnalysis.AnalysisResults.ContainsKey("NormalValues");

                    if (hasCriticalValues || hasAbnormalValues || hasNormalValues)
                    {
                        context.AppendLine("Current Status Assessment:");

                        if (hasCriticalValues)
                        {
                            var criticalValuesElement = latestAnalysis.AnalysisResults["CriticalValues"];
                            if (criticalValuesElement is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                            {
                                var criticalValues = jsonElement.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                                context.AppendLine("  🚨 Critical Values: " + string.Join(", ", criticalValues));
                            }
                        }

                        if (hasAbnormalValues)
                        {
                            var abnormalValuesElement = latestAnalysis.AnalysisResults["AbnormalValues"];
                            if (abnormalValuesElement is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                            {
                                var abnormalValues = jsonElement.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                                context.AppendLine("  ⚠️ Abnormal Values: " + string.Join(", ", abnormalValues));
                            }
                        }

                        if (hasNormalValues)
                        {
                            var normalValuesElement = latestAnalysis.AnalysisResults["NormalValues"];
                            if (normalValuesElement is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                            {
                                var normalValues = jsonElement.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                                context.AppendLine("  ✅ Normal Values: " + string.Join(", ", normalValues));
                            }
                        }
                        context.AppendLine();
                    }

                    // Show recent patient activity from journal entries
                    if (recentEntries.Any())
                    {
                        context.AppendLine("Recent Patient Activity:");
                        foreach (var entry in recentEntries.Take(3))
                        {
                            context.AppendLine($"  [{entry.CreatedAt:MM/dd/yyyy}] Mood: {entry.Mood}");
                        }
                        context.AppendLine();
                    }
                }
                else
                {
                    _logger.LogInformation("No content analysis found for patient {PatientId}", patientId);
                    context.AppendLine("=== NO MEDICAL CONTENT ANALYSIS AVAILABLE ===");
                    context.AppendLine("No uploaded medical documents or test results found for this patient.");
                    context.AppendLine();
                }

                // Add emergency trend analysis
                if (recentEmergencies.Any())
                {
                    context.AppendLine("=== EMERGENCY TREND ANALYSIS ===");

                    var criticalEmergencies = recentEmergencies.Count(e => e.Severity == "Critical");
                    var highEmergencies = recentEmergencies.Count(e => e.Severity == "High");
                    var acknowledgedEmergencies = recentEmergencies.Count(e => e.IsAcknowledged);

                    if (criticalEmergencies > 0)
                    {
                        context.AppendLine($"🚨 CRITICAL ALERT: {criticalEmergencies} critical emergency(ies) in recent history");
                    }
                    if (highEmergencies > 0)
                    {
                        context.AppendLine($"⚠️ HIGH PRIORITY: {highEmergencies} high-severity emergency(ies) in recent history");
                    }

                    var emergencyTypes = recentEmergencies.GroupBy(e => e.EmergencyType)
                        .Select(g => $"{g.Key} ({g.Count()})")
                        .ToList();
                    context.AppendLine($"Emergency Types: {string.Join(", ", emergencyTypes)}");

                    // Add specific analysis for Fall incidents
                    var fallIncidents = recentEmergencies.Where(e => e.EmergencyType.ToLower() == "fall").ToList();
                    if (fallIncidents.Any())
                    {
                        context.AppendLine($"⚠️ FALL RISK ALERT: {fallIncidents.Count} fall incident(s) detected - this is a critical safety concern");
                        context.AppendLine("   - Falls are the leading cause of injury in elderly and at-risk patients");
                        context.AppendLine("   - Multiple falls may indicate declining mobility or medication issues");
                        context.AppendLine("   - Immediate fall risk assessment and prevention measures recommended");
                        context.AppendLine("   - Consider physical therapy, home safety evaluation, and medication review");
                    }

                    var responseRate = recentEmergencies.Count > 0 ? (double)acknowledgedEmergencies / recentEmergencies.Count * 100 : 100;
                    context.AppendLine($"Response Rate: {responseRate:F1}% ({acknowledgedEmergencies}/{recentEmergencies.Count} acknowledged)");

                    if (responseRate < 100)
                    {
                        context.AppendLine("⚠️ Some emergencies are still pending acknowledgment");
                    }

                    context.AppendLine();
                }

                context.AppendLine($"=== USER QUESTION ===");
                context.AppendLine(originalPrompt);
                context.AppendLine();
                context.AppendLine("INSTRUCTIONS FOR AI RESPONSE:");
                context.AppendLine("RESPOND WITH THIS EXACT FORMAT:");
                context.AppendLine("1. Start with: '🚨 CRITICAL EMERGENCY ALERT: [number] emergency incidents detected'");
                context.AppendLine("2. List each emergency incident");
                context.AppendLine("3. Then discuss other medical data");
                context.AppendLine("4. Keep response under 200 words");

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

        public async Task<List<SM_MentalHealthApp.Shared.ContentAlert>> GenerateContentAlertsAsync(int patientId)
        {
            var alerts = new List<SM_MentalHealthApp.Shared.ContentAlert>();
            var analyses = await GetContentAnalysisForPatientAsync(patientId);

            foreach (var analysis in analyses)
            {
                // Check for concerning patterns
                if (analysis.Alerts.Any())
                {
                    alerts.Add(new SM_MentalHealthApp.Shared.ContentAlert
                    {
                        ContentId = analysis.ContentId,
                        AlertType = "Content Analysis",
                        Title = "Content Analysis Alert",
                        Description = string.Join(", ", analysis.Alerts),
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
                        alerts.Add(new SM_MentalHealthApp.Shared.ContentAlert
                        {
                            ContentId = analysis.ContentId,
                            PatientId = patientId,
                            AlertType = "Test Results",
                            Title = "Abnormal Test Values",
                            Description = "Abnormal test values detected",
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
            try
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                return extension switch
                {
                    ".pdf" => await ExtractTextFromPdfAsync(fileStream),
                    ".doc" or ".docx" => await ExtractTextFromWordAsync(fileStream),
                    ".txt" => await new StreamReader(fileStream).ReadToEndAsync(),
                    ".rtf" => await ExtractTextFromRtfAsync(fileStream),
                    _ => await ExtractTextFromGenericFileAsync(fileStream, fileName)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from document {FileName}", fileName);
                return $"Error extracting text from {fileName}: {ex.Message}";
            }
        }

        private async Task<string> ExtractTextFromPdfAsync(Stream fileStream)
        {
            try
            {
                using var pdfReader = new PdfReader(fileStream);
                var text = new StringBuilder();

                for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    text.AppendLine(PdfTextExtractor.GetTextFromPage(pdfReader, page));
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                return $"PDF text extraction failed: {ex.Message}";
            }
        }

        private async Task<string> ExtractTextFromWordAsync(Stream fileStream)
        {
            try
            {
                using var document = WordprocessingDocument.Open(fileStream, false);
                var body = document.MainDocumentPart?.Document?.Body;

                if (body == null)
                    return "No content found in Word document";

                var text = new StringBuilder();
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from Word document");
                return $"Word document text extraction failed: {ex.Message}";
            }
        }

        private async Task<string> ExtractTextFromRtfAsync(Stream fileStream)
        {
            try
            {
                // Enhanced RTF text extraction for medical documents
                var content = await new StreamReader(fileStream).ReadToEndAsync();


                // Remove RTF control words but preserve text
                var plainText = Regex.Replace(content, @"\\[a-z]+\d*\s?", " ");

                // Remove RTF braces but preserve content inside
                plainText = Regex.Replace(plainText, @"[{}]", " ");

                // Clean up multiple spaces and newlines
                plainText = Regex.Replace(plainText, @"\s+", " ");
                plainText = plainText.Trim();


                return plainText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from RTF document");
                return $"RTF text extraction failed: {ex.Message}";
            }
        }

        private async Task<string> ExtractTextFromImageAsync(Stream fileStream, string fileName)
        {
            try
            {
                // First try Azure Computer Vision for better accuracy
                var azureText = await ExtractTextWithAzureVisionAsync(fileStream);
                if (!string.IsNullOrWhiteSpace(azureText))
                {
                    return azureText;
                }

                // Fallback to Tesseract OCR
                return await ExtractTextWithTesseractAsync(fileStream, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from image {FileName}", fileName);
                return $"Image OCR failed: {ex.Message}";
            }
        }

        private async Task<string> ExtractTextWithAzureVisionAsync(Stream fileStream)
        {
            try
            {
                // Note: This requires Azure Computer Vision service setup
                // For now, return empty string - will be implemented when Azure credentials are configured
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Computer Vision not available, falling back to Tesseract");
                return string.Empty;
            }
        }

        private async Task<string> ExtractTextWithTesseractAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Convert stream to image
                using var image = Image.FromStream(fileStream);
                using var bitmap = new Bitmap(image);

                // Save to temporary file for Tesseract
                var tempPath = Path.GetTempFileName();
                bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

                try
                {
                    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
                    using var img = Pix.LoadFromFile(tempPath);
                    using var page = engine.Process(img);

                    var text = page.GetText();
                    return text?.Trim() ?? string.Empty;
                }
                finally
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tesseract OCR failed for {FileName}", fileName);
                return $"OCR processing failed: {ex.Message}";
            }
        }

        private async Task<string> ExtractTextFromVideoAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Extract frames from video and process them with OCR
                var frames = await ExtractFramesFromVideoAsync(fileStream, fileName);
                var text = new StringBuilder();

                foreach (var frame in frames)
                {
                    var frameText = await ExtractTextWithTesseractAsync(new MemoryStream(frame), fileName);
                    if (!string.IsNullOrWhiteSpace(frameText))
                    {
                        text.AppendLine($"Frame: {frameText}");
                    }
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from video {FileName}", fileName);
                return $"Video processing failed: {ex.Message}";
            }
        }

        private async Task<string> ExtractTextFromAudioAsync(Stream fileStream, string fileName)
        {
            try
            {
                // For now, return placeholder - would need speech-to-text service
                // In production, use Azure Speech Services, Google Speech-to-Text, or AWS Transcribe
                return "Audio transcription not implemented yet. Would use speech-to-text service to convert audio to text.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from audio {FileName}", fileName);
                return $"Audio processing failed: {ex.Message}";
            }
        }

        private async Task<List<byte[]>> ExtractFramesFromVideoAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Extract key frames from video for OCR processing
                // This is a simplified implementation - in production, use FFmpeg or similar
                var frames = new List<byte[]>();

                // For now, return empty list - would need FFmpeg integration
                // In production, extract frames at regular intervals (e.g., every 5 seconds)

                return frames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting frames from video {FileName}", fileName);
                return new List<byte[]>();
            }
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

        private async Task AnalyzeDocumentContent(SM_MentalHealthApp.Shared.ContentAnalysis analysis, string text)
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

        private async Task AnalyzeTestResults(SM_MentalHealthApp.Shared.ContentAnalysis analysis, string text)
        {
            // Look for test result patterns with more comprehensive patterns
            var testPatterns = new Dictionary<string, string>
            {
                [@"blood pressure[:\s]+(\d+)/(\d+)"] = "Blood Pressure",
                [@"bp[:\s]+(\d+)/(\d+)"] = "Blood Pressure",
                [@"hemoglobin[:\s]+(\d+\.?\d*)"] = "Hemoglobin",
                [@"hgb[:\s]+(\d+\.?\d*)"] = "Hemoglobin",
                [@"triglycerides[:\s]+(\d+\.?\d*)"] = "Triglycerides",
                [@"heart rate[:\s]+(\d+)"] = "Heart Rate",
                [@"temperature[:\s]+(\d+\.?\d*)"] = "Temperature",
                [@"glucose[:\s]+(\d+\.?\d*)"] = "Glucose",
                [@"cholesterol[:\s]+(\d+\.?\d*)"] = "Cholesterol"
            };

            var testResults = new Dictionary<string, object>();
            var abnormalValues = new List<string>();
            var criticalValues = new List<string>();

            foreach (var pattern in testPatterns)
            {
                var matches = Regex.Matches(text, pattern.Key, RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    var testName = pattern.Value;
                    var values = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToList();
                    testResults[testName] = values;

                    // Enhanced abnormal value detection
                    if (testName == "Blood Pressure" && values.Count >= 2)
                    {
                        if (int.TryParse(values[0], out int systolic) && int.TryParse(values[1], out int diastolic))
                        {
                            if (systolic >= 180 || diastolic >= 110)
                            {
                                criticalValues.Add($"🚨 CRITICAL: Hypertensive Crisis - Blood Pressure {systolic}/{diastolic} (Normal: <120/80)");
                            }
                            else if (systolic > 140 || diastolic > 90)
                            {
                                abnormalValues.Add($"⚠️ HIGH: Blood Pressure {systolic}/{diastolic} (Normal: <120/80)");
                            }
                            else
                            {
                                // Normal values - add to show improvement
                                analysis.AnalysisResults["NormalValues"] = analysis.AnalysisResults.ContainsKey("NormalValues")
                                    ? (List<string>)analysis.AnalysisResults["NormalValues"]
                                    : new List<string>();
                                ((List<string>)analysis.AnalysisResults["NormalValues"]).Add($"✅ NORMAL: Blood Pressure {systolic}/{diastolic} (Normal: <120/80)");
                            }
                        }
                    }
                    else if (testName == "Hemoglobin" && values.Count > 0)
                    {
                        if (double.TryParse(values[0], out double hgb))
                        {
                            if (hgb < 7.0)
                            {
                                criticalValues.Add($"🚨 CRITICAL: Severe Anemia - Hemoglobin {hgb} g/dL (Normal: 12-16 g/dL)");
                            }
                            else if (hgb < 10.0)
                            {
                                abnormalValues.Add($"⚠️ LOW: Hemoglobin {hgb} g/dL (Normal: 12-16 g/dL)");
                            }
                            else
                            {
                                // Normal values - add to show improvement
                                analysis.AnalysisResults["NormalValues"] = analysis.AnalysisResults.ContainsKey("NormalValues")
                                    ? (List<string>)analysis.AnalysisResults["NormalValues"]
                                    : new List<string>();
                                ((List<string>)analysis.AnalysisResults["NormalValues"]).Add($"✅ NORMAL: Hemoglobin {hgb} g/dL (Normal: 12-16 g/dL)");
                            }
                        }
                    }
                    else if (testName == "Triglycerides" && values.Count > 0)
                    {
                        if (double.TryParse(values[0], out double trig))
                        {
                            if (trig >= 500)
                            {
                                criticalValues.Add($"🚨 CRITICAL: Extremely High Triglycerides - {trig} mg/dL (Normal: <150 mg/dL)");
                            }
                            else if (trig >= 200)
                            {
                                abnormalValues.Add($"⚠️ HIGH: Triglycerides {trig} mg/dL (Normal: <150 mg/dL)");
                            }
                            else
                            {
                                // Normal values - add to show improvement
                                analysis.AnalysisResults["NormalValues"] = analysis.AnalysisResults.ContainsKey("NormalValues")
                                    ? (List<string>)analysis.AnalysisResults["NormalValues"]
                                    : new List<string>();
                                ((List<string>)analysis.AnalysisResults["NormalValues"]).Add($"✅ NORMAL: Triglycerides {trig} mg/dL (Normal: <150 mg/dL)");
                            }
                        }
                    }
                }
            }

            if (testResults.Any())
            {
                analysis.AnalysisResults["TestResults"] = testResults;
            }

            // Add critical values first (highest priority)
            if (criticalValues.Any())
            {
                analysis.AnalysisResults["CriticalValues"] = criticalValues;
                analysis.Alerts.AddRange(criticalValues);
            }

            // Add abnormal values
            if (abnormalValues.Any())
            {
                analysis.AnalysisResults["AbnormalValues"] = abnormalValues;
                analysis.Alerts.AddRange(abnormalValues);
            }

            // Add summary for AI context
            if (criticalValues.Any() || abnormalValues.Any())
            {
                var summary = new List<string>();
                if (criticalValues.Any())
                {
                    summary.Add($"CRITICAL MEDICAL VALUES DETECTED: {string.Join("; ", criticalValues)}");
                }
                if (abnormalValues.Any())
                {
                    summary.Add($"ABNORMAL VALUES: {string.Join("; ", abnormalValues)}");
                }
                analysis.AnalysisResults["MedicalSummary"] = string.Join(" | ", summary);
            }
        }

        private async Task AnalyzeMedicalReports(SM_MentalHealthApp.Shared.ContentAnalysis analysis, string text)
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

        public async Task ProcessAllUnanalyzedContentAsync()
        {
            try
            {
                _logger.LogInformation("Starting to process all unanalyzed content...");

                var unanalyzedContent = await _context.Contents
                    .Where(c => c.IsActive)
                    .Where(c => !_context.ContentAnalyses.Any(ca => ca.ContentId == c.Id))
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} unanalyzed content items", unanalyzedContent.Count);

                foreach (var content in unanalyzedContent)
                {
                    try
                    {
                        _logger.LogInformation("Processing unanalyzed content ID: {ContentId}, File: {FileName}", content.Id, content.OriginalFileName);
                        await AnalyzeContentAsync(content);
                        _logger.LogInformation("Successfully processed content ID: {ContentId}", content.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process content ID: {ContentId}", content.Id);
                    }
                }

                _logger.LogInformation("Completed processing all unanalyzed content");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unanalyzed content");
            }
        }

        /// <summary>
        /// Analyze clinical notes for AI insights and search optimization
        /// </summary>
        public async Task<List<ClinicalNoteDto>> SearchClinicalNotesWithAIAsync(string searchTerm, int? patientId = null, int? doctorId = null)
        {
            try
            {
                _logger.LogInformation("Starting AI-powered clinical notes search for term: {SearchTerm}", searchTerm);

                // Get clinical notes from the database
                var clinicalNotesQuery = _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.Doctor)
                    .Where(cn => cn.IsActive);

                if (patientId.HasValue)
                    clinicalNotesQuery = clinicalNotesQuery.Where(cn => cn.PatientId == patientId.Value);

                if (doctorId.HasValue)
                    clinicalNotesQuery = clinicalNotesQuery.Where(cn => cn.DoctorId == doctorId.Value);

                var clinicalNotes = await clinicalNotesQuery.ToListAsync();

                // Get content analyses for the same patients
                var patientIds = clinicalNotes.Select(cn => cn.PatientId).Distinct().ToList();
                var contentAnalyses = await _context.ContentAnalyses
                    .Include(ca => ca.Content)
                    .Where(ca => patientIds.Contains(ca.Content.PatientId))
                    .ToListAsync();

                // Combine clinical notes and content analyses for AI analysis
                var combinedData = new List<object>();

                foreach (var note in clinicalNotes)
                {
                    combinedData.Add(new
                    {
                        Type = "ClinicalNote",
                        Id = note.Id,
                        PatientId = note.PatientId,
                        Title = note.Title,
                        Content = note.Content,
                        NoteType = note.NoteType,
                        Priority = note.Priority,
                        CreatedAt = note.CreatedAt,
                        Tags = note.Tags
                    });
                }

                foreach (var analysis in contentAnalyses)
                {
                    combinedData.Add(new
                    {
                        Type = "ContentAnalysis",
                        Id = analysis.Id,
                        PatientId = analysis.Content.PatientId,
                        Title = $"Content Analysis - {analysis.Content.Title}",
                        Content = analysis.ExtractedText,
                        NoteType = "Content Analysis",
                        Priority = "Normal",
                        CreatedAt = analysis.ProcessedAt,
                        Tags = analysis.ContentTypeName
                    });
                }

                // Use AI to find relevant notes based on semantic similarity
                var relevantNotes = await FindRelevantNotesWithAIAsync(searchTerm, combinedData);

                _logger.LogInformation("AI search found {Count} relevant notes", relevantNotes.Count);
                return relevantNotes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI-powered clinical notes search");
                throw;
            }
        }

        /// <summary>
        /// Use AI to find semantically relevant clinical notes
        /// </summary>
        private async Task<List<ClinicalNoteDto>> FindRelevantNotesWithAIAsync(string searchTerm, List<object> combinedData)
        {
            try
            {
                // This is a simplified AI search - in a real implementation, you would:
                // 1. Use embeddings to convert text to vectors
                // 2. Use vector similarity search
                // 3. Apply machine learning models for relevance scoring

                var relevantNotes = new List<ClinicalNoteDto>();

                foreach (var item in combinedData)
                {
                    var itemType = item.GetType().GetProperty("Type")?.GetValue(item)?.ToString();
                    var content = item.GetType().GetProperty("Content")?.GetValue(item)?.ToString() ?? "";
                    var title = item.GetType().GetProperty("Title")?.GetValue(item)?.ToString() ?? "";

                    // Simple keyword matching for now - in production, use proper AI/ML
                    if (content.ToLower().Contains(searchTerm.ToLower()) ||
                        title.ToLower().Contains(searchTerm.ToLower()))
                    {
                        if (itemType == "ClinicalNote")
                        {
                            var itemId = item.GetType().GetProperty("Id")?.GetValue(item);
                            if (itemId != null)
                            {
                                var note = await _context.ClinicalNotes
                                    .Include(cn => cn.Patient)
                                    .Include(cn => cn.Doctor)
                                    .FirstOrDefaultAsync(cn => cn.Id == (int)itemId);

                                if (note != null)
                                {
                                    relevantNotes.Add(new ClinicalNoteDto
                                    {
                                        Id = note.Id,
                                        PatientId = note.PatientId,
                                        DoctorId = note.DoctorId,
                                        Title = note.Title,
                                        Content = note.Content,
                                        NoteType = note.NoteType,
                                        Priority = note.Priority,
                                        IsConfidential = note.IsConfidential,
                                        CreatedAt = note.CreatedAt,
                                        UpdatedAt = note.UpdatedAt,
                                        Tags = note.Tags,
                                        PatientName = note.Patient?.FullName ?? "Unknown",
                                        DoctorName = note.Doctor?.FullName ?? "Unknown"
                                    });
                                }
                            }
                        }
                    }
                }

                return relevantNotes.OrderByDescending(n => n.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI note relevance search");
                return new List<ClinicalNoteDto>();
            }
        }

    }
}
