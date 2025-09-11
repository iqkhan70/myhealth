using System.Security.Cryptography;
using System.Text;

Console.WriteLine("Password Hash Generator");
Console.WriteLine("======================");

// Generate hash for demo@mentalhealth.app
string password1 = "demo123";
string hash1 = HashPassword(password1);
Console.WriteLine($"Password: {password1}");
Console.WriteLine($"Hash: {hash1}");
Console.WriteLine();

// Generate hash for john.smith@example.com
string password2 = "john123";
string hash2 = HashPassword(password2);
Console.WriteLine($"Password: {password2}");
Console.WriteLine($"Hash: {hash2}");
Console.WriteLine();

Console.WriteLine("SQL Commands to update database:");
Console.WriteLine($"UPDATE Patients SET PasswordHash = '{hash1}' WHERE Email = 'demo@mentalhealth.app';");
Console.WriteLine($"UPDATE Patients SET PasswordHash = '{hash2}' WHERE Email = 'john.smith@example.com';");

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
