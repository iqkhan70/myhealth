using SM_MentalHealthApp.Server;
using SM_MentalHealthApp.Server.Scripts;
using StackExchange.Redis;

// Check for encryption script arguments
if (args.Contains("--encrypt-mobilephones"))
{
    await EncryptExistingMobilePhoneData.RunAsync();
    return;
}

if (args.Contains("--encrypt-dob"))
{
    var tempBuilder = WebApplication.CreateBuilder(args);
    tempBuilder.Services.AddApplicationServices(tempBuilder.Configuration, tempBuilder.Environment);
    var tempApp = tempBuilder.Build();
    var serviceProvider = tempApp.Services;
    await EncryptExistingDateOfBirthData.RunAsync(serviceProvider);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Register all services using centralized dependency injection
builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ Only redirect to HTTPS in production (not needed for API server)
// app.UseHttpsRedirection(); // Disabled - server runs on HTTP, client handles HTTPS

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
