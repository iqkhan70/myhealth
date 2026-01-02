using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers.OData
{
    [Authorize]
    [Route("odata/UserAssignments")]
    public class UserAssignmentsODataController : ODataController
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<UserAssignmentsODataController> _logger;

        public UserAssignmentsODataController(
            JournalDbContext context,
            ILogger<UserAssignmentsODataController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [EnableQuery(
            MaxExpansionDepth = 2,
            MaxAnyAllExpressionDepth = 3,
            AllowedArithmeticOperators = AllowedArithmeticOperators.All,
            AllowedFunctions = AllowedFunctions.AllFunctions,
            AllowedLogicalOperators = AllowedLogicalOperators.All,
            AllowedQueryOptions = AllowedQueryOptions.All)]
        [HttpGet]
        public IQueryable<UserAssignment> Get(ODataQueryOptions<UserAssignment> queryOptions)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                var query = _context.UserAssignments
                    .Include(ua => ua.Assigner)
                    .Include(ua => ua.Assignee)
                    .Where(ua => ua.IsActive)
                    .AsQueryable();

                // Role-based filtering:
                // - Admins and Coordinators see all assignments
                // - Doctors see only their own assignments (as assigner)
                // - Patients see only their own assignments (as assignee)
                // - Attorneys see assignments for their assigned patients
                if (currentRoleId.HasValue && currentUserId.HasValue)
                {
                    if (currentRoleId.Value == 2) // Doctor
                    {
                        query = query.Where(ua => ua.AssignerId == currentUserId.Value);
                    }
                    else if (currentRoleId.Value == 1) // Patient
                    {
                        query = query.Where(ua => ua.AssigneeId == currentUserId.Value);
                    }
                    else if (currentRoleId.Value == 5 || currentRoleId.Value == 6) // Attorney or SME
                    {
                        // Attorneys see assignments for their assigned patients
                        var assignedPatientIds = _context.UserAssignments
                            .Where(ua => ua.AssignerId == currentUserId.Value && ua.IsActive)
                            .Select(ua => ua.AssigneeId);

                        query = query.Where(ua => assignedPatientIds.Contains(ua.AssigneeId));
                    }
                    // Admins (3) and Coordinators (4) see all assignments - no filtering needed
                }

                return query;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserAssignments OData query: {Message}", ex.Message);
                throw;
            }
        }

        [EnableQuery]
        [HttpGet("({assignerId:int},{assigneeId:int})")]
        public SingleResult<UserAssignment> Get([FromODataUri] int assignerId, [FromODataUri] int assigneeId)
        {
            try
            {
                var query = _context.UserAssignments
                    .Include(ua => ua.Assigner)
                    .Include(ua => ua.Assignee)
                    .Where(ua => ua.AssignerId == assignerId && ua.AssigneeId == assigneeId && ua.IsActive);

                return SingleResult.Create(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user assignment {AssignerId}-{AssigneeId}", assignerId, assigneeId);
                throw;
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? 
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

