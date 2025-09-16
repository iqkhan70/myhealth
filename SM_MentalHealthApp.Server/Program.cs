using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<JournalDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("MySQL"),
    new MySqlServerVersion(new Version(8, 0, 21))));

builder.Services.AddHttpClient();

// Configure S3
builder.Services.Configure<S3Config>(builder.Configuration.GetSection("S3"));
builder.Services.AddSingleton<IAmazonS3>(provider =>
{
    var config = builder.Configuration.GetSection("S3").Get<S3Config>();

    // Priority: Environment Variables > appsettings.json
    var accessKey = Environment.GetEnvironmentVariable("DIGITALOCEAN_ACCESS_KEY") ?? config.AccessKey;
    var secretKey = Environment.GetEnvironmentVariable("DIGITALOCEAN_SECRET_KEY") ?? config.SecretKey;

    // Log warning if using appsettings.json credentials (for development awareness)
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DIGITALOCEAN_ACCESS_KEY")))
    {
        Console.WriteLine("⚠️  WARNING: Using credentials from appsettings.json. For production, use environment variables.");
    }

    var s3Config = new AmazonS3Config
    {
        ServiceURL = config.ServiceUrl,
        ForcePathStyle = true,
        UseHttp = false,
        AuthenticationRegion = config.Region,
        SignatureVersion = "4"
    };
    return new AmazonS3Client(accessKey, secretKey, s3Config);
});

// Register services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JournalService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<HuggingFaceService>();
builder.Services.AddScoped<ConversationRepository>();
builder.Services.AddScoped<LlmClient>();
builder.Services.AddScoped<S3Service>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<IContentAnalysisService, ContentAnalysisService>();
builder.Services.AddScoped<IMultimediaAnalysisService, MultimediaAnalysisService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("http://localhost:5262")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve static files from the client's wwwroot directory
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "..", "SM_MentalHealthApp.Client", "bin", "Debug", "net9.0", "wwwroot")),
    RequestPath = "",
    OnPrepareResponse = ctx =>
    {
        // Set proper MIME types for Blazor WebAssembly files
        if (ctx.File.Name.EndsWith(".wasm"))
        {
            ctx.Context.Response.Headers.Append("Content-Type", "application/wasm");
        }
        else if (ctx.File.Name.EndsWith(".js"))
        {
            ctx.Context.Response.Headers.Append("Content-Type", "application/javascript");
        }
    }
});

app.UseCors("AllowBlazorClient");
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapFallbackToFile("index.html");

// TODO: Add database seeding later

app.Run();