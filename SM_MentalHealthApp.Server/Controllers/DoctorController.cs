using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly IAuthService _authService;

        public DoctorController(IDoctorService doctorService, IAuthService authService)
        {
            _doctorService = doctorService;
            _authService = authService;
        }

        [HttpGet("my-patients")]
        public async Task<ActionResult<List<User>>> GetMyPatients()
        {
            try
            {
                // Get doctor ID from JWT token claims
                var doctorId = await GetCurrentDoctorIdAsync();
                if (doctorId == null)
                {
                    return Unauthorized("Invalid or missing authentication token");
                }

                var patients = await _doctorService.GetMyPatientsAsync(doctorId.Value);
                return Ok(patients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving patients: {ex.Message}");
            }
        }

        [HttpGet("doctors")]
        public async Task<ActionResult<List<User>>> GetAllDoctors()
        {
            try
            {
                var doctors = await _doctorService.GetAllDoctorsAsync();
                return Ok(doctors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving doctors: {ex.Message}");
            }
        }

        [HttpPost("assign-patient")]
        public async Task<ActionResult> AssignMyPatientToDoctor([FromBody] DoctorAssignPatientRequest request)
        {
            try
            {
                // Get doctor ID from JWT token claims
                var fromDoctorId = await GetCurrentDoctorIdAsync();
                if (fromDoctorId == null)
                {
                    return Unauthorized("Invalid or missing authentication token");
                }

                // Verify that the requesting doctor is actually assigned to this patient
                var isPatientAssignedToMe = await _doctorService.IsPatientAssignedToMeAsync(request.PatientId, fromDoctorId.Value);
                if (!isPatientAssignedToMe)
                {
                    return BadRequest("You can only assign patients that are currently assigned to you");
                }

                var success = await _doctorService.AssignMyPatientToDoctorAsync(request.PatientId, fromDoctorId.Value, request.ToDoctorId);
                if (success)
                {
                    return Ok(new { message = "Patient assigned to doctor successfully" });
                }
                return BadRequest("Failed to assign patient to doctor. Target doctor may not exist, or assignment may already exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error assigning patient to doctor: {ex.Message}");
            }
        }

        [HttpDelete("unassign-patient")]
        public async Task<ActionResult> UnassignMyPatientFromMe([FromBody] DoctorUnassignPatientRequest request)
        {
            try
            {
                // Get doctor ID from JWT token claims
                var doctorId = await GetCurrentDoctorIdAsync();
                if (doctorId == null)
                {
                    return Unauthorized("Invalid or missing authentication token");
                }

                var success = await _doctorService.UnassignMyPatientFromMeAsync(request.PatientId, doctorId.Value);
                if (success)
                {
                    return Ok(new { message = "Patient unassigned from you successfully" });
                }
                return BadRequest("Failed to unassign patient. Patient may not be assigned to you.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error unassigning patient: {ex.Message}");
            }
        }

        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var user = await _authService.GetUserFromTokenAsync(token);

            if (user == null || user.RoleId != 2) // Role 2 = Doctor
            {
                return null;
            }

            return user.Id;
        }
    }

    public class DoctorAssignPatientRequest
    {
        public int PatientId { get; set; }
        public int ToDoctorId { get; set; }
    }

    public class DoctorUnassignPatientRequest
    {
        public int PatientId { get; set; }
    }
}
