using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly PatientService _patientService;

        public PatientController(PatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient([FromBody] Patient patient)
        {
            try
            {
                var createdPatient = await _patientService.CreatePatientAsync(patient);
                return Ok(createdPatient);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null)
            {
                return NotFound();
            }
            return Ok(patient);
        }

        [HttpGet("email/{email}")]
        public async Task<ActionResult<Patient>> GetPatientByEmail(string email)
        {
            var patient = await _patientService.GetPatientByEmailAsync(email);
            if (patient == null)
            {
                return NotFound();
            }
            return Ok(patient);
        }

        [HttpGet]
        public async Task<ActionResult<List<Patient>>> GetAllPatients()
        {
            var patients = await _patientService.GetAllPatientsAsync();
            return Ok(patients);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Patient>> UpdatePatient(int id, [FromBody] Patient patient)
        {
            if (id != patient.Id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                var updatedPatient = await _patientService.UpdatePatientAsync(patient);
                return Ok(updatedPatient);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeactivatePatient(int id)
        {
            var result = await _patientService.DeactivatePatientAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpGet("demo")]
        public async Task<ActionResult<Patient>> GetDemoPatient()
        {
            var demoPatient = await _patientService.GetOrCreateDemoPatientAsync();
            return Ok(demoPatient);
        }

        [HttpGet("{id}/stats")]
        public async Task<ActionResult<PatientStats>> GetPatientStats(int id)
        {
            try
            {
                var stats = await _patientService.GetPatientStatsAsync(id);
                return Ok(stats);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
