using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;

namespace SM_MentalHealthApp.Server
{
    public class SeedProgram
    {
        public static async Task Main(string[] args)
        {
            var connectionString = "Server=localhost;Database=mentalhealthdb;User=root;Password=;";

            var options = new DbContextOptionsBuilder<JournalDbContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
                .Options;

            using var context = new JournalDbContext(options);

            try
            {
                await SeedContentTypes.SeedAsync(context);
                Console.WriteLine("Seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during seeding: {ex.Message}");
            }
        }
    }
}
