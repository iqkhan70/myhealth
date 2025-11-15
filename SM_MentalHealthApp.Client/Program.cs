using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SM_MentalHealthApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register all services using centralized dependency injection
builder.Services.AddClientServices(builder);

try
{
    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Fatal error starting Blazor app: {ex.Message}");
    Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
    throw; // Re-throw to show error in browser
}
