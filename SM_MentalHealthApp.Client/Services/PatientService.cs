using SM_MentalHealthApp.Shared;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class PatientService : BaseService, IPatientService
{
    public PatientService(HttpClient http, IAuthService authService) : base(http, authService)
    {
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
        return await _http.GetFromJsonAsync<User>($"api/patient/{id}", ct);
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
        var response = await _http.DeleteAsync($"api/patient/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserStats?> GetStatsAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<UserStats>($"api/patient/{id}/stats", ct);
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

