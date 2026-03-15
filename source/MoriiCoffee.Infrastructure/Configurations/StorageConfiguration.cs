using Microsoft.Extensions.DependencyInjection;
using Minio;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>Registers the MinIO client as a singleton in the DI container.</summary>
public static class StorageConfiguration
{
    /// <summary>
    /// Adds and configures the <see cref="IMinioClient"/> singleton using
    /// settings from the <see cref="MinioSettings"/> section in appsettings.json.
    /// </summary>
    public static IServiceCollection ConfigureStorage(this IServiceCollection services)
    {
        var settings = services.GetOptions<MinioSettings>(nameof(MinioSettings));

        services.AddMinio(client => client
            .WithEndpoint(settings.Endpoint)
            .WithCredentials(settings.AccessKey, settings.SecretKey)
            .WithSSL(settings.WithSSL)
            .Build());

        return services;
    }
}
