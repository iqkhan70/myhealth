using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server
{
    public class SeedContentTypesProgram
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
                // Check if ContentTypes already exist
                if (await context.ContentTypes.AnyAsync())
                {
                    Console.WriteLine("ContentTypes already seeded.");
                    return;
                }

                var contentTypes = new[]
                {
                    new ContentTypeModel
                    {
                        Name = "Document",
                        Description = "General document files (PDF, DOC, TXT, etc.)",
                        Icon = "📄",
                        IsActive = true,
                        SortOrder = 1,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ContentTypeModel
                    {
                        Name = "Image",
                        Description = "Image files (JPG, PNG, GIF, etc.)",
                        Icon = "🖼️",
                        IsActive = true,
                        SortOrder = 2,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ContentTypeModel
                    {
                        Name = "Video",
                        Description = "Video files (MP4, AVI, MOV, etc.)",
                        Icon = "🎥",
                        IsActive = true,
                        SortOrder = 3,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ContentTypeModel
                    {
                        Name = "Audio",
                        Description = "Audio files (MP3, WAV, FLAC, etc.)",
                        Icon = "🎵",
                        IsActive = true,
                        SortOrder = 4,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ContentTypeModel
                    {
                        Name = "Other",
                        Description = "Other file types",
                        Icon = "📁",
                        IsActive = true,
                        SortOrder = 5,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.ContentTypes.AddRange(contentTypes);
                await context.SaveChangesAsync();
                Console.WriteLine("ContentTypes seeded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during seeding: {ex.Message}");
            }
        }
    }
}
