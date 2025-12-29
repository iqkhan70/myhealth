using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Shared.Constants;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentController : BaseController
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IServiceRequestService _serviceRequestService;
        private readonly ILogger<AppointmentController> _logger;
        private readonly JournalDbContext _context;

        public AppointmentController(IAppointmentService appointmentService, IServiceRequestService serviceRequestService, ILogger<AppointmentController> logger, JournalDbContext context)
        {
            _appointmentService = appointmentService;
            _serviceRequestService = serviceRequestService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Validate an appointment before creating it
        /// </summary>
        [HttpPost("validate")]
        [Authorize(Roles = "Admin,Doctor,Coordinator")] // Admin, Doctor, and Coordinator can create appointments
        public async Task<ActionResult<AppointmentValidationResult>> ValidateAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                var result = await _appointmentService.ValidateAppointmentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating appointment: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new AppointmentValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Error validating appointment: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Create a new appointment (Admin, Doctor, or Coordinator)
        /// Doctors can only create appointments for themselves with their assigned patients
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Doctor,Coordinator")]
        public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user token");

                // Get current user's role
                var userClaim = User.FindFirst("roleId")?.Value;
                if (!int.TryParse(userClaim, out int roleId))
                    return Unauthorized("Invalid role");

                // If doctor, enforce restrictions:
                // 1. Doctor can only create appointments for themselves
                // 2. Patient must be assigned to them
                int? serviceRequestId = null;
                if (roleId == Roles.Doctor || roleId == Roles.Attorney)
                {
                    if (request.DoctorId != userId.Value)
                    {
                        return BadRequest("You can only create appointments for yourself");
                    }

                    // Get default ServiceRequest for this patient
                    var defaultSr = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(request.PatientId);
                    if (defaultSr != null)
                    {
                        // Verify user is assigned to this SR
                        var isAssigned = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(defaultSr.Id, userId.Value);
                        if (isAssigned)
                        {
                            serviceRequestId = defaultSr.Id;
                        }
                        else
                        {
                            return BadRequest("You are not assigned to this patient's service request. Please contact admin to assign you.");
                        }
                    }
                }

                var appointment = await _appointmentService.CreateAppointmentAsync(request, userId.Value, serviceRequestId);
                return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update an existing appointment (Admin, Doctor, or Coordinator)
        /// Doctors can only update their own appointments
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Doctor,Coordinator")]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
        {
            try
            {
                if (id != request.Id)
                    return BadRequest("Appointment ID mismatch");

                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user token");

                // Get current user's role
                var userClaim = User.FindFirst("roleId")?.Value;
                if (!int.TryParse(userClaim, out int roleId))
                    return Unauthorized("Invalid role");

                // If doctor, check if this is their appointment
                if (roleId == Roles.Doctor)
                {
                    var existingAppointment = await _appointmentService.GetAppointmentByIdAsync(id);
                    if (existingAppointment == null)
                        return NotFound("Appointment not found");

                    if (existingAppointment.DoctorId != userId.Value)
                    {
                        return Forbid("Doctors can only update their own appointments");
                    }

                    // Ensure doctor can't change the doctor ID
                    if (request.AppointmentDateTime.HasValue)
                    {
                        // Verify patient is still assigned if date/time changed
                        var isAssigned = await _appointmentService.IsPatientAssignedToDoctorAsync(existingAppointment.PatientId, userId.Value);
                        if (!isAssigned)
                        {
                            return BadRequest("Patient must be assigned to you to update this appointment");
                        }
                    }
                }

                var appointment = await _appointmentService.UpdateAppointmentAsync(request);
                if (appointment == null)
                    return NotFound("Appointment not found");

                return Ok(appointment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Cancel an appointment (Admin or Doctor)
        /// Doctors can only cancel their own appointments
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user token");

                // Get current user's role
                var userClaim = User.FindFirst("roleId")?.Value;
                if (!int.TryParse(userClaim, out int roleId))
                    return Unauthorized("Invalid role");

                // If doctor, check if this is their appointment
                if (roleId == Roles.Doctor)
                {
                    var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                    if (appointment == null)
                        return NotFound("Appointment not found");

                    if (appointment.DoctorId != userId.Value)
                    {
                        return Forbid("Doctors can only cancel their own appointments");
                    }
                }

                var success = await _appointmentService.CancelAppointmentAsync(id);
                if (!success)
                    return NotFound("Appointment not found");

                return Ok(new { message = "Appointment cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get appointments with optional filters
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AppointmentDto>>> GetAppointments(
            [FromQuery] int? doctorId = null,
            [FromQuery] int? patientId = null,
            [FromQuery] int? serviceRequestId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user token");

                // Get current user's role
                var userClaim = User.FindFirst("roleId")?.Value;
                if (!int.TryParse(userClaim, out int roleId))
                    return Unauthorized("Invalid role");

                // For doctors and attorneys, filter by assigned ServiceRequests
                if (roleId == Roles.Doctor || roleId == Roles.Attorney)
                {
                    doctorId = userId.Value; // Show only their appointments
                    
                    // Get assigned ServiceRequest IDs for this SME
                    var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(userId.Value);

                    if (!serviceRequestIds.Any())
                    {
                        return Ok(new List<AppointmentDto>());
                    }

                    // If specific SR requested, verify access
                    if (serviceRequestId.HasValue)
                    {
                        if (!serviceRequestIds.Contains(serviceRequestId.Value))
                            return Forbid("You are not assigned to this service request");
                        
                        serviceRequestIds = new List<int> { serviceRequestId.Value };
                    }

                    // Filter appointments by ServiceRequestId
                    var query = _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .Include(a => a.CreatedByUser)
                        .Where(a => a.IsActive && 
                            a.DoctorId == doctorId.Value &&
                            a.ServiceRequestId.HasValue &&
                            serviceRequestIds.Contains(a.ServiceRequestId.Value));

                    if (patientId.HasValue)
                        query = query.Where(a => a.PatientId == patientId.Value);
                    if (startDate.HasValue)
                        query = query.Where(a => a.AppointmentDateTime >= startDate.Value);
                    if (endDate.HasValue)
                        query = query.Where(a => a.AppointmentDateTime <= endDate.Value);

                    var appointments = await query
                        .OrderBy(a => a.AppointmentDateTime)
                        .ToListAsync();

                    // Map to DTOs
                    var appointmentDtos = appointments.Select(a => new AppointmentDto
                    {
                        Id = a.Id,
                        DoctorId = a.DoctorId,
                        DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                        DoctorEmail = a.Doctor.Email,
                        PatientId = a.PatientId,
                        PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                        PatientEmail = a.Patient.Email,
                        AppointmentDateTime = a.AppointmentDateTime,
                        EndDateTime = a.EndDateTime,
                        Duration = a.Duration,
                        AppointmentType = a.AppointmentType,
                        Status = a.Status,
                        Reason = a.Reason,
                        Notes = a.Notes,
                        IsUrgentCare = a.IsUrgentCare,
                        IsBusinessHours = a.IsBusinessHours,
                        TimeZoneId = a.TimeZoneId,
                        CreatedBy = $"{a.CreatedByUser.FirstName} {a.CreatedByUser.LastName}",
                        CreatedAt = a.CreatedAt,
                        ServiceRequestId = a.ServiceRequestId
                    }).ToList();

                    return Ok(appointmentDtos);
                }
                // If patient, only show their appointments
                else if (roleId == Roles.Patient)
                {
                    patientId = userId.Value;
                }
                // Admin and Coordinator can see all

                var allAppointments = await _appointmentService.GetAppointmentsAsync(doctorId, patientId, startDate, endDate);
                
                if (serviceRequestId.HasValue)
                    allAppointments = allAppointments.Where(a => a.ServiceRequestId == serviceRequestId.Value).ToList();
                
                return Ok(allAppointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointments: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a specific appointment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
        {
            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (appointment == null)
                    return NotFound("Appointment not found");

                return Ok(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Check for appointment conflicts
        /// </summary>
        [HttpPost("check-conflicts")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AppointmentConflictCheck>> CheckConflicts(
            [FromBody] CreateAppointmentRequest request)
        {
            try
            {
                var endDateTime = request.AppointmentDateTime.Add(request.Duration);
                var conflictCheck = await _appointmentService.CheckConflictsAsync(
                    request.DoctorId,
                    request.AppointmentDateTime,
                    endDateTime);

                return Ok(conflictCheck);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking conflicts");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get doctor availability for a specific date
        /// </summary>
        [HttpGet("doctor-availability/{doctorId}")]
        public async Task<ActionResult<DoctorAvailability>> GetDoctorAvailability(int doctorId, [FromQuery] DateTime date)
        {
            try
            {
                var availability = await _appointmentService.GetDoctorAvailabilityAsync(doctorId, date);
                if (availability == null)
                    return Ok(new DoctorAvailability
                    {
                        DoctorId = doctorId,
                        Date = date,
                        IsOutOfOffice = false
                    });

                return Ok(availability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor availability");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all doctor availability/OOO periods for a date range
        /// </summary>
        [HttpGet("doctor-availability/{doctorId}/range")]
        public async Task<ActionResult<List<DoctorAvailability>>> GetDoctorAvailabilities(
            int doctorId, 
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            try
            {
                var availabilities = await _appointmentService.GetDoctorAvailabilitiesAsync(doctorId, startDate, endDate);
                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctor availabilities");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Set doctor availability (Admin or Doctor)
        /// Doctors can only set availability for themselves
        /// </summary>
        [HttpPost("doctor-availability")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult<DoctorAvailability>> SetDoctorAvailability([FromBody] DoctorAvailabilityRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized("Invalid user token");

                // Get current user's role
                var userClaim = User.FindFirst("roleId")?.Value;
                if (!int.TryParse(userClaim, out int roleId))
                    return Unauthorized("Invalid role");

                // If doctor, ensure they can only set availability for themselves
                if (roleId == 2) // Doctor role
                {
                    if (request.DoctorId != userId.Value)
                    {
                        return Forbid("Doctors can only set availability for themselves");
                    }
                }

                var availability = await _appointmentService.SetDoctorAvailabilityAsync(request);
                return Ok(availability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting doctor availability");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
