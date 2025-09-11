using System.Security.Cryptography;

Console.WriteLine("Password Verification Test");
Console.WriteLine("=========================");

// Get the stored hash from database
string storedHash = "0Ft5RcSM6H9GOpKJDf/ybPqgiV2/dQcs8S2vYzTKUjQBDBCa19rIJsh5pZXyguc6ZZIisirU38CGpx/c30nZ9A==";
string password = "demo123";

Console.WriteLine($"Stored Hash: {storedHash}");
Console.WriteLine($"Password to verify: {password}");
Console.WriteLine();

// Verify password
bool isValid = VerifyPassword(password, storedHash);
Console.WriteLine($"Password is valid: {isValid}");

// Also test with a new hash generation to see if there's a difference
string newHash = HashPassword(password);
Console.WriteLine($"New hash for same password: {newHash}");
Console.WriteLine($"Hashes are equal: {storedHash == newHash}");

static bool VerifyPassword(string password, string storedHash)
{
    try
    {
        var hashBytes = Convert.FromBase64String(storedHash);
        var salt = new byte[32];
        Array.Copy(hashBytes, 0, salt, 0, 32);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        
        for (int i = 0; i < 32; i++)
        {
            if (hashBytes[i + 32] != hash[i])
                return false;
        }
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error verifying password: {ex.Message}");
        return false;
    }
}

static string HashPassword(string password)
{
    using var rng = RandomNumberGenerator.Create();
    var salt = new byte[32];
    rng.GetBytes(salt);
    
    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
    var hash = pbkdf2.GetBytes(32);
    
    var combined = new byte[64];
    Array.Copy(salt, 0, combined, 0, 32);
    Array.Copy(hash, 0, combined, 32, 32);
    
    return Convert.ToBase64String(combined);
}
