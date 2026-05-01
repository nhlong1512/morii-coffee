using MoriiCoffee.Infrastructure;
using MoriiCoffee.Infrastructure.Configurations;
using MoriiCoffee.Presentation.Extensions;
using Serilog;

const string AppCors = "MoriiCoffeeAppCors";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(LoggingConfiguration.Configure);
builder.AddAppConfigurations();
builder.Services.ConfigureInfrastructureServices(builder.Configuration, AppCors);

Log.Information("Starting MoriiCoffee API...");

WebApplication app = builder.Build();
app.UseInfrastructure(AppCors);
app.RegisterRecurringJobs();
app.Run();
