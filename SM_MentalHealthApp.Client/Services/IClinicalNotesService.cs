using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services;

public interface IClinicalNotesService
{
    Task<List<ClinicalNoteDto>> ListAsync(int? patientId = null, int? doctorId = null, CancellationToken ct = default);
    Task<ClinicalNoteDto?> GetAsync(int id, CancellationToken ct = default);
    Task<ClinicalNoteDto> CreateAsync(CreateClinicalNoteRequest request, CancellationToken ct = default);
    Task<ClinicalNoteDto> UpdateAsync(int id, UpdateClinicalNoteRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ToggleIgnoreAsync(int id, CancellationToken ct = default);
    Task<List<string>> GetNoteTypesAsync(CancellationToken ct = default);
    Task<List<string>> GetPrioritiesAsync(CancellationToken ct = default);
}

