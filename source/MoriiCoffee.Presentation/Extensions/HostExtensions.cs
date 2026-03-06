namespace MoriiCoffee.Presentation.Extensions;

internal static class HostExtensions
{
    public static void AddAppConfigurations(this WebApplicationBuilder builder)
    {
        string environment = builder.Environment.EnvironmentName;

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    }
}
