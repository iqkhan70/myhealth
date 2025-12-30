using SM_MentalHealthApp.Shared;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class JournalService : BaseService, IJournalService
{
    public JournalService(HttpClient http, IAuthService authService) : base(http, authService)
    {
    }

    public async Task<IEnumerable<JournalEntry>> GetEntriesForUserAsync(int userId, int? serviceRequestId = null, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>();
        if (serviceRequestId.HasValue) queryParams.Add($"serviceRequestId={serviceRequestId.Value}");
        
        var url = queryParams.Any()
            ? $"api/journal/user/{userId}?{string.Join("&", queryParams)}"
            : $"api/journal/user/{userId}";
        
        var response = await _http.GetFromJsonAsync<List<JournalEntry>>(url, ct);
        return response ?? new List<JournalEntry>();
    }

    public async Task<JournalEntry> CreateEntryAsync(int userId, JournalEntry entry, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync($"api/journal/user/{userId}", entry, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JournalEntry>(ct) ?? throw new Exception("Failed to create journal entry");
    }

    public async Task<JournalEntry> CreateEntryForPatientAsync(int doctorId, int patientId, JournalEntry entry, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync($"api/journal/doctor/{doctorId}/patient/{patientId}", entry, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JournalEntry>(ct) ?? throw new Exception("Failed to create journal entry");
    }
}

