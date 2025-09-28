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
