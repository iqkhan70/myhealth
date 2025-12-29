using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IMultimediaAnalysisService
    {
        Task<SM_MentalHealthApp.Shared.ContentAnalysis> ProcessContentAsync(ContentItem content);
        Task<MedicalDocumentAnalysis> AnalyzeMedicalDocumentAsync(string extractedText, string fileName);
        Task<VideoAnalysis> AnalyzeVideoAsync(string extractedText, string fileName);
        Task<AudioAnalysis> AnalyzeAudioAsync(string extractedText, string fileName);
        Task<List<SM_MentalHealthApp.Shared.ContentAlert>> GenerateAlertsAsync(SM_MentalHealthApp.Shared.ContentAnalysis analysis, int patientId);
        Task<string> BuildEnhancedContextAsync(int patientId, string originalPrompt);
    }

    public class MultimediaAnalysisService : IMultimediaAnalysisService
    {
        private readonly JournalDbContext _context;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MultimediaAnalysisService> _logger;
        private readonly string _openAiApiKey;
        private readonly string _openAiBaseUrl = "https://api.openai.com/v1";

        public MultimediaAnalysisService(
            JournalDbContext context,
            IContentAnalysisService contentAnalysisService,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<MultimediaAnalysisService> logger)
        {
            _context = context;
            _contentAnalysisService = contentAnalysisService;
            _httpClient = httpClient;
            _logger = logger;
            _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not found");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiApiKey);
        }

        public async Task<SM_MentalHealthApp.Shared.ContentAnalysis> ProcessContentAsync(ContentItem content)
        {
            try
            {
                _logger.LogInformation("Processing content {ContentId} of type {ContentType}", content.Id, content.ContentTypeModel?.Name ?? "Unknown");

                // Extract text from the content
                var extractedText = await _contentAnalysisService.ExtractTextFromContentAsync(content);

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

                // Analyze based on content type
                var contentTypeName = content.ContentTypeModel?.Name ?? "Document";
                switch (contentTypeName)
                {
                    case "Document":
                        var medicalAnalysis = await AnalyzeMedicalDocumentAsync(extractedText, content.OriginalFileName);
                        analysis.AnalysisResults["MedicalAnalysis"] = medicalAnalysis;
                        break;
                    case "Video":
                        var videoAnalysis = await AnalyzeVideoAsync(extractedText, content.OriginalFileName);
                        analysis.AnalysisResults["VideoAnalysis"] = videoAnalysis;
                        break;
                    case "Audio":
                        var audioAnalysis = await AnalyzeAudioAsync(extractedText, content.OriginalFileName);
                        analysis.AnalysisResults["AudioAnalysis"] = audioAnalysis;
                        break;
                    case "Image":
                        var imageAnalysis = await AnalyzeImageAsync(extractedText, content.OriginalFileName);
                        analysis.AnalysisResults["ImageAnalysis"] = imageAnalysis;
                        break;
                }

                // Generate alerts
                var alerts = await GenerateAlertsAsync(analysis, content.PatientId);
                analysis.Alerts = alerts.Select(a => a.Description).ToList();
                analysis.ProcessingStatus = "Completed";

                // Save to database
                _context.ContentAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing content {ContentId}", content.Id);
                return new SM_MentalHealthApp.Shared.ContentAnalysis
                {
                    ContentId = content.Id,
                    ContentTypeName = content.ContentTypeModel?.Name ?? "Unknown",
                    ExtractedText = string.Empty,
                    AnalysisResults = new Dictionary<string, object>(),
                    Alerts = new List<string>(),
                    ProcessedAt = DateTime.UtcNow,
                    ProcessingStatus = "Failed",
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<MedicalDocumentAnalysis> AnalyzeMedicalDocumentAsync(string extractedText, string fileName)
        {
            try
            {
                var prompt = $@"
Analyze the following medical document and extract key information:

Document: {fileName}
Content: {extractedText}

Please provide a structured analysis including:
1. Document type (Lab Report, X-Ray, Prescription, etc.)
2. Medications mentioned
3. Symptoms described
4. Diagnoses mentioned
5. Vital signs or test results
6. Key values (numbers, measurements, etc.)
7. Summary of findings
8. Any concerns or red flags
9. Recommendations

Format the response as JSON with the following structure:
{{
    ""DocumentType"": ""string"",
    ""Medications"": [""string""],
    ""Symptoms"": [""string""],
    ""Diagnoses"": [""string""],
    ""VitalSigns"": [""string""],
    ""TestResults"": [""string""],
    ""KeyValues"": {{""key"": ""value""}},
    ""Summary"": ""string"",
    ""Concerns"": [""string""],
    ""Recommendations"": [""string""]
}}";

                var response = await CallOpenAIAsync(prompt);
                return ParseMedicalDocumentResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing medical document {FileName}", fileName);
                return new MedicalDocumentAnalysis
                {
                    DocumentType = "Unknown",
                    Summary = $"Analysis failed: {ex.Message}"
                };
            }
        }

        public async Task<VideoAnalysis> AnalyzeVideoAsync(string extractedText, string fileName)
        {
            try
            {
                var prompt = $@"
Analyze the following video content and extract information:

Video: {fileName}
Extracted Text: {extractedText}

Please provide:
1. Text extracted from video frames
2. Objects detected in the video
3. Activities observed
4. Audio transcription if available
5. Summary of the video content
6. Key moments or important scenes
7. Any health-related observations

Format as JSON:
{{
    ""ExtractedText"": [""string""],
    ""DetectedObjects"": [""string""],
    ""DetectedActivities"": [""string""],
    ""AudioTranscription"": [""string""],
    ""Summary"": ""string"",
    ""KeyMoments"": [""string""]
}}";

                var response = await CallOpenAIAsync(prompt);
                return ParseVideoAnalysisResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing video {FileName}", fileName);
                return new VideoAnalysis
                {
                    Summary = $"Video analysis failed: {ex.Message}"
                };
            }
        }

        public async Task<AudioAnalysis> AnalyzeAudioAsync(string extractedText, string fileName)
        {
            try
            {
                var prompt = $@"
Analyze the following audio content:

Audio: {fileName}
Transcription: {extractedText}

Please provide:
1. Keywords extracted from the audio
2. Sentiment analysis
3. Emotions detected
4. Summary of the audio content
5. Any health-related concerns mentioned
6. Recommendations based on the content

Format as JSON:
{{
    ""Transcription"": ""string"",
    ""Keywords"": [""string""],
    ""Sentiment"": ""string"",
    ""Emotions"": [""string""],
    ""Summary"": ""string"",
    ""Concerns"": [""string""]
}}";

                var response = await CallOpenAIAsync(prompt);
                return ParseAudioAnalysisResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing audio {FileName}", fileName);
                return new AudioAnalysis
                {
                    Summary = $"Audio analysis failed: {ex.Message}"
                };
            }
        }

        private async Task<object> AnalyzeImageAsync(string extractedText, string fileName)
        {
            try
            {
                var prompt = $@"
Analyze the following image content:

Image: {fileName}
Extracted Text: {extractedText}

Please provide:
1. Description of what's in the image
2. Any text visible in the image
3. Medical relevance if applicable
4. Objects or structures identified
5. Any concerns or observations

Format as JSON:
{{
    ""Description"": ""string"",
    ""ExtractedText"": ""string"",
    ""MedicalRelevance"": ""string"",
    ""Objects"": [""string""],
    ""Concerns"": [""string""]
}}";

                var response = await CallOpenAIAsync(prompt);
                return JsonSerializer.Deserialize<object>(response) ?? new object();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image {FileName}", fileName);
                return new { Error = $"Image analysis failed: {ex.Message}" };
            }
        }

        public async Task<List<SM_MentalHealthApp.Shared.ContentAlert>> GenerateAlertsAsync(SM_MentalHealthApp.Shared.ContentAnalysis analysis, int patientId)
        {
            var alerts = new List<SM_MentalHealthApp.Shared.ContentAlert>();

            try
            {
                // Check for critical medical values
                if (analysis.AnalysisResults.ContainsKey("MedicalAnalysis"))
                {
                    var medicalAnalysis = analysis.AnalysisResults["MedicalAnalysis"] as MedicalDocumentAnalysis;
                    if (medicalAnalysis != null)
                    {
                        // Check for critical vital signs
                        foreach (var vital in medicalAnalysis.VitalSigns)
                        {
                            if (IsCriticalVitalSign(vital))
                            {
                                alerts.Add(new SM_MentalHealthApp.Shared.ContentAlert
                                {
                                    ContentId = analysis.ContentId,
                                    PatientId = patientId,
                                    AlertType = "Critical",
                                    Title = "Critical Vital Sign Detected",
                                    Description = $"Critical vital sign found: {vital}",
                                    Severity = "High",
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }

                        // Check for concerning symptoms
                        foreach (var symptom in medicalAnalysis.Concerns)
                        {
                            alerts.Add(new SM_MentalHealthApp.Shared.ContentAlert
                            {
                                ContentId = analysis.ContentId,
                                PatientId = patientId,
                                AlertType = "Warning",
                                Title = "Concerning Symptom",
                                Description = symptom,
                                Severity = "Medium",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                // Check for crisis keywords in any content
                var crisisKeywords = new[] { "suicide", "self harm", "emergency", "crisis", "urgent", "critical" };
                var lowerText = analysis.ExtractedText.ToLowerInvariant();

                foreach (var keyword in crisisKeywords)
                {
                    if (lowerText.Contains(keyword))
                    {
                        alerts.Add(new SM_MentalHealthApp.Shared.ContentAlert
                        {
                            ContentId = analysis.ContentId,
                            PatientId = patientId,
                            AlertType = "Critical",
                            Title = "Crisis Content Detected",
                            Description = $"Content contains crisis-related keyword: {keyword}",
                            Severity = "High",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Save alerts to database
                if (alerts.Any())
                {
                    _context.ContentAlerts.AddRange(alerts);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating alerts for content {ContentId}", analysis.ContentId);
            }

            return alerts;
        }

        public async Task<string> BuildEnhancedContextAsync(int patientId, string originalPrompt)
        {
            try
            {
                // Get recent journal entries
                var recentEntries = await _context.JournalEntries
                    .Where(e => e.UserId == patientId)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                // Get recent content analyses
                var recentAnalyses = await _context.ContentAnalyses
                    .Where(ca => _context.Contents.Any(c => c.Id == ca.ContentId && c.PatientId == patientId))
                    .OrderByDescending(ca => ca.ProcessedAt)
                    .Take(5)
                    .ToListAsync();

                // Get active alerts
                var activeAlerts = await _context.ContentAlerts
                    .Where(a => a.PatientId == patientId && !a.IsResolved)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                var context = new StringBuilder();
                context.AppendLine("=== ENHANCED CLIENT CONTEXT ===");
                context.AppendLine($"Patient ID: {patientId}");
                context.AppendLine($"Current Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                context.AppendLine();

                // Add journal entries context
                if (recentEntries.Any())
                {
                    context.AppendLine("📝 **Recent Journal Entries:**");
                    foreach (var entry in recentEntries)
                    {
                        context.AppendLine($"- [{entry.CreatedAt:MM/dd/yyyy}] Mood: {entry.Mood}");
                        context.AppendLine($"  Entry: {entry.Text.Substring(0, Math.Min(entry.Text.Length, 100))}...");
                    }
                    context.AppendLine();
                }

                // Add content analysis context
                if (recentAnalyses.Any())
                {
                    context.AppendLine("📁 **Recent Content Analysis:**");
                    foreach (var analysis in recentAnalyses)
                    {
                        context.AppendLine($"- Type: {analysis.ContentTypeName}");
                        context.AppendLine($"  Status: {analysis.ProcessingStatus}");
                        if (!string.IsNullOrEmpty(analysis.ExtractedText))
                        {
                            context.AppendLine($"  Content: {analysis.ExtractedText.Substring(0, Math.Min(analysis.ExtractedText.Length, 200))}...");
                        }
                    }
                    context.AppendLine();
                }

                // Add alerts context
                if (activeAlerts.Any())
                {
                    context.AppendLine("⚠️ **Active Alerts:**");
                    foreach (var alert in activeAlerts)
                    {
                        context.AppendLine($"- {alert.Severity}: {alert.Title}");
                        context.AppendLine($"  {alert.Description}");
                    }
                    context.AppendLine();
                }

                context.AppendLine("=== END ENHANCED CONTEXT ===");
                context.AppendLine();
                context.AppendLine(originalPrompt);

                return context.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building enhanced context for patient {PatientId}", patientId);
                return originalPrompt; // Fallback to original prompt
            }
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a medical AI assistant specialized in analyzing health-related content. Provide accurate, helpful analysis in the requested JSON format." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3,
                    max_tokens = 2000
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_openAiBaseUrl}/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<JsonElement>(responseContent);

                return responseObj.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
                return "{}"; // Return empty JSON on error
            }
        }

        private MedicalDocumentAnalysis ParseMedicalDocumentResponse(string response)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<MedicalDocumentAnalysis>(response, options) ?? new MedicalDocumentAnalysis();
            }
            catch
            {
                return new MedicalDocumentAnalysis { Summary = "Failed to parse medical document analysis" };
            }
        }

        private VideoAnalysis ParseVideoAnalysisResponse(string response)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<VideoAnalysis>(response, options) ?? new VideoAnalysis();
            }
            catch
            {
                return new VideoAnalysis { Summary = "Failed to parse video analysis" };
            }
        }

        private AudioAnalysis ParseAudioAnalysisResponse(string response)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<AudioAnalysis>(response, options) ?? new AudioAnalysis();
            }
            catch
            {
                return new AudioAnalysis { Summary = "Failed to parse audio analysis" };
            }
        }

        private bool IsCriticalVitalSign(string vitalSign)
        {
            var criticalPatterns = new[]
            {
                @"blood pressure.*(?:1[8-9]\d|2\d\d)\/(?:1[1-9]\d|2\d\d)", // High BP
                @"heart rate.*(?:1[2-9]\d|2\d\d)", // High HR
                @"temperature.*(?:10[4-9]|1[1-9]\d)", // High temp
                @"oxygen.*(?:[0-8]\d|9[0-2])", // Low oxygen
                @"glucose.*(?:[4-6]\d\d|7\d\d)", // High glucose
            };

            return criticalPatterns.Any(pattern => Regex.IsMatch(vitalSign, pattern, RegexOptions.IgnoreCase));
        }

    }
}
