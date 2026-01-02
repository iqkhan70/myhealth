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
    [Route("odata/Appointments")]
    public class AppointmentsODataController : ODataController
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<AppointmentsODataController> _logger;
        private readonly IServiceRequestService _serviceRequestService;

        public AppointmentsODataController(
            JournalDbContext context,
            ILogger<AppointmentsODataController> logger,
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
                        // Doctors see only appointments for their assigned ServiceRequests
                        var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                        
                        query = query.Where(a => 
                            a.DoctorId == currentUserId.Value && 
                            ((a.ServiceRequestId.HasValue && serviceRequestIds.Contains(a.ServiceRequestId.Value)) ||
                             (!a.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                                sr.ClientId == a.PatientId && 
                                sr.Title == "General" && 
                                sr.IsActive && 
                                serviceRequestIds.Contains(sr.Id)
                            )))
                        );
                    }
                    else if (currentRoleId.Value == 4) // Coordinator
                    {
                        // Coordinators see only appointments for their assigned ServiceRequests
                        var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                        
                        query = query.Where(a => 
                            (a.ServiceRequestId.HasValue && serviceRequestIds.Contains(a.ServiceRequestId.Value)) ||
                            (!a.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                                sr.ClientId == a.PatientId && 
                                sr.Title == "General" && 
                                sr.IsActive && 
                                serviceRequestIds.Contains(sr.Id)
                            ))
                        );
                    }
                    else if (currentRoleId.Value == 1) // Patient
                    {
                        // Patients see only their own appointments
                        query = query.Where(a => a.PatientId == currentUserId.Value);
                    }
                    else if (currentRoleId.Value == 5 || currentRoleId.Value == 6) // Attorney or SME
                    {
                        // Attorneys see appointments for their assigned ServiceRequests
                        var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                        
                        query = query.Where(a => 
                            (a.ServiceRequestId.HasValue && serviceRequestIds.Contains(a.ServiceRequestId.Value)) ||
                            (!a.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                                sr.ClientId == a.PatientId && 
                                sr.Title == "General" && 
                                sr.IsActive && 
                                serviceRequestIds.Contains(sr.Id)
                            ))
                        );
                    }
                    // Admin (3) sees all appointments - no filtering needed
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
                    // Doctors see only appointments for their assigned ServiceRequests
                    var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                    
                    query = query.Where(a => 
                        a.DoctorId == currentUserId.Value && 
                        ((a.ServiceRequestId.HasValue && serviceRequestIds.Contains(a.ServiceRequestId.Value)) ||
                         (!a.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                            sr.ClientId == a.PatientId && 
                            sr.Title == "General" && 
                            sr.IsActive && 
                            serviceRequestIds.Contains(sr.Id)
                        )))
                    );
                }
                else if (currentRoleId.Value == 4) // Coordinator
                {
                    // Coordinators see only appointments for their assigned ServiceRequests
                    var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                    
                    query = query.Where(a => 
                        (a.ServiceRequestId.HasValue && serviceRequestIds.Contains(a.ServiceRequestId.Value)) ||
                        (!a.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                            sr.ClientId == a.PatientId && 
                            sr.Title == "General" && 
                            sr.IsActive && 
                            serviceRequestIds.Contains(sr.Id)
                        ))
                    );
                }
                else if (currentRoleId.Value == 1) // Patient
                {
                    query = query.Where(a => a.PatientId == currentUserId.Value);
                }
                else if (currentRoleId.Value == 5 || currentRoleId.Value == 6) // Attorney or SME
                {
                    // Attorneys see appointments for their assigned ServiceRequests
                    var serviceRequestIds = _serviceRequestService.GetServiceRequestIdsForSmeAsync(currentUserId.Value).Result;
                    
                    query = query.Where(a => 
                        (a.ServiceRequestId.HasValue && serviceRequestIds.Contains(a.ServiceRequestId.Value)) ||
                        (!a.ServiceRequestId.HasValue && _context.ServiceRequests.Any(sr => 
                            sr.ClientId == a.PatientId && 
                            sr.Title == "General" && 
                            sr.IsActive && 
                            serviceRequestIds.Contains(sr.Id)
                        ))
                    );
                }
                // Admin (3) sees all appointments - no filtering needed
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

