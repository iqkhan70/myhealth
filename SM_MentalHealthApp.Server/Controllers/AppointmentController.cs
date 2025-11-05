using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(IAppointmentService appointmentService, ILogger<AppointmentController> logger)
        {
            _appointmentService = appointmentService;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }

        /// <summary>
        /// Validate an appointment before creating it
        /// </summary>
        [HttpPost("validate")]
        [Authorize(Roles = "Admin,Doctor")] // Admin and Doctor can create appointments
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
        /// Create a new appointment (Admin or Doctor)
        /// Doctors can only create appointments for themselves with their assigned patients
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Doctor")]
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
                if (roleId == 2) // Doctor role
                {
                    if (request.DoctorId != userId.Value)
                    {
                        return BadRequest("Doctors can only create appointments for themselves");
                    }

                    // Check if patient is assigned to this doctor
                    var isAssigned = await _appointmentService.IsPatientAssignedToDoctorAsync(request.PatientId, userId.Value);
                    if (!isAssigned)
                    {
                        return BadRequest("Patient must be assigned to you before scheduling an appointment. Please contact admin to assign the patient.");
                    }
                }

                var appointment = await _appointmentService.CreateAppointmentAsync(request, userId.Value);
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
        /// Update an existing appointment (Admin or Doctor)
        /// Doctors can only update their own appointments
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Doctor")]
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
                if (roleId == 2) // Doctor role
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
                if (roleId == 2) // Doctor role
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

                // If doctor, only show their appointments
                if (roleId == 2) // Doctor role
                {
                    doctorId = userId.Value;
                }
                // If patient, only show their appointments
                else if (roleId == 1) // Patient role
                {
                    patientId = userId.Value;
                }
                // Admin can see all

                var appointments = await _appointmentService.GetAppointmentsAsync(doctorId, patientId, startDate, endDate);
                return Ok(appointments);
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
