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
        private readonly IServiceRequestService _serviceRequestService;
        private readonly ILogger<ClinicalNotesController> _logger;
        private readonly JournalDbContext _context;

        public ClinicalNotesController(IClinicalNotesService clinicalNotesService, IContentAnalysisService contentAnalysisService, IServiceRequestService serviceRequestService, ILogger<ClinicalNotesController> logger, JournalDbContext context)
        {
            _clinicalNotesService = clinicalNotesService;
            _contentAnalysisService = contentAnalysisService;
            _serviceRequestService = serviceRequestService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Get all clinical notes with optional filtering (non-paginated - for backward compatibility)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ClinicalNoteDto>>> GetClinicalNotes(
            [FromQuery] int? patientId = null,
            [FromQuery] int? doctorId = null,
            [FromQuery] int? serviceRequestId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                // For doctors and attorneys, filter by assigned ServiceRequests
                if ((currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney) && currentUserId.HasValue)
                {
                    // Get assigned ServiceRequest IDs for this SME
                    var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value);

                    if (!serviceRequestIds.Any())
                    {
                        // No assigned service requests, return empty list
                        return Ok(new List<ClinicalNoteDto>());
                    }

                    // If specific SR requested, verify access
                    if (serviceRequestId.HasValue)
                    {
                        if (!serviceRequestIds.Contains(serviceRequestId.Value))
                            return Forbid("You are not assigned to this service request");
                        
                        serviceRequestIds = new List<int> { serviceRequestId.Value };
                    }

                    // Filter notes by ServiceRequestId
                    var query = _context.ClinicalNotes
                        .Include(cn => cn.Patient)
                        .Include(cn => cn.Doctor)
                        .Where(cn => cn.IsActive && 
                            cn.ServiceRequestId.HasValue && 
                            serviceRequestIds.Contains(cn.ServiceRequestId.Value));

                    if (patientId.HasValue)
                        query = query.Where(cn => cn.PatientId == patientId.Value);

                    if (doctorId.HasValue)
                        query = query.Where(cn => cn.DoctorId == doctorId.Value);

                    var notes = await query
                        .OrderByDescending(cn => cn.CreatedAt)
                        .ToListAsync();

                    return Ok(notes.Select(MapToDto).ToList());
                }

                // For admins, return all notes (or filter by serviceRequestId if provided)
                var allNotes = await _clinicalNotesService.GetClinicalNotesAsync(patientId, doctorId);
                
                if (serviceRequestId.HasValue)
                    allNotes = allNotes.Where(n => n.ServiceRequestId == serviceRequestId.Value).ToList();
                
                // Always return OK with list (empty list if no notes) - never error on empty
                return Ok(allNotes ?? new List<ClinicalNoteDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical notes - returning empty list");
                // Return empty list instead of error - allows UI to show empty grid
                return Ok(new List<ClinicalNoteDto>());
            }
        }

        private ClinicalNoteDto MapToDto(ClinicalNote cn)
        {
            return new ClinicalNoteDto
            {
                Id = cn.Id,
                PatientId = cn.PatientId,
                DoctorId = cn.DoctorId,
                Title = cn.Title,
                Content = cn.Content,
                NoteType = cn.NoteType,
                Priority = cn.Priority,
                IsConfidential = cn.IsConfidential,
                CreatedAt = cn.CreatedAt,
                UpdatedAt = cn.UpdatedAt,
                Tags = cn.Tags,
                PatientName = $"{cn.Patient.FirstName} {cn.Patient.LastName}",
                DoctorName = $"{cn.Doctor.FirstName} {cn.Doctor.LastName}",
                IsIgnoredByDoctor = cn.IsIgnoredByDoctor,
                IgnoredByDoctorId = cn.IgnoredByDoctorId,
                IgnoredAt = cn.IgnoredAt,
                ServiceRequestId = cn.ServiceRequestId
            };
        }

        /// <summary>
        /// Get paginated clinical notes with optional filtering
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ClinicalNoteDto>>> GetClinicalNotesPaged(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 10,
            [FromQuery] int? patientId = null,
            [FromQuery] int? doctorId = null,
            [FromQuery] int? serviceRequestId = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? noteType = null,
            [FromQuery] string? priority = null,
            [FromQuery] bool? isIgnoredByDoctor = null,
            [FromQuery] DateTime? createdDateFrom = null,
            [FromQuery] DateTime? createdDateTo = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                // For doctors and attorneys, filter by assigned ServiceRequests
                if ((currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney) && currentUserId.HasValue)
                {
                    // Get assigned ServiceRequest IDs for this SME
                    var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value);

                    if (!serviceRequestIds.Any())
                    {
                        // No assigned service requests, return empty result
                        return Ok(new PagedResult<ClinicalNoteDto>
                        {
                            Items = new List<ClinicalNoteDto>(),
                            TotalCount = 0,
                            PageNumber = 1,
                            PageSize = take
                        });
                    }

                    // If specific SR requested, verify access
                    if (serviceRequestId.HasValue)
                    {
                        if (!serviceRequestIds.Contains(serviceRequestId.Value))
                            return Forbid("You are not assigned to this service request");
                        
                        serviceRequestIds = new List<int> { serviceRequestId.Value };
                    }

                    // Build query filtered by ServiceRequestId
                    var query = _context.ClinicalNotes
                        .Include(cn => cn.Patient)
                        .Include(cn => cn.Doctor)
                        .Where(cn => cn.IsActive && 
                            cn.ServiceRequestId.HasValue && 
                            serviceRequestIds.Contains(cn.ServiceRequestId.Value));

                    if (patientId.HasValue)
                        query = query.Where(cn => cn.PatientId == patientId.Value);
                    if (doctorId.HasValue)
                        query = query.Where(cn => cn.DoctorId == doctorId.Value);

                    // Apply search filters
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        var searchLower = searchTerm.ToLower();
                        query = query.Where(cn =>
                            cn.Title.ToLower().Contains(searchLower) ||
                            cn.Content.ToLower().Contains(searchLower) ||
                            (cn.Tags != null && cn.Tags.ToLower().Contains(searchLower)) ||
                            (cn.Patient.FirstName + " " + cn.Patient.LastName).ToLower().Contains(searchLower) ||
                            (cn.Doctor.FirstName + " " + cn.Doctor.LastName).ToLower().Contains(searchLower));
                    }
                    if (!string.IsNullOrWhiteSpace(noteType))
                        query = query.Where(cn => cn.NoteType == noteType);
                    if (!string.IsNullOrWhiteSpace(priority))
                        query = query.Where(cn => cn.Priority == priority);
                    if (isIgnoredByDoctor.HasValue)
                        query = query.Where(cn => cn.IsIgnoredByDoctor == isIgnoredByDoctor.Value);
                    if (createdDateFrom.HasValue)
                        query = query.Where(cn => cn.CreatedAt >= createdDateFrom.Value);
                    if (createdDateTo.HasValue)
                        query = query.Where(cn => cn.CreatedAt <= createdDateTo.Value);

                    // Get total count
                    var totalCount = await query.CountAsync();

                    // Apply pagination
                    var notes = await query
                        .OrderByDescending(cn => cn.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .ToListAsync();

                    var items = notes.Select(MapToDto).ToList();

                    return Ok(new PagedResult<ClinicalNoteDto>
                    {
                        Items = items,
                        TotalCount = totalCount,
                        PageNumber = (skip / take) + 1,
                        PageSize = take
                    });
                }

                // For admins, return all notes (or filter by serviceRequestId if provided)
                var pagedNotes = await _clinicalNotesService.GetClinicalNotesPagedAsync(skip, take, patientId, doctorId, searchTerm, noteType, priority, isIgnoredByDoctor, createdDateFrom, createdDateTo);
                
                if (serviceRequestId.HasValue)
                {
                    // Filter by ServiceRequestId in the database query
                    var filteredQuery = _context.ClinicalNotes
                        .Where(cn => cn.IsActive && cn.ServiceRequestId == serviceRequestId.Value);
                    
                    // Apply other filters
                    if (patientId.HasValue) filteredQuery = filteredQuery.Where(cn => cn.PatientId == patientId.Value);
                    if (doctorId.HasValue) filteredQuery = filteredQuery.Where(cn => cn.DoctorId == doctorId.Value);
                    
                    var filteredCount = await filteredQuery.CountAsync();
                    pagedNotes.Items = pagedNotes.Items?.Where(n => n.ServiceRequestId == serviceRequestId.Value).ToList() ?? new List<ClinicalNoteDto>();
                    pagedNotes.TotalCount = filteredCount;
                }
                
                return Ok(pagedNotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated clinical notes - returning empty result");
                return Ok(new PagedResult<ClinicalNoteDto>
                {
                    Items = new List<ClinicalNoteDto>(),
                    TotalCount = 0,
                    PageNumber = 1,
                    PageSize = take
                });
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

                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                // For doctors and attorneys, verify access via ServiceRequest
                if ((currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney) && currentUserId.HasValue)
                {
                    // Get the actual note from DB to check ServiceRequestId
                    var noteEntity = await _context.ClinicalNotes
                        .FirstOrDefaultAsync(cn => cn.Id == id && cn.IsActive);

                    if (noteEntity == null)
                        return NotFound();

                    if (noteEntity.ServiceRequestId.HasValue)
                    {
                        var hasAccess = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(
                            noteEntity.ServiceRequestId.Value, currentUserId.Value);
                        
                        if (!hasAccess)
                            return Forbid("You are not assigned to this service request");
                    }
                }

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

                // Get or set ServiceRequestId - use default if not provided
                int? serviceRequestId = null;
                if (currentRoleId == Roles.Doctor || currentRoleId == Roles.Attorney)
                {
                    // Get default ServiceRequest for this patient
                    var defaultSr = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(request.PatientId);
                    if (defaultSr != null)
                    {
                        // Verify doctor is assigned to this SR
                        var isAssigned = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(defaultSr.Id, doctorId.Value);
                        if (isAssigned)
                        {
                            serviceRequestId = defaultSr.Id;
                        }
                    }
                }

                var note = await _clinicalNotesService.CreateClinicalNoteAsync(request, doctorId.Value, serviceRequestId);
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

                // For doctors and attorneys, filter by assigned ServiceRequests
                if ((currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney) && currentUserId.HasValue)
                {
                    // Get assigned ServiceRequest IDs for this SME
                    var serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value);

                    if (!serviceRequestIds.Any())
                    {
                        // No assigned service requests, return empty list
                        return Ok(new List<ClinicalNoteDto>());
                    }

                    // Filter notes by ServiceRequestId
                    var query = _context.ClinicalNotes
                        .Include(cn => cn.Patient)
                        .Include(cn => cn.Doctor)
                        .Where(cn => cn.IsActive && 
                            cn.ServiceRequestId.HasValue && 
                            serviceRequestIds.Contains(cn.ServiceRequestId.Value));

                    if (patientId.HasValue)
                        query = query.Where(cn => cn.PatientId == patientId.Value);
                    if (doctorId.HasValue)
                        query = query.Where(cn => cn.DoctorId == doctorId.Value);

                    // Apply search filter
                    var searchLower = searchTerm.ToLower();
                    query = query.Where(cn =>
                        cn.Title.ToLower().Contains(searchLower) ||
                        cn.Content.ToLower().Contains(searchLower) ||
                        (cn.Tags != null && cn.Tags.ToLower().Contains(searchLower)));

                    var notes = await query
                        .OrderByDescending(cn => cn.CreatedAt)
                        .ToListAsync();

                    return Ok(notes.Select(MapToDto).ToList());
                }

                // For admins, return all notes (no filtering)
                var allNotes = await _clinicalNotesService.SearchClinicalNotesAsync(searchTerm, patientId, doctorId);
                return Ok(allNotes);
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

                // For doctors and attorneys, filter by assigned ServiceRequests
                List<int>? serviceRequestIds = null;
                if (currentRoleId == Shared.Constants.Roles.Doctor || currentRoleId == Shared.Constants.Roles.Attorney)
                {
                    // Get assigned ServiceRequest IDs for this SME
                    serviceRequestIds = await _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value);

                    if (!serviceRequestIds.Any())
                    {
                        // No assigned service requests, return empty list
                        return Ok(new List<ClinicalNoteDto>());
                    }
                }

                // Use AI-powered search that includes both clinical notes and content analyses
                var notes = await _contentAnalysisService.SearchClinicalNotesWithAIAsync(searchTerm, patientId, doctorId);

                // Filter by ServiceRequestId if applicable
                if (serviceRequestIds != null && serviceRequestIds.Any())
                {
                    // Get notes with ServiceRequestId from the database
                    var notesWithSr = await _context.ClinicalNotes
                        .Where(cn => cn.IsActive && 
                            cn.ServiceRequestId.HasValue && 
                            serviceRequestIds.Contains(cn.ServiceRequestId.Value))
                        .Select(cn => cn.Id)
                        .ToListAsync();

                    notes = notes?.Where(n => notesWithSr.Contains(n.Id)).ToList() ?? new List<ClinicalNoteDto>();
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
                return Ok(new
                {
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
