using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Server.Helpers;
using SM_MentalHealthApp.Shared;
using System.Text.RegularExpressions;

namespace SM_MentalHealthApp.Server.Controllers.OData
{
    [Authorize]
    [Route("odata/Users")]
    public class UsersODataController : ODataController
    {
        private readonly JournalDbContext _context;
        private readonly IPiiEncryptionService _encryptionService;
        private readonly ILogger<UsersODataController> _logger;

        public UsersODataController(
            JournalDbContext context,
            IPiiEncryptionService encryptionService,
            ILogger<UsersODataController> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
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
        public IQueryable<User> Get(ODataQueryOptions<User> queryOptions)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentRoleId = GetCurrentRoleId();

                var query = _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.IsActive)
                    .AsQueryable();

                // For doctors, coordinators, and attorneys: filter to assigned patients only
                if (currentRoleId.HasValue && currentUserId.HasValue)
                {
                    if (currentRoleId.Value == 2 ||   // Doctor
                        currentRoleId.Value == 4 ||   // Coordinator
                        currentRoleId.Value == 5)     // Attorney
                    {
                        var assignedPatientIds = _context.UserAssignments
                            .Where(ua => ua.AssignerId == currentUserId.Value && ua.IsActive)
                            .Select(ua => ua.AssigneeId);

                        query = query.Where(u =>
                            u.RoleId == 1 &&                  // Patient
                            assignedPatientIds.Contains(u.Id));
                    }
                }

                // Check if the filter contains DateOfBirth (encrypted PII that needs special handling)
                var filterRaw = queryOptions?.Filter?.RawValue;
                if (!string.IsNullOrEmpty(filterRaw) && filterRaw.Contains("DateOfBirth", StringComparison.OrdinalIgnoreCase))
                {
                    // DateOfBirth filtering requires special handling:
                    // 1. Apply all non-DateOfBirth filters via OData
                    // 2. Materialize the query
                    // 3. Decrypt DateOfBirth
                    // 4. Apply DateOfBirth filter in memory
                    // 5. Apply pagination/sorting in memory

                    _logger.LogInformation("DateOfBirth filter detected, applying special handling for encrypted PII");

                    // First, try to apply OData filters (excluding DateOfBirth which is not in EDM model)
                    // OData will reject DateOfBirth filters since it's excluded from EDM, so we need to handle it manually

                    // Materialize the query first (before OData tries to apply DateOfBirth filter)
                    var allUsers = query.ToList();

                    // Decrypt DateOfBirth for all users
                    UserEncryptionHelper.DecryptUserData(allUsers, _encryptionService);

                    // Parse and apply DateOfBirth filter manually
                    var filteredUsers = ApplyDateOfBirthFilter(allUsers.AsQueryable(), filterRaw);

                    // Apply pagination and sorting
                    var skip = queryOptions?.Skip?.Value ?? 0;
                    var top = queryOptions?.Top?.Value ?? 1000;
                    var orderBy = queryOptions?.OrderBy;

                    if (orderBy != null && orderBy.OrderByClause != null)
                    {
                        // Apply sorting (simplified - handle common cases)
                        var orderByProperty = orderBy.OrderByClause.Expression.ToString();
                        var isDescending = orderBy.OrderByClause.Direction == Microsoft.OData.UriParser.OrderByDirection.Descending;

                        if (orderByProperty == "DateOfBirth")
                        {
                            filteredUsers = isDescending
                                ? filteredUsers.OrderByDescending(u => u.DateOfBirth)
                                : filteredUsers.OrderBy(u => u.DateOfBirth);
                        }
                        // Add other sorting properties as needed
                    }

                    // Return as IQueryable - OData will apply pagination/sorting on the already-filtered data
                    // Note: OData won't try to apply the DateOfBirth filter again because it's already applied
                    return filteredUsers;
                }

                // No DateOfBirth filter - use normal flow with DecryptingQueryable
                return new DecryptingQueryable<User>(query, _encryptionService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Users OData query: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Applies DateOfBirth filter to an in-memory queryable after decryption
        /// </summary>
        private IQueryable<User> ApplyDateOfBirthFilter(IQueryable<User> users, string filterRaw)
        {
            // Parse the filter to extract DateOfBirth conditions
            // This is a simplified parser - handles common patterns like:
            // DateOfBirth eq '2001-12-07'
            // DateOfBirth gt '2001-12-07'
            // DateOfBirth ge '2001-12-07' and DateOfBirth lt '2002-01-01'

            try
            {
                // Extract DateOfBirth filter conditions using regex
                var dateOfBirthPattern = @"DateOfBirth\s+(eq|ne|gt|ge|lt|le)\s+['""]?(\d{4}-\d{2}-\d{2})['""]?";
                var matches = System.Text.RegularExpressions.Regex.Matches(filterRaw, dateOfBirthPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (matches.Count == 0)
                {
                    return users; // No DateOfBirth filter found
                }

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var op = match.Groups[1].Value.ToLower();
                    var dateStr = match.Groups[2].Value;

                    if (DateTime.TryParse(dateStr, out var filterDate))
                    {
                        // Normalize to date-only (remove time)
                        var filterDateOnly = filterDate.Date;

                        users = op switch
                        {
                            "eq" => users.Where(u => u.DateOfBirth.Date == filterDateOnly),
                            "ne" => users.Where(u => u.DateOfBirth.Date != filterDateOnly),
                            "gt" => users.Where(u => u.DateOfBirth.Date > filterDateOnly),
                            "ge" => users.Where(u => u.DateOfBirth.Date >= filterDateOnly),
                            "lt" => users.Where(u => u.DateOfBirth.Date < filterDateOnly),
                            "le" => users.Where(u => u.DateOfBirth.Date <= filterDateOnly),
                            _ => users
                        };
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying DateOfBirth filter: {Message}", ex.Message);
                return users; // Return unfiltered on error
            }
        }

        [EnableQuery]
        [HttpGet("({key:int})")]
        public SingleResult<User> Get([FromODataUri] int key)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Id == key && u.IsActive);

                // Wrap in DecryptingQueryable to decrypt PII
                var decryptingQuery = new DecryptingQueryable<User>(query, _encryptionService);
                return SingleResult.Create(decryptingQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {Id}", key);
                throw;
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst("userId");
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
        }

        private int? GetCurrentRoleId()
        {
            var roleIdClaim = User.FindFirst("RoleId") ?? User.FindFirst("roleId");
            return roleIdClaim != null && int.TryParse(roleIdClaim.Value, out var roleId) ? roleId : null;
        }
    }
}
