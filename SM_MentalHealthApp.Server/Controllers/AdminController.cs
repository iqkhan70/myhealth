using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
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
}
