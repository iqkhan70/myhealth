using SM_MentalHealthApp.Shared;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class ClinicalNotesService : IClinicalNotesService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public ClinicalNotesService(HttpClient http, IAuthService authService)
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

    public async Task<List<ClinicalNoteDto>> ListAsync(int? patientId = null, int? doctorId = null, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>();
        if (patientId.HasValue) queryParams.Add($"patientId={patientId.Value}");
        if (doctorId.HasValue) queryParams.Add($"doctorId={doctorId.Value}");
        
        var url = queryParams.Any() 
            ? $"api/clinicalnotes?{string.Join("&", queryParams)}"
            : "api/clinicalnotes";
        
        var response = await _http.GetFromJsonAsync<List<ClinicalNoteDto>>(url, ct);
        return response ?? new List<ClinicalNoteDto>();
    }

    public async Task<ClinicalNoteDto?> GetAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<ClinicalNoteDto>($"api/clinicalnotes/{id}", ct);
    }

    public async Task<ClinicalNoteDto> CreateAsync(CreateClinicalNoteRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync("api/clinicalnotes", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClinicalNoteDto>(ct) ?? throw new Exception("Failed to create clinical note");
    }

    public async Task<ClinicalNoteDto> UpdateAsync(int id, UpdateClinicalNoteRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PutAsJsonAsync($"api/clinicalnotes/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClinicalNoteDto>(ct) ?? throw new Exception("Failed to update clinical note");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.DeleteAsync($"api/clinicalnotes/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<string>> GetNoteTypesAsync(CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.GetFromJsonAsync<List<string>>("api/clinicalnotes/note-types", ct);
        return response ?? new List<string>();
    }

    public async Task<List<string>> GetPrioritiesAsync(CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.GetFromJsonAsync<List<string>>("api/clinicalnotes/priorities", ct);
        return response ?? new List<string>();
    }
}

