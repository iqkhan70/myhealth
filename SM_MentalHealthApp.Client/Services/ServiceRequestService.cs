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

    // Assignment lifecycle methods
    public async Task<List<SmeRecommendationDto>> GetSmeRecommendationsAsync(int serviceRequestId, string? specialization = null)
    {
        AddAuthorizationHeader();
        var query = specialization != null ? $"?specialization={Uri.EscapeDataString(specialization)}" : "";
        return await _http.GetFromJsonAsync<List<SmeRecommendationDto>>($"api/ServiceRequest/{serviceRequestId}/sme-recommendations{query}") ?? new List<SmeRecommendationDto>();
    }

    public async Task<bool> AcceptAssignmentAsync(int assignmentId)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsync($"api/ServiceRequest/assignments/{assignmentId}/accept", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectAssignmentAsync(int assignmentId, OutcomeReason reason, string? notes = null)
    {
        AddAuthorizationHeader();
        var request = new RejectAssignmentRequest
        {
            AssignmentId = assignmentId,
            Reason = reason,
            Notes = notes
        };
        var response = await _http.PostAsJsonAsync($"api/ServiceRequest/assignments/{assignmentId}/reject", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> StartAssignmentAsync(int assignmentId)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsync($"api/ServiceRequest/assignments/{assignmentId}/start", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CompleteAssignmentAsync(int assignmentId)
    {
        AddAuthorizationHeader();
        var response = await _http.PostAsync($"api/ServiceRequest/assignments/{assignmentId}/complete", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAssignmentStatusAsync(int assignmentId, AssignmentStatus status, OutcomeReason? outcomeReason = null, ResponsibilityParty? responsibilityParty = null, string? notes = null)
    {
        AddAuthorizationHeader();
        var request = new UpdateAssignmentStatusRequest
        {
            AssignmentId = assignmentId,
            Status = status,
            OutcomeReason = outcomeReason,
            ResponsibilityParty = responsibilityParty,
            Notes = notes
        };
        var response = await _http.PutAsJsonAsync($"api/ServiceRequest/assignments/{assignmentId}/status", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AdminOverrideAssignmentStatusAsync(int assignmentId, AssignmentStatus status, OutcomeReason? outcomeReason = null, ResponsibilityParty? responsibilityParty = null, string? notes = null)
    {
        AddAuthorizationHeader();
        var request = new UpdateAssignmentStatusRequest
        {
            AssignmentId = assignmentId,
            Status = status,
            OutcomeReason = outcomeReason,
            ResponsibilityParty = responsibilityParty,
            Notes = notes
        };
        var response = await _http.PutAsJsonAsync($"api/ServiceRequest/assignments/{assignmentId}/status/admin-override", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<int> GetMySmeScoreAsync()
    {
        AddAuthorizationHeader();
        var response = await _http.GetAsync("api/ServiceRequest/sme-score");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
            return result?.GetValueOrDefault("score", 100) ?? 100;
        }
        return 100;
    }

    // Billing methods
    public async Task<List<BillableAssignmentDto>> GetBillableAssignmentsAsync(int? smeUserId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        AddAuthorizationHeader();
        var queryParams = new List<string>();
        if (smeUserId.HasValue)
            queryParams.Add($"smeUserId={smeUserId.Value}");
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        
        var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _http.GetFromJsonAsync<List<BillableAssignmentDto>>($"api/ServiceRequest/billing/assignments{query}") ?? new List<BillableAssignmentDto>();
    }
}

