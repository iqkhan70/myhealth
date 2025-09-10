#!/bin/bash
set -e

# Project root
APP_NAME="SM_MentalHealthApp"
rm -rf $APP_NAME $APP_NAME.zip

echo "📦 Creating solution..."
dotnet new sln -n $APP_NAME

# Create projects
dotnet new blazorwasm -o $APP_NAME.Client --pwa --no-https
dotnet new webapi -o $APP_NAME.Server
dotnet new classlib -o $APP_NAME.Shared

# Add to solution
dotnet sln $APP_NAME.sln add $APP_NAME.Client $APP_NAME.Server $APP_NAME.Shared
dotnet add $APP_NAME.Client reference $APP_NAME.Shared
dotnet add $APP_NAME.Server reference $APP_NAME.Shared

# Add NuGet packages
dotnet add $APP_NAME.Server package Pomelo.EntityFrameworkCore.MySql
dotnet add $APP_NAME.Server package Microsoft.EntityFrameworkCore.Design
dotnet add $APP_NAME.Server package OpenAI

################################
# Shared Project
################################
mkdir -p $APP_NAME.Shared
cat > $APP_NAME.Shared/JournalEntry.cs <<'EOF'
namespace SM_MentalHealthApp.Shared
{
    public class JournalEntry
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? AIResponse { get; set; }
        public string? Mood { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
EOF

################################
# Server Project
################################
mkdir -p $APP_NAME.Server/Data
cat > $APP_NAME.Server/Data/JournalDbContext.cs <<'EOF'
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Data
{
    public class JournalDbContext : DbContext
    {
        public JournalDbContext(DbContextOptions<JournalDbContext> options) : base(options) { }
        public DbSet<JournalEntry> JournalEntries { get; set; }
    }
}
EOF

mkdir -p $APP_NAME.Server/Services
cat > $APP_NAME.Server/Services/JournalService.cs <<'EOF'
using SM_MentalHealthApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Services
{
    public class JournalService
    {
        private readonly JournalDbContext _context;
        private readonly OpenAIService _openAIService;

        public JournalService(JournalDbContext context, OpenAIService openAIService)
        {
            _context = context;
            _openAIService = openAIService;
        }

        public async Task<JournalEntry> ProcessEntry(JournalEntry entry)
        {
            var (response, mood) = await _openAIService.AnalyzeEntry(entry.Text);
            entry.AIResponse = response;
            entry.Mood = mood;
            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<List<JournalEntry>> GetEntries()
        {
            return await _context.JournalEntries.OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
    }
}
EOF

cat > $APP_NAME.Server/Services/OpenAIService.cs <<'EOF'
using OpenAI.Chat;

namespace SM_MentalHealthApp.Server.Services
{
    public class OpenAIService
    {
        private readonly ChatClient _chatClient;

        public OpenAIService(IConfiguration config)
        {
            _chatClient = new ChatClient(
                model: "gpt-4o-mini",
                apiKey: config["OpenAI:ApiKey"]
            );
        }

        public async Task<(string response, string mood)> AnalyzeEntry(string text)
        {
            var completion = await _chatClient.CompleteAsync(
                new List<ChatMessage>
                {
                    ChatMessage.User($"Journal entry: {text}\n\nRespond empathetically and summarize mood (happy, sad, anxious, neutral).")
                }
            );

            string aiResponse = completion.Content[0].Text ?? "";
            string mood = "Neutral";

            if (aiResponse.Contains("happy", StringComparison.OrdinalIgnoreCase)) mood = "Happy";
            else if (aiResponse.Contains("sad", StringComparison.OrdinalIgnoreCase)) mood = "Sad";
            else if (aiResponse.Contains("anxious", StringComparison.OrdinalIgnoreCase)) mood = "Anxious";

            return (aiResponse, mood);
        }
    }
}
EOF

mkdir -p $APP_NAME.Server/Controllers
cat > $APP_NAME.Server/Controllers/JournalController.cs <<'EOF'
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Shared;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JournalController : ControllerBase
    {
        private readonly JournalService _journalService;
        public JournalController(JournalService journalService) => _journalService = journalService;

        [HttpPost]
        public async Task<ActionResult<JournalEntry>> PostEntry([FromBody] JournalEntry entry)
        {
            var savedEntry = await _journalService.ProcessEntry(entry);
            return Ok(savedEntry);
        }

        [HttpGet]
        public async Task<ActionResult<List<JournalEntry>>> GetEntries()
        {
            return Ok(await _journalService.GetEntries());
        }
    }
}
EOF

# Update appsettings.json
cat > $APP_NAME.Server/appsettings.json <<'EOF'
{
  "ConnectionStrings": {
    "MySQL": "server=localhost;port=3306;database=mentalhealthdb;user=root;password=yourpassword"
  },
  "OpenAI": {
    "ApiKey": "your_openai_api_key"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
EOF

# Update Program.cs
cat > $APP_NAME.Server/Program.cs <<'EOF'
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MySQL");
builder.Services.AddDbContext<JournalDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<JournalService>();
builder.Services.AddScoped<OpenAIService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
EOF

################################
# Client Project
################################
mkdir -p $APP_NAME.Client/Pages
cat > $APP_NAME.Client/Pages/Journal.razor <<'EOF'
@page "/journal"
@inject HttpClient Http
@using SM_MentalHealthApp.Shared

<h3>Journal</h3>

<textarea @bind="entryText" rows="5" style="width:100%"></textarea>
<br />
<button @onclick="SaveEntry">Save</button>

<hr />

<ul>
    @foreach (var e in entries)
    {
        <li>
            <b>@e.CreatedAt.ToLocalTime():</b> @e.Text <br />
            <i>Mood: @e.Mood</i> <br />
            <blockquote>@e.AIResponse</blockquote>
        </li>
    }
</ul>

@code {
    private string entryText = string.Empty;
    private List<JournalEntry> entries = new();

    protected override async Task OnInitializedAsync()
    {
        entries = await Http.GetFromJsonAsync<List<JournalEntry>>("api/journal") ?? new();
    }

    private async Task SaveEntry()
    {
        var newEntry = new JournalEntry { Text = entryText };
        var result = await Http.PostAsJsonAsync("api/journal", newEntry);

        if (result.IsSuccessStatusCode)
        {
            var saved = await result.Content.ReadFromJsonAsync<JournalEntry>();
            if (saved != null) entries.Insert(0, saved);
            entryText = string.Empty;
        }
    }
}
EOF

################################
# Build & Zip
################################
echo "🛠 Building solution..."
dotnet build $APP_NAME.sln

echo "📦 Creating zip..."
zip -r $APP_NAME.zip $APP_NAME

echo "✅ Done! Project packaged as $APP_NAME.zip"
