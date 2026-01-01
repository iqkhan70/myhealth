namespace SM_MentalHealthApp.Shared.Constants;

/// <summary>
/// Role constants for the application
/// </summary>
public static class Roles
{
    public const int Patient = 1;
    public const int Doctor = 2;
    public const int Admin = 3;
    public const int Coordinator = 4;
    public const int Attorney = 5;
    public const int Sme = 6;
    
    /// <summary>
    /// Gets the role name for a given role ID
    /// </summary>
    public static string GetRoleName(int roleId) => roleId switch
    {
        Patient => "Patient",
        Doctor => "Doctor",
        Admin => "Admin",
        Coordinator => "Coordinator",
        Attorney => "Attorney",
        Sme => "SME",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Checks if a role ID is valid
    /// </summary>
    public static bool IsValid(int roleId) => roleId >= Patient && roleId <= Sme;
}

