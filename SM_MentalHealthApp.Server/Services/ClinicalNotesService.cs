using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IClinicalNotesService
    {
        Task<List<ClinicalNoteDto>> GetClinicalNotesAsync(int? patientId = null, int? doctorId = null);
        Task<PagedResult<ClinicalNoteDto>> GetClinicalNotesPagedAsync(int skip, int take, int? patientId = null, int? doctorId = null, string? searchTerm = null, string? noteType = null, string? priority = null, bool? isIgnoredByDoctor = null, DateTime? createdDateFrom = null, DateTime? createdDateTo = null);
        Task<ClinicalNoteDto?> GetClinicalNoteByIdAsync(int id);
        Task<ClinicalNoteDto> CreateClinicalNoteAsync(CreateClinicalNoteRequest request, int doctorId, int? serviceRequestId = null);
        Task<ClinicalNoteDto?> UpdateClinicalNoteAsync(int id, UpdateClinicalNoteRequest request, int doctorId);
        Task<bool> DeleteClinicalNoteAsync(int id, int? doctorId);
        Task<bool> ToggleIgnoreAsync(int noteId, int doctorId);
        Task<List<ClinicalNoteDto>> SearchClinicalNotesAsync(string searchTerm, int? patientId = null, int? doctorId = null);
        Task<List<string>> GetNoteTypesAsync();
        Task<List<string>> GetPrioritiesAsync();
    }

    public class ClinicalNotesService : IClinicalNotesService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ClinicalNotesService> _logger;

        public ClinicalNotesService(JournalDbContext context, ILogger<ClinicalNotesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ClinicalNoteDto>> GetClinicalNotesAsync(int? patientId = null, int? doctorId = null)
        {
            try
            {
                var query = _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.Doctor)
                    .Where(cn => cn.IsActive);

                if (patientId.HasValue)
                    query = query.Where(cn => cn.PatientId == patientId.Value);

                if (doctorId.HasValue)
                    query = query.Where(cn => cn.DoctorId == doctorId.Value);

                var notes = await query
                    .OrderByDescending(cn => cn.CreatedAt)
                    .ToListAsync();

                return notes?.Select(MapToDto).ToList() ?? new List<ClinicalNoteDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical notes - returning empty list");
                // Return empty list instead of throwing - allows UI to show empty grid
                return new List<ClinicalNoteDto>();
            }
        }

        public async Task<PagedResult<ClinicalNoteDto>> GetClinicalNotesPagedAsync(
            int skip,
            int take,
            int? patientId = null,
            int? doctorId = null,
            string? searchTerm = null,
            string? noteType = null,
            string? priority = null,
            bool? isIgnoredByDoctor = null,
            DateTime? createdDateFrom = null,
            DateTime? createdDateTo = null)
        {
            try
            {
                var query = _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.Doctor)
                    .Where(cn => cn.IsActive);

                if (patientId.HasValue)
                    query = query.Where(cn => cn.PatientId == patientId.Value);

                if (doctorId.HasValue)
                    query = query.Where(cn => cn.DoctorId == doctorId.Value);

                // Apply text search filter
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

                // Apply note type filter
                if (!string.IsNullOrWhiteSpace(noteType))
                {
                    query = query.Where(cn => cn.NoteType == noteType);
                }

                // Apply priority filter
                if (!string.IsNullOrWhiteSpace(priority))
                {
                    query = query.Where(cn => cn.Priority == priority);
                }

                // Apply AI status filter (IsIgnoredByDoctor)
                if (isIgnoredByDoctor.HasValue)
                {
                    query = query.Where(cn => cn.IsIgnoredByDoctor == isIgnoredByDoctor.Value);
                }

                // Apply date filter (CreatedAt)
                if (createdDateFrom.HasValue)
                {
                    query = query.Where(cn => cn.CreatedAt >= createdDateFrom.Value);
                }
                if (createdDateTo.HasValue)
                {
                    // createdDateTo is already end of day, so use <=
                    query = query.Where(cn => cn.CreatedAt <= createdDateTo.Value);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination and ordering
                var notes = await query
                    .OrderByDescending(cn => cn.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();

                var items = notes?.Select(MapToDto).ToList() ?? new List<ClinicalNoteDto>();

                return new PagedResult<ClinicalNoteDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = (skip / take) + 1,
                    PageSize = take
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated clinical notes - returning empty result");
                return new PagedResult<ClinicalNoteDto>
                {
                    Items = new List<ClinicalNoteDto>(),
                    TotalCount = 0,
                    PageNumber = 1,
                    PageSize = take
                };
            }
        }

        public async Task<ClinicalNoteDto?> GetClinicalNoteByIdAsync(int id)
        {
            try
            {
                var note = await _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.Doctor)
                    .FirstOrDefaultAsync(cn => cn.Id == id && cn.IsActive);

                return note != null ? MapToDto(note) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical note by ID: {Id}", id);
                throw;
            }
        }

        public async Task<ClinicalNoteDto> CreateClinicalNoteAsync(CreateClinicalNoteRequest request, int doctorId, int? serviceRequestId = null)
        {
            try
            {
                // Validate patient ID is provided
                if (request.PatientId <= 0)
                    throw new ArgumentException("Patient must be selected. Please select a patient before saving the clinical note.");

                // Verify patient exists and is active
                var patient = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.PatientId && u.RoleId == 1 && u.IsActive); // Role 1 = Patient, must be active

                if (patient == null)
                    throw new ArgumentException("Patient not found or inactive. Please select a valid active patient.");

                // Verify doctor exists
                var doctor = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == doctorId && u.RoleId == 2); // Role 2 = Doctor

                if (doctor == null)
                    throw new ArgumentException("Doctor not found");

                var clinicalNote = new ClinicalNote
                {
                    PatientId = request.PatientId,
                    DoctorId = doctorId,
                    ServiceRequestId = serviceRequestId,
                    Title = request.Title,
                    Content = request.Content,
                    NoteType = request.NoteType,
                    Priority = request.Priority,
                    IsConfidential = request.IsConfidential,
                    Tags = request.Tags,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ClinicalNotes.Add(clinicalNote);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Clinical note created with ID: {Id} for patient {PatientId} by doctor {DoctorId}",
                    clinicalNote.Id, clinicalNote.PatientId, clinicalNote.DoctorId);

                // Load the note with navigation properties for the response
                var createdNote = await _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.Doctor)
                    .FirstAsync(cn => cn.Id == clinicalNote.Id);

                return MapToDto(createdNote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating clinical note");
                throw;
            }
        }

        public async Task<ClinicalNoteDto?> UpdateClinicalNoteAsync(int id, UpdateClinicalNoteRequest request, int doctorId)
        {
            try
            {
                var note = await _context.ClinicalNotes
                    .FirstOrDefaultAsync(cn => cn.Id == id && cn.IsActive);

                if (note == null)
                    return null;

                // Verify the doctor owns this note
                if (note.DoctorId != doctorId)
                    throw new UnauthorizedAccessException("You can only update your own clinical notes");

                note.Title = request.Title;
                note.Content = request.Content;
                note.NoteType = request.NoteType;
                note.Priority = request.Priority;
                note.IsConfidential = request.IsConfidential;
                note.Tags = request.Tags;
                note.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Clinical note updated with ID: {Id}", id);

                // Load the updated note with navigation properties
                var updatedNote = await _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.Doctor)
                    .FirstAsync(cn => cn.Id == id);

                return MapToDto(updatedNote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating clinical note: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteClinicalNoteAsync(int id, int? doctorId)
        {
            try
            {
                var note = await _context.ClinicalNotes
                    .FirstOrDefaultAsync(cn => cn.Id == id && cn.IsActive);

                if (note == null)
                {
                    _logger.LogWarning("Clinical note {Id} not found or already inactive", id);
                    return false;
                }

                _logger.LogInformation("Attempting to delete clinical note {Id}. Note DoctorId: {NoteDoctorId}, Requesting DoctorId: {RequestingDoctorId}",
                    id, note.DoctorId, doctorId);

                // If doctorId is provided (not null), verify the doctor owns this note
                // If doctorId is null (admin), allow deletion of any note
                if (doctorId.HasValue)
                {
                    if (note.DoctorId != doctorId.Value)
                    {
                        _logger.LogWarning("Doctor {DoctorId} attempted to delete note {Id} owned by doctor {NoteDoctorId}",
                            doctorId.Value, id, note.DoctorId);
                        throw new UnauthorizedAccessException("You can only delete your own clinical notes");
                    }
                    _logger.LogInformation("Doctor {DoctorId} is authorized to delete their own note {Id}", doctorId.Value, id);
                }
                else
                {
                    _logger.LogInformation("Admin user deleting note {Id} (no doctorId restriction)", id);
                }

                note.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Clinical note deleted with ID: {Id} by user {UserId}", id, doctorId);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // Re-throw authorization exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting clinical note: {Id}", id);
                throw;
            }
        }

        public async Task<List<ClinicalNoteDto>> SearchClinicalNotesAsync(string searchTerm, int? patientId = null, int? doctorId = null)
        {
            try
            {
                var query = _context.ClinicalNotes
                    .Include(cn => cn.Patient)
                    .Include(cn => cn.Doctor)
                    .Where(cn => cn.IsActive);

                if (patientId.HasValue)
                    query = query.Where(cn => cn.PatientId == patientId.Value);

                if (doctorId.HasValue)
                    query = query.Where(cn => cn.DoctorId == doctorId.Value);

                // Search in title, content, and tags
                var searchLower = searchTerm.ToLower();
                query = query.Where(cn =>
                    cn.Title.ToLower().Contains(searchLower) ||
                    cn.Content.ToLower().Contains(searchLower) ||
                    (cn.Tags != null && cn.Tags.ToLower().Contains(searchLower)));

                var notes = await query
                    .OrderByDescending(cn => cn.CreatedAt)
                    .ToListAsync();

                return notes.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching clinical notes");
                throw;
            }
        }

        public async Task<List<string>> GetNoteTypesAsync()
        {
            try
            {
                var noteTypes = await _context.ClinicalNotes
                    .Where(cn => cn.IsActive)
                    .Select(cn => cn.NoteType)
                    .Distinct()
                    .OrderBy(nt => nt)
                    .ToListAsync();

                // Add default note types if none exist
                var defaultTypes = new List<string> { "General", "Assessment", "Treatment", "Follow-up", "Emergency" };
                return noteTypes.Union(defaultTypes).Distinct().OrderBy(nt => nt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note types");
                return new List<string> { "General", "Assessment", "Treatment", "Follow-up", "Emergency" };
            }
        }

        public async Task<List<string>> GetPrioritiesAsync()
        {
            try
            {
                var priorities = await _context.ClinicalNotes
                    .Where(cn => cn.IsActive)
                    .Select(cn => cn.Priority)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                // Add default priorities if none exist
                var defaultPriorities = new List<string> { "Low", "Normal", "High", "Critical" };
                return priorities.Union(defaultPriorities).Distinct().OrderBy(p => p).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting priorities");
                return new List<string> { "Low", "Normal", "High", "Critical" };
            }
        }

        public async Task<bool> ToggleIgnoreAsync(int noteId, int doctorId)
        {
            try
            {
                var note = await _context.ClinicalNotes.FindAsync(noteId);
                if (note == null)
                {
                    throw new ArgumentException("Clinical note not found");
                }

                // Toggle ignore status
                if (note.IsIgnoredByDoctor)
                {
                    // Unignore
                    note.IsIgnoredByDoctor = false;
                    note.IgnoredByDoctorId = null;
                    note.IgnoredAt = null;
                    _logger.LogInformation("Clinical note {NoteId} unignored by doctor {DoctorId}", noteId, doctorId);
                }
                else
                {
                    // Ignore
                    note.IsIgnoredByDoctor = true;
                    note.IgnoredByDoctorId = doctorId;
                    note.IgnoredAt = DateTime.UtcNow;
                    _logger.LogInformation("Clinical note {NoteId} ignored by doctor {DoctorId}", noteId, doctorId);
                }

                await _context.SaveChangesAsync();
                return note.IsIgnoredByDoctor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling ignore status for clinical note {NoteId}", noteId);
                throw;
            }
        }

        private static ClinicalNoteDto MapToDto(ClinicalNote note)
        {
            return new ClinicalNoteDto
            {
                Id = note.Id,
                PatientId = note.PatientId,
                DoctorId = note.DoctorId,
                Title = note.Title,
                Content = note.Content,
                NoteType = note.NoteType,
                Priority = note.Priority,
                IsConfidential = note.IsConfidential,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
                Tags = note.Tags,
                PatientName = note.Patient?.FullName ?? "Unknown",
                DoctorName = note.Doctor?.FullName ?? "Unknown",
                IsIgnoredByDoctor = note.IsIgnoredByDoctor,
                IgnoredByDoctorId = note.IgnoredByDoctorId,
                IgnoredAt = note.IgnoredAt,
                ServiceRequestId = note.ServiceRequestId
            };
        }
    }
}
