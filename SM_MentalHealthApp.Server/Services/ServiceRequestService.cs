using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface IServiceRequestService
    {
        Task<List<ServiceRequestDto>> GetServiceRequestsAsync(int? clientId = null, int? smeUserId = null);
        Task<ServiceRequestDto?> GetServiceRequestByIdAsync(int id);
        Task<ServiceRequestDto> CreateServiceRequestAsync(CreateServiceRequestRequest request, int? createdByUserId = null);
        Task<ServiceRequestDto?> UpdateServiceRequestAsync(int id, UpdateServiceRequestRequest request);
        Task<bool> DeleteServiceRequestAsync(int id);
        Task<bool> AssignSmeToServiceRequestAsync(int serviceRequestId, int smeUserId, int? assignedByUserId = null);
        Task<bool> UnassignSmeFromServiceRequestAsync(int serviceRequestId, int smeUserId);
        Task<List<int>> GetServiceRequestIdsForSmeAsync(int smeUserId);
        Task<bool> IsSmeAssignedToServiceRequestAsync(int serviceRequestId, int smeUserId);
        Task<ServiceRequestDto?> GetDefaultServiceRequestForClientAsync(int clientId);
        Task<Shared.AutoCompleteServiceRequestsResult> AutoCompleteServiceRequestsAsync();
    }

    public class ServiceRequestService : IServiceRequestService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ServiceRequestService> _logger;
        private readonly IExpertiseService _expertiseService;

        public ServiceRequestService(JournalDbContext context, ILogger<ServiceRequestService> logger, IExpertiseService expertiseService)
        {
            _context = context;
            _logger = logger;
            _expertiseService = expertiseService;
        }

        /// <summary>
        /// Get service requests, optionally filtered by client or SME
        /// </summary>
        public async Task<List<ServiceRequestDto>> GetServiceRequestsAsync(int? clientId = null, int? smeUserId = null)
        {
            try
            {
                var query = _context.ServiceRequests
                    .Include(sr => sr.Client)
                    .Include(sr => sr.Assignments)
                        .ThenInclude(a => a.SmeUser)
                    .Where(sr => sr.IsActive);

                if (clientId.HasValue)
                    query = query.Where(sr => sr.ClientId == clientId.Value);

                if (smeUserId.HasValue)
                {
                    // Filter by SME assignments
                    query = query.Where(sr => sr.Assignments.Any(a => a.SmeUserId == smeUserId.Value && a.IsActive));
                }

                var serviceRequests = await query
                    .OrderByDescending(sr => sr.CreatedAt)
                    .ToListAsync();

                return serviceRequests.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service requests");
                return new List<ServiceRequestDto>();
            }
        }

        /// <summary>
        /// Get a service request by ID
        /// </summary>
        public async Task<ServiceRequestDto?> GetServiceRequestByIdAsync(int id)
        {
            try
            {
                var serviceRequest = await _context.ServiceRequests
                    .Include(sr => sr.Client)
                    .Include(sr => sr.Assignments)
                        .ThenInclude(a => a.SmeUser)
                    .Include(sr => sr.Expertises)
                        .ThenInclude(e => e.Expertise)
                    .FirstOrDefaultAsync(sr => sr.Id == id && sr.IsActive);

                return serviceRequest != null ? MapToDto(serviceRequest) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service request by ID: {Id}", id);
                return null;
            }
        }

        /// <summary>
        /// Create a new service request
        /// </summary>
        public async Task<ServiceRequestDto> CreateServiceRequestAsync(CreateServiceRequestRequest request, int? createdByUserId = null)
        {
            try
            {
                // Verify client exists and is active
                var client = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.ClientId && u.IsActive);

                if (client == null)
                    throw new ArgumentException($"Client with ID {request.ClientId} not found or inactive");

                var serviceRequest = new ServiceRequest
                {
                    ClientId = request.ClientId,
                    Title = request.Title,
                    Type = request.Type,
                    Status = request.Status,
                    Description = request.Description,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ServiceRequests.Add(serviceRequest);
                await _context.SaveChangesAsync();

                // Set expertise if provided
                if (request.ExpertiseIds != null && request.ExpertiseIds.Any())
                {
                    await _expertiseService.SetServiceRequestExpertisesAsync(serviceRequest.Id, request.ExpertiseIds);
                }

                // If SME is provided, assign them immediately
                if (request.SmeUserId.HasValue)
                {
                    await AssignSmeToServiceRequestAsync(serviceRequest.Id, request.SmeUserId.Value, createdByUserId);
                }

                // Reload with includes
                return await GetServiceRequestByIdAsync(serviceRequest.Id) ?? MapToDto(serviceRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service request");
                throw;
            }
        }

        /// <summary>
        /// Update a service request
        /// </summary>
        public async Task<ServiceRequestDto?> UpdateServiceRequestAsync(int id, UpdateServiceRequestRequest request)
        {
            try
            {
                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(sr => sr.Id == id && sr.IsActive);

                if (serviceRequest == null)
                    return null;

                if (!string.IsNullOrWhiteSpace(request.Title))
                    serviceRequest.Title = request.Title;

                if (request.Type != null)
                    serviceRequest.Type = request.Type;

                if (!string.IsNullOrWhiteSpace(request.Status))
                    serviceRequest.Status = request.Status;

                if (request.Description != null)
                    serviceRequest.Description = request.Description;

                // Update expertise if provided
                if (request.ExpertiseIds != null)
                {
                    await _expertiseService.SetServiceRequestExpertisesAsync(id, request.ExpertiseIds);
                }

                serviceRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return await GetServiceRequestByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service request: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Soft delete a service request
        /// </summary>
        public async Task<bool> DeleteServiceRequestAsync(int id)
        {
            try
            {
                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(sr => sr.Id == id && sr.IsActive);

                if (serviceRequest == null)
                    return false;

                serviceRequest.IsActive = false;
                serviceRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service request: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Assign an SME to a service request
        /// </summary>
        public async Task<bool> AssignSmeToServiceRequestAsync(int serviceRequestId, int smeUserId, int? assignedByUserId = null)
        {
            try
            {
                // Verify service request exists
                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId && sr.IsActive);

                if (serviceRequest == null)
                    return false;

                // Verify SME exists and is a doctor or attorney
                var sme = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == smeUserId && 
                        (u.RoleId == Shared.Constants.Roles.Doctor || u.RoleId == Shared.Constants.Roles.Attorney || u.RoleId == Shared.Constants.Roles.Sme) && 
                        u.IsActive);

                if (sme == null)
                    return false;

                // Check if assignment already exists and is active
                var existingAssignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.ServiceRequestId == serviceRequestId && 
                        a.SmeUserId == smeUserId && 
                        a.IsActive);

                if (existingAssignment != null)
                    return false; // Already assigned

                // Deactivate any previous assignments (if reassigning)
                var previousAssignments = await _context.ServiceRequestAssignments
                    .Where(a => a.ServiceRequestId == serviceRequestId && 
                        a.SmeUserId == smeUserId && 
                        !a.IsActive)
                    .ToListAsync();

                foreach (var prev in previousAssignments)
                {
                    prev.IsActive = false;
                    prev.UnassignedAt = DateTime.UtcNow;
                }

                // Create new assignment
                var assignment = new ServiceRequestAssignment
                {
                    ServiceRequestId = serviceRequestId,
                    SmeUserId = smeUserId,
                    AssignedByUserId = assignedByUserId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = AssignmentStatus.Assigned.ToString(), // Initial status
                    IsBillable = false, // Not billable until work starts
                    BillingStatus = BillingStatus.NotBillable.ToString() // Not billable initially
                };

                _context.ServiceRequestAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning SME {SmeUserId} to service request {ServiceRequestId}. Error: {ErrorMessage}", 
                    smeUserId, serviceRequestId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Unassign an SME from a service request
        /// </summary>
        public async Task<bool> UnassignSmeFromServiceRequestAsync(int serviceRequestId, int smeUserId)
        {
            try
            {
                var assignment = await _context.ServiceRequestAssignments
                    .FirstOrDefaultAsync(a => a.ServiceRequestId == serviceRequestId && 
                        a.SmeUserId == smeUserId && 
                        a.IsActive);

                if (assignment == null)
                    return false;

                assignment.IsActive = false;
                assignment.UnassignedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning SME from service request");
                return false;
            }
        }

        /// <summary>
        /// Get all ServiceRequest IDs that an SME is assigned to
        /// </summary>
        public async Task<List<int>> GetServiceRequestIdsForSmeAsync(int smeUserId)
        {
            try
            {
                return await _context.ServiceRequestAssignments
                    .Where(a => a.SmeUserId == smeUserId && a.IsActive)
                    .Select(a => a.ServiceRequestId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service request IDs for SME: {SmeUserId}", smeUserId);
                return new List<int>();
            }
        }

        /// <summary>
        /// Check if an SME is assigned to a service request
        /// </summary>
        public async Task<bool> IsSmeAssignedToServiceRequestAsync(int serviceRequestId, int smeUserId)
        {
            try
            {
                return await _context.ServiceRequestAssignments
                    .AnyAsync(a => a.ServiceRequestId == serviceRequestId && 
                        a.SmeUserId == smeUserId && 
                        a.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking SME assignment");
                return false;
            }
        }

        /// <summary>
        /// Get the default "General" ServiceRequest for a client
        /// </summary>
        public async Task<ServiceRequestDto?> GetDefaultServiceRequestForClientAsync(int clientId)
        {
            try
            {
                var serviceRequest = await _context.ServiceRequests
                    .Include(sr => sr.Client)
                    .Include(sr => sr.Assignments)
                        .ThenInclude(a => a.SmeUser)
                    .FirstOrDefaultAsync(sr => sr.ClientId == clientId && 
                        sr.Title == "General" && 
                        sr.IsActive);

                return serviceRequest != null ? MapToDto(serviceRequest) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default service request for client: {ClientId}", clientId);
                return null;
            }
        }

        /// <summary>
        /// Map ServiceRequest entity to DTO
        /// </summary>
        private ServiceRequestDto MapToDto(ServiceRequest sr)
        {
            return new ServiceRequestDto
            {
                Id = sr.Id,
                ClientId = sr.ClientId,
                ClientName = $"{sr.Client.FirstName} {sr.Client.LastName}",
                Title = sr.Title,
                Type = sr.Type,
                Status = sr.Status,
                CreatedAt = sr.CreatedAt,
                UpdatedAt = sr.UpdatedAt,
                Description = sr.Description,
                ExpertiseIds = sr.Expertises?.Select(e => e.ExpertiseId).ToList() ?? new List<int>(),
                ExpertiseNames = sr.Expertises?.Select(e => e.Expertise.Name).ToList() ?? new List<string>(),
                Assignments = sr.Assignments
                    .Where(a => a.IsActive)
                    .Select(a => new ServiceRequestAssignmentDto
                    {
                        Id = a.Id,
                        ServiceRequestId = a.ServiceRequestId,
                        SmeUserId = a.SmeUserId,
                        SmeUserName = $"{a.SmeUser.FirstName} {a.SmeUser.LastName}",
                        SmeScore = a.SmeUser.SmeScore,
                        AssignedAt = a.AssignedAt,
                        AcceptedAt = a.AcceptedAt,
                        StartedAt = a.StartedAt,
                        CompletedAt = a.CompletedAt,
                        IsActive = a.IsActive,
                        Status = a.Status ?? "Assigned",
                        OutcomeReason = a.OutcomeReason,
                        ResponsibilityParty = a.ResponsibilityParty,
                        IsBillable = a.IsBillable,
                        AssignedByUserId = a.AssignedByUserId,
                        AssignedByUserName = a.AssignedByUser != null ? $"{a.AssignedByUser.FirstName} {a.AssignedByUser.LastName}" : null
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Auto-complete Service Requests where all assigned SMEs have completed their work
        /// </summary>
        public async Task<Shared.AutoCompleteServiceRequestsResult> AutoCompleteServiceRequestsAsync()
        {
            var result = new Shared.AutoCompleteServiceRequestsResult
            {
                StartedAt = DateTime.UtcNow
            };

            try
            {
                // Get all active Service Requests that are not already completed
                var serviceRequests = await _context.ServiceRequests
                    .Include(sr => sr.Assignments.Where(a => a.IsActive))
                    .Where(sr => sr.IsActive && 
                        sr.Status != "Completed" && 
                        sr.Status != "Cancelled") // Don't process already completed or cancelled SRs
                    .ToListAsync();

                result.TotalServiceRequestsChecked = serviceRequests.Count;

                foreach (var sr in serviceRequests)
                {
                    // Get all active assignments for this SR
                    var activeAssignments = sr.Assignments
                        .Where(a => a.IsActive)
                        .ToList();

                    // Skip SRs with no active assignments
                    if (!activeAssignments.Any())
                    {
                        result.SkippedNoAssignments++;
                        continue;
                    }

                    // Check if ALL active assignments are completed
                    var allCompleted = activeAssignments.All(a => 
                        a.Status == AssignmentStatus.Completed.ToString());

                    if (allCompleted)
                    {
                        // Mark SR as completed
                        sr.Status = "Completed";
                        sr.UpdatedAt = DateTime.UtcNow;
                        result.CompletedServiceRequests++;
                        result.CompletedServiceRequestIds.Add(sr.Id);
                        
                        _logger.LogInformation(
                            "Auto-completed ServiceRequest {ServiceRequestId} ({Title}) - all {AssignmentCount} assignments are completed",
                            sr.Id, sr.Title, activeAssignments.Count);
                    }
                    else
                    {
                        // Count how many are completed vs total
                        var completedCount = activeAssignments.Count(a => 
                            a.Status == AssignmentStatus.Completed.ToString());
                        result.PendingServiceRequests++;
                        
                        _logger.LogDebug(
                            "ServiceRequest {ServiceRequestId} ({Title}) not completed - {CompletedCount}/{TotalCount} assignments completed",
                            sr.Id, sr.Title, completedCount, activeAssignments.Count);
                    }
                }

                // Save all changes
                if (result.CompletedServiceRequests > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "Auto-complete job completed: {CompletedCount} Service Requests marked as completed, {PendingCount} still pending, {SkippedCount} skipped (no assignments)",
                        result.CompletedServiceRequests, result.PendingServiceRequests, result.SkippedNoAssignments);
                }
                else
                {
                    _logger.LogInformation(
                        "Auto-complete job completed: No Service Requests were ready to be completed. {PendingCount} still pending, {SkippedCount} skipped (no assignments)",
                        result.PendingServiceRequests, result.SkippedNoAssignments);
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoCompleteServiceRequestsAsync");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }
    }
}

