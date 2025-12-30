using SM_MentalHealthApp.Shared;
using System.Net.Http.Json;

namespace SM_MentalHealthApp.Client.Services;

public class ServiceRequestService : BaseService, IServiceRequestService
{
    public ServiceRequestService(HttpClient http, IAuthService authService) : base(http, authService)
    {
    }

    public async Task<List<ServiceRequestDto>> GetServiceRequestsAsync(int? clientId = null, int? smeUserId = null)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>();
        if (clientId.HasValue)
            queryParams.Add($"clientId={clientId.Value}");
        if (smeUserId.HasValue)
            queryParams.Add($"smeUserId={smeUserId.Value}");
        
        var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _http.GetFromJsonAsync<List<ServiceRequestDto>>($"api/ServiceRequest{query}") ?? new List<ServiceRequestDto>();
    }

    public async Task<ServiceRequestDto?> GetServiceRequestByIdAsync(int id)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<ServiceRequestDto>($"api/ServiceRequest/{id}");
    }

    public async Task<ServiceRequestDto> CreateServiceRequestAsync(CreateServiceRequestRequest request)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsJsonAsync("api/ServiceRequest", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceRequestDto>() ?? throw new Exception("Failed to create service request");
    }

    public async Task<ServiceRequestDto?> UpdateServiceRequestAsync(int id, UpdateServiceRequestRequest request)
    {
        AddAuthorizationHeader();
        var response = await _http.PutAsJsonAsync($"api/ServiceRequest/{id}", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ServiceRequestDto>();
        }
        return null;
    }

    public async Task<bool> DeleteServiceRequestAsync(int id)
    {
        AddAuthorizationHeader();
        var response = await _http.DeleteAsync($"api/ServiceRequest/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AssignSmeToServiceRequestAsync(int serviceRequestId, int smeUserId)
    {
        AddAuthorizationHeader();
        var request = new AssignSmeToServiceRequestRequest
        {
            ServiceRequestId = serviceRequestId,
            SmeUserId = smeUserId
        };
        var response = await _http.PostAsJsonAsync($"api/ServiceRequest/{serviceRequestId}/assign", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnassignSmeFromServiceRequestAsync(int serviceRequestId, int smeUserId)
    {
        AddAuthorizationHeader();
        var request = new UnassignSmeFromServiceRequestRequest
        {
            ServiceRequestId = serviceRequestId,
            SmeUserId = smeUserId
        };
        var response = await _http.PostAsJsonAsync($"api/ServiceRequest/{serviceRequestId}/unassign", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ServiceRequestDto>> GetMyServiceRequestsAsync()
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<List<ServiceRequestDto>>("api/ServiceRequest/my-assignments") ?? new List<ServiceRequestDto>();
    }

    public async Task<ServiceRequestDto?> GetDefaultServiceRequestForClientAsync(int clientId)
    {
        AddAuthorizationHeader();
        return await _http.GetFromJsonAsync<ServiceRequestDto>($"api/ServiceRequest/default/{clientId}");
    }
}

