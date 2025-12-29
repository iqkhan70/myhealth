using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Client.Services;

public interface IServiceRequestService
{
    Task<List<ServiceRequestDto>> GetServiceRequestsAsync(int? clientId = null, int? smeUserId = null);
    Task<ServiceRequestDto?> GetServiceRequestByIdAsync(int id);
    Task<ServiceRequestDto> CreateServiceRequestAsync(CreateServiceRequestRequest request);
    Task<ServiceRequestDto?> UpdateServiceRequestAsync(int id, UpdateServiceRequestRequest request);
    Task<bool> DeleteServiceRequestAsync(int id);
    Task<bool> AssignSmeToServiceRequestAsync(int serviceRequestId, int smeUserId);
    Task<bool> UnassignSmeFromServiceRequestAsync(int serviceRequestId, int smeUserId);
    Task<List<ServiceRequestDto>> GetMyServiceRequestsAsync();
    Task<ServiceRequestDto?> GetDefaultServiceRequestForClientAsync(int clientId);
}

