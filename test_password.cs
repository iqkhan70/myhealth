using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        string password = "Password123!";
        string storedHash = "A8WLh4qdHNE5vmejYE07wywTtFQkt2iD0buU5tUO8tEgal9WQ+IY2Ij4CJX6fHzeU7F5z3EWqHxT2CkWwWKG0w==";
        
        Console.WriteLine($"Testing password: {password}");
        Console.WriteLine($"Stored hash: {storedHash}");
        
        bool isValid = VerifyPassword(password, storedHash);
        Console.WriteLine($"Password verification result: {isValid}");
        
        // Also test generating a new hash
        string newHash = HashPassword(password);
        Console.WriteLine($"New hash: {newHash}");
        Console.WriteLine($"New hash verification: {VerifyPassword(password, newHash)}");
    }
    
    private static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            var hashBytes = Convert.FromBase64String(passwordHash);
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
        catch
        {
            return false;
        }
    }
    
    private static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[32];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var hashBytes = new byte[64];
        Array.Copy(salt, 0, hashBytes, 0, 32);
        Array.Copy(hash, 0, hashBytes, 32, 32);

        return Convert.ToBase64String(hashBytes);
    }
}
