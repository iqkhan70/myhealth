using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SM_MentalHealthApp.Client.Services;
using Radzen;
using System;

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

                // ✅ Detect if running through ngrok
                var isNgrok = baseUri.Host.Contains("ngrok.io") || baseUri.Host.Contains("ngrok-free.app");

                string serverUrl;
                if (isNgrok)
                {
                    // When using ngrok, we need a separate ngrok tunnel for the server
                    // The server URL can be provided via:
                    // 1. Query parameter: ?server=https://abc.ngrok.io (used directly, not stored)
                    // 2. Environment variable: SERVER_NGROK_URL (build-time)

                    // Try environment variable first (build-time configuration)
                    var serverNgrokUrl = Environment.GetEnvironmentVariable("SERVER_NGROK_URL");

                    // If not set, we'll use a JavaScript function to read from query/localStorage
                    // This will be handled by a service that updates the HttpClient after initialization
                    if (string.IsNullOrEmpty(serverNgrokUrl))
                    {
                        // Use a default that will be updated by ServerUrlService
                        // Default to HTTPS for better security in development
                        serverUrl = "https://localhost:5263/";
                    }
                    else
                    {
                        serverUrl = serverNgrokUrl.EndsWith("/") ? serverNgrokUrl : serverNgrokUrl + "/";
                    }
                }
                else
                {
                    // ✅ Determine server URL based on client location and scheme
                    var isLocalhost = baseUri.Host == "localhost" || baseUri.Host == "127.0.0.1";
                    var clientPort = baseUri.Port;
                    var clientScheme = baseUri.Scheme; // http or https

                    if (isLocalhost)
                    {
                        // Localhost: connect to server on correct port based on scheme
                        // HTTP client (5282) -> HTTP server (5262)
                        // HTTPS client (5283) -> HTTPS server (5263)
                        var serverPort = clientScheme == "https" ? 5263 : 5262;
                        serverUrl = $"{clientScheme}://{baseUri.Host}:{serverPort}/";
                    }
                    else if (clientPort == 5282 || clientPort == 5283)
                    {
                        // Local network access (macip): client on 5282/5283, server on 5262/5263
                        // Match the client's scheme (http or https) and use correct server port
                        // HTTP client (5282) -> HTTP server (5262)
                        // HTTPS client (5283) -> HTTPS server (5263)
                        var serverPort = clientScheme == "https" ? 5263 : 5262;
                        serverUrl = $"{clientScheme}://{baseUri.Host}:{serverPort}/";
                    }
                    else
                    {
                        // DigitalOcean or other production: client on port 443 (default HTTPS)
                        // Use same host without port (goes through Nginx proxy)
                        serverUrl = $"{baseUri.Scheme}://{baseUri.Host}/";
                    }
                }

                // ✅ Create HttpClient with proper configuration
                var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };

                // ✅ Set timeout for long-running AI requests (15 minutes)
                // AI generation can take 3-5 minutes, so we need a longer timeout
                httpClient.Timeout = TimeSpan.FromMinutes(15);

                // ✅ Note: In Blazor WebAssembly, SSL certificate validation is handled by the browser
                // If you get "failed to fetch" errors with self-signed certificates:
                // 1. Accept the certificate warning in the browser when accessing the server directly
                // 2. Or use ngrok which provides valid certificates
                // 3. Or configure the server with a trusted certificate

                return httpClient;
            }
            catch (Exception ex)
            {
                // Fallback to localhost HTTPS if host extraction fails
                var fallbackClient = new HttpClient { BaseAddress = new Uri("https://localhost:5263/") };
                fallbackClient.Timeout = TimeSpan.FromMinutes(15);
                return fallbackClient;
            }
        });

        // Register a service to update HttpClient BaseAddress from query parameter
        services.AddScoped<ServerUrlService>();

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
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<IInvoicingService, InvoicingService>();
        services.AddScoped<ODataService>(); // OData service for server-side pagination

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

