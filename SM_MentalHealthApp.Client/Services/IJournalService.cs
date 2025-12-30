using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services;

public interface IJournalService
{
    Task<IEnumerable<JournalEntry>> GetEntriesForUserAsync(int userId, int? serviceRequestId = null, CancellationToken ct = default);
    Task<JournalEntry> CreateEntryAsync(int userId, JournalEntry entry, CancellationToken ct = default);
    Task<JournalEntry> CreateEntryForPatientAsync(int doctorId, int patientId, JournalEntry entry, CancellationToken ct = default);
}

