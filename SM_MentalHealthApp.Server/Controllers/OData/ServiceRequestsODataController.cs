using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Controllers.OData
{
    [Authorize]
    [Route("odata/ServiceRequests")]
    public class ServiceRequestsODataController : ODataController
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ServiceRequestsODataController> _logger;
        private readonly IServiceRequestService _serviceRequestService;

        public ServiceRequestsODataController(
            JournalDbContext context,
            ILogger<ServiceRequestsODataController> logger,
            IServiceRequestService serviceRequestService)
        {
            _context = context;
            _logger = logger;
            _serviceRequestService = serviceRequestService;
        }

        [EnableQuery(
            MaxExpansionDepth = 2,
            MaxAnyAllExpressionDepth = 3,
            AllowedArithmeticOperators = AllowedArithmeticOperators.All,
            AllowedFunctions = AllowedFunctions.AllFunctions,
            AllowedLogicalOperators = AllowedLogicalOperators.All,
            AllowedQueryOptions = AllowedQueryOptions.All)]
        [HttpGet]
        public IQueryable<ServiceRequest> Get(ODataQueryOptions<ServiceRequest> queryOptions)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                var query = _context.ServiceRequests
                    .Include(sr => sr.Client)
                    .Include(sr => sr.Assignments)
                        .ThenInclude(a => a.SmeUser)
                    .Include(sr => sr.Expertises)
                        .ThenInclude(e => e.Expertise)
                    .Include(sr => sr.PrimaryExpertise)
                    .Where(sr => sr.IsActive)
                    .AsQueryable();

                // Role-based filtering
                if (currentRoleId.HasValue && currentUserId.HasValue)
                {
                    if (currentRoleId.Value == 1) // Patient
                    {
                        // Patients see only their own service requests
                        query = query.Where(sr => sr.ClientId == currentUserId.Value);
                    }
                    else if (currentRoleId.Value == 2 || currentRoleId.Value == 5 || currentRoleId.Value == 6) // Doctor, Attorney, or SME
                    {
                        // Get ServiceRequest IDs assigned to this SME
                        var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                        query = query.Where(sr => serviceRequestIds.Contains(sr.Id));
                    }
                    // Admin (3) and Coordinator (4) see all service requests
                }

                return query;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ServiceRequestsODataController.Get");
                throw;
            }
        }

        [EnableQuery]
        [HttpGet("{key}")]
        public SingleResult<ServiceRequest> Get([FromRoute] int key)
        {
            var currentUserId = GetCurrentUserId();
            var currentRoleId = GetCurrentRoleId();

            var query = _context.ServiceRequests
                .Include(sr => sr.Client)
                .Include(sr => sr.Assignments)
                    .ThenInclude(a => a.SmeUser)
                .Where(sr => sr.Id == key && sr.IsActive);

            // Role-based filtering
            if (currentRoleId.HasValue && currentUserId.HasValue)
            {
                if (currentRoleId.Value == 1) // Patient
                {
                    query = query.Where(sr => sr.ClientId == currentUserId.Value);
                }
                else if (currentRoleId.Value == 2 || currentRoleId.Value == 5 || currentRoleId.Value == 6) // Doctor, Attorney, or SME
                {
                    // Get ServiceRequest IDs assigned to this SME
                    var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                    query = query.Where(sr => serviceRequestIds.Contains(sr.Id));
                }
            }

            return SingleResult.Create(query);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? 
                             User.FindFirst("UserId") ?? 
                             User.FindFirst("userId");
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
        }

        private int? GetCurrentRoleId()
        {
            var roleIdClaim = User.FindFirst("RoleId") ?? User.FindFirst("roleId");
            return roleIdClaim != null && int.TryParse(roleIdClaim.Value, out var roleId) ? roleId : null;
        }
    }
}

