using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JournalController : ControllerBase
    {
        private readonly JournalService _journalService;
        public JournalController(JournalService journalService) => _journalService = journalService;

        [HttpPost("patient/{patientId}")]
        public async Task<ActionResult<JournalEntry>> PostEntry(int patientId, [FromBody] JournalEntry entry)
        {
            var savedEntry = await _journalService.ProcessEntry(entry, patientId);
            return Ok(savedEntry);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<JournalEntry>>> GetEntriesForPatient(int patientId)
        {
            return Ok(await _journalService.GetEntriesForPatient(patientId));
        }

        [HttpGet("patient/{patientId}/recent")]
        public async Task<ActionResult<List<JournalEntry>>> GetRecentEntriesForPatient(int patientId, [FromQuery] int days = 30)
        {
            return Ok(await _journalService.GetRecentEntriesForPatient(patientId, days));
        }

        [HttpGet("patient/{patientId}/mood-distribution")]
        public async Task<ActionResult<Dictionary<string, int>>> GetMoodDistributionForPatient(int patientId, [FromQuery] int days = 30)
        {
            return Ok(await _journalService.GetMoodDistributionForPatient(patientId, days));
        }

        [HttpGet("patient/{patientId}/entry/{entryId}")]
        public async Task<ActionResult<JournalEntry>> GetEntryById(int patientId, int entryId)
        {
            var entry = await _journalService.GetEntryById(entryId, patientId);
            if (entry == null)
            {
                return NotFound();
            }
            return Ok(entry);
        }

        [HttpDelete("patient/{patientId}/entry/{entryId}")]
        public async Task<ActionResult> DeleteEntry(int patientId, int entryId)
        {
            var result = await _journalService.DeleteEntry(entryId, patientId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        // Legacy endpoints for backward compatibility
        [HttpPost]
        public async Task<ActionResult<JournalEntry>> PostEntry([FromBody] JournalEntry entry)
        {
            // For backward compatibility, use demo patient
            var savedEntry = await _journalService.ProcessEntry(entry, 1);
            return Ok(savedEntry);
        }

        [HttpGet]
        public async Task<ActionResult<List<JournalEntry>>> GetEntries()
        {
            return Ok(await _journalService.GetEntries());
        }
    }
}
