using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SM_MentalHealthApp.Client.Services;
using Radzen;

namespace SM_MentalHealthApp.Client;

/// <summary>
/// Centralized dependency injection configuration for the client application.
/// This class provides a clean separation of concerns and makes service registration
/// easier to maintain, test, and understand.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all client-side services including HTTP clients, Radzen services, and application services.
    /// </summary>
    public static IServiceCollection AddClientServices(this IServiceCollection services, WebAssemblyHostBuilder builder)
    {
        // HTTP Client Configuration
        services.AddHttpClient(builder);

        // Radzen UI Services
        services.AddRadzenServices();

        // Application Services
        services.AddApplicationServices();

        // Real-time Communication Services
        services.AddRealtimeServices();

        return services;
    }

    /// <summary>
    /// Configures the HTTP client with the correct base address
    /// </summary>
    private static IServiceCollection AddHttpClient(this IServiceCollection services, WebAssemblyHostBuilder builder)
    {
        services.AddScoped(sp =>
        {
            try
            {
                // Get the base address where the app is running
                var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
                // ✅ Always use HTTP for server API (server runs on HTTP, not HTTPS)
                // Client can run on HTTPS for Agora, but server API is always HTTP
                var serverUrl = $"http://{baseUri.Host}:5262/";
                Console.WriteLine($"🌐 HttpClient BaseAddress configured: {serverUrl}");
                var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
                
                // ✅ Set timeout to prevent hanging
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                return httpClient;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error configuring HttpClient: {ex.Message}");
                // Fallback to localhost if host extraction fails
                var fallbackClient = new HttpClient { BaseAddress = new Uri("http://localhost:5262/") };
                fallbackClient.Timeout = TimeSpan.FromSeconds(30);
                return fallbackClient;
            }
        });

        return services;
    }

    /// <summary>
    /// Registers Radzen Blazor UI component services
    /// </summary>
    private static IServiceCollection AddRadzenServices(this IServiceCollection services)
    {
        services.AddScoped<DialogService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<TooltipService>();
        services.AddScoped<ContextMenuService>();

        return services;
    }

    /// <summary>
    /// Registers application business logic services
    /// </summary>
    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Authentication & Session
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISessionTimeoutService, SessionTimeoutService>();

        // Core Application Services
        services.AddScoped<IDocumentUploadService, DocumentUploadService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddScoped<IClinicalNotesService, ClinicalNotesService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IEmergencyService, EmergencyService>();

        return services;
    }

    /// <summary>
    /// Registers real-time communication services (SignalR, WebSocket, Agora)
    /// </summary>
    private static IServiceCollection AddRealtimeServices(this IServiceCollection services)
    {
        services.AddScoped<ISignalRService, SignalRService>();
        services.AddScoped<IWebSocketService, WebSocketService>();
        services.AddScoped<IRealtimeService, RealtimeService>();
        services.AddScoped<IAgoraService, AgoraService>();

        return services;
    }
}

