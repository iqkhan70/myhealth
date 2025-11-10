using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using System.Text;
using StackExchange.Redis;

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

// Register application services
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
builder.Services.AddScoped<IClinicalNotesService, ClinicalNotesService>();
builder.Services.AddScoped<IMultimediaAnalysisService, MultimediaAnalysisService>();
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();
builder.Services.AddScoped<IIntelligentContextService, IntelligentContextService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISmsService, VonageSmsService>();
builder.Services.AddScoped<IClinicalDecisionSupportService, ClinicalDecisionSupportService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddHostedService<AppointmentReminderService>();
builder.Services.AddScoped<AgoraTokenService>();
builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();

// ✅ Add Redis (ConnectionMultiplexer + Cache Service)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse("localhost:6379,password=StrongPassword123!");
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// Add controllers and JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true; // Backward compatible - existing clients work
    options.ReportApiVersions = true; // Include version info in response headers
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Version"),
        new UrlSegmentApiVersionReader()
    );
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();

// ✅ FIX: Avoid Swagger schema name collisions
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName);
    
    // Add versioning support to Swagger
    var provider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>();
    
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(description.GroupName, new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Mental Health App API",
            Version = description.ApiVersion.ToString(),
            Description = description.IsDeprecated 
                ? "This API version has been deprecated." 
                : "Current API version"
        });
    }
    
    options.DocInclusionPredicate((name, api) => true);
});

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // More permissive CORS for development
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(
                    "http://localhost:5262",
                    "http://localhost:5282",
                    "http://192.168.86.113:5262",
                    "http://localhost:8080",
                    "http://localhost:8081")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Required for SignalR
        }
    });
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(
                    builder.Configuration["Jwt:Key"]
                    ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
                )),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/mobilehub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable static files (not Blazor WebAssembly)
// app.UseStaticFiles(); // Removed - this was serving Blazor client files

app.UseCors("AllowBlazorClient");
app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.MapControllers();
app.MapRazorPages();

// Map SignalR hub
app.MapHub<SM_MentalHealthApp.Server.Hubs.MobileHub>("/mobilehub");

// Optional: Redis health check log
var redis = app.Services.GetRequiredService<IConnectionMultiplexer>();
if (redis.IsConnected)
    Console.WriteLine("✅ Redis connected successfully");
else
    Console.WriteLine("❌ Redis connection failed");

// TODO: Add database seeding later
app.Run();
