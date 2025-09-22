using System;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;

class Program
{
    static void Main()
    {
        string connectionString = "server=localhost;port=3306;database=mentalhealthdb;user=root;password=UthmanBasima70";

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Check what's in the database
                string selectQuery = "SELECT Id, FirstName, LastName, Email, PasswordHash FROM Users WHERE Email = 'john@doe.com'";
                using (var command = new MySqlCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Console.WriteLine($"User: {reader.GetString("FirstName")} {reader.GetString("LastName")}");
                        Console.WriteLine($"Email: {reader.GetString("Email")}");
                        Console.WriteLine($"Stored Hash: {reader.GetString("PasswordHash")}");

                        // Generate hash for "demo123" to compare
                        string testPassword = "demo123";
                        string generatedHash = HashPassword(testPassword);
                        Console.WriteLine($"Generated Hash for 'demo123': {generatedHash}");
                        Console.WriteLine($"Hashes match: {reader.GetString("PasswordHash") == generatedHash}");
                    }
                    else
                    {
                        Console.WriteLine("User not found!");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string HashPassword(string password)
    {
        using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256))
        {
            byte[] salt = rfc2898DeriveBytes.Salt;
            byte[] hash = rfc2898DeriveBytes.GetBytes(32);
            byte[] hashBytes = new byte[48];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
