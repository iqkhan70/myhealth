using SM_MentalHealthApp.Shared;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class ChatHistoryService : IChatHistoryService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public ChatHistoryService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    public async Task<IEnumerable<ChatSession>> ListAsync(int? patientId, CancellationToken ct = default)
    {
        var token = _authService.Token;
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var url = patientId.HasValue 
            ? $"api/chathistory/sessions?patientId={patientId.Value}"
            : "api/chathistory/sessions";
        
        var response = await _http.GetFromJsonAsync<List<ChatSession>>(url, ct);
        return response ?? new List<ChatSession>();
    }

    public async Task<ChatSession?> GetAsync(int sessionId, CancellationToken ct = default)
    {
        var token = _authService.Token;
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _http.GetFromJsonAsync<ChatSession>($"api/chathistory/sessions/{sessionId}", ct);
    }

    public async Task DeleteAsync(int sessionId, CancellationToken ct = default)
    {
        var token = _authService.Token;
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        await _http.DeleteAsync($"api/chathistory/sessions/{sessionId}", ct);
    }
}

