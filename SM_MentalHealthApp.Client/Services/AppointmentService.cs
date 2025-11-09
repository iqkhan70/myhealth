using SM_MentalHealthApp.Shared;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class AppointmentService : IAppointmentService
{
    private readonly HttpClient _http;
    private readonly IAuthService _authService;

    public AppointmentService(HttpClient http, IAuthService authService)
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

    public async Task<List<AppointmentDto>> ListAsync(int? doctorId = null, int? patientId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>();
        if (doctorId.HasValue) queryParams.Add($"doctorId={doctorId.Value}");
        if (patientId.HasValue) queryParams.Add($"patientId={patientId.Value}");
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        
        var url = queryParams.Any() 
            ? $"api/appointment?{string.Join("&", queryParams)}"
            : "api/appointment";
        
        var response = await _http.GetFromJsonAsync<List<AppointmentDto>>(url, ct);
        return response ?? new List<AppointmentDto>();
    }

    public async Task<AppointmentDto?> GetAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<AppointmentDto>($"api/appointment/{id}", ct);
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync("api/appointment", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppointmentDto>(ct) ?? throw new Exception("Failed to create appointment");
    }

    public async Task<AppointmentDto> UpdateAsync(int id, UpdateAppointmentRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PutAsJsonAsync($"api/appointment/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppointmentDto>(ct) ?? throw new Exception("Failed to update appointment");
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsync($"api/appointment/{id}/cancel", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<AppointmentValidationResult> ValidateAsync(CreateAppointmentRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync("api/appointment/validate", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppointmentValidationResult>(ct) ?? throw new Exception("Failed to validate appointment");
    }

    public async Task<List<DoctorAvailabilityDto>> GetDoctorAvailabilitiesAsync(int? doctorId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>();
        if (doctorId.HasValue) queryParams.Add($"doctorId={doctorId.Value}");
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        
        var url = queryParams.Any() 
            ? $"api/appointment/availability?{string.Join("&", queryParams)}"
            : "api/appointment/availability";
        
        var response = await _http.GetFromJsonAsync<List<DoctorAvailabilityDto>>(url, ct);
        return response ?? new List<DoctorAvailabilityDto>();
    }

    public async Task<DoctorAvailabilityDto> SetDoctorAvailabilityAsync(DoctorAvailabilityRequest request, CancellationToken ct = default)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync("api/appointment/availability", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DoctorAvailabilityDto>(ct) ?? throw new Exception("Failed to set doctor availability");
    }
}

