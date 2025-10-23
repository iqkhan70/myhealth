using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor")]
    public class ClinicalNotesController : ControllerBase
    {
        private readonly IClinicalNotesService _clinicalNotesService;
        private readonly IContentAnalysisService _contentAnalysisService;
        private readonly ILogger<ClinicalNotesController> _logger;

        public ClinicalNotesController(IClinicalNotesService clinicalNotesService, IContentAnalysisService contentAnalysisService, ILogger<ClinicalNotesController> logger)
        {
            _clinicalNotesService = clinicalNotesService;
            _contentAnalysisService = contentAnalysisService;
            _logger = logger;
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
                var notes = await _clinicalNotesService.GetClinicalNotesAsync(patientId, doctorId);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical notes");
                return StatusCode(500, "Internal server error");
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
                // Get doctor ID from the authenticated user
                var doctorId = GetCurrentUserId();
                if (doctorId == null)
                    return Unauthorized("Doctor ID not found");

                var deleted = await _clinicalNotesService.DeleteClinicalNoteAsync(id, doctorId.Value);
                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to clinical note: {Id}", id);
                return Forbid(ex.Message);
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

                // Get doctor ID from the authenticated user
                var currentDoctorId = GetCurrentUserId();
                if (currentDoctorId == null)
                    return Unauthorized("Doctor ID not found");

                // Use AI-powered search that includes both clinical notes and content analyses
                var notes = await _contentAnalysisService.SearchClinicalNotesWithAIAsync(searchTerm, patientId, doctorId);
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
        /// Get current user ID from JWT token
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;
            return null;
        }
    }
}
