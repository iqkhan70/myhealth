using SM_MentalHealthApp.Server;
using StackExchange.Redis;

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
