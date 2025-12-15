using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LookupController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<LookupController> _logger;

        public LookupController(JournalDbContext context, ILogger<LookupController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("states")]
        public async Task<ActionResult<List<State>>> GetStates()
        {
            try
            {
                var states = await _context.States
                    .OrderBy(s => s.Name)
                    .ToListAsync();
                return Ok(states);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading states");
                // If table doesn't exist, return empty list instead of error
                if (ex.Message.Contains("doesn't exist") || ex.Message.Contains("Unknown table"))
                {
                    return Ok(new List<State>());
                }
                return StatusCode(500, new { message = "Error loading states", error = ex.Message });
            }
        }

        [HttpGet("accident-participant-roles")]
        public async Task<ActionResult<List<AccidentParticipantRole>>> GetAccidentParticipantRoles()
        {
            try
            {
                var roles = await _context.AccidentParticipantRoles
                    .OrderBy(r => r.Label)
                    .ToListAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading accident participant roles");
                if (ex.Message.Contains("doesn't exist") || ex.Message.Contains("Unknown table"))
                {
                    return Ok(new List<AccidentParticipantRole>());
                }
                return StatusCode(500, new { message = "Error loading accident participant roles", error = ex.Message });
            }
        }

        [HttpGet("vehicle-dispositions")]
        public async Task<ActionResult<List<VehicleDisposition>>> GetVehicleDispositions()
        {
            try
            {
                var dispositions = await _context.VehicleDispositions
                    .OrderBy(d => d.Label)
                    .ToListAsync();
                return Ok(dispositions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vehicle dispositions");
                if (ex.Message.Contains("doesn't exist") || ex.Message.Contains("Unknown table"))
                {
                    return Ok(new List<VehicleDisposition>());
                }
                return StatusCode(500, new { message = "Error loading vehicle dispositions", error = ex.Message });
            }
        }

        [HttpGet("transport-to-care-methods")]
        public async Task<ActionResult<List<TransportToCareMethod>>> GetTransportToCareMethods()
        {
            try
            {
                var methods = await _context.TransportToCareMethods
                    .OrderBy(m => m.Label)
                    .ToListAsync();
                return Ok(methods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading transport to care methods");
                if (ex.Message.Contains("doesn't exist") || ex.Message.Contains("Unknown table"))
                {
                    return Ok(new List<TransportToCareMethod>());
                }
                return StatusCode(500, new { message = "Error loading transport to care methods", error = ex.Message });
            }
        }

        [HttpGet("medical-attention-types")]
        public async Task<ActionResult<List<MedicalAttentionType>>> GetMedicalAttentionTypes()
        {
            try
            {
                var types = await _context.MedicalAttentionTypes
                    .OrderBy(t => t.Label)
                    .ToListAsync();
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading medical attention types");
                if (ex.Message.Contains("doesn't exist") || ex.Message.Contains("Unknown table"))
                {
                    return Ok(new List<MedicalAttentionType>());
                }
                return StatusCode(500, new { message = "Error loading medical attention types", error = ex.Message });
            }
        }

        [HttpGet("symptom-ongoing-statuses")]
        public async Task<ActionResult<List<SymptomOngoingStatus>>> GetSymptomOngoingStatuses()
        {
            try
            {
                var statuses = await _context.SymptomOngoingStatuses
                    .OrderBy(s => s.Label)
                    .ToListAsync();
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading symptom ongoing statuses");
                if (ex.Message.Contains("doesn't exist") || ex.Message.Contains("Unknown table"))
                {
                    return Ok(new List<SymptomOngoingStatus>());
                }
                return StatusCode(500, new { message = "Error loading symptom ongoing statuses", error = ex.Message });
            }
        }
    }
}

