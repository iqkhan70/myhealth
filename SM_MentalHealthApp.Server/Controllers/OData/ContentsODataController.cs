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
    [Route("odata/Contents")]
    public class ContentsODataController : ODataController
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<ContentsODataController> _logger;
        private readonly IServiceRequestService _serviceRequestService;

        public ContentsODataController(
            JournalDbContext context,
            ILogger<ContentsODataController> logger,
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
        public IQueryable<ContentItem> Get(ODataQueryOptions<ContentItem> queryOptions)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                var query = _context.Contents
                    .Include(c => c.Patient)
                    .Include(c => c.AddedByUser)
                    .Include(c => c.IgnoredByDoctor)
                    .Include(c => c.ServiceRequest)
                    .Where(c => c.IsActive)
                    .AsQueryable();

                // Role-based filtering
                if (currentRoleId.HasValue && currentUserId.HasValue)
                {
                    if (currentRoleId.Value == 1) // Patient
                    {
                        // Patients see only their own content
                        query = query.Where(c => c.PatientId == currentUserId.Value);
                    }
                    else if (currentRoleId.Value == 2 || currentRoleId.Value == 4 || currentRoleId.Value == 5) // Doctor, Coordinator, or Attorney
                    {
                        // Get ServiceRequest IDs assigned to this SME
                        var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                        
                        // Filter content to only ServiceRequests assigned to this SME
                        query = query.Where(c => 
                            (c.ServiceRequestId.HasValue && serviceRequestIds.Contains(c.ServiceRequestId.Value)) ||
                            (!c.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                                sr.ClientId == c.PatientId && 
                                sr.Title == "General" && 
                                sr.IsActive && 
                                serviceRequestIds.Contains(sr.Id)
                            ))
                        );
                    }
                    // Admin (3) sees all content
                }

                return query;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ContentsODataController.Get");
                throw;
            }
        }

        [EnableQuery]
        [HttpGet("{key}")]
        public SingleResult<ContentItem> Get([FromRoute] int key)
        {
            var currentUserId = GetCurrentUserId();
            var currentRoleId = GetCurrentRoleId();

            var query = _context.Contents
                .Include(c => c.Patient)
                .Include(c => c.AddedByUser)
                .Include(c => c.IgnoredByDoctor)
                .Include(c => c.ServiceRequest)
                .Where(c => c.Id == key && c.IsActive);

            // Role-based filtering
            if (currentRoleId.HasValue && currentUserId.HasValue)
            {
                if (currentRoleId.Value == 1) // Patient
                {
                    query = query.Where(c => c.PatientId == currentUserId.Value);
                }
                else if (currentRoleId.Value == 2 || currentRoleId.Value == 4 || currentRoleId.Value == 5) // Doctor, Coordinator, or Attorney
                {
                    // Get ServiceRequest IDs assigned to this SME
                    var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                    
                    // Filter content to only ServiceRequests assigned to this SME
                    query = query.Where(c => 
                        (c.ServiceRequestId.HasValue && serviceRequestIds.Contains(c.ServiceRequestId.Value)) ||
                        (!c.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                            sr.ClientId == c.PatientId && 
                            sr.Title == "General" && 
                            sr.IsActive && 
                            serviceRequestIds.Contains(sr.Id)
                        ))
                    );
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

