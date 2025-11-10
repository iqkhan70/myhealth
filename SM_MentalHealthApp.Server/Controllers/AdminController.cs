using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Data;
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
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly JournalDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, JournalDbContext context, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _context = context;
            _logger = logger;
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

                // Build comprehensive context for AI analysis
                var contentAnalysisService = HttpContext.RequestServices.GetRequiredService<IContentAnalysisService>();
                var originalPrompt = $"AI Health Check for Patient {patient.FirstName} {patient.LastName}";
                _logger.LogInformation("Building context for patient {PatientId} with prompt: {Prompt}", patientId, originalPrompt);

                var patientContext = await contentAnalysisService.BuildEnhancedContextAsync(patientId, originalPrompt);

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

                // Check if AI indicates high severity
                bool isHighSeverity = IsAiResponseIndicatingHighSeverity(aiResponse);

                if (isHighSeverity)
                {
                    _logger.LogInformation("AI detected high severity for patient {PatientId}. Sending alerts to doctors.", patientId);

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

                    return Ok(new AiHealthCheckResult
                    {
                        Success = true,
                        Severity = "High",
                        AiResponse = aiResponse,
                        AlertsSent = alertsSent,
                        DoctorsNotified = assignedDoctors.Count,
                        Message = $"AI detected high severity. {alertsSent} doctors notified."
                    });
                }
                else
                {
                    _logger.LogInformation("AI health check for patient {PatientId} shows normal status", patientId);

                    return Ok(new AiHealthCheckResult
                    {
                        Success = true,
                        Severity = "Normal",
                        AiResponse = aiResponse,
                        AlertsSent = 0,
                        DoctorsNotified = 0,
                        Message = "AI health check shows normal status. No alerts sent."
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

                        if (!isHistorical)
                        {
                            _logger.LogWarning("Critical keyword '{Keyword}' found in current context despite positive indicators. Flagging as high severity.", keyword);
                            return true;
                        }
                        else
                        {
                            _logger.LogDebug("Critical keyword '{Keyword}' found but appears to be historical (context: '{Context}'). Ignoring.", keyword, contextBefore + "..." + contextAfter);
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
                "self-harm", "overdose", "chest pain", "difficulty breathing"
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
            bool isHighSeverity = severityScore >= threshold ||
                   (response.Contains("critical") && !response.Substring(0, Math.Min(500, response.Length)).Contains("recent")) ||
                   (response.Contains("emergency") && !response.Substring(0, Math.Min(500, response.Length)).Contains("recent")) ||
                   response.Contains("unacknowledged");

            _logger.LogDebug("Severity analysis complete. Score: {Score}, Threshold: {Threshold}, Result: {IsHighSeverity}",
                severityScore, threshold, isHighSeverity);

            return isHighSeverity;
        }

        [HttpGet("assignments")]
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
        public async Task<ActionResult> AssignPatientToDoctor([FromBody] AssignPatientRequest request)
        {
            try
            {
                var success = await _adminService.AssignPatientToDoctorAsync(request.PatientId, request.DoctorId);
                if (success)
                {
                    return Ok(new { message = "Patient assigned to doctor successfully" });
                }
                return BadRequest("Failed to assign patient to doctor. Patient or doctor may not exist, or assignment may already exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error assigning patient to doctor: {ex.Message}");
            }
        }

        [HttpDelete("unassign")]
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

                _context.Users.Add(patient);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Patient created successfully", patientId = patient.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating patient: {ex.Message}");
            }
        }

        [HttpPut("update-patient/{id}")]
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

                if (request.DateOfBirth.HasValue && patient.DateOfBirth != request.DateOfBirth.Value)
                {
                    patient.DateOfBirth = request.DateOfBirth.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Gender) && patient.Gender != request.Gender)
                {
                    patient.Gender = request.Gender;
                }

                // MobilePhone can be null, so check if it's provided and different
                if (request.MobilePhone != null && patient.MobilePhone != request.MobilePhone)
                {
                    patient.MobilePhone = request.MobilePhone;
                }

                // Update password only if provided and not empty
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    patient.PasswordHash = HashPassword(request.Password);
                    patient.MustChangePassword = true;
                }

                await _context.SaveChangesAsync();

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

                if (request.DateOfBirth.HasValue && doctor.DateOfBirth != request.DateOfBirth.Value)
                {
                    doctor.DateOfBirth = request.DateOfBirth.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Gender) && doctor.Gender != request.Gender)
                {
                    doctor.Gender = request.Gender;
                }

                if (request.MobilePhone != null && doctor.MobilePhone != request.MobilePhone)
                {
                    doctor.MobilePhone = request.MobilePhone;
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
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    doctor.PasswordHash = HashPassword(request.Password);
                    doctor.MustChangePassword = true;
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

        private string HashPassword(string password)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[16];
                rng.GetBytes(salt);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
                {
                    var hash = pbkdf2.GetBytes(32);
                    var hashBytes = new byte[48];
                    Array.Copy(salt, 0, hashBytes, 0, 16);
                    Array.Copy(hash, 0, hashBytes, 16, 32);
                    return Convert.ToBase64String(hashBytes);
                }
            }
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
}
