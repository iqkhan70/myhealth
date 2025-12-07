using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Controllers.OData
{
    [Authorize]
    [Route("odata/Appointments")]
    public class AppointmentsODataController : ODataController
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<AppointmentsODataController> _logger;

        public AppointmentsODataController(
            JournalDbContext context,
            ILogger<AppointmentsODataController> logger)
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
        public IQueryable<Appointment> Get(ODataQueryOptions<Appointment> queryOptions)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                var query = _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .Include(a => a.CreatedByUser)
                    .Where(a => a.IsActive)
                    .AsQueryable();

                // Role-based filtering
                if (currentRoleId.HasValue && currentUserId.HasValue)
                {
                    if (currentRoleId.Value == 2) // Doctor
                    {
                        // Doctors see only their own appointments
                        query = query.Where(a => a.DoctorId == currentUserId.Value);
                    }
                    else if (currentRoleId.Value == 1) // Patient
                    {
                        // Patients see only their own appointments
                        query = query.Where(a => a.PatientId == currentUserId.Value);
                    }
                    // Admin (3), Coordinator (4), and Attorney (5) see all appointments
                }

                return query;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AppointmentsODataController.Get");
                throw;
            }
        }

        [EnableQuery]
        [HttpGet("{key}")]
        public SingleResult<Appointment> Get([FromRoute] int key)
        {
            var currentUserId = GetCurrentUserId();
            var currentRoleId = GetCurrentRoleId();

            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.CreatedByUser)
                .Where(a => a.Id == key && a.IsActive);

            // Role-based filtering
            if (currentRoleId.HasValue && currentUserId.HasValue)
            {
                if (currentRoleId.Value == 2) // Doctor
                {
                    query = query.Where(a => a.DoctorId == currentUserId.Value);
                }
                else if (currentRoleId.Value == 1) // Patient
                {
                    query = query.Where(a => a.PatientId == currentUserId.Value);
                }
            }

            return SingleResult.Create(query);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }

        private int? GetCurrentRoleId()
        {
            var roleIdClaim = User.FindFirst("roleId")?.Value;
            if (int.TryParse(roleIdClaim, out int roleId))
                return roleId;
            return null;
        }
    }
}

