using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services;

public interface IPatientService
{
    Task<List<User>> ListAsync(CancellationToken ct = default);
    Task<User?> GetAsync(int id, CancellationToken ct = default);
    Task<User> CreateAsync(CreatePatientRequest request, CancellationToken ct = default);
    Task<User> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<UserStats?> GetStatsAsync(int id, CancellationToken ct = default);
    Task<AiHealthCheckResult?> PerformAiHealthCheckAsync(int id, CancellationToken ct = default);
    Task<List<User>> GetDoctorsAsync(CancellationToken ct = default);
    Task AssignToDoctorAsync(DoctorAssignPatientRequest request, CancellationToken ct = default);
    Task UnassignFromDoctorAsync(DoctorUnassignPatientRequest request, CancellationToken ct = default);
    Task<List<JournalEntry>> GetJournalEntriesAsync(int userId, CancellationToken ct = default);
}

