using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceRequestController : BaseController
    {
        private readonly IServiceRequestService _serviceRequestService;
        private readonly IAssignmentLifecycleService _assignmentLifecycleService;
        private readonly ILogger<ServiceRequestController> _logger;
        private readonly JournalDbContext _context;

        public ServiceRequestController(
            IServiceRequestService serviceRequestService,
            IAssignmentLifecycleService assignmentLifecycleService,
            ILogger<ServiceRequestController> logger,
            JournalDbContext context)
        {
            _serviceRequestService = serviceRequestService;
            _assignmentLifecycleService = assignmentLifecycleService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Get all service requests, optionally filtered by client or SME
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ServiceRequestDto>>> GetServiceRequests(
            [FromQuery] int? clientId = null,
            [FromQuery] int? smeUserId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                // For patients, only show their own service requests
                if (currentRoleId == Roles.Patient && currentUserId.HasValue)
                {
                    var serviceRequests = await _serviceRequestService.GetServiceRequestsAsync(currentUserId.Value, null);
                    return Ok(serviceRequests);
                }

                // For doctors and attorneys, only show service requests they're assigned to
                if ((currentRoleId == Roles.Doctor || currentRoleId == Roles.Attorney || currentRoleId == Roles.Sme) && currentUserId.HasValue)
                {
                    var serviceRequests = await _serviceRequestService.GetServiceRequestsAsync(clientId, currentUserId.Value);
                    return Ok(serviceRequests);
                }

                // For admins and coordinators, show all or filtered by parameters
                var allServiceRequests = await _serviceRequestService.GetServiceRequestsAsync(clientId, smeUserId);
                return Ok(allServiceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service requests");
                return StatusCode(500, "An error occurred while retrieving service requests");
            }
        }

        /// <summary>
        /// Get a service request by ID (with access control)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceRequestDto>> GetServiceRequest(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                var serviceRequest = await _serviceRequestService.GetServiceRequestByIdAsync(id);
                if (serviceRequest == null)
                    return NotFound();

                // For doctors and attorneys, verify they're assigned to this SR
                if ((currentRoleId == Roles.Doctor || currentRoleId == Roles.Attorney || currentRoleId == Roles.Sme) && currentUserId.HasValue)
                {
                    var isAssigned = await _serviceRequestService.IsSmeAssignedToServiceRequestAsync(id, currentUserId.Value);
                    if (!isAssigned)
                        return Forbid("You are not assigned to this service request");
                }

                return Ok(serviceRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service request: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the service request");
            }
        }

        /// <summary>
        /// Create a new service request
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Coordinator,Patient")]
        public async Task<ActionResult<ServiceRequestDto>> CreateServiceRequest([FromBody] CreateServiceRequestRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();
                
                // If patient is creating, ensure they can only create for themselves
                if (currentRoleId == Roles.Patient && request.ClientId != currentUserId)
                {
                    _logger.LogWarning("Patient {UserId} attempted to create SR for different client {ClientId}", currentUserId, request.ClientId);
                    return BadRequest("Patients can only create service requests for themselves.");
                }
                
                // Auto-set ClientId for patients
                if (currentRoleId == Roles.Patient)
                {
                    request.ClientId = currentUserId.Value;
                }
                
                // Patients cannot assign SMEs - coordinators/admins will do that
                if (currentRoleId == Roles.Patient)
                {
                    request.SmeUserId = null;
                }
                
                var serviceRequest = await _serviceRequestService.CreateServiceRequestAsync(request, currentUserId);
                return CreatedAtAction(nameof(GetServiceRequest), new { id = serviceRequest.Id }, serviceRequest);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request to create service request");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service request");
                return StatusCode(500, "An error occurred while creating the service request");
            }
        }

        /// <summary>
        /// Update a service request
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<ServiceRequestDto>> UpdateServiceRequest(int id, [FromBody] UpdateServiceRequestRequest request)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.UpdateServiceRequestAsync(id, request);
                if (serviceRequest == null)
                    return NotFound();

                return Ok(serviceRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service request: {Id}", id);
                return StatusCode(500, "An error occurred while updating the service request");
            }
        }

        /// <summary>
        /// Delete (soft delete) a service request
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult> DeleteServiceRequest(int id)
        {
            try
            {
                var deleted = await _serviceRequestService.DeleteServiceRequestAsync(id);
                if (!deleted)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service request: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the service request");
            }
        }

        /// <summary>
        /// Assign an SME to a service request
        /// </summary>
        [HttpPost("{id}/assign")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult> AssignSmeToServiceRequest(int id, [FromBody] AssignSmeToServiceRequestRequest request)
        {
            try
            {
                if (request.ServiceRequestId != id)
                    return BadRequest("Service request ID mismatch");

                var currentUserId = GetCurrentUserId();
                var assigned = await _serviceRequestService.AssignSmeToServiceRequestAsync(id, request.SmeUserId, currentUserId);
                
                if (!assigned)
                {
                    _logger.LogWarning("Failed to assign SME {SmeUserId} to ServiceRequest {ServiceRequestId}. SME may already be assigned or service request/SME may not exist.", 
                        request.SmeUserId, id);
                    return BadRequest("Failed to assign SME. The SME may already be assigned or the service request/SME may not exist.");
                }

                return Ok(new { message = "SME assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning SME to service request: {Id}, SME: {SmeUserId}", id, request?.SmeUserId);
                return StatusCode(500, $"An error occurred while assigning the SME: {ex.Message}");
            }
        }

        /// <summary>
        /// Unassign an SME from a service request
        /// </summary>
        [HttpPost("{id}/unassign")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult> UnassignSmeFromServiceRequest(int id, [FromBody] UnassignSmeFromServiceRequestRequest request)
        {
            try
            {
                if (request.ServiceRequestId != id)
                    return BadRequest("Service request ID mismatch");

                var unassigned = await _serviceRequestService.UnassignSmeFromServiceRequestAsync(id, request.SmeUserId);
                
                if (!unassigned)
                    return BadRequest("Failed to unassign SME. The assignment may not exist.");

                return Ok(new { message = "SME unassigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning SME from service request: {Id}", id);
                return StatusCode(500, "An error occurred while unassigning the SME");
            }
        }

        /// <summary>
        /// Get service requests for the current SME (convenience endpoint)
        /// </summary>
        [HttpGet("my-assignments")]
        [Authorize(Roles = "Doctor,Attorney,SME")]
        public async Task<ActionResult<List<ServiceRequestDto>>> GetMyServiceRequests()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var serviceRequests = await _serviceRequestService.GetServiceRequestsAsync(null, currentUserId.Value);
                return Ok(serviceRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my service requests");
                return StatusCode(500, "An error occurred while retrieving your service requests");
            }
        }

        /// <summary>
        /// Get the default service request for a client
        /// </summary>
        [HttpGet("default/{clientId}")]
        public async Task<ActionResult<ServiceRequestDto>> GetDefaultServiceRequest(int clientId)
        {
            try
            {
                var serviceRequest = await _serviceRequestService.GetDefaultServiceRequestForClientAsync(clientId);
                if (serviceRequest == null)
                    return NotFound("Default service request not found for this client");

                return Ok(serviceRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default service request for client: {ClientId}", clientId);
                return StatusCode(500, "An error occurred while retrieving the default service request");
            }
        }

        /// <summary>
        /// Get SME recommendations for a service request (sorted by score, workload, etc.)
        /// </summary>
        [HttpGet("{id}/sme-recommendations")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<List<SmeRecommendationDto>>> GetSmeRecommendations(int id, [FromQuery] string? specialization = null)
        {
            try
            {
                var recommendations = await _assignmentLifecycleService.GetSmeRecommendationsAsync(id, specialization);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SME recommendations for service request: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving SME recommendations");
            }
        }

        /// <summary>
        /// SME accepts an assignment
        /// </summary>
        [HttpPost("assignments/{assignmentId}/accept")]
        [Authorize(Roles = "Doctor,Attorney,SME")]
        public async Task<ActionResult> AcceptAssignment(int assignmentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var accepted = await _assignmentLifecycleService.AcceptAssignmentAsync(assignmentId, currentUserId.Value);
                if (!accepted)
                    return BadRequest("Failed to accept assignment. The assignment may not exist or is not in the correct status.");

                return Ok(new { message = "Assignment accepted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting assignment: {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while accepting the assignment");
            }
        }

        /// <summary>
        /// SME rejects an assignment
        /// </summary>
        [HttpPost("assignments/{assignmentId}/reject")]
        [Authorize(Roles = "Doctor,Attorney,SME")]
        public async Task<ActionResult> RejectAssignment(int assignmentId, [FromBody] RejectAssignmentRequest request)
        {
            try
            {
                if (request.AssignmentId != assignmentId)
                    return BadRequest("Assignment ID mismatch");

                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var rejected = await _assignmentLifecycleService.RejectAssignmentAsync(assignmentId, currentUserId.Value, request.Reason, request.Notes);
                if (!rejected)
                    return BadRequest("Failed to reject assignment. The assignment may not exist or is not in the correct status.");

                return Ok(new { message = "Assignment rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting assignment: {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while rejecting the assignment");
            }
        }

        /// <summary>
        /// SME starts working on an assignment
        /// </summary>
        [HttpPost("assignments/{assignmentId}/start")]
        [Authorize(Roles = "Doctor,Attorney,SME")]
        public async Task<ActionResult> StartAssignment(int assignmentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var started = await _assignmentLifecycleService.StartAssignmentAsync(assignmentId, currentUserId.Value);
                if (!started)
                    return BadRequest("Failed to start assignment. The assignment may not exist or is not in the correct status.");

                return Ok(new { message = "Assignment started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting assignment: {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while starting the assignment");
            }
        }

        /// <summary>
        /// SME completes an assignment
        /// </summary>
        [HttpPost("assignments/{assignmentId}/complete")]
        [Authorize(Roles = "Doctor,Attorney,SME")]
        public async Task<ActionResult> CompleteAssignment(int assignmentId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var completed = await _assignmentLifecycleService.CompleteAssignmentAsync(assignmentId, currentUserId.Value);
                if (!completed)
                    return BadRequest("Failed to complete assignment. The assignment may not exist or is not in the correct status.");

                return Ok(new { message = "Assignment completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing assignment: {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while completing the assignment");
            }
        }

        /// <summary>
        /// Update assignment status (for coordinators/admins)
        /// </summary>
        [HttpPut("assignments/{assignmentId}/status")]
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult> UpdateAssignmentStatus(int assignmentId, [FromBody] UpdateAssignmentStatusRequest request)
        {
            try
            {
                if (request.AssignmentId != assignmentId)
                    return BadRequest("Assignment ID mismatch");

                var updated = await _assignmentLifecycleService.UpdateAssignmentStatusAsync(
                    assignmentId, 
                    request.Status, 
                    request.OutcomeReason, 
                    request.ResponsibilityParty, 
                    request.Notes);

                if (!updated)
                    return BadRequest("Failed to update assignment status. The assignment may not exist.");

                return Ok(new { message = "Assignment status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assignment status: {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while updating the assignment status");
            }
        }

        /// <summary>
        /// Admin-only: Override assignment status (for correcting mistakes, e.g., SME marked complete but client reports incomplete)
        /// </summary>
        [HttpPut("assignments/{assignmentId}/status/admin-override")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AdminOverrideAssignmentStatus(int assignmentId, [FromBody] UpdateAssignmentStatusRequest request)
        {
            try
            {
                if (request.AssignmentId != assignmentId)
                    return BadRequest("Assignment ID mismatch");

                var currentUserId = GetCurrentUserId();
                var updated = await _assignmentLifecycleService.AdminOverrideAssignmentStatusAsync(
                    assignmentId, 
                    request.Status, 
                    request.OutcomeReason, 
                    request.ResponsibilityParty, 
                    request.Notes,
                    currentUserId);

                if (!updated)
                    return BadRequest("Failed to update assignment status. The assignment may not exist.");

                _logger.LogWarning("Admin {AdminUserId} overrode assignment {AssignmentId} status to {Status}. Notes: {Notes}", 
                    currentUserId, assignmentId, request.Status, request.Notes);

                return Ok(new { message = "Assignment status overridden successfully by admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error overriding assignment status: {AssignmentId}", assignmentId);
                return StatusCode(500, "An error occurred while overriding the assignment status");
            }
        }

        /// <summary>
        /// Get current SME score
        /// </summary>
        [HttpGet("sme-score")]
        [Authorize(Roles = "Doctor,Attorney,SME")]
        public async Task<ActionResult<int>> GetMySmeScore()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized();

                var score = await _assignmentLifecycleService.GetSmeScoreAsync(currentUserId.Value);
                return Ok(new { score });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting SME score");
                return StatusCode(500, "An error occurred while retrieving the SME score");
            }
        }

        /// <summary>
        /// Get billable assignments for billing reports
        /// </summary>
        [HttpGet("billing/assignments")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<BillableAssignmentDto>>> GetBillableAssignments(
            [FromQuery] int? smeUserId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Get billable assignments that are Ready to be invoiced
                // Include ACCEPTED, INPROGRESS, and COMPLETED assignments that haven't been invoiced yet (BillingStatus = Ready)
                // Billing starts when assignment is accepted (per user request)
                var query = _context.ServiceRequestAssignments
                    .Include(a => a.ServiceRequest)
                        .ThenInclude(sr => sr.Client)
                    .Include(a => a.ServiceRequest)
                        .ThenInclude(sr => sr.Expertises)
                    .Include(a => a.SmeUser)
                        .ThenInclude(u => u.Company)
                    .Where(a => a.IsActive && 
                        a.BillingStatus == BillingStatus.Ready.ToString() &&
                        a.InvoiceId == null && // Not yet invoiced
                        (a.Status == AssignmentStatus.Accepted.ToString() || 
                         a.Status == AssignmentStatus.InProgress.ToString() || 
                         a.Status == AssignmentStatus.Completed.ToString()) && // Accepted, InProgress, or Completed assignments are ready to bill
                        a.IsBillable && // Must be marked as billable
                        // PrimaryExpertiseId validation: must be set OR SR must have exactly 1 expertise (for auto-detection)
                        (a.ServiceRequest.PrimaryExpertiseId.HasValue || 
                         a.ServiceRequest.Expertises.Count == 1)); // Allow if PrimaryExpertiseId is set OR exactly 1 expertise (auto-detect)

                if (smeUserId.HasValue)
                    query = query.Where(a => a.SmeUserId == smeUserId.Value);

                // Date filtering: Use StartedAt if available, otherwise use AssignedAt
                // This ensures we capture all billable assignments in the date range
                if (startDate.HasValue)
                {
                    query = query.Where(a => 
                        (a.StartedAt.HasValue && a.StartedAt >= startDate.Value) ||
                        (!a.StartedAt.HasValue && a.AssignedAt >= startDate.Value));
                }

                if (endDate.HasValue)
                {
                    // Include the full end date (set to end of day)
                    var endDateTime = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(a => 
                        (a.StartedAt.HasValue && a.StartedAt <= endDateTime) ||
                        (!a.StartedAt.HasValue && a.AssignedAt <= endDateTime));
                }

                var assignments = await query
                    .OrderByDescending(a => a.StartedAt ?? a.AssignedAt)
                    .ToListAsync();

                _logger.LogInformation(
                    "Billing query: Found {Count} billable assignments. Filters: SME={SmeUserId}, StartDate={StartDate}, EndDate={EndDate}",
                    assignments.Count, smeUserId, startDate, endDate);

                var result = assignments.Select(a => new BillableAssignmentDto
                {
                    AssignmentId = a.Id,
                    ServiceRequestId = a.ServiceRequestId,
                    ServiceRequestTitle = a.ServiceRequest.Title,
                    ClientId = a.ServiceRequest.ClientId,
                    ClientName = $"{a.ServiceRequest.Client.FirstName} {a.ServiceRequest.Client.LastName}",
                    SmeUserId = a.SmeUserId,
                    SmeUserName = $"{a.SmeUser.FirstName} {a.SmeUser.LastName}",
                    SmeCompany = a.SmeUser.Company != null ? a.SmeUser.Company.Name : null, // Get company name from Company navigation property
                    Status = a.Status,
                    StartedAt = a.StartedAt,
                    CompletedAt = a.CompletedAt,
                    AssignedAt = a.AssignedAt,
                    IsBillable = a.IsBillable,
                    BillingStatus = a.BillingStatus ?? "NotBillable",
                    InvoiceId = a.InvoiceId,
                    BilledAt = a.BilledAt,
                    PaidAt = a.PaidAt,
                    DaysToComplete = a.CompletedAt.HasValue && a.StartedAt.HasValue
                        ? (int?)(a.CompletedAt.Value - a.StartedAt.Value).TotalDays
                        : null
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billable assignments");
                return StatusCode(500, "An error occurred while retrieving billable assignments");
            }
        }
    }
}

