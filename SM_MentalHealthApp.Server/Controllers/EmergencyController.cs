using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SM_MentalHealthApp.Server.Data;
using global::SM_MentalHealthApp.Server.Models;
using SM_MentalHealthApp.Server.Services;
using global::SM_MentalHealthApp.Shared;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class EmergencyController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmergencyController> _logger;

        public EmergencyController(
            JournalDbContext context,
            INotificationService notificationService,
            ILogger<EmergencyController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Receives emergency messages from registered devices
        /// Bypasses normal authentication for emergency situations
        /// </summary>
        [HttpPost("receive")]
        public async Task<ActionResult> ReceiveEmergencyMessage([FromBody] EmergencyMessage message)
        {
            try
            {
                _logger.LogInformation("Emergency message received from device: {DeviceToken}", message.DeviceToken);

                // Security validation layers
                var validationResult = await ValidateEmergencyMessage(message);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Emergency message validation failed: {Reason}", validationResult.Reason);
                    return BadRequest(new { message = "Invalid emergency message", reason = validationResult.Reason });
                }

                // Get device and patient information
                var device = validationResult.Device!;
                var patient = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == device.PatientId);

                if (patient == null)
                {
                    _logger.LogError("Patient not found for device: {DeviceId}", device.DeviceId);
                    return BadRequest("Patient not found");
                }

                // Create emergency incident log
                var incident = new EmergencyIncident
                {
                    PatientId = patient.Id,
                    EmergencyType = message.EmergencyType,
                    Severity = message.Severity,
                    Message = message.Message ?? "Emergency detected",
                    Timestamp = message.Timestamp,
                    DeviceId = device.DeviceId,
                    DeviceToken = message.DeviceToken,
                    VitalSignsJson = message.VitalSigns != null ? JsonSerializer.Serialize(message.VitalSigns) : null,
                    LocationJson = message.Location != null ? JsonSerializer.Serialize(message.Location) : null,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };

                _context.EmergencyIncidents.Add(incident);
                await _context.SaveChangesAsync();

                // Update device last used timestamp
                device.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Get assigned doctors for this patient
                var assignedDoctors = await _context.UserAssignments
                    .Where(ua => ua.AssigneeId == patient.Id)
                    .Include(ua => ua.Assigner)
                    .Select(ua => ua.Assigner)
                    .Where(d => d.IsActive)
                    .ToListAsync();

                // Create emergency alert
                var alert = new global::SM_MentalHealthApp.Shared.EmergencyAlert
                {
                    Id = incident.Id,
                    PatientId = patient.Id,
                    PatientName = $"{patient.FirstName} {patient.LastName}",
                    PatientEmail = patient.Email,
                    EmergencyType = message.EmergencyType,
                    Severity = message.Severity,
                    Message = message.Message ?? "Emergency detected",
                    Timestamp = message.Timestamp,
                    VitalSigns = message.VitalSigns != null ? new global::SM_MentalHealthApp.Shared.VitalSigns
                    {
                        HeartRate = message.VitalSigns.HeartRate,
                        BloodPressure = message.VitalSigns.BloodPressure,
                        Temperature = message.VitalSigns.Temperature,
                        OxygenSaturation = message.VitalSigns.OxygenSaturation
                    } : null,
                    Location = message.Location != null ? new global::SM_MentalHealthApp.Shared.LocationData
                    {
                        Latitude = message.Location.Latitude,
                        Longitude = message.Location.Longitude,
                        Accuracy = message.Location.Accuracy,
                        Address = message.Location.Address,
                        Timestamp = message.Location.Timestamp
                    } : null,
                    DeviceId = device.DeviceId
                };

                // Send notifications to assigned doctors
                await NotifyDoctors(alert, assignedDoctors);

                _logger.LogInformation("Emergency processed successfully for patient {PatientId}, incident {IncidentId}",
                    patient.Id, incident.Id);

                return Ok(new
                {
                    success = true,
                    incidentId = incident.Id,
                    message = "Emergency received and doctors notified"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing emergency message");
                return StatusCode(500, "Internal server error processing emergency");
            }
        }

        /// <summary>
        /// Test endpoint to check if AllowAnonymous works
        /// </summary>
        [AllowAnonymous]
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Test endpoint works!" });
        }

        /// <summary>
        /// Simple test endpoint for mobile app connectivity
        /// </summary>
        [AllowAnonymous]
        [HttpGet("mobile-test")]
        public IActionResult MobileTest()
        {
            return Ok(new
            {
                success = true,
                message = "Mobile app can reach server!",
                timestamp = DateTime.UtcNow,
                server = "SM_MentalHealthApp.Server"
            });
        }

        /// <summary>
        /// Clear all registered devices (for testing)
        /// </summary>
        [AllowAnonymous]
        [HttpDelete("clear-devices")]
        public async Task<IActionResult> ClearDevices()
        {
            try
            {
                var devices = await _context.RegisteredDevices.ToListAsync();
                _context.RegisteredDevices.RemoveRange(devices);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Cleared {devices.Count} devices",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing devices");
                return StatusCode(500, "Error clearing devices");
            }
        }

        /// <summary>
        /// Simple test endpoint for device registration
        /// </summary>
        [AllowAnonymous]
        [HttpPost("test-register")]
        public async Task<IActionResult> TestRegister([FromBody] DeviceRegistrationRequest request)
        {
            try
            {
                // Verify patient exists and is active
                var patient = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.IsActive);

                if (patient == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Patient not found or inactive"
                    });
                }

                // Generate device token (use unique token for testing)
                var deviceToken = GenerateDeviceToken();

                // Check if device is already registered for this patient
                var existingDevice = await _context.RegisteredDevices
                    .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && d.PatientId == request.PatientId);

                if (existingDevice != null)
                {
                    // Update existing device
                    _logger.LogInformation("Updating existing device: {DeviceId}", request.DeviceId);
                    existingDevice.DeviceName = request.DeviceName;
                    existingDevice.DeviceType = request.DeviceType;
                    existingDevice.DeviceModel = request.DeviceModel ?? string.Empty;
                    existingDevice.OperatingSystem = request.OperatingSystem ?? string.Empty;
                    existingDevice.DeviceToken = deviceToken;
                    existingDevice.ExpiresAt = DateTime.UtcNow.AddYears(1);
                    existingDevice.IsActive = true;
                    existingDevice.LastUsedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new device
                    _logger.LogInformation("Creating new device: {DeviceId}", request.DeviceId);
                    var newDevice = new RegisteredDevice
                    {
                        PatientId = request.PatientId,
                        DeviceId = request.DeviceId,
                        DeviceName = request.DeviceName,
                        DeviceType = request.DeviceType,
                        DeviceModel = request.DeviceModel ?? string.Empty,
                        OperatingSystem = request.OperatingSystem ?? string.Empty,
                        DeviceToken = deviceToken,
                        PublicKey = "test-key",
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddYears(1),
                        IsActive = true
                    };
                    _context.RegisteredDevices.Add(newDevice);
                }

                try
                {
                    var saveResult = await _context.SaveChangesAsync();
                    _logger.LogInformation("Save result: {SaveResult}", saveResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving device to database");
                    throw;
                }

                // Get the actual stored device token
                var storedDevice = await _context.RegisteredDevices
                    .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && d.PatientId == request.PatientId);

                _logger.LogInformation("Device registered: DeviceId={DeviceId}, PatientId={PatientId}, Token={Token}",
                    request.DeviceId, request.PatientId, storedDevice?.DeviceToken);

                return Ok(new
                {
                    success = true,
                    message = "Test registration successful!",
                    deviceToken = storedDevice?.DeviceToken ?? deviceToken,
                    deviceId = storedDevice?.DeviceId,
                    patientId = storedDevice?.PatientId,
                    isActive = storedDevice?.IsActive,
                    expiresAt = DateTime.UtcNow.AddYears(1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test registration");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        /// <summary>
        /// Registers a device for emergency messaging
        /// Requires patient authentication
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register-device")]
        public async Task<ActionResult<DeviceRegistrationResponse>> RegisterDevice([FromBody] DeviceRegistrationRequest request)
        {
            int patientId = 0;
            try
            {
                // Get patient ID from request body (temporary for testing)
                patientId = request.PatientId;

                // Verify patient exists and is active
                var patient = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == patientId && u.IsActive);

                if (patient == null)
                {
                    return BadRequest(new DeviceRegistrationResponse
                    {
                        Success = false,
                        Message = "Patient not found or inactive"
                    });
                }

                // Check if device is already registered for this patient
                var existingDevice = await _context.RegisteredDevices
                    .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && d.PatientId == patientId);

                if (existingDevice != null)
                {
                    // Update existing device
                    existingDevice.DeviceName = request.DeviceName;
                    existingDevice.DeviceType = request.DeviceType;
                    existingDevice.DeviceModel = request.DeviceModel ?? string.Empty;
                    existingDevice.OperatingSystem = request.OperatingSystem ?? string.Empty;
                    existingDevice.ExpiresAt = DateTime.UtcNow.AddYears(1);
                    existingDevice.IsActive = true;
                    existingDevice.LastUsedAt = DateTime.UtcNow;
                }
                else
                {
                    // Generate device token and key pair
                    var deviceToken = GenerateDeviceToken();
                    var keyPair = GenerateKeyPair();

                    var newDevice = new RegisteredDevice
                    {
                        PatientId = patientId,
                        DeviceId = request.DeviceId,
                        DeviceName = request.DeviceName,
                        DeviceType = request.DeviceType,
                        DeviceModel = request.DeviceModel ?? string.Empty,
                        OperatingSystem = request.OperatingSystem ?? string.Empty,
                        DeviceToken = deviceToken,
                        PublicKey = keyPair.PublicKey,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddYears(1),
                        IsActive = true,
                        LastUsedAt = DateTime.UtcNow
                    };

                    _context.RegisteredDevices.Add(newDevice);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Device registered successfully for patient {PatientId}: {DeviceId}",
                    patientId, request.DeviceId);

                return Ok(new DeviceRegistrationResponse
                {
                    Success = true,
                    DeviceToken = existingDevice?.DeviceToken ?? GenerateDeviceToken(),
                    Message = "Device registered successfully",
                    ExpiresAt = DateTime.UtcNow.AddYears(1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device for patient {PatientId}", patientId);
                return StatusCode(500, "Internal server error registering device");
            }
        }

        /// <summary>
        /// Gets all emergency incidents (for debugging)
        /// </summary>
        [HttpGet("incidents-all")]
        public async Task<ActionResult<List<global::SM_MentalHealthApp.Shared.EmergencyAlert>>> GetAllEmergencyIncidents()
        {
            try
            {
                var incidents = await _context.EmergencyIncidents
                    .Include(ei => ei.Patient)
                    .OrderByDescending(ei => ei.Timestamp)
                    .Take(50)
                    .Select(ei => new global::SM_MentalHealthApp.Shared.EmergencyAlert
                    {
                        Id = ei.Id,
                        PatientId = ei.PatientId,
                        PatientName = $"{ei.Patient.FirstName} {ei.Patient.LastName}",
                        PatientEmail = ei.Patient.Email,
                        EmergencyType = ei.EmergencyType,
                        Severity = ei.Severity,
                        Message = ei.Message,
                        Timestamp = ei.Timestamp,
                        DeviceId = ei.DeviceId,
                        IsAcknowledged = ei.IsAcknowledged,
                        AcknowledgedAt = ei.AcknowledgedAt,
                        AcknowledgedByDoctorId = ei.DoctorId,
                        VitalSigns = null, // Will be populated after query
                        Location = null // Will be populated after query
                    })
                    .ToListAsync();

                // Populate VitalSigns and Location after query
                foreach (var incident in incidents)
                {
                    var dbIncident = await _context.EmergencyIncidents.FindAsync(incident.Id);
                    if (dbIncident != null)
                    {
                        if (!string.IsNullOrEmpty(dbIncident.VitalSignsJson))
                        {
                            incident.VitalSigns = JsonSerializer.Deserialize<global::SM_MentalHealthApp.Shared.VitalSigns>(dbIncident.VitalSignsJson);
                        }
                        if (!string.IsNullOrEmpty(dbIncident.LocationJson))
                        {
                            incident.Location = JsonSerializer.Deserialize<global::SM_MentalHealthApp.Shared.LocationData>(dbIncident.LocationJson);
                        }
                    }
                }

                return Ok(incidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all emergency incidents");
                return StatusCode(500, "Internal server error retrieving incidents");
            }
        }

        /// <summary>
        /// Gets emergency incidents for a doctor
        /// </summary>
        [HttpGet("incidents/{doctorId}")]
        public async Task<ActionResult<List<global::SM_MentalHealthApp.Shared.EmergencyAlert>>> GetEmergencyIncidents(int doctorId)
        {
            try
            {
                var incidents = await _context.EmergencyIncidents
                    .Where(ei => ei.DoctorId == doctorId ||
                                ei.DoctorId == null || // Include unassigned incidents
                                _context.UserAssignments.Any(ua =>
                                    ua.AssignerId == doctorId && ua.AssigneeId == ei.PatientId))
                    .Include(ei => ei.Patient)
                    .OrderByDescending(ei => ei.Timestamp)
                    .Take(50)
                    .Select(ei => new global::SM_MentalHealthApp.Shared.EmergencyAlert
                    {
                        Id = ei.Id,
                        PatientId = ei.PatientId,
                        PatientName = $"{ei.Patient.FirstName} {ei.Patient.LastName}",
                        PatientEmail = ei.Patient.Email,
                        EmergencyType = ei.EmergencyType,
                        Severity = ei.Severity,
                        Message = ei.Message,
                        Timestamp = ei.Timestamp,
                        DeviceId = ei.DeviceId,
                        IsAcknowledged = ei.IsAcknowledged,
                        AcknowledgedAt = ei.AcknowledgedAt,
                        AcknowledgedByDoctorId = ei.DoctorId,
                        VitalSigns = null, // Will be populated after query
                        Location = null // Will be populated after query
                    })
                    .ToListAsync();

                // Populate VitalSigns and Location after query
                foreach (var incident in incidents)
                {
                    var dbIncident = await _context.EmergencyIncidents.FindAsync(incident.Id);
                    if (dbIncident != null)
                    {
                        if (!string.IsNullOrEmpty(dbIncident.VitalSignsJson))
                        {
                            incident.VitalSigns = JsonSerializer.Deserialize<global::SM_MentalHealthApp.Shared.VitalSigns>(dbIncident.VitalSignsJson);
                        }
                        if (!string.IsNullOrEmpty(dbIncident.LocationJson))
                        {
                            incident.Location = JsonSerializer.Deserialize<global::SM_MentalHealthApp.Shared.LocationData>(dbIncident.LocationJson);
                        }
                    }
                }

                return Ok(incidents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving emergency incidents for doctor {DoctorId}", doctorId);
                return StatusCode(500, "Internal server error retrieving incidents");
            }
        }

        /// <summary>
        /// Test endpoint to simulate emergency messages (for development/testing)
        /// </summary>
        [HttpPost("test-emergency")]
        public async Task<ActionResult> TestEmergency([FromBody] TestEmergencyRequest request)
        {
            try
            {
                // Create a test emergency message
                var testMessage = new EmergencyMessage
                {
                    DeviceToken = request.DeviceToken,
                    EmergencyType = request.EmergencyType,
                    Timestamp = DateTime.UtcNow,
                    Message = request.Message,
                    Severity = request.Severity,
                    VitalSigns = new global::SM_MentalHealthApp.Server.Models.VitalSigns
                    {
                        HeartRate = request.HeartRate,
                        BloodPressure = request.BloodPressure,
                        Temperature = request.Temperature,
                        OxygenSaturation = request.OxygenSaturation
                    },
                    Location = new global::SM_MentalHealthApp.Server.Models.LocationData
                    {
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        Accuracy = 10.0,
                        Address = "Test Location",
                        Timestamp = DateTime.UtcNow
                    },
                    DeviceId = request.DeviceId
                };

                // Process the test emergency
                var result = await ReceiveEmergencyMessage(testMessage);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test emergency endpoint");
                return StatusCode(500, "Error processing test emergency");
            }
        }

        /// <summary>
        /// Acknowledges an emergency incident
        /// </summary>
        [HttpPost("acknowledge/{incidentId}")]
        public async Task<ActionResult> AcknowledgeIncident(int incidentId, [FromBody] AcknowledgeRequest request)
        {
            try
            {
                var incident = await _context.EmergencyIncidents.FindAsync(incidentId);
                if (incident == null)
                {
                    return NotFound("Incident not found");
                }

                incident.IsAcknowledged = true;
                incident.AcknowledgedAt = DateTime.UtcNow;
                incident.DoctorId = request.DoctorId;
                incident.DoctorResponse = request.Response;
                incident.ActionTaken = request.ActionTaken;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Emergency incident {IncidentId} acknowledged by doctor {DoctorId}",
                    incidentId, request.DoctorId);

                return Ok(new { message = "Incident acknowledged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging incident {IncidentId}", incidentId);
                return StatusCode(500, "Internal server error acknowledging incident");
            }
        }

        #region Private Methods

        private async Task<ValidationResult> ValidateEmergencyMessage(EmergencyMessage message)
        {
            // Check rate limiting (prevent spam)
            var recentMessages = await _context.EmergencyIncidents
                .Where(ei => ei.DeviceToken == message.DeviceToken &&
                            ei.Timestamp > DateTime.UtcNow.AddMinutes(-5))
                .CountAsync();

            if (recentMessages > 10) // Max 10 messages per 5 minutes
            {
                return new ValidationResult { IsValid = false, Reason = "Rate limit exceeded" };
            }

            // Find device by token
            var device = await _context.RegisteredDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == message.DeviceToken && d.IsActive);

            _logger.LogInformation("Device validation: Token={Token}, DeviceFound={DeviceFound}",
                message.DeviceToken, device != null);

            if (device == null)
            {
                return new ValidationResult { IsValid = false, Reason = "Invalid device token" };
            }

            // Check if device token is expired
            if (device.ExpiresAt < DateTime.UtcNow)
            {
                return new ValidationResult { IsValid = false, Reason = "Device token expired" };
            }

            // Validate emergency type
            if (!Enum.IsDefined(typeof(EmergencyType), message.EmergencyType))
            {
                return new ValidationResult { IsValid = false, Reason = "Invalid emergency type" };
            }

            // Validate severity
            if (!Enum.IsDefined(typeof(EmergencySeverity), message.Severity))
            {
                return new ValidationResult { IsValid = false, Reason = "Invalid severity level" };
            }

            // TODO: Add signature verification if needed
            // if (!VerifyMessageSignature(message, device.PublicKey))
            // {
            //     return new ValidationResult { IsValid = false, Reason = "Invalid message signature" };
            // }

            return new ValidationResult { IsValid = true, Device = device };
        }

        private async Task NotifyDoctors(global::SM_MentalHealthApp.Shared.EmergencyAlert alert, List<User> doctors)
        {
            foreach (var doctor in doctors)
            {
                try
                {
                    // Send real-time notification (WebSocket)
                    await _notificationService.SendEmergencyAlert(doctor.Id, alert);

                    // Send push notification
                    await _notificationService.SendPushNotification(doctor.Id,
                        "🚨 EMERGENCY ALERT",
                        $"{alert.PatientName} - {alert.EmergencyType}");

                    _logger.LogInformation("Emergency notification sent to doctor {DoctorId}", doctor.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send emergency notification to doctor {DoctorId}", doctor.Id);
                }
            }
        }

        private string GenerateDeviceToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private (string PublicKey, string PrivateKey) GenerateKeyPair()
        {
            using var rsa = RSA.Create(2048);
            return (
                Convert.ToBase64String(rsa.ExportRSAPublicKey()),
                Convert.ToBase64String(rsa.ExportRSAPrivateKey())
            );
        }

        private string GetClientIpAddress()
        {
            var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ipAddress ?? "Unknown";
        }

        #endregion
    }

    // Helper classes
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? Reason { get; set; }
        public RegisteredDevice? Device { get; set; }
    }

    public class AcknowledgeRequest
    {
        public int DoctorId { get; set; }
        public string? Response { get; set; }
        public string? ActionTaken { get; set; }
    }
}
