using MoriiCoffee.Infrastructure;
using MoriiCoffee.Infrastructure.Configurations;
using MoriiCoffee.Presentation.Extensions;
using Serilog;
using StackExchange.Redis;

const string AppCors = "MoriiCoffeeAppCors";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(LoggingConfiguration.Configure);
builder.AddAppConfigurations();
builder.Services.ConfigureInfrastructureServices(builder.Configuration, AppCors);

Log.Information("Starting MoriiCoffee API...");

WebApplication app = builder.Build();

// Validate Redis connectivity at startup so misconfigurations surface immediately.
try
{
    var redis = app.Services.GetRequiredService<IConnectionMultiplexer>();
    var db = redis.GetDatabase();
    await db.PingAsync();
    Log.Information("Redis connectivity check passed.");
}
catch (Exception ex)
{
    Log.Warning(ex, "Redis connectivity check failed — catalog caching is degraded. Cart and password-reset ticket flows will be unavailable.");
}

app.UseInfrastructure(AppCors);
app.Run();
