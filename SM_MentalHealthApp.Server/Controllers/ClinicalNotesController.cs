using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Controllers;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor,Admin,Attorney")]
    public class ClinicalNotesController : BaseController
    {
        private readonly IClinicalNotesService _clinicalNotesService;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly ILogger<ClinicalNotesController> _logger;
        private readonly JournalDbContext _context;

        public ClinicalNotesController(IClinicalNotesService clinicalNotesService, IContentAnalysisService contentAnalysisService, ILogger<ClinicalNotesController> logger, JournalDbContext context)
        {
            _clinicalNotesService = clinicalNotesService;
            _contentAnalysisService = contentAnalysisService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Get all clinical notes with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ClinicalNoteDto>>> GetClinicalNotes(
            [FromQuery] int? patientId = null,
            [FromQuery] int? doctorId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();
                
                // For doctors and attorneys, filter by assigned patients
                if ((currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney) && currentUserId.HasValue)
                {
                    // Get assigned patient IDs for this doctor/attorney
                    var assignedPatientIds = await _context.UserAssignments
                        .Where(ua => ua.AssignerId == currentUserId.Value && ua.IsActive)
                        .Select(ua => ua.AssigneeId)
                        .ToListAsync();
                    
                    if (!assignedPatientIds.Any())
                    {
                        // No assigned patients, return empty list
                        return Ok(new List<ClinicalNoteDto>());
                    }
                    
                    // Filter notes to only include assigned patients
                    var allNotes = await _clinicalNotesService.GetClinicalNotesAsync(patientId, doctorId);
                    var filteredNotes = allNotes?.Where(n => assignedPatientIds.Contains(n.PatientId)).ToList() ?? new List<ClinicalNoteDto>();
                    return Ok(filteredNotes);
                }
                
                // For admins, return all notes (no filtering)
                var notes = await _clinicalNotesService.GetClinicalNotesAsync(patientId, doctorId);
                // Always return OK with list (empty list if no notes) - never error on empty
                return Ok(notes ?? new List<ClinicalNoteDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical notes - returning empty list");
                // Return empty list instead of error - allows UI to show empty grid
                return Ok(new List<ClinicalNoteDto>());
            }
        }

        /// <summary>
        /// Get a specific clinical note by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ClinicalNoteDto>> GetClinicalNote(int id)
        {
            try
            {
                var note = await _clinicalNotesService.GetClinicalNoteByIdAsync(id);
                if (note == null)
                    return NotFound();

                return Ok(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical note: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a new clinical note
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ClinicalNoteDto>> CreateClinicalNote([FromBody] CreateClinicalNoteRequest request)
        {
            try
            {
                // Attorneys cannot create clinical notes (read-only access)
                var currentRoleId = GetCurrentRoleId();
                if (currentRoleId == Roles.Attorney)
                {
                    return Forbid("Attorneys have read-only access to clinical notes and cannot create them.");
                }

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Get doctor ID from the authenticated user
                var doctorId = GetCurrentUserId();
                if (doctorId == null)
                    return Unauthorized("Doctor ID not found");

                var note = await _clinicalNotesService.CreateClinicalNoteAsync(request, doctorId.Value);
                return CreatedAtAction(nameof(GetClinicalNote), new { id = note.Id }, note);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for creating clinical note");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating clinical note");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update an existing clinical note
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ClinicalNoteDto>> UpdateClinicalNote(int id, [FromBody] UpdateClinicalNoteRequest request)
        {
            try
            {
                // Attorneys cannot edit clinical notes (read-only access)
                var currentRoleId = GetCurrentRoleId();
                if (currentRoleId == Roles.Attorney)
                {
                    return Forbid("Attorneys have read-only access to clinical notes and cannot edit them.");
                }

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Get doctor ID from the authenticated user
                var doctorId = GetCurrentUserId();
                if (doctorId == null)
                    return Unauthorized("Doctor ID not found");

                var note = await _clinicalNotesService.UpdateClinicalNoteAsync(id, request, doctorId.Value);
                if (note == null)
                    return NotFound();

                return Ok(note);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to clinical note: {Id}", id);
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating clinical note: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete a clinical note
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteClinicalNote(int id)
        {
            try
            {
                // Get user ID and role from the authenticated user
                var userId = GetCurrentUserId();
                var userRole = GetCurrentRoleId();
                
                if (userId == null)
                    return Unauthorized("User ID not found");

                _logger.LogInformation("Delete request for clinical note {Id} by user {UserId} with role {RoleId}", id, userId, userRole);

                // Admins can delete any note, doctors can only delete their own
                int? doctorId = null;
                if (userRole == Shared.Constants.Roles.Doctor)
                {
                    doctorId = userId.Value;
                    _logger.LogInformation("User is a doctor, will only allow deletion of own notes. DoctorId: {DoctorId}", doctorId);
                }
                else if (userRole == Shared.Constants.Roles.Admin)
                {
                    _logger.LogInformation("User is an admin, will allow deletion of any note");
                    // doctorId remains null for admins, allowing deletion of any note
                }
                else
                {
                    _logger.LogWarning("User {UserId} has unexpected role {RoleId}, treating as doctor for safety", userId, userRole);
                    // For safety, if role is unexpected, treat as doctor (most restrictive)
                    doctorId = userId.Value;
                }

                var deleted = await _clinicalNotesService.DeleteClinicalNoteAsync(id, doctorId);
                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to clinical note: {Id}. Message: {Message}", id, ex.Message);
                return StatusCode(403, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting clinical note: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search clinical notes
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<ClinicalNoteDto>>> SearchClinicalNotes(
            [FromQuery] string searchTerm,
            [FromQuery] int? patientId = null,
            [FromQuery] int? doctorId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest("Search term is required");

                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();
                
                // For doctors and attorneys, filter by assigned patients
                if ((currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney) && currentUserId.HasValue)
                {
                    // Get assigned patient IDs for this doctor/attorney
                    var assignedPatientIds = await _context.UserAssignments
                        .Where(ua => ua.AssignerId == currentUserId.Value && ua.IsActive)
                        .Select(ua => ua.AssigneeId)
                        .ToListAsync();
                    
                    if (!assignedPatientIds.Any())
                    {
                        // No assigned patients, return empty list
                        return Ok(new List<ClinicalNoteDto>());
                    }
                    
                    // Filter notes to only include assigned patients
                    var allNotes = await _clinicalNotesService.SearchClinicalNotesAsync(searchTerm, patientId, doctorId);
                    var filteredNotes = allNotes?.Where(n => assignedPatientIds.Contains(n.PatientId)).ToList() ?? new List<ClinicalNoteDto>();
                    return Ok(filteredNotes);
                }
                
                // For admins, return all notes (no filtering)
                var notes = await _clinicalNotesService.SearchClinicalNotesAsync(searchTerm, patientId, doctorId);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching clinical notes");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// AI-powered search clinical notes with content analysis integration
        /// </summary>
        [HttpGet("ai-search")]
        public async Task<ActionResult<List<ClinicalNoteDto>>> AISearchClinicalNotes(
            [FromQuery] string searchTerm,
            [FromQuery] int? patientId = null,
            [FromQuery] int? doctorId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest("Search term is required");

                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();
                
                if (currentUserId == null)
                    return Unauthorized("User ID not found");

                // For doctors and attorneys, filter by assigned patients
                List<int>? assignedPatientIds = null;
                if (currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney)
                {
                    // Get assigned patient IDs for this doctor/attorney
                    assignedPatientIds = await _context.UserAssignments
                        .Where(ua => ua.AssignerId == currentUserId.Value && ua.IsActive)
                        .Select(ua => ua.AssigneeId)
                        .ToListAsync();
                    
                    if (!assignedPatientIds.Any())
                    {
                        // No assigned patients, return empty list
                        return Ok(new List<ClinicalNoteDto>());
                    }
                }

                // Use AI-powered search that includes both clinical notes and content analyses
                var notes = await _contentAnalysisService.SearchClinicalNotesWithAIAsync(searchTerm, patientId, doctorId);
                
                // Filter by assigned patients if applicable
                if (assignedPatientIds != null && assignedPatientIds.Any())
                {
                    notes = notes?.Where(n => assignedPatientIds.Contains(n.PatientId)).ToList() ?? new List<ClinicalNoteDto>();
                }
                
                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI search for clinical notes");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get available note types
        /// </summary>
        [HttpGet("note-types")]
        public async Task<ActionResult<List<string>>> GetNoteTypes()
        {
            try
            {
                var noteTypes = await _clinicalNotesService.GetNoteTypesAsync();
                return Ok(noteTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note types");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get available priorities
        /// </summary>
        [HttpGet("priorities")]
        public async Task<ActionResult<List<string>>> GetPriorities()
        {
            try
            {
                var priorities = await _clinicalNotesService.GetPrioritiesAsync();
                return Ok(priorities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting priorities");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Toggle ignore status for a clinical note (Doctor only)
        /// </summary>
        [HttpPost("{id}/toggle-ignore")]
        [Authorize(Roles = "Doctor")]
        public async Task<ActionResult> ToggleIgnoreNote(int id)
        {
            try
            {
                var doctorId = GetCurrentUserId();
                if (!doctorId.HasValue)
                {
                    return Unauthorized("Doctor not authenticated");
                }

                var note = await _clinicalNotesService.GetClinicalNoteByIdAsync(id);
                if (note == null)
                {
                    return NotFound("Clinical note not found");
                }

                // Verify doctor has access to this note (can only ignore own notes or notes for assigned patients)
                // For now, allow doctors to ignore their own notes
                if (note.DoctorId != doctorId.Value)
                {
                    return Forbid("You can only ignore your own clinical notes");
                }

                var isIgnored = await _clinicalNotesService.ToggleIgnoreAsync(id, doctorId.Value);
                
                // Reload note to get updated status
                var updatedNote = await _clinicalNotesService.GetClinicalNoteByIdAsync(id);
                return Ok(new { 
                    message = isIgnored ? "Clinical note ignored" : "Clinical note unignored", 
                    isIgnored = isIgnored 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling ignore status for clinical note {NoteId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
