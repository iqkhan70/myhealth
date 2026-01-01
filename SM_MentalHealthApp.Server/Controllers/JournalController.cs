using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Controllers;
using SM_MentalHealthApp.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class JournalController : BaseController
    {
        private readonly JournalService _journalService;
        private readonly IServiceRequestService _serviceRequestService;
        private readonly ILogger<JournalController> _logger;
        private readonly JournalDbContext _context;

        public JournalController(JournalService journalService, IServiceRequestService serviceRequestService, ILogger<JournalController> logger, JournalDbContext context)
        {
            _journalService = journalService;
            _serviceRequestService = serviceRequestService;
            _logger = logger;
            _context = context;
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
            // Get or set ServiceRequestId - use default if not provided
            int? serviceRequestId = entry.ServiceRequestId;
            if (!serviceRequestId.HasValue)
            {
                // Get default ServiceRequest for this patient
                var defaultSr = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(patientId);
                if (defaultSr != null)
                {
                    // Verify doctor is assigned to this SR
                    var isAssigned = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(defaultSr.Id, doctorId);
                    if (isAssigned)
                    {
                        serviceRequestId = defaultSr.Id;
                        entry.ServiceRequestId = serviceRequestId;
                    }
                }
            }

            var savedEntry = await _journalService.ProcessDoctorEntry(entry, patientId, doctorId);
            return Ok(savedEntry);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<JournalEntry>>> GetEntriesForUser(int userId, [FromQuery] int? serviceRequestId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                // For doctors and attorneys viewing patient journals, filter by ServiceRequestId
                if ((currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney || currentRoleId == Shared.Constants.Roles.Sme) && 
                    currentUserId.HasValue && userId != currentUserId.Value)
                {
                    // Get assigned ServiceRequest IDs for this SME
                    var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value);

                    if (!serviceRequestIds.Any())
                    {
                        return Ok(new List<JournalEntry>());
                    }

                    // If specific SR requested, verify access
                    if (serviceRequestId.HasValue)
                    {
                        if (!serviceRequestIds.Contains(serviceRequestId.Value))
                            return Forbid("You are not assigned to this service request");
                        
                        serviceRequestIds = new List<int> { serviceRequestId.Value };
                    }

                    // Filter journal entries by ServiceRequestId
                    var entries = await _context.JournalEntries
                        .Where(je => je.IsActive && 
                            je.UserId == userId &&
                            je.ServiceRequestId.HasValue &&
                            serviceRequestIds.Contains(je.ServiceRequestId.Value))
                        .OrderByDescending(je => je.CreatedAt)
                        .ToListAsync();

                    return Ok(entries);
                }

                // For patients viewing their own journals, or admins, use existing service
                var allEntries = await _journalService.GetEntriesForUser(userId);
                
                if (serviceRequestId.HasValue)
                    allEntries = allEntries?.Where(e => e.ServiceRequestId == serviceRequestId.Value).ToList() ?? new List<JournalEntry>();
                
                // Always return OK with list (empty list if no entries) - never error on empty
                return Ok(allEntries ?? new List<JournalEntry>());
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

                // Verify doctor has access to this journal entry via ServiceRequest
                bool hasAccess = false;
                if (entry.ServiceRequestId.HasValue)
                {
                    hasAccess = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(
                        entry.ServiceRequestId.Value, doctorId.Value);
                }
                else
                {
                    // Fallback to old access check for entries without ServiceRequestId
                    hasAccess = await _journalService.VerifyDoctorAccessAsync(entry.UserId, doctorId.Value);
                }
                
                if (!hasAccess)
                {
                    return StatusCode(403, "You can only ignore journal entries for your assigned service requests");
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

        /// <summary>
        /// Get journal entry counts for multiple users (batch endpoint to avoid N+1 requests)
        /// </summary>
        [HttpPost("counts/batch")]
        public async Task<ActionResult<Dictionary<int, int>>> GetJournalEntryCountsBatch([FromBody] List<int> userIds)
        {
            try
            {
                if (userIds == null || !userIds.Any())
                {
                    return Ok(new Dictionary<int, int>());
                }

                var counts = await _journalService.GetJournalEntryCountsBatchAsync(userIds);
                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting journal entry counts batch");
                return StatusCode(500, "Internal server error");
            }
        }

        // Legacy endpoints removed - use user-specific endpoints instead
    }
}
