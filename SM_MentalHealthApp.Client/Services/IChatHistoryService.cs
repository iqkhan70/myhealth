using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services;

public interface IChatHistoryService
{
    Task<IEnumerable<ChatSession>> ListAsync(int? patientId, int? serviceRequestId = null, CancellationToken ct = default);
    Task<ChatSession?> GetAsync(int sessionId, CancellationToken ct = default);
    Task DeleteAsync(int sessionId, CancellationToken ct = default);
    Task<bool> ToggleIgnoreAsync(int sessionId, CancellationToken ct = default);
}

