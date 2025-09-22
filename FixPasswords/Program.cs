using System;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;

class Program
{
    static void Main()
    {
        string connectionString = "server=localhost;port=3306;database=mentalhealthdb;user=root;password=UthmanBasima70";
        string newPassword = "demo123";
        string hashedPassword = HashPassword(newPassword);

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Update all users' passwords
                string updateQuery = "UPDATE Users SET PasswordHash = @passwordHash, MustChangePassword = 0";
                using (var command = new MySqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@passwordHash", hashedPassword);
                    int rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"Updated {rowsAffected} users' passwords to 'demo123'");
                }

                // Show updated users
                string selectQuery = "SELECT Id, FirstName, LastName, Email, RoleId FROM Users";
                using (var command = new MySqlCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    Console.WriteLine("\nCurrent users:");
                    Console.WriteLine("ID | Name | Email | Role");
                    Console.WriteLine("---|------|-------|-----");
                    while (reader.Read())
                    {
                        string role = reader.GetInt32("RoleId") switch
                        {
                            1 => "Admin",
                            2 => "Doctor",
                            3 => "Patient",
                            _ => "Unknown"
                        };
                        Console.WriteLine($"{reader.GetInt32("Id")} | {reader.GetString("FirstName")} {reader.GetString("LastName")} | {reader.GetString("Email")} | {role}");
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
