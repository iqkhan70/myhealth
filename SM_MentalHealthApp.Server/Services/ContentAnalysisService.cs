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
                    content.Id, content.S3Key, content.Type);

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
                var extractedText = content.Type switch
                {
                    ContentType.Document => await ExtractTextFromDocumentAsync(fileStream, content.OriginalFileName),
                    ContentType.Image => await ExtractTextFromImageAsync(fileStream, content.OriginalFileName),
                    ContentType.Video => await ExtractTextFromVideoAsync(fileStream, content.OriginalFileName),
                    ContentType.Audio => await ExtractTextFromAudioAsync(fileStream, content.OriginalFileName),
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
                _logger.LogInformation("Starting content analysis for content {ContentId} of type {ContentType}", content.Id, content.Type);

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
                        ContentType = content.Type.ToString(),
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
                    ContentType = content.Type.ToString(),
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
                    ContentType = content.Type.ToString(),
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
                    content.Id, content.Title, content.Type, content.CreatedAt);
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

                // Get content analyses for medical data
                var allContentAnalyses = await GetContentAnalysisForPatientAsync(patientId);
                _logger.LogInformation("Found {ContentCount} content analyses for patient {PatientId}", allContentAnalyses.Count, patientId);

                if (allContentAnalyses.Any())
                {
                    // Sort by date - most recent first
                    var sortedAnalyses = allContentAnalyses.OrderByDescending(a => a.ProcessedAt).ToList();
                    var latestAnalysis = sortedAnalyses.First();

                    context.AppendLine("=== MEDICAL DATA SUMMARY ===");
                    context.AppendLine($"Latest Update: {latestAnalysis.ProcessedAt:MM/dd/yyyy HH:mm}");
                    context.AppendLine($"Content Type: {latestAnalysis.ContentType}");

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

                context.AppendLine($"=== USER QUESTION ===");
                context.AppendLine(originalPrompt);
                context.AppendLine();
                context.AppendLine("INSTRUCTIONS FOR AI RESPONSE:");
                context.AppendLine("1. Provide a professional, clinical assessment based on available data");
                context.AppendLine("2. Be concise and avoid repetitive information");
                context.AppendLine("3. Focus on actionable recommendations for the healthcare provider");
                context.AppendLine("4. If critical values are present, provide clear next steps");
                context.AppendLine("5. Maintain a professional, clinical tone throughout");

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
    }
}
