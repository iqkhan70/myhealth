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
    
    // Assignment lifecycle methods
    Task<List<SmeRecommendationDto>> GetSmeRecommendationsAsync(int serviceRequestId, string? specialization = null);
    Task<bool> AcceptAssignmentAsync(int assignmentId);
    Task<bool> RejectAssignmentAsync(int assignmentId, OutcomeReason reason, string? notes = null);
    Task<bool> StartAssignmentAsync(int assignmentId);
    Task<bool> CompleteAssignmentAsync(int assignmentId);
    Task<bool> UpdateAssignmentStatusAsync(int assignmentId, AssignmentStatus status, OutcomeReason? outcomeReason = null, ResponsibilityParty? responsibilityParty = null, string? notes = null);
    Task<bool> AdminOverrideAssignmentStatusAsync(int assignmentId, AssignmentStatus status, OutcomeReason? outcomeReason = null, ResponsibilityParty? responsibilityParty = null, string? notes = null);
    Task<int> GetMySmeScoreAsync();
    
    // Billing methods
    Task<List<BillableAssignmentDto>> GetBillableAssignmentsAsync(int? smeUserId = null, DateTime? startDate = null, DateTime? endDate = null);
}

