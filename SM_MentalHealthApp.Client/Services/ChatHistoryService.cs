using SM_MentalHealthApp.Shared;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class ChatHistoryService : BaseService, IChatHistoryService
{
    public ChatHistoryService(HttpClient http, IAuthService authService) : base(http, authService)
    {
    }

    public async Task<IEnumerable<ChatSession>> ListAsync(int? patientId, CancellationToken ct = default)
    {
        AddAuthorizationHeader();

        var url = patientId.HasValue
            ? $"api/chathistory/sessions?patientId={patientId.Value}"
            : "api/chathistory/sessions";

        var response = await _http.GetFromJsonAsync<List<ChatSession>>(url, ct);
        return response ?? new List<ChatSession>();
    }

    public async Task<ChatSession?> GetAsync(int sessionId, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<ChatSession>($"api/chathistory/sessions/{sessionId}", ct);
    }

    public async Task DeleteAsync(int sessionId, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        await _http.DeleteAsync($"api/chathistory/sessions/{sessionId}", ct);
    }
}

