using SM_MentalHealthApp.Server;
using SM_MentalHealthApp.Server.Scripts;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Config: appsettings -> appsettings.{ENV} -> env vars
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// One-off jobs
if (args.Contains("--encrypt-mobilephones"))
{
    builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);
    var appForJob = builder.Build();
    await EncryptExistingMobilePhoneData.RunAsync();
    return;
}

if (args.Contains("--encrypt-dob"))
{
    builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);
    var appForJob = builder.Build();
    await EncryptExistingDateOfBirthData.RunAsync(appForJob.Services);
    return;
}

// Normal startup
builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// nginx terminates TLS in Docker, so keep Kestrel HTTP-only
app.UseCors("AllowBlazorClient");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHub<SM_MentalHealthApp.Server.Hubs.MobileHub>("/mobilehub");

// Redis check (non-fatal)
try
{
    var redis = app.Services.GetService<IConnectionMultiplexer>();
    Console.WriteLine(redis?.IsConnected == true
        ? "✅ Redis connected successfully"
        : "⚠️ Redis not connected (app will continue)");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Redis check failed (app will continue): {ex.Message}");
}

app.Run();
