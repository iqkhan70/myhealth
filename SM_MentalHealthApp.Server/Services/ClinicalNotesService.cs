using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IClinicalNotesService
    {
        Task<List<ClinicalNoteDto>> GetClinicalNotesAsync(int? patientId = null, int? doctorId = null);
        Task<ClinicalNoteDto?> GetClinicalNoteByIdAsync(int id);
        Task<ClinicalNoteDto> CreateClinicalNoteAsync(CreateClinicalNoteRequest request, int doctorId);
        Task<ClinicalNoteDto?> UpdateClinicalNoteAsync(int id, UpdateClinicalNoteRequest request, int doctorId);
        Task<bool> DeleteClinicalNoteAsync(int id, int doctorId);
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

                return notes.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical notes");
                throw;
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

        public async Task<ClinicalNoteDto> CreateClinicalNoteAsync(CreateClinicalNoteRequest request, int doctorId)
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

        public async Task<bool> DeleteClinicalNoteAsync(int id, int doctorId)
        {
            try
            {
                var note = await _context.ClinicalNotes
                    .FirstOrDefaultAsync(cn => cn.Id == id && cn.IsActive);

                if (note == null)
                    return false;

                // Verify the doctor owns this note
                if (note.DoctorId != doctorId)
                    throw new UnauthorizedAccessException("You can only delete your own clinical notes");

                note.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Clinical note deleted with ID: {Id}", id);
                return true;
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
                DoctorName = note.Doctor?.FullName ?? "Unknown"
            };
        }
    }
}
