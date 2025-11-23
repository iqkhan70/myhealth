using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Controllers;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class JournalController : BaseController
    {
        private readonly JournalService _journalService;
        private readonly ILogger<JournalController> _logger;

        public JournalController(JournalService journalService, ILogger<JournalController> logger)
        {
            _journalService = journalService;
            _logger = logger;
        }

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
            try
            {
                var entries = await _journalService.GetEntriesForUser(userId);
                // Always return OK with list (empty list if no entries) - never error on empty
                return Ok(entries ?? new List<JournalEntry>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting journal entries for user {UserId}", userId);
                // Return empty list instead of error - allows UI to show empty grid
                return Ok(new List<JournalEntry>());
            }
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

        /// <summary>
        /// Toggle ignore status for a journal entry (doctors only)
        /// </summary>
        [HttpPost("entry/{entryId}/toggle-ignore")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult> ToggleIgnoreEntry(int entryId)
        {
            try
            {
                var doctorId = GetCurrentUserId();
                if (!doctorId.HasValue)
                {
                    return Unauthorized("Doctor not authenticated");
                }

                var entry = await _journalService.GetEntryById(entryId, null);
                if (entry == null)
                {
                    return NotFound("Journal entry not found");
                }

                // Verify doctor has access to this patient's journal entry
                var hasAccess = await _journalService.VerifyDoctorAccessAsync(entry.UserId, doctorId.Value);
                if (!hasAccess)
                {
                    return StatusCode(403, "You can only ignore journal entries for your assigned patients");
                }

                await _journalService.ToggleIgnoreAsync(entryId, doctorId.Value);
                
                // Reload entry to get updated status
                var updatedEntry = await _journalService.GetEntryById(entryId, null);
                return Ok(new { message = updatedEntry?.IsIgnoredByDoctor == true ? "Journal entry ignored" : "Journal entry unignored", isIgnored = updatedEntry?.IsIgnoredByDoctor ?? false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling ignore status for journal entry {EntryId}", entryId);
                return StatusCode(500, "Internal server error");
            }
        }

        // Legacy endpoints removed - use user-specific endpoints instead
    }
}
