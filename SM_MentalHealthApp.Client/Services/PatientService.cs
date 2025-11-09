using SM_MentalHealthApp.Shared;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class PatientService : IPatientService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public PatientService(HttpClient http, IAuthService authService)
    {
        _http = http;
        _authService = authService;
    }

    private void AddAuthorizationHeader()
    {
        var token = _authService.Token;
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<List<User>> ListAsync(CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var currentUser = _authService.CurrentUser;
        if (currentUser?.RoleId == 2) // Doctor
        {
            return await _http.GetFromJsonAsync<List<User>>("api/doctor/my-patients", ct) ?? new List<User>();
        }
        else if (currentUser?.RoleId == 3) // Admin
        {
            return await _http.GetFromJsonAsync<List<User>>("api/admin/patients", ct) ?? new List<User>();
        }
        return new List<User>();
    }

    public async Task<User?> GetAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<User>($"api/user/{id}", ct);
    }

    public async Task<User> CreateAsync(CreatePatientRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync("api/admin/create-patient", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<User>(ct) ?? throw new Exception("Failed to create patient");
    }

    public async Task<User> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PutAsJsonAsync($"api/admin/update-patient/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<User>(ct) ?? throw new Exception("Failed to update patient");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.DeleteAsync($"api/user/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserStats?> GetStatsAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<UserStats>($"api/user/{id}/stats", ct);
    }

    public async Task<AiHealthCheckResult?> PerformAiHealthCheckAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsync($"api/admin/ai-health-check/{id}", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AiHealthCheckResult>(ct);
    }

    public async Task<List<User>> GetDoctorsAsync(CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<List<User>>("api/doctor/doctors", ct) ?? new List<User>();
    }

    public async Task AssignToDoctorAsync(DoctorAssignPatientRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync("api/doctor/assign-patient", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnassignFromDoctorAsync(DoctorUnassignPatientRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, "api/doctor/unassign-patient")
        {
            Content = JsonContent.Create(request)
        };
        var response = await _http.SendAsync(requestMessage, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<JournalEntry>> GetJournalEntriesAsync(int userId, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<List<JournalEntry>>($"api/journal/user/{userId}", ct) ?? new List<JournalEntry>();
    }
}

