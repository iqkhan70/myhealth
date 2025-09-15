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

        [HttpPost("user/{userId}")]
        public async Task<ActionResult<JournalEntry>> PostEntry(int userId, [FromBody] JournalEntry entry)
        {
            var savedEntry = await _journalService.ProcessEntry(entry, userId);
            return Ok(savedEntry);
        }

        [HttpPost("doctor/{doctorId}/patient/{patientId}")]
        public async Task<ActionResult<JournalEntry>> PostDoctorEntry(int doctorId, int patientId, [FromBody] JournalEntry entry)
        {
            var savedEntry = await _journalService.ProcessDoctorEntry(entry, patientId, doctorId);
            return Ok(savedEntry);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<JournalEntry>>> GetEntriesForUser(int userId)
        {
            return Ok(await _journalService.GetEntriesForUser(userId));
        }

        [HttpGet("user/{userId}/recent")]
        public async Task<ActionResult<List<JournalEntry>>> GetRecentEntriesForUser(int userId, [FromQuery] int days = 30)
        {
            return Ok(await _journalService.GetRecentEntriesForUser(userId, days));
        }

        [HttpGet("user/{userId}/mood-distribution")]
        public async Task<ActionResult<Dictionary<string, int>>> GetMoodDistributionForUser(int userId, [FromQuery] int days = 30)
        {
            return Ok(await _journalService.GetMoodDistributionForUser(userId, days));
        }

        [HttpGet("user/{userId}/entry/{entryId}")]
        public async Task<ActionResult<JournalEntry>> GetEntryById(int userId, int entryId)
        {
            var entry = await _journalService.GetEntryById(entryId, userId);
            if (entry == null)
            {
                return NotFound();
            }
            return Ok(entry);
        }

        [HttpDelete("user/{userId}/entry/{entryId}")]
        public async Task<ActionResult> DeleteEntry(int userId, int entryId)
        {
            var result = await _journalService.DeleteEntry(entryId, userId);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        // Legacy endpoints removed - use user-specific endpoints instead
    }
}
