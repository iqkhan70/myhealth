using SM_MentalHealthApp.Shared;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class ClinicalNotesService : BaseService, IClinicalNotesService
{
    public ClinicalNotesService(HttpClient http, IAuthService authService) : base(http, authService)
    {
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

    public async Task<PagedResult<ClinicalNoteDto>> ListPagedAsync(int skip, int take, int? patientId = null, int? doctorId = null, string? searchTerm = null, string? noteType = null, string? priority = null, bool? isIgnoredByDoctor = null, DateTime? createdDateFrom = null, DateTime? createdDateTo = null, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>
        {
            $"skip={skip}",
            $"take={take}"
        };
        if (patientId.HasValue) queryParams.Add($"patientId={patientId.Value}");
        if (doctorId.HasValue) queryParams.Add($"doctorId={doctorId.Value}");
        if (!string.IsNullOrWhiteSpace(searchTerm)) queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        if (!string.IsNullOrWhiteSpace(noteType)) queryParams.Add($"noteType={Uri.EscapeDataString(noteType)}");
        if (!string.IsNullOrWhiteSpace(priority)) queryParams.Add($"priority={Uri.EscapeDataString(priority)}");
        if (isIgnoredByDoctor.HasValue) queryParams.Add($"isIgnoredByDoctor={isIgnoredByDoctor.Value}");
        if (createdDateFrom.HasValue) queryParams.Add($"createdDateFrom={createdDateFrom.Value:yyyy-MM-dd}");
        if (createdDateTo.HasValue) queryParams.Add($"createdDateTo={createdDateTo.Value:yyyy-MM-dd}");

        var url = $"api/clinicalnotes/paged?{string.Join("&", queryParams)}";
        var response = await _http.GetFromJsonAsync<PagedResult<ClinicalNoteDto>>(url, ct);
        return response ?? new PagedResult<ClinicalNoteDto>
        {
            Items = new List<ClinicalNoteDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = take
        };
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

    public async Task<bool> ToggleIgnoreAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsync($"api/clinicalnotes/{id}/toggle-ignore", null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ToggleIgnoreResponse>(ct);
        return result?.IsIgnored ?? false;
    }

    private class ToggleIgnoreResponse
    {
        public bool IsIgnored { get; set; }
        public string? Message { get; set; }
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

