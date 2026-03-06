using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace MoriiCoffee.Infrastructure.Configurations;

public static class LoggingConfiguration
{
    public static Action<HostBuilderContext, LoggerConfiguration> Configure =>
        (context, config) =>
        {
            config
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .ReadFrom.Configuration(context.Configuration);
        };
}
