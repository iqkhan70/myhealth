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

                // Check if the filter or orderBy contains DateOfBirth (encrypted PII that needs special handling)
                var filterRaw = queryOptions?.Filter?.RawValue;
                var orderBy = queryOptions?.OrderBy;
                var hasDateOfBirthFilter = !string.IsNullOrEmpty(filterRaw) && filterRaw.Contains("DateOfBirth", StringComparison.OrdinalIgnoreCase);
                
                // Check if orderBy contains DateOfBirth
                bool hasDateOfBirthSort = false;
                string? orderByProperty = null;
                bool isDescending = false;
                if (orderBy != null && orderBy.OrderByClause != null)
                {
                    // Extract the actual property name from the OData expression
                    var expression = orderBy.OrderByClause.Expression;
                    if (expression is Microsoft.OData.UriParser.SingleValuePropertyAccessNode propertyNode)
                    {
                        orderByProperty = propertyNode.Property.Name;
                    }
                    else
                    {
                        // Fallback to ToString() if it's not a property access node
                        orderByProperty = expression.ToString();
                    }
                    
                    hasDateOfBirthSort = orderByProperty != null && orderByProperty.Equals("DateOfBirth", StringComparison.OrdinalIgnoreCase);
                    isDescending = orderBy.OrderByClause.Direction == Microsoft.OData.UriParser.OrderByDirection.Descending;
                    _logger.LogInformation($"[UsersODataController] OrderBy detected: {orderByProperty}, hasDateOfBirthSort: {hasDateOfBirthSort}, isDescending: {isDescending}");
                }

                if (hasDateOfBirthFilter || hasDateOfBirthSort)
                {
                    // DateOfBirth filtering/sorting requires special handling:
                    // 1. Apply all non-DateOfBirth filters via OData (if no DateOfBirth filter)
                    // 2. Materialize the query
                    // 3. Decrypt DateOfBirth
                    // 4. Apply DateOfBirth filter in memory (if present)
                    // 5. Apply DateOfBirth sorting in memory (if present)
                    // 6. Apply pagination

                    _logger.LogInformation("DateOfBirth filter or sort detected, applying special handling for encrypted PII");

                    // Materialize the query (OData can't sort/filter encrypted DateOfBirth)
                    var allUsers = query.ToList();

                    // Decrypt DateOfBirth for all users
                    UserEncryptionHelper.DecryptUserData(allUsers, _encryptionService);

                    // Apply DateOfBirth filter if present
                    IQueryable<User> filteredUsers = allUsers.AsQueryable();
                    if (hasDateOfBirthFilter)
                    {
                        filteredUsers = ApplyDateOfBirthFilter(filteredUsers, filterRaw!);
                    }
                    else
                    {
                        // No DateOfBirth filter - apply other OData filters if any
                        try
                        {
                            if (queryOptions != null)
                            {
                                // Create a temporary query options without orderBy and skip/top to apply filters
                                var filterOnlyOptions = new ODataQueryOptions<User>(queryOptions.Context, queryOptions.Request);
                                // Note: We can't easily exclude DateOfBirth from filter, so we'll apply all filters
                                // and let ApplyDateOfBirthFilter handle DateOfBirth if present
                                filteredUsers = filterOnlyOptions.ApplyTo(filteredUsers) as IQueryable<User> ?? filteredUsers;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error applying OData filters, using unfiltered results");
                        }
                    }

                    // Apply DateOfBirth sorting if present
                    if (hasDateOfBirthSort && !string.IsNullOrEmpty(orderByProperty))
                    {
                        filteredUsers = isDescending
                            ? filteredUsers.OrderByDescending(u => u.DateOfBirth)
                            : filteredUsers.OrderBy(u => u.DateOfBirth);
                    }
                    else if (orderBy != null && orderBy.OrderByClause != null && !hasDateOfBirthSort)
                    {
                        // Apply other sorting (non-DateOfBirth) - this should work since DateOfBirth is already decrypted
                        try
                        {
                            var sortQueryOptions = new ODataQueryOptions<User>(queryOptions.Context, queryOptions.Request);
                            filteredUsers = sortQueryOptions.ApplyTo(filteredUsers) as IQueryable<User> ?? filteredUsers;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error applying OData sorting, using unsorted results");
                        }
                    }

                    // Apply pagination
                    var skip = queryOptions?.Skip?.Value ?? 0;
                    var top = queryOptions?.Top?.Value ?? 1000;
                    filteredUsers = filteredUsers.Skip(skip).Take(top);

                    // IMPORTANT: Materialize the query to prevent OData from trying to translate DateOfBirth to SQL
                    // This ensures that all DateOfBirth operations happen in memory after decryption
                    _logger.LogInformation($"[UsersODataController] Materializing query - hasDateOfBirthSort: {hasDateOfBirthSort}, hasDateOfBirthFilter: {hasDateOfBirthFilter}");
                    var materializedUsers = filteredUsers.ToList();
                    _logger.LogInformation($"[UsersODataController] Materialized {materializedUsers.Count} users");
                    
                    // Return as IQueryable from the materialized list - OData will handle serialization
                    // The filter should detect this is already materialized and skip enumeration
                    return materializedUsers.AsQueryable();
                }

                // No DateOfBirth filter or sort - use normal flow with DecryptingQueryable
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
