using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Server.Services;
using System.Text;
using StackExchange.Redis;

namespace SM_MentalHealthApp.Server;

/// <summary>
/// Centralized dependency injection configuration for the server application.
/// This class provides a clean separation of concerns and makes service registration
/// easier to maintain, test, and understand.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all application services, infrastructure services, and framework services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Database
        services.AddDatabase(configuration);

        // Infrastructure Services
        services.AddInfrastructureServices(configuration);

        // Application Services
        services.AddApplicationServicesInternal();

        // Framework Services
        services.AddFrameworkServices(configuration, environment);

        // Authentication & Authorization
        services.AddAuthenticationServices(configuration);

        return services;
    }

    /// <summary>
    /// Registers database-related services
    /// </summary>
    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<JournalDbContext>(options =>
            options.UseMySql(
                configuration.GetConnectionString("MySQL"),
                new MySqlServerVersion(new Version(8, 0, 21))
            ));

        return services;
    }

    /// <summary>
    /// Registers infrastructure services (S3, Redis, HTTP clients, etc.)
    /// </summary>
    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // HTTP Client
        services.AddHttpClient();

        // S3 Configuration
        services.Configure<S3Config>(configuration.GetSection("S3"));
        services.AddSingleton<IAmazonS3>(provider =>
        {
            var config = configuration.GetSection("S3").Get<S3Config>();

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

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConfig = ConfigurationOptions.Parse("localhost:6379,password=StrongPassword123!");
            redisConfig.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisConfig);
        });
        services.AddScoped<IRedisCacheService, RedisCacheService>();

        return services;
    }

    /// <summary>
    /// Registers application business logic services
    /// </summary>
    private static IServiceCollection AddApplicationServicesInternal(this IServiceCollection services)
    {
        // Core Services
        services.AddScoped<UserService>();
        services.AddScoped<JournalService>();
        services.AddScoped<ChatService>();

        // Interface-based Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IContentAnalysisService, ContentAnalysisService>();
        services.AddScoped<IClinicalNotesService, ClinicalNotesService>();
        services.AddScoped<IMultimediaAnalysisService, MultimediaAnalysisService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddScoped<IIntelligentContextService, IntelligentContextService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISmsService, VonageSmsService>();
        services.AddScoped<IClinicalDecisionSupportService, ClinicalDecisionSupportService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IDocumentUploadService, DocumentUploadService>();
        services.AddScoped<ICriticalValuePatternService, CriticalValuePatternService>();
        services.AddScoped<ICriticalValueKeywordService, CriticalValueKeywordService>();

        // AI & ML Services
        services.AddScoped<HuggingFaceService>();
        services.AddScoped<ConversationRepository>();
        services.AddScoped<LlmClient>();

        // External Service Integrations
        services.AddScoped<S3Service>();
        services.AddScoped<ContentService>();
        services.AddScoped<AgoraTokenService>();

        // Background Services
        services.AddHostedService<AppointmentReminderService>();

        return services;
    }

    /// <summary>
    /// Registers framework services (API, Swagger, SignalR, CORS, etc.)
    /// </summary>
    private static IServiceCollection AddFrameworkServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Controllers and JSON Options
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
            });

        // API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true; // Backward compatible
            options.ReportApiVersions = true; // Include version info in response headers
            options.ApiVersionReader = ApiVersionReader.Combine(
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("X-Version"),
                new UrlSegmentApiVersionReader()
            );
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Swagger
        services.AddRazorPages();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName);

            // Add versioning support to Swagger
            var provider = services.BuildServiceProvider()
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

        // SignalR
        services.AddSignalR();

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazorClient", policy =>
            {
                if (environment.IsDevelopment())
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

        return services;
    }

    /// <summary>
    /// Registers authentication and authorization services
    /// </summary>
    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(
                            configuration["Jwt:Key"]
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

        services.AddAuthorization();

        return services;
    }
}

