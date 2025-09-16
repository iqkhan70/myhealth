using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

Console.WriteLine("=== CHECKING JOHN DOE'S CONTENT ===");

var connectionString = "Server=localhost;Database=mental_health_journal;Uid=root;Pwd=password;";

var options = new DbContextOptionsBuilder<JournalDbContext>()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
    .Options;

using var context = new JournalDbContext(options);

// Find John Doe
var johnDoe = await context.Users.FirstOrDefaultAsync(u => u.FirstName == "John" && u.LastName == "Doe");
if (johnDoe == null)
{
    Console.WriteLine("❌ John Doe not found in database");
    return;
}

Console.WriteLine($"✅ Found John Doe: ID={johnDoe.Id}, Name={johnDoe.FirstName} {johnDoe.LastName}");

// Check content for John Doe
var contents = await context.Contents
    .Where(c => c.PatientId == johnDoe.Id && c.IsActive)
    .ToListAsync();

Console.WriteLine($"📁 Found {contents.Count} content items for John Doe:");
foreach (var content in contents)
{
    Console.WriteLine($"  - ID: {content.Id}, Title: {content.Title}, Type: {content.Type}, Created: {content.CreatedAt}");
}

// Check content analyses for John Doe
var analyses = await context.ContentAnalyses
    .Where(ca => context.Contents.Any(c => c.Id == ca.ContentId && c.PatientId == johnDoe.Id && c.IsActive))
    .ToListAsync();

Console.WriteLine($"🔍 Found {analyses.Count} content analyses for John Doe:");
foreach (var analysis in analyses)
{
    Console.WriteLine($"  - Analysis ID: {analysis.Id}, Content ID: {analysis.ContentId}, Status: {analysis.ProcessingStatus}");
    Console.WriteLine($"    Text Length: {analysis.ExtractedText?.Length ?? 0}, Alerts: {analysis.Alerts.Count}");
    if (analysis.Alerts.Any())
    {
        Console.WriteLine($"    Alerts: {string.Join(", ", analysis.Alerts)}");
    }
}

Console.WriteLine("=== END CHECK ===");
