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
        private readonly ILogger<ServiceRequestController> _logger;
        private readonly JournalDbContext _context;

        public ServiceRequestController(
            IServiceRequestService serviceRequestService,
            ILogger<ServiceRequestController> logger,
            JournalDbContext context)
        {
            _serviceRequestService = serviceRequestService;
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
                if ((currentRoleId == Roles.Doctor || currentRoleId == Roles.Attorney) && currentUserId.HasValue)
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
                if ((currentRoleId == Roles.Doctor || currentRoleId == Roles.Attorney) && currentUserId.HasValue)
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
        [Authorize(Roles = "Admin,Coordinator")]
        public async Task<ActionResult<ServiceRequestDto>> CreateServiceRequest([FromBody] CreateServiceRequestRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
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
                    return BadRequest("Failed to assign SME. The SME may already be assigned or the service request/SME may not exist.");

                return Ok(new { message = "SME assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning SME to service request: {Id}", id);
                return StatusCode(500, "An error occurred while assigning the SME");
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
        [Authorize(Roles = "Doctor,Attorney")]
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
    }
}

