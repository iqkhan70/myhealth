using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Server.Controllers;
using SM_MentalHealthApp.Shared;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class AdminController : BaseController
    {
        private readonly IAdminService _adminService;
        private readonly JournalDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IPiiEncryptionService _encryptionService;

        public AdminController(IAdminService adminService, JournalDbContext context, ILogger<AdminController> logger, IPiiEncryptionService encryptionService)
        {
            _adminService = adminService;
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        [HttpGet("doctors")]
        public async Task<ActionResult<List<User>>> GetDoctors()
        {
            try
            {
                var doctors = await _adminService.GetAllDoctorsAsync();
                return Ok(doctors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving doctors: {ex.Message}");
            }
        }

        [HttpGet("patients")]
        public async Task<ActionResult<List<User>>> GetPatients()
        {
            try
            {
                var patients = await _adminService.GetAllPatientsAsync();
                return Ok(patients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving patients: {ex.Message}");
            }
        }

        [HttpGet("coordinators")]
        [Authorize(Roles = "Admin")] // Only Admin can view coordinators list
        public async Task<ActionResult<List<User>>> GetCoordinators()
        {
            try
            {
                var coordinators = await _adminService.GetAllCoordinatorsAsync();
                return Ok(coordinators);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving coordinators: {ex.Message}");
            }
        }

        [HttpGet("attorneys")]
        [Authorize(Roles = "Admin,Coordinator,Doctor,Attorney")] // Admin, Coordinator, Doctor, and Attorney can view attorneys list (for assignment purposes)
        public async Task<ActionResult<List<User>>> GetAttorneys()
        {
            try
            {
                var attorneys = await _adminService.GetAllAttorneysAsync();
                return Ok(attorneys);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving attorneys: {ex.Message}");
            }
        }

        /// <summary>
        /// Perform AI health check on a patient and send alert if severe
        /// </summary>
        [HttpPost("ai-health-check/{patientId}")]
        public async Task<ActionResult<AiHealthCheckResult>> PerformAiHealthCheck(int patientId)
        {
            try
            {
                _logger.LogInformation("Starting AI health check for patient {PatientId}", patientId);

                // Verify patient exists and is active
                var patient = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == patientId && u.RoleId == 1 && u.IsActive);

                if (patient == null)
                {
                    return NotFound("Patient not found or inactive");
                }

                // Get assigned doctors for this patient
                var assignedDoctors = await _context.UserAssignments
                    .Where(ua => ua.AssigneeId == patientId)
                    .Include(ua => ua.Assigner)
                    .Select(ua => ua.Assigner)
                    .Where(d => d.IsActive)
                    .ToListAsync();

                if (!assignedDoctors.Any())
                {
                    return BadRequest("No doctors assigned to this patient");
                }

                // Build comprehensive context for AI analysis with source tracking
                var contentAnalysisService = HttpContext.RequestServices.GetRequiredService<IContentAnalysisService>();
                var originalPrompt = $"AI Health Check for Patient {patient.FirstName} {patient.LastName}";
                _logger.LogInformation("Building context for patient {PatientId} with prompt: {Prompt}", patientId, originalPrompt);

                var (patientContext, sources) = await contentAnalysisService.BuildEnhancedContextWithSourcesAsync(patientId, originalPrompt);

                _logger.LogInformation("Built context for patient {PatientId}, length: {ContextLength}", patientId, patientContext.Length);
                _logger.LogInformation("Context preview (first 500 chars): {ContextPreview}", patientContext.Length > 500 ? patientContext.Substring(0, 500) : patientContext);

                // Verify context contains patient data
                if (string.IsNullOrWhiteSpace(patientContext) || patientContext == originalPrompt)
                {
                    _logger.LogWarning("Context appears to be empty or unchanged for patient {PatientId}. This may indicate no patient data is available.", patientId);
                }

                // Use HuggingFace service to analyze patient's current status
                var huggingFaceService = HttpContext.RequestServices.GetRequiredService<HuggingFaceService>();

                // Get AI analysis for this patient with full context
                var aiResponse = await huggingFaceService.GenerateResponse(patientContext);

                _logger.LogInformation("AI Response for patient {PatientId}: {Response}", patientId, aiResponse);
                _logger.LogInformation("Sources tracked for patient {PatientId}: {SourceCount}", patientId, sources.Count);

                // Check if AI indicates high severity
                bool isHighSeverity = IsAiResponseIndicatingHighSeverity(aiResponse);

                // Identify which sources contributed to severity (show sources for both High and Normal severity)
                var severitySources = IdentifySeveritySources(sources, aiResponse, isHighSeverity);
                _logger.LogInformation("Identified {SeveritySourceCount} severity sources for patient {PatientId}", severitySources.Count, patientId);

                if (isHighSeverity)
                {
                    _logger.LogInformation("AI detected high severity for patient {PatientId}. Alerts can be sent manually by doctor.", patientId);

                    // ✅ Don't send alerts automatically - doctor will choose to send them manually
                    return Ok(new AiHealthCheckResult
                    {
                        Success = true,
                        Severity = "High",
                        AiResponse = aiResponse,
                        AlertsSent = 0, // No alerts sent automatically
                        DoctorsNotified = assignedDoctors.Count, // Number of doctors who can be notified
                        Message = "AI detected high severity. You can send alerts to assigned doctors if needed.",
                        SeveritySources = severitySources
                    });
                }
                else
                {
                    _logger.LogInformation("AI health check for patient {PatientId} shows normal status", patientId);

                    // ✅ Check if there are severity sources (indicating criticality was found but AI says normal)
                    var hasCriticalSources = severitySources != null && severitySources.Any();
                    var message = hasCriticalSources 
                        ? "AI health check shows normal status. Nothing to alert at this time."
                        : "AI health check shows normal status.";

                    return Ok(new AiHealthCheckResult
                    {
                        Success = true,
                        Severity = "Normal",
                        AiResponse = aiResponse,
                        AlertsSent = 0,
                        DoctorsNotified = 0,
                        Message = message,
                        SeveritySources = severitySources
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing AI health check for patient {PatientId}: {Message}", patientId, ex.Message);
                return StatusCode(500, new AiHealthCheckResult
                {
                    Success = false,
                    Severity = "Error",
                    Message = $"Error performing AI health check: {ex.Message}",
                    AiResponse = string.Empty,
                    AlertsSent = 0,
                    DoctorsNotified = 0
                });
            }
        }

        /// <summary>
        /// Manually send alerts to assigned doctors for a patient
        /// </summary>
        [HttpPost("send-ai-health-alerts/{patientId}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> SendAiHealthAlerts(int patientId)
        {
            try
            {
                var patient = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == patientId && u.RoleId == 1);

                if (patient == null)
                    return NotFound("Patient not found");

                // Get assigned doctors
                var assignedDoctors = await _context.UserAssignments
                    .Where(ua => ua.AssigneeId == patientId && ua.IsActive)
                    .Select(ua => ua.Assigner)
                    .Where(d => d != null && d.IsActive)
                    .ToListAsync();

                if (!assignedDoctors.Any())
                {
                    return BadRequest("No doctors assigned to this patient");
                }

                // Send SMS alerts to all assigned doctors
                var notificationService = HttpContext.RequestServices.GetRequiredService<INotificationService>();
                var alertsSent = 0;

                foreach (var doctor in assignedDoctors)
                {
                    try
                    {
                        var alert = new global::SM_MentalHealthApp.Shared.EmergencyAlert
                        {
                            Id = 0, // AI-generated alert
                            PatientId = patient.Id,
                            PatientName = $"{patient.FirstName} {patient.LastName}",
                            PatientEmail = patient.Email,
                            EmergencyType = "AI Health Alert",
                            Severity = "High",
                            Message = $"AI Health Check detected concerning patterns for {patient.FirstName} {patient.LastName}",
                            Timestamp = DateTime.UtcNow,
                            DeviceId = "AI-SYSTEM"
                        };

                        await notificationService.SendEmergencyAlert(doctor.Id, alert);
                        alertsSent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send AI health alert to doctor {DoctorId}", doctor.Id);
                    }
                }

                _logger.LogInformation("Manually sent {AlertsSent} AI health alerts for patient {PatientId}", alertsSent, patientId);

                return Ok(new { 
                    success = true, 
                    alertsSent = alertsSent,
                    doctorsNotified = assignedDoctors.Count,
                    message = $"Alerts sent to {alertsSent} doctor(s)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending AI health alerts for patient {PatientId}: {Message}", patientId, ex.Message);
                return StatusCode(500, new { success = false, message = $"Failed to send alerts: {ex.Message}" });
            }
        }

        /// <summary>
        /// Identify which sources contributed to the severity assessment
        /// </summary>
        private List<SeveritySource> IdentifySeveritySources(List<SeveritySourceMetadata> sources, string aiResponse, bool isHighSeverity)
        {
            var severitySources = new List<SeveritySource>();
            
            if (!sources.Any())
            {
                return severitySources;
            }

            // Always include sources if severity is High, or if they have concerning content even for Normal severity
            var shouldIncludeAllSources = isHighSeverity;

            var responseLower = aiResponse.ToLower();
            var highSeverityKeywords = new[]
            {
                "critical", "urgent", "immediate", "emergency", "severe", "concerning",
                "high risk", "dangerous", "alarming", "serious", "crisis", "acute",
                "suicide", "self-harm", "overdose", "chest pain", "difficulty breathing",
                "heart attack", "stroke", "abnormal", "elevated", "high", "low", "irregular",
                "medical concerns detected", "abnormal medical values", "concerning clinical observations"
            };

            foreach (var source in sources)
            {
                var sourceContentLower = source.SourceContent.ToLower();
                var sourceTitleLower = source.SourceTitle.ToLower();
                var contributionReasons = new List<string>();
                var shouldInclude = false;

                // Check if source has high severity keywords in content or title
                var matchingKeywords = highSeverityKeywords.Where(k => 
                    sourceContentLower.Contains(k) || sourceTitleLower.Contains(k)).ToList();

                // For Content sources, also check alerts
                if (source.SourceType == "Content" && source.Alerts != null && source.Alerts.Any())
                {
                    var alertsText = string.Join(" ", source.Alerts).ToLower();
                    var alertKeywords = highSeverityKeywords.Where(k => alertsText.Contains(k)).ToList();
                    if (alertKeywords.Any())
                    {
                        matchingKeywords = matchingKeywords.Union(alertKeywords).ToList();
                        contributionReasons.Add($"Content has critical alerts: {string.Join(", ", source.Alerts.Take(2))}");
                        shouldInclude = true;
                    }
                }

                if (matchingKeywords.Any())
                {
                    contributionReasons.Add($"Contains concerning keywords: {string.Join(", ", matchingKeywords.Take(3))}");
                    shouldInclude = true;
                }

                // Emergency incidents always contribute
                if (source.SourceType == "Emergency")
                {
                    if (source.Severity == "Critical" || source.Severity == "High")
                    {
                        contributionReasons.Add($"Unacknowledged {source.Severity.ToLower()} emergency incident");
                        shouldInclude = true;
                    }
                    else
                    {
                        contributionReasons.Add("Emergency incident");
                        shouldInclude = true; // Always include emergencies
                    }
                }

                // Clinical notes with high priority contribute
                if (source.SourceType == "ClinicalNote")
                {
                    if (source.Priority == "High" || source.Priority == "Critical")
                    {
                        contributionReasons.Add($"High priority clinical note");
                        shouldInclude = true;
                    }
                    else if (shouldIncludeAllSources)
                    {
                        contributionReasons.Add("Clinical note reviewed");
                        shouldInclude = true;
                    }
                }

                // Chat sessions with concerning content contribute
                if (source.SourceType == "ChatSession")
                {
                    // Check if chat session has concerning keywords
                    if (matchingKeywords.Any())
                    {
                        contributionReasons.Add("Chat session contains concerning content");
                        shouldInclude = true;
                    }
                    else if (shouldIncludeAllSources)
                    {
                        // For high severity, include chat sessions that may have contributed
                        contributionReasons.Add("Chat session included in analysis");
                        shouldInclude = true;
                    }
                }

                // For High severity, include sources that were analyzed and may have contributed
                // This ensures we show sources that the AI considered, even if they don't match keywords exactly
                if (shouldIncludeAllSources && !shouldInclude)
                {
                    // Only include if it's a type that could contribute to severity
                    if (source.SourceType == "Content" && source.Alerts != null && source.Alerts.Any())
                    {
                        contributionReasons.Add("Content with alerts included in analysis");
                        shouldInclude = true;
                    }
                    else if (source.SourceType == "ClinicalNote" || source.SourceType == "JournalEntry" || source.SourceType == "ChatSession")
                    {
                        // Check if mentioned in AI response
                        var keyPhrases = ExtractKeyPhrases(source.SourceContent);
                        var mentionedInResponse = keyPhrases.Any(phrase => 
                            responseLower.Contains(phrase.ToLower()));

                        if (mentionedInResponse)
                        {
                            contributionReasons.Add("Referenced in AI analysis");
                            shouldInclude = true;
                        }
                    }
                }

                // Check if source content appears in AI response (indicating it was considered)
                if (!shouldInclude)
                {
                    var keyPhrases = ExtractKeyPhrases(source.SourceContent);
                    var mentionedInResponse = keyPhrases.Any(phrase => 
                        responseLower.Contains(phrase.ToLower()));

                    if (mentionedInResponse)
                    {
                        contributionReasons.Add("Referenced in AI analysis");
                        shouldInclude = true;
                    }
                }

                // If source should be included, add it
                if (shouldInclude)
                {
                    var navigationRoute = source.SourceType switch
                    {
                        "JournalEntry" => $"/journal",
                        "ClinicalNote" => $"/clinical-notes",
                        "Content" => $"/content",
                        "Emergency" => $"/emergencies",
                        "ChatSession" => $"/chat-history",
                        _ => ""
                    };

                    severitySources.Add(new SeveritySource
                    {
                        SourceType = source.SourceType,
                        SourceId = source.SourceId,
                        SourceTitle = source.SourceTitle,
                        SourcePreview = source.SourceContent.Length > 150 
                            ? source.SourceContent.Substring(0, 150) + "..." 
                            : source.SourceContent,
                        SourceDate = source.SourceDate,
                        ContributionReason = contributionReasons.Any() 
                            ? string.Join("; ", contributionReasons) 
                            : "Included in analysis",
                        NavigationRoute = navigationRoute
                    });
                }
            }

            return severitySources;
        }

        /// <summary>
        /// Extract key phrases from text for matching
        /// </summary>
        private List<string> ExtractKeyPhrases(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var phrases = new List<string>();
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Extract 3-word phrases
            for (int i = 0; i < words.Length - 2; i++)
            {
                var phrase = $"{words[i]} {words[i + 1]} {words[i + 2]}";
                if (phrase.Length > 10 && phrase.Length < 50)
                {
                    phrases.Add(phrase);
                }
            }

            return phrases.Take(10).ToList(); // Limit to 10 phrases
        }

        /// <summary>
        /// Analyze AI response to determine if it indicates high severity
        /// </summary>
        private bool IsAiResponseIndicatingHighSeverity(string aiResponse)
        {
            if (string.IsNullOrEmpty(aiResponse))
                return false;

            var response = aiResponse.ToLower();

            _logger.LogDebug("Analyzing AI response for severity. Response length: {Length}", response.Length);

            // First, check for positive indicators that suggest normal/stable status
            // If these are present prominently, we should be more cautious about flagging as high severity
            var positiveIndicators = new[]
            {
                "stable", "normal", "no immediate concerns", "no concerns", "good", "healthy",
                "within normal range", "acceptable", "improving", "resolved"
            };

            bool hasPositiveIndicators = false;
            foreach (var indicator in positiveIndicators)
            {
                if (response.Contains(indicator))
                {
                    int indicatorPos = response.IndexOf(indicator);
                    // Check context before the indicator to see if it's negated
                    int startPos = Math.Max(0, indicatorPos - 15);
                    string contextBefore = response.Substring(startPos, Math.Min(15, indicatorPos));
                    if (!contextBefore.Contains("not "))
                    {
                        hasPositiveIndicators = true;
                        _logger.LogDebug("Found positive indicator: {Indicator}", indicator);
                        break;
                    }
                }
            }

            // Check for "MEDICAL CONCERNS DETECTED" first - this should override stable status
            bool hasMedicalConcerns = response.Contains("medical concerns detected") ||
                                     response.Contains("abnormal medical values") ||
                                     response.Contains("concerning clinical observations");

            // If medical concerns are detected, it's high severity regardless of other indicators
            if (hasMedicalConcerns)
            {
                _logger.LogWarning("MEDICAL CONCERNS DETECTED found in AI response. Flagging as high severity.");
                return true;
            }

            // Check for explicit current status statements
            bool hasExplicitStableStatus = response.Contains("current status: stable") ||
                                          response.Contains("status: stable") ||
                                          response.Contains("current status is stable") ||
                                          (response.Contains("stable") &&
                                           (response.IndexOf("stable") < 200 || // Early in response
                                            response.Substring(0, Math.Min(500, response.Length)).Contains("stable")));

            // If we have strong positive indicators, only flag as high severity if there are very strong negative signals
            if (hasPositiveIndicators || hasExplicitStableStatus)
            {
                _logger.LogDebug("Positive indicators found. Checking for critical keywords in current context only.");

                // Only trigger on very strong indicators when status is otherwise stable
                var criticalKeywords = new[] { "critical", "emergency", "immediate", "urgent", "suicide", "self-harm" };

                foreach (var keyword in criticalKeywords)
                {
                    if (response.Contains(keyword))
                    {
                        // Look for context - if it's in a "recent activity" or historical section, be more lenient
                        int keywordIndex = response.IndexOf(keyword);
                        int contextStart = Math.Max(0, keywordIndex - 200);
                        int contextEnd = Math.Min(response.Length, keywordIndex + 200);
                        string contextBefore = response.Substring(contextStart, Math.Min(200, keywordIndex - contextStart));
                        string contextAfter = response.Substring(keywordIndex, Math.Min(200, contextEnd - keywordIndex));

                        // Check if keyword is negated (e.g., "unable to detect critical", "no critical", "not critical")
                        bool isNegated = contextBefore.Contains("unable to detect") ||
                                       contextBefore.Contains("unable to") ||
                                       contextBefore.Contains("no ") ||
                                       contextBefore.Contains("not ") ||
                                       contextBefore.Contains("without ") ||
                                       contextBefore.Contains("lack of") ||
                                       contextBefore.Contains("did not detect") ||
                                       contextBefore.Contains("could not detect");

                        // If keyword appears near "recent", "history", "past", "previous", "activity", or in date brackets, it's likely historical
                        bool isHistorical = contextBefore.Contains("recent") ||
                                          contextBefore.Contains("history") ||
                                          contextBefore.Contains("past") ||
                                          contextBefore.Contains("previous") ||
                                          contextBefore.Contains("activity") ||
                                          contextBefore.Contains("[") ||
                                          contextAfter.Contains("recent activity") ||
                                          contextAfter.Contains("patient activity") ||
                                          (contextBefore.Contains("[") && contextBefore.Contains("/")); // Date format like [09/16/2025]

                        if (!isHistorical && !isNegated)
                        {
                            _logger.LogWarning("Critical keyword '{Keyword}' found in current context despite positive indicators. Flagging as high severity.", keyword);
                            return true;
                        }
                        else
                        {
                            _logger.LogDebug("Critical keyword '{Keyword}' found but appears to be negated or historical (negated: {IsNegated}, historical: {IsHistorical}, context: '{Context}'). Ignoring.",
                                keyword, isNegated, isHistorical, contextBefore + "..." + contextAfter);
                        }
                    }
                }

                // Also check for "crisis" in historical context (like "Mood: Crisis" in Recent Patient Activity)
                if (response.Contains("crisis"))
                {
                    int crisisIndex = response.IndexOf("crisis");
                    int contextStart = Math.Max(0, crisisIndex - 200);
                    int contextEnd = Math.Min(response.Length, crisisIndex + 200);
                    string contextBefore = response.Substring(contextStart, Math.Min(200, crisisIndex - contextStart));
                    string contextAfter = response.Substring(crisisIndex, Math.Min(200, contextEnd - crisisIndex));

                    // If "crisis" appears in "Recent Patient Activity" or near a date, it's historical
                    bool isHistoricalCrisis = contextBefore.Contains("recent patient activity") ||
                                             contextBefore.Contains("recent activity") ||
                                             contextBefore.Contains("mood:") ||
                                             (contextBefore.Contains("[") && contextBefore.Contains("/")); // Date format

                    if (isHistoricalCrisis)
                    {
                        _logger.LogDebug("'Crisis' found in historical context (likely from journal entries). Ignoring for severity calculation.");
                    }
                    else
                    {
                        _logger.LogWarning("'Crisis' found in current context despite stable status. Flagging as high severity.");
                        return true;
                    }
                }

                // For stable status, no high severity indicators found in current context
                _logger.LogDebug("Stable status confirmed. No high severity indicators in current context.");
                return false;
            }

            // Check for high-severity keywords and patterns (original logic for non-stable cases)
            var highSeverityKeywords = new[]
            {
                "critical", "urgent", "immediate", "emergency", "severe", "concerning",
                "high risk", "dangerous", "alarming", "serious", "crisis", "acute",
                "unacknowledged", "fall", "heart attack", "stroke", "suicide",
                "self-harm", "overdose", "chest pain", "difficulty breathing",
                "medical concerns detected", "abnormal medical values", "concerning clinical observations",
                "requires attention and monitoring", "requires monitoring", "needs monitoring"
            };

            var severityScore = 0;
            foreach (var keyword in highSeverityKeywords)
            {
                if (response.Contains(keyword))
                {
                    // Check if keyword is in historical context
                    int keywordPos = response.IndexOf(keyword);
                    if (keywordPos > 0)
                    {
                        string contextBefore = response.Substring(Math.Max(0, keywordPos - 150), Math.Min(150, keywordPos));
                        bool isHistorical = contextBefore.Contains("recent") ||
                                          contextBefore.Contains("history") ||
                                          contextBefore.Contains("past") ||
                                          contextBefore.Contains("previous") ||
                                          contextBefore.Contains("activity") ||
                                          contextBefore.Contains("[");

                        // Reduce score if it's historical
                        if (!isHistorical)
                        {
                            severityScore++;
                        }
                    }
                    else
                    {
                        severityScore++;
                    }
                }
            }

            // Also check for specific medical value alerts (current, not historical)
            if (response.Contains("blood pressure") && (response.Contains("high") || response.Contains("elevated")))
            {
                // Check if this is in current context
                int bpIndex = response.IndexOf("blood pressure");
                if (bpIndex < 300 || !response.Substring(0, Math.Min(500, response.Length)).Contains("recent"))
                {
                    severityScore += 2;
                }
            }

            if (response.Contains("hemoglobin") && response.Contains("low"))
            {
                int hbIndex = response.IndexOf("hemoglobin");
                if (hbIndex < 300 || !response.Substring(0, Math.Min(500, response.Length)).Contains("recent"))
                {
                    severityScore += 2;
                }
            }

            if (response.Contains("triglycerides") && response.Contains("high"))
            {
                int trigIndex = response.IndexOf("triglycerides");
                if (trigIndex < 300 || !response.Substring(0, Math.Min(500, response.Length)).Contains("recent"))
                {
                    severityScore += 1;
                }
            }

            // Consider high severity if multiple indicators or critical keywords found
            // But require higher threshold if we have positive indicators
            int threshold = hasPositiveIndicators ? 3 : 2;

            // Check for "critical" but exclude negated cases (e.g., "unable to detect critical", "no critical", "not critical")
            bool hasCriticalInCurrentContext = false;
            if (response.Contains("critical"))
            {
                int criticalIndex = response.IndexOf("critical");
                if (criticalIndex > 0)
                {
                    int contextStart = Math.Max(0, criticalIndex - 50);
                    string contextBefore = response.Substring(contextStart, Math.Min(50, criticalIndex - contextStart));
                    // Check if it's negated
                    bool isNegated = contextBefore.Contains("unable to detect") ||
                                   contextBefore.Contains("no ") ||
                                   contextBefore.Contains("not ") ||
                                   contextBefore.Contains("without ") ||
                                   contextBefore.Contains("lack of");

                    // Check if it's in historical context
                    bool isHistorical = response.Substring(0, Math.Min(500, response.Length)).Contains("recent");

                    hasCriticalInCurrentContext = !isNegated && !isHistorical;
                }
            }

            // Check for "emergency" but exclude negated cases
            bool hasEmergencyInCurrentContext = false;
            if (response.Contains("emergency"))
            {
                int emergencyIndex = response.IndexOf("emergency");
                if (emergencyIndex > 0)
                {
                    int contextStart = Math.Max(0, emergencyIndex - 50);
                    string contextBefore = response.Substring(contextStart, Math.Min(50, emergencyIndex - contextStart));
                    bool isNegated = contextBefore.Contains("no ") ||
                                   contextBefore.Contains("not ") ||
                                   contextBefore.Contains("without ");
                    bool isHistorical = response.Substring(0, Math.Min(500, response.Length)).Contains("recent");
                    hasEmergencyInCurrentContext = !isNegated && !isHistorical;
                }
            }

            bool isHighSeverity = severityScore >= threshold ||
                   hasCriticalInCurrentContext ||
                   hasEmergencyInCurrentContext ||
                   response.Contains("unacknowledged");

            _logger.LogDebug("Severity analysis complete. Score: {Score}, Threshold: {Threshold}, Result: {IsHighSeverity}",
                severityScore, threshold, isHighSeverity);

            return isHighSeverity;
        }

        [HttpGet("assignments")]
        [Authorize(Roles = "Admin,Coordinator")] // Admin and Coordinator can view assignments
        public async Task<ActionResult<List<UserAssignment>>> GetAssignments()
        {
            try
            {
                var assignments = await _adminService.GetUserAssignmentsAsync();
                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving assignments: {ex.Message}");
            }
        }

        [HttpPost("assign")]
        [Authorize(Roles = "Admin,Coordinator,Doctor,Attorney")] // Admin, Coordinator, Doctor, and Attorney can create assignments
        public async Task<ActionResult> AssignPatientToDoctor([FromBody] AssignPatientRequest request)
        {
            try
            {
                // Get current user's role to validate assignment rules
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();
                
                // For attorneys: validate they can only assign to other attorneys
                if (currentRoleId == Shared.Constants.Roles.Attorney)
                {
                    // Verify target is an attorney
                    var targetUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == request.DoctorId && u.IsActive);
                    
                    if (targetUser == null || targetUser.RoleId != Shared.Constants.Roles.Attorney)
                    {
                        return BadRequest("Attorneys can only assign patients to other attorneys.");
                    }
                }
                // For doctors: they can assign to doctors or attorneys (no restriction needed)
                // For coordinators and admins: they can assign to anyone (no restriction needed)
                
                var success = await _adminService.AssignPatientToDoctorAsync(request.PatientId, request.DoctorId);
                if (success)
                {
                    return Ok(new { message = "Patient assigned successfully" });
                }
                return BadRequest("Failed to assign patient. Patient or assigner may not exist, or assignment may already exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error assigning patient: {ex.Message}");
            }
        }

        [HttpDelete("unassign")]
        [Authorize(Roles = "Admin,Coordinator,Doctor,Attorney")] // Admin, Coordinator, Doctor, and Attorney can remove assignments
        public async Task<ActionResult> UnassignPatientFromDoctor([FromBody] UnassignPatientRequest request)
        {
            try
            {
                var success = await _adminService.UnassignPatientFromDoctorAsync(request.PatientId, request.DoctorId);
                if (success)
                {
                    return Ok(new { message = "Patient unassigned from doctor successfully" });
                }
                return BadRequest("Failed to unassign patient from doctor. Assignment may not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error unassigning patient from doctor: {ex.Message}");
            }
        }

        [HttpGet("doctor/{doctorId}/patients")]
        public async Task<ActionResult<List<User>>> GetPatientsForDoctor(int doctorId)
        {
            try
            {
                var patients = await _adminService.GetPatientsForDoctorAsync(doctorId);
                return Ok(patients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving patients for doctor: {ex.Message}");
            }
        }

        [HttpGet("patient/{patientId}/doctors")]
        [Authorize(Roles = "Admin,Doctor,Coordinator,Attorney")] // Admin, Doctor, Coordinator, and Attorney can view assigners for a patient
        public async Task<ActionResult<List<User>>> GetDoctorsForPatient(int patientId)
        {
            try
            {
                var doctors = await _adminService.GetDoctorsForPatientAsync(patientId);
                return Ok(doctors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving doctors for patient: {ex.Message}");
            }
        }

        [HttpPost("create-doctor")]
        public async Task<ActionResult> CreateDoctor([FromBody] CreateDoctorRequest request)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest("A doctor with this email already exists.");
                }

                // Create new doctor user
                var doctor = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    MobilePhone = request.MobilePhone,
                    RoleId = 2, // Doctor role
                    Specialization = request.Specialization,
                    LicenseNumber = request.LicenseNumber,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    MustChangePassword = true // Force password change on first login
                };

                // Encrypt DateOfBirth before saving
                UserEncryptionHelper.EncryptUserData(doctor, _encryptionService);

                _context.Users.Add(doctor);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Doctor created successfully", doctorId = doctor.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating doctor: {ex.Message}");
            }
        }

        [HttpPost("create-patient")]
        public async Task<ActionResult> CreatePatient([FromBody] CreatePatientRequest request)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest("A patient with this email already exists.");
                }

                // Create new patient user
                var patient = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    MobilePhone = request.MobilePhone,
                    RoleId = 1, // Patient role
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    MustChangePassword = true // Force password change on first login
                };

                // Encrypt DateOfBirth before saving
                UserEncryptionHelper.EncryptUserData(patient, _encryptionService);

                _context.Users.Add(patient);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Patient created successfully", patientId = patient.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating patient: {ex.Message}");
            }
        }

        [HttpPost("create-attorney")]
        [Authorize(Roles = "Admin")] // Only Admin can create attorneys
        public async Task<ActionResult> CreateAttorney([FromBody] CreateAttorneyRequest request)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest("An attorney with this email already exists.");
                }

                // Create new attorney user
                var attorney = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    MobilePhone = request.MobilePhone,
                    RoleId = 5, // Attorney role
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    MustChangePassword = true // Force password change on first login
                };

                // Encrypt DateOfBirth before saving
                UserEncryptionHelper.EncryptUserData(attorney, _encryptionService);

                _context.Users.Add(attorney);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Attorney created successfully", attorneyId = attorney.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating attorney: {ex.Message}");
            }
        }

        [HttpPost("create-coordinator")]
        [Authorize(Roles = "Admin")] // Only Admin can create coordinators
        public async Task<ActionResult> CreateCoordinator([FromBody] CreateCoordinatorRequest request)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest("A coordinator with this email already exists.");
                }

                // Create new coordinator user
                var coordinator = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = HashPassword(request.Password),
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    MobilePhone = request.MobilePhone,
                    RoleId = 4, // Coordinator role
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    MustChangePassword = true // Force password change on first login
                };

                // Encrypt DateOfBirth before saving
                UserEncryptionHelper.EncryptUserData(coordinator, _encryptionService);

                _context.Users.Add(coordinator);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Coordinator created successfully", coordinatorId = coordinator.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating coordinator: {ex.Message}");
            }
        }

        [HttpPut("update-patient/{id}")]
        [Authorize(Roles = "Admin,Doctor")] // Admin and Doctor can update patients
        public async Task<ActionResult> UpdatePatient(int id, [FromBody] UpdatePatientRequest request)
        {
            try
            {
                var patient = await _context.Users.FindAsync(id);
                if (patient == null)
                {
                    return NotFound("Patient not found.");
                }

                if (patient.RoleId != 1)
                {
                    return BadRequest("User is not a patient.");
                }

                // Debug logging
                _logger.LogInformation("UpdatePatient - ID: {Id}, Request: FirstName='{FirstName}', LastName='{LastName}', Email='{Email}', MobilePhone='{MobilePhone}', Password='{Password}'",
                    id, request.FirstName, request.LastName, request.Email, request.MobilePhone,
                    string.IsNullOrEmpty(request.Password) ? "NULL" : "PROVIDED");

                _logger.LogInformation("UpdatePatient - Current: FirstName='{FirstName}', LastName='{LastName}', Email='{Email}', MobilePhone='{MobilePhone}'",
                    patient.FirstName, patient.LastName, patient.Email, patient.MobilePhone);

                // Check if email already exists (excluding current patient)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);

                if (existingUser != null)
                {
                    return BadRequest("A user with this email already exists.");
                }

                // Update patient information only if field is provided, not empty, and different
                if (!string.IsNullOrWhiteSpace(request.FirstName) && patient.FirstName != request.FirstName)
                {
                    patient.FirstName = request.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName) && patient.LastName != request.LastName)
                {
                    patient.LastName = request.LastName;
                }

                if (!string.IsNullOrWhiteSpace(request.Email) && patient.Email != request.Email)
                {
                    patient.Email = request.Email;
                }

                if (request.DateOfBirth.HasValue)
                {
                    // Always update DateOfBirth when provided and encrypt it
                    patient.DateOfBirth = request.DateOfBirth.Value;
                    // Encrypt before saving
                    UserEncryptionHelper.EncryptUserData(patient, _encryptionService);
                }

                if (!string.IsNullOrWhiteSpace(request.Gender) && patient.Gender != request.Gender)
                {
                    patient.Gender = request.Gender;
                }

                // MobilePhone can be null, so check if it's provided and different
                // Decrypt current MobilePhone for comparison
                UserEncryptionHelper.DecryptUserData(patient, _encryptionService);
                if (request.MobilePhone != null && patient.MobilePhone != request.MobilePhone)
                {
                    patient.MobilePhone = request.MobilePhone;
                    // Encrypt before saving
                    UserEncryptionHelper.EncryptUserData(patient, _encryptionService);
                }
                else if (!request.DateOfBirth.HasValue)
                {
                    // If MobilePhone wasn't updated but DateOfBirth wasn't either, ensure encryption is still applied
                    UserEncryptionHelper.EncryptUserData(patient, _encryptionService);
                }

                // Update password only if provided and not empty
                // Don't change MustChangePassword for existing users - preserve existing value
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    patient.PasswordHash = HashPassword(request.Password);
                    // Only set MustChangePassword if it's a new user (IsFirstLogin is true)
                    // For existing users, preserve the current MustChangePassword value
                    if (patient.IsFirstLogin)
                    {
                        patient.MustChangePassword = true;
                    }
                    // Otherwise, keep the existing MustChangePassword value unchanged
                }

                // Ensure DateOfBirth is encrypted before saving (only if DateOfBirth was set but not encrypted above)
                // This handles the case where DateOfBirth wasn't in the request but we need to ensure existing data is encrypted
                if (!request.DateOfBirth.HasValue && !string.IsNullOrEmpty(patient.DateOfBirthEncrypted))
                {
                    // Decrypt existing DateOfBirth to ensure it's valid, then re-encrypt
                    UserEncryptionHelper.DecryptUserData(patient, _encryptionService);
                    if (patient.DateOfBirth != DateTime.MinValue)
                    {
                        UserEncryptionHelper.EncryptUserData(patient, _encryptionService);
                    }
                }

                await _context.SaveChangesAsync();
                
                // Decrypt after saving for response
                UserEncryptionHelper.DecryptUserData(patient, _encryptionService);

                // Debug logging after update
                _logger.LogInformation("UpdatePatient - After: FirstName='{FirstName}', LastName='{LastName}', Email='{Email}', MobilePhone='{MobilePhone}', MustChangePassword={MustChangePassword}",
                    patient.FirstName, patient.LastName, patient.Email, patient.MobilePhone, patient.MustChangePassword);

                return Ok(new { message = "Patient updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating patient: {ex.Message}");
            }
        }

        /// <summary>
        /// Update accident information for a patient - Admin only
        /// </summary>
        [HttpPut("update-patient/{id}/accident-info")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdatePatientAccidentInfo(int id, [FromBody] UpdateUserAccidentInfoRequest request)
        {
            try
            {
                var patient = await _context.Users.FindAsync(id);
                if (patient == null)
                {
                    return NotFound("Patient not found.");
                }

                if (patient.RoleId != 1)
                {
                    return BadRequest("User is not a patient.");
                }

                // Update accident-related fields
                patient.Age = request.Age;
                patient.Race = request.Race;
                patient.AccidentAddress = request.AccidentAddress;
                patient.AccidentDate = request.AccidentDate;
                patient.VehicleDetails = request.VehicleDetails;
                patient.DateReported = request.DateReported;
                patient.PoliceCaseNumber = request.PoliceCaseNumber;
                patient.AccidentDetails = request.AccidentDetails;
                patient.RoadConditions = request.RoadConditions;
                patient.DoctorsInformation = request.DoctorsInformation;
                patient.LawyersInformation = request.LawyersInformation;
                patient.AdditionalNotes = request.AdditionalNotes;

                await _context.SaveChangesAsync();

                // Decrypt for return
                UserEncryptionHelper.DecryptUserData(patient, _encryptionService);

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient accident information");
                return StatusCode(500, $"Error updating patient accident information: {ex.Message}");
            }
        }

        [HttpPut("doctors/{id}")]
        public async Task<ActionResult> UpdateDoctor(int id, [FromBody] UpdateDoctorRequest request)
        {
            try
            {
                var doctor = await _context.Users.FindAsync(id);
                if (doctor == null)
                {
                    return NotFound("Doctor not found.");
                }

                if (doctor.RoleId != 2)
                {
                    return BadRequest("User is not a doctor.");
                }

                // Check if email already exists (excluding current doctor)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);

                if (existingUser != null)
                {
                    return BadRequest("A user with this email already exists.");
                }

                // Update doctor information only if field is provided, not empty, and different
                if (!string.IsNullOrWhiteSpace(request.FirstName) && doctor.FirstName != request.FirstName)
                {
                    doctor.FirstName = request.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName) && doctor.LastName != request.LastName)
                {
                    doctor.LastName = request.LastName;
                }

                if (!string.IsNullOrWhiteSpace(request.Email) && doctor.Email != request.Email)
                {
                    doctor.Email = request.Email;
                }

                if (request.DateOfBirth.HasValue)
                {
                    // Decrypt current DateOfBirth for comparison
                    UserEncryptionHelper.DecryptUserData(doctor, _encryptionService);
                    if (doctor.DateOfBirth != request.DateOfBirth.Value)
                    {
                        doctor.DateOfBirth = request.DateOfBirth.Value;
                        // Encrypt before saving
                        UserEncryptionHelper.EncryptUserData(doctor, _encryptionService);
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.Gender) && doctor.Gender != request.Gender)
                {
                    doctor.Gender = request.Gender;
                }

                // Decrypt current MobilePhone for comparison
                if (request.MobilePhone != null)
                {
                    if (doctor.MobilePhone != request.MobilePhone)
                    {
                        doctor.MobilePhone = request.MobilePhone;
                    }
                    // Encrypt before saving
                    UserEncryptionHelper.EncryptUserData(doctor, _encryptionService);
                }
                else if (!request.DateOfBirth.HasValue)
                {
                    // If MobilePhone wasn't updated but DateOfBirth wasn't either, ensure encryption is still applied
                    UserEncryptionHelper.EncryptUserData(doctor, _encryptionService);
                }

                if (!string.IsNullOrWhiteSpace(request.Specialization) && doctor.Specialization != request.Specialization)
                {
                    doctor.Specialization = request.Specialization;
                }

                if (!string.IsNullOrWhiteSpace(request.LicenseNumber) && doctor.LicenseNumber != request.LicenseNumber)
                {
                    doctor.LicenseNumber = request.LicenseNumber;
                }

                // Update password only if provided and not empty
                // Don't change MustChangePassword for existing users - preserve existing value
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    doctor.PasswordHash = HashPassword(request.Password);
                    // Only set MustChangePassword if it's a new user (IsFirstLogin is true)
                    // For existing users, preserve the current MustChangePassword value
                    if (doctor.IsFirstLogin)
                    {
                        doctor.MustChangePassword = true;
                    }
                    // Otherwise, keep the existing MustChangePassword value unchanged
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Doctor updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating doctor: {ex.Message}");
            }
        }

        [HttpDelete("doctors/{id}/deactivate")]
        public async Task<ActionResult> DeactivateDoctor(int id)
        {
            try
            {
                var doctor = await _context.Users.FindAsync(id);
                if (doctor == null)
                {
                    return NotFound("Doctor not found.");
                }

                if (doctor.RoleId != 2)
                {
                    return BadRequest("User is not a doctor.");
                }

                doctor.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Doctor deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deactivating doctor: {ex.Message}");
            }
        }

        [HttpPost("doctors/{id}/reactivate")]
        public async Task<ActionResult> ReactivateDoctor(int id)
        {
            try
            {
                var doctor = await _context.Users.FindAsync(id);
                if (doctor == null)
                {
                    return NotFound("Doctor not found.");
                }

                if (doctor.RoleId != 2)
                {
                    return BadRequest("User is not a doctor.");
                }

                doctor.IsActive = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Doctor reactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reactivating doctor: {ex.Message}");
            }
        }

        [HttpPut("coordinators/{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can update coordinators
        public async Task<ActionResult> UpdateCoordinator(int id, [FromBody] UpdateCoordinatorRequest request)
        {
            try
            {
                var coordinator = await _context.Users.FindAsync(id);
                if (coordinator == null)
                {
                    return NotFound("Coordinator not found.");
                }

                if (coordinator.RoleId != 4)
                {
                    return BadRequest("User is not a coordinator.");
                }

                // Check if email already exists (excluding current coordinator)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);

                if (existingUser != null)
                {
                    return BadRequest("A user with this email already exists.");
                }

                // Update coordinator information
                if (!string.IsNullOrWhiteSpace(request.FirstName) && coordinator.FirstName != request.FirstName)
                {
                    coordinator.FirstName = request.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName) && coordinator.LastName != request.LastName)
                {
                    coordinator.LastName = request.LastName;
                }

                if (!string.IsNullOrWhiteSpace(request.Email) && coordinator.Email != request.Email)
                {
                    coordinator.Email = request.Email;
                }

                if (request.DateOfBirth.HasValue)
                {
                    // Decrypt current DateOfBirth for comparison
                    UserEncryptionHelper.DecryptUserData(coordinator, _encryptionService);
                    if (coordinator.DateOfBirth != request.DateOfBirth.Value)
                    {
                        coordinator.DateOfBirth = request.DateOfBirth.Value;
                        // Encrypt before saving
                        UserEncryptionHelper.EncryptUserData(coordinator, _encryptionService);
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.Gender) && coordinator.Gender != request.Gender)
                {
                    coordinator.Gender = request.Gender;
                }

                // Decrypt current MobilePhone for comparison
                if (request.MobilePhone != null)
                {
                    if (coordinator.MobilePhone != request.MobilePhone)
                    {
                        coordinator.MobilePhone = request.MobilePhone;
                    }
                    // Encrypt before saving
                    UserEncryptionHelper.EncryptUserData(coordinator, _encryptionService);
                }
                else if (!request.DateOfBirth.HasValue)
                {
                    // If MobilePhone wasn't updated but DateOfBirth wasn't either, ensure encryption is still applied
                    UserEncryptionHelper.EncryptUserData(coordinator, _encryptionService);
                }

                // Update password only if provided
                // Don't change MustChangePassword for existing users - preserve existing value
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    coordinator.PasswordHash = HashPassword(request.Password);
                    // Only set MustChangePassword if it's a new user (IsFirstLogin is true)
                    // For existing users, preserve the current MustChangePassword value
                    if (coordinator.IsFirstLogin)
                    {
                        coordinator.MustChangePassword = true;
                    }
                    // Otherwise, keep the existing MustChangePassword value unchanged
                }

                // Ensure DateOfBirth is encrypted before saving (in case it wasn't updated above)
                UserEncryptionHelper.EncryptUserData(coordinator, _encryptionService);

                await _context.SaveChangesAsync();
                
                // Decrypt after saving for response
                UserEncryptionHelper.DecryptUserData(coordinator, _encryptionService);

                return Ok(new { message = "Coordinator updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating coordinator: {ex.Message}");
            }
        }

        [HttpDelete("coordinators/{id}/deactivate")]
        [Authorize(Roles = "Admin")] // Only Admin can deactivate coordinators
        public async Task<ActionResult> DeactivateCoordinator(int id)
        {
            try
            {
                var coordinator = await _context.Users.FindAsync(id);
                if (coordinator == null)
                {
                    return NotFound("Coordinator not found.");
                }

                if (coordinator.RoleId != 4)
                {
                    return BadRequest("User is not a coordinator.");
                }

                coordinator.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Coordinator deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deactivating coordinator: {ex.Message}");
            }
        }

        [HttpPost("coordinators/{id}/reactivate")]
        [Authorize(Roles = "Admin")] // Only Admin can reactivate coordinators
        public async Task<ActionResult> ReactivateCoordinator(int id)
        {
            try
            {
                var coordinator = await _context.Users.FindAsync(id);
                if (coordinator == null)
                {
                    return NotFound("Coordinator not found.");
                }

                if (coordinator.RoleId != 4)
                {
                    return BadRequest("User is not a coordinator.");
                }

                coordinator.IsActive = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Coordinator reactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reactivating coordinator: {ex.Message}");
            }
        }

        [HttpPut("update-attorney/{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can update attorneys
        public async Task<ActionResult> UpdateAttorney(int id, [FromBody] UpdateAttorneyRequest request)
        {
            try
            {
                var attorney = await _context.Users.FindAsync(id);
                if (attorney == null)
                {
                    return NotFound("Attorney not found.");
                }

                if (attorney.RoleId != 5)
                {
                    return BadRequest("User is not an attorney.");
                }

                // Check if email already exists (excluding current attorney)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);

                if (existingUser != null)
                {
                    return BadRequest("A user with this email already exists.");
                }

                // Update attorney information
                if (!string.IsNullOrWhiteSpace(request.FirstName) && attorney.FirstName != request.FirstName)
                {
                    attorney.FirstName = request.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName) && attorney.LastName != request.LastName)
                {
                    attorney.LastName = request.LastName;
                }

                if (!string.IsNullOrWhiteSpace(request.Email) && attorney.Email != request.Email)
                {
                    attorney.Email = request.Email;
                }

                if (request.DateOfBirth.HasValue)
                {
                    // Decrypt current DateOfBirth for comparison
                    UserEncryptionHelper.DecryptUserData(attorney, _encryptionService);
                    if (attorney.DateOfBirth != request.DateOfBirth.Value)
                    {
                        attorney.DateOfBirth = request.DateOfBirth.Value;
                        // Encrypt before saving
                        UserEncryptionHelper.EncryptUserData(attorney, _encryptionService);
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.Gender) && attorney.Gender != request.Gender)
                {
                    attorney.Gender = request.Gender;
                }

                // Decrypt current MobilePhone for comparison
                if (request.MobilePhone != null)
                {
                    if (attorney.MobilePhone != request.MobilePhone)
                    {
                        attorney.MobilePhone = request.MobilePhone;
                    }
                    // Encrypt before saving
                    UserEncryptionHelper.EncryptUserData(attorney, _encryptionService);
                }
                else if (!request.DateOfBirth.HasValue)
                {
                    // If MobilePhone wasn't updated but DateOfBirth wasn't either, ensure encryption is still applied
                    UserEncryptionHelper.EncryptUserData(attorney, _encryptionService);
                }

                // Update password only if provided
                // Don't change MustChangePassword for existing users - preserve existing value
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    attorney.PasswordHash = HashPassword(request.Password);
                    // Only set MustChangePassword if it's a new user (IsFirstLogin is true)
                    // For existing users, preserve the current MustChangePassword value
                    if (attorney.IsFirstLogin)
                    {
                        attorney.MustChangePassword = true;
                    }
                    // Otherwise, keep the existing MustChangePassword value unchanged
                }

                // Ensure DateOfBirth is encrypted before saving (in case it wasn't updated above)
                UserEncryptionHelper.EncryptUserData(attorney, _encryptionService);

                await _context.SaveChangesAsync();
                
                // Decrypt after saving for response
                UserEncryptionHelper.DecryptUserData(attorney, _encryptionService);

                return Ok(new { message = "Attorney updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating attorney: {ex.Message}");
            }
        }

        [HttpDelete("attorneys/{id}/deactivate")]
        [Authorize(Roles = "Admin")] // Only Admin can deactivate attorneys
        public async Task<ActionResult> DeactivateAttorney(int id)
        {
            try
            {
                var attorney = await _context.Users.FindAsync(id);
                if (attorney == null)
                {
                    return NotFound("Attorney not found.");
                }

                if (attorney.RoleId != 5)
                {
                    return BadRequest("User is not an attorney.");
                }

                attorney.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Attorney deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deactivating attorney: {ex.Message}");
            }
        }

        [HttpPost("attorneys/{id}/reactivate")]
        [Authorize(Roles = "Admin")] // Only Admin can reactivate attorneys
        public async Task<ActionResult> ReactivateAttorney(int id)
        {
            try
            {
                var attorney = await _context.Users.FindAsync(id);
                if (attorney == null)
                {
                    return NotFound("Attorney not found.");
                }

                if (attorney.RoleId != 5)
                {
                    return BadRequest("User is not an attorney.");
                }

                attorney.IsActive = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Attorney reactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reactivating attorney: {ex.Message}");
            }
        }

        /// <summary>
        /// Hash password using the same method as AuthService for consistency
        /// This ensures passwords created here can be verified by AuthService
        /// </summary>
        private string HashPassword(string password)
        {
            // Use the same PBKDF2 hashing method as AuthService for consistency
            // 32-byte salt, 100,000 iterations, SHA256, 64-byte output
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[32];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            var hashBytes = new byte[64];
            Array.Copy(salt, 0, hashBytes, 0, 32);
            Array.Copy(hash, 0, hashBytes, 32, 32);

            return Convert.ToBase64String(hashBytes);
        }
    }

    public class AssignPatientRequest
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
    }

    public class UnassignPatientRequest
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
    }

    public class CreateDoctorRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
    }

    public class UpdateDoctorRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MobilePhone { get; set; }
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Password { get; set; }
    }

    public class CreatePatientRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
    }

    public class UpdatePatientRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MobilePhone { get; set; }
        public string? Password { get; set; }
    }
    
    public class UpdateUserAccidentInfoRequest
    {
        public int? Age { get; set; }
        public string? Race { get; set; }
        public string? AccidentAddress { get; set; }
        public DateTime? AccidentDate { get; set; }
        public string? VehicleDetails { get; set; }
        public DateTime? DateReported { get; set; }
        public string? PoliceCaseNumber { get; set; }
        public string? AccidentDetails { get; set; }
        public string? RoadConditions { get; set; }
        public string? DoctorsInformation { get; set; }
        public string? LawyersInformation { get; set; }
        public string? AdditionalNotes { get; set; }
    }

    public class CreateCoordinatorRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
    }

    public class UpdateCoordinatorRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MobilePhone { get; set; }
        public string? Password { get; set; }
    }

    public class CreateAttorneyRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? MobilePhone { get; set; }
    }

    public class UpdateAttorneyRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MobilePhone { get; set; }
        public string? Password { get; set; }
    }
}
