using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
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
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using SM_MentalHealthApp.Shared;

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
        services.AddApplicationServicesInternal(configuration);

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
            var config = sp.GetRequiredService<IConfiguration>();

            // Priority: appsettings -> env var -> dev default
            var connString =
                config.GetSection("Redis")["ConnectionString"] ??
                Environment.GetEnvironmentVariable("Redis__ConnectionString") ??
                "localhost:6379"; // dev fallback (no password for local Redis)

            var redisConfig = ConfigurationOptions.Parse(connString);
            redisConfig.AbortOnConnectFail = false; // Don't abort on connect fail - allow lazy connection
            redisConfig.ConnectTimeout = 5000;
            redisConfig.SyncTimeout = 5000;
            redisConfig.AsyncTimeout = 5000;
            redisConfig.ConnectRetry = 3;

            // Create connection - it will connect lazily when first used
            // If Redis is unavailable, operations will fail gracefully in RedisCacheService
            return ConnectionMultiplexer.Connect(redisConfig);
        });
        services.AddScoped<IRedisCacheService, RedisCacheService>();

        return services;
    }

    /// <summary>
    /// Registers application business logic services
    /// </summary>
    private static IServiceCollection AddApplicationServicesInternal(this IServiceCollection services, IConfiguration configuration)
    {
        // PII Encryption Service (must be singleton to maintain consistent encryption key)
        services.AddSingleton<IPiiEncryptionService, PiiEncryptionService>();

        // Core Services
        services.AddScoped<UserService>();
        services.AddScoped<JournalService>();
        services.AddScoped<ChatService>(sp =>
        {
            // Inject agentic AI service if available (for service request chats)
            var agenticAIService = sp.GetService<IServiceRequestAgenticAIService>();
            // Inject Redis cache service if available (for conversation history caching)
            var redisCache = sp.GetService<IRedisCacheService>();
            return new ChatService(
                sp.GetRequiredService<ConversationRepository>(),
                sp.GetRequiredService<HuggingFaceService>(),
                sp.GetRequiredService<JournalService>(),
                sp.GetRequiredService<UserService>(),
                sp.GetRequiredService<IContentAnalysisService>(),
                sp.GetRequiredService<IIntelligentContextService>(),
                sp.GetRequiredService<IChatHistoryService>(),
                sp.GetRequiredService<IServiceRequestService>(),
                sp.GetRequiredService<JournalDbContext>(),
                sp.GetRequiredService<ILogger<ChatService>>(),
                agenticAIService,
                redisCache);
        });

        // HTTP Context Accessor (needed for AuthService to get base URL from request)
        services.AddHttpContextAccessor();

        // Interface-based Services
        services.AddScoped<IAuthService, AuthService>(sp =>
            new AuthService(
                sp.GetRequiredService<JournalDbContext>(),
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IPiiEncryptionService>(),
                sp.GetService<INotificationService>(), // Optional - may be null if not configured
                sp.GetService<IHttpContextAccessor>() // Optional - for getting base URL from request
            ));
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IUserRequestService, UserRequestService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IContentAnalysisService, ContentAnalysisService>();
        services.AddScoped<IClinicalNotesService, ClinicalNotesService>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<IAssignmentLifecycleService, AssignmentLifecycleService>();
        services.AddScoped<IExpertiseService, ExpertiseService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IBillingRateService, BillingRateService>();
        services.AddScoped<IServiceRequestChargeService, ServiceRequestChargeService>();
        services.AddScoped<IInvoicingService, InvoicingService>();
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
        services.AddScoped<IAIInstructionService, AIInstructionService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<IAIResponseTemplateService, AIResponseTemplateService>();
        services.AddScoped<IGenericQuestionPatternService, GenericQuestionPatternService>();
        services.AddScoped<IMedicalThresholdService, MedicalThresholdService>();
        services.AddScoped<ISectionMarkerService, SectionMarkerService>();
        services.AddScoped<IQuestionClassificationService, QuestionClassificationService>();

        // Response Handler System (refactored from HuggingFaceService)
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.ContextExtractor>();
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.QuestionExtractor>();
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.QuestionClassifier>();
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.StatusResponseHandler>();
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.StatisticsResponseHandler>();
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.RecommendationsResponseHandler>();
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.ConcernsResponseHandler>();
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.OverviewResponseHandler>();
        services.AddScoped<SM_MentalHealthApp.Server.Services.ResponseHandlers.ResponseHandlerFactory>(sp =>
        {
            var handlers = new List<SM_MentalHealthApp.Server.Services.ResponseHandlers.IResponseHandler>
            {
                    sp.GetRequiredService<SM_MentalHealthApp.Server.Services.ResponseHandlers.StatusResponseHandler>(),
                    sp.GetRequiredService<SM_MentalHealthApp.Server.Services.ResponseHandlers.StatisticsResponseHandler>(),
                    sp.GetRequiredService<SM_MentalHealthApp.Server.Services.ResponseHandlers.RecommendationsResponseHandler>(),
                    sp.GetRequiredService<SM_MentalHealthApp.Server.Services.ResponseHandlers.ConcernsResponseHandler>(),
                    sp.GetRequiredService<SM_MentalHealthApp.Server.Services.ResponseHandlers.OverviewResponseHandler>()
            };
            var logger = sp.GetRequiredService<ILogger<SM_MentalHealthApp.Server.Services.ResponseHandlers.ResponseHandlerFactory>>();
            return new SM_MentalHealthApp.Server.Services.ResponseHandlers.ResponseHandlerFactory(handlers, logger);
        });
        services.AddScoped<EnhancedContextResponseService>();

        // AI & ML Services
        services.AddScoped<HuggingFaceService>();
        services.AddScoped<ConversationRepository>();
        services.AddScoped<LlmClient>();
        services.AddScoped<IChainedAIService, ChainedAIService>();

        // Client Profile System for Agentic AI
        services.AddScoped<IClientProfileService, ClientProfileService>();
        services.AddScoped<IClientAgentSessionService, ClientAgentSessionService>();
        services.AddScoped<IServiceRequestAgenticAIService, ServiceRequestAgenticAIService>();

        // External Service Integrations
        services.AddScoped<S3Service>();
        services.AddScoped<ContentService>();
        services.AddScoped<AgoraTokenService>();

        // Content Cleanup Service
        services.AddScoped<IContentCleanupService, ContentCleanupService>();

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
        services.AddControllers(options =>
            {
                // Add filter to decrypt User PII in OData responses
                options.Filters.Add<Filters.ODataUserDecryptionActionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never; // Include all properties, even if default values
            })
            .AddOData(options => options
                .Select()
                .Filter()
                .OrderBy()
                .Count()
                .SetMaxTop(1000)
                .AddRouteComponents("odata", GetEdmModel())
            );

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
                    Title = "Customer App API",
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

    /// <summary>
    /// Creates the Entity Data Model (EDM) for OData
    /// </summary>
    private static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();

        // Users (Patients, Doctors, Coordinators, Attorneys)
        var userSet = builder.EntitySet<User>("Users");
        userSet.EntityType.HasKey(u => u.Id);
        // Ignore sensitive/computed properties
        userSet.EntityType.Ignore(u => u.PasswordHash);
        userSet.EntityType.Ignore(u => u.DateOfBirthEncrypted);
        userSet.EntityType.Ignore(u => u.MobilePhoneEncrypted);
        userSet.EntityType.Ignore(u => u.JournalEntries);
        userSet.EntityType.Ignore(u => u.ChatSessions);
        userSet.EntityType.Ignore(u => u.FullName);
        // Note: DateOfBirth is kept in EDM for filtering, but will be handled specially
        // in the controller (materialize, decrypt, filter in memory)

        // Appointments
        var appointmentSet = builder.EntitySet<Appointment>("Appointments");
        appointmentSet.EntityType.HasKey(a => a.Id);
        // Allow navigation properties to be expanded (don't ignore them)
        // Note: Navigation properties are marked [JsonIgnore] in the entity, but OData can still expand them
        // Ignore only computed properties and ServiceRequest (not exposed via OData)
        appointmentSet.EntityType.Ignore(a => a.EndDateTime);
        appointmentSet.EntityType.Ignore(a => a.IsUrgentCare);
        appointmentSet.EntityType.Ignore(a => a.IsBusinessHours);
        appointmentSet.EntityType.Ignore(a => a.ServiceRequest);

        // Contents
        var contentSet = builder.EntitySet<ContentItem>("Contents");
        contentSet.EntityType.HasKey(c => c.Id);
        // Ignore sensitive/internal properties
        contentSet.EntityType.Ignore(c => c.S3Bucket);
        contentSet.EntityType.Ignore(c => c.S3Key);
        contentSet.EntityType.Ignore(c => c.FileName);
        contentSet.EntityType.Ignore(c => c.ContentGuid);
        contentSet.EntityType.Ignore(c => c.ServiceRequest);
        // Note: Navigation properties are marked [JsonIgnore] in the entity, but OData can still expand them

        // UserAssignments
        var userAssignmentSet = builder.EntitySet<UserAssignment>("UserAssignments");
        userAssignmentSet.EntityType.HasKey(ua => new { ua.AssignerId, ua.AssigneeId });
        // Navigation properties are NOT ignored to allow expansion

        // Expertise - Required for ServiceRequest navigation property expansion
        var expertiseSet = builder.EntitySet<Expertise>("Expertise");
        expertiseSet.EntityType.HasKey(e => e.Id);

        // ServiceRequests - Expose as EntitySet for server-side pagination
        var serviceRequestSet = builder.EntitySet<ServiceRequest>("ServiceRequests");
        serviceRequestSet.EntityType.HasKey(sr => sr.Id);
        // Ignore navigation properties to avoid circular references (we'll load them via Include in controller)
        serviceRequestSet.EntityType.Ignore(sr => sr.Client);
        serviceRequestSet.EntityType.Ignore(sr => sr.CreatedByUser);
        // Note: Assignments, Expertises, and PrimaryExpertise are NOT ignored so they can be expanded via $expand
        // The controller uses Include() to load them, and OData will serialize them

        return builder.GetEdmModel();
    }
}

