using System;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

class Program
{
    static void Main()
    {
        string connectionString = "Server=localhost;Database=mental_health_app;Uid=root;Pwd=;";
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
        using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, 16, 10000))
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
