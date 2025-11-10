using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SM_MentalHealthApp.Server.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers
/// </summary>
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Gets the current user ID from the JWT token claims
    /// </summary>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : null;
    }
    
    /// <summary>
    /// Gets the current user's role ID from the JWT token claims
    /// </summary>
    protected int? GetCurrentRoleId()
    {
        var roleIdClaim = User.FindFirst("roleId")?.Value;
        return int.TryParse(roleIdClaim, out int roleId) ? roleId : null;
    }
    
    /// <summary>
    /// Gets the current user's role name from the JWT token claims
    /// </summary>
    protected string? GetCurrentRoleName()
    {
        return User.FindFirst("roleName")?.Value;
    }
    
    /// <summary>
    /// Gets the current user's email from the JWT token claims
    /// </summary>
    protected string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("email")?.Value;
    }
    
    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    protected bool IsCurrentUserInRole(int roleId)
    {
        var currentRoleId = GetCurrentRoleId();
        return currentRoleId.HasValue && currentRoleId.Value == roleId;
    }
    
    /// <summary>
    /// Validates that the current user is authenticated and returns the user ID
    /// </summary>
    protected ActionResult<int> RequireAuthenticatedUser()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized("Invalid or missing authentication token");
        }
        return userId.Value;
    }
}

