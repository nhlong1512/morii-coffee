using Amazon;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>Registers all storage clients (MinIO and AWS S3) in the DI container.</summary>
public static class StorageConfiguration
{
    /// <summary>
    /// Adds the <see cref="IMinioClient"/> singleton and the two keyed <see cref="IAmazonS3"/>
    /// singletons (<c>"s3-public"</c> and <c>"s3-private"</c>) using settings from appsettings.json.
    /// </summary>
    public static IServiceCollection ConfigureStorage(this IServiceCollection services)
    {
        // MinIO — only register if an endpoint is configured (skipped when using AWS S3)
        var minioSettings = services.GetOptions<MinioSettings>(nameof(MinioSettings));

        if (!string.IsNullOrWhiteSpace(minioSettings.Endpoint))
        {
            services.AddMinio(client => client
                .WithEndpoint(minioSettings.Endpoint)
                .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey)
                .WithSSL(minioSettings.WithSSL)
                .Build());
        }

        // AWS S3 — two keyed clients, one per IAM credential pair
        services.AddKeyedSingleton<IAmazonS3>("s3-public", (sp, _) =>
        {
            var s = sp.GetRequiredService<AwsS3Settings>();
            return new AmazonS3Client(
                s.PublicAccessKeyId,
                s.PublicSecretAccessKey,
                new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(s.Region) });
        });

        services.AddKeyedSingleton<IAmazonS3>("s3-private", (sp, _) =>
        {
            var s = sp.GetRequiredService<AwsS3Settings>();
            return new AmazonS3Client(
                s.PrivateAccessKeyId,
                s.PrivateSecretAccessKey,
                new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(s.Region) });
        });

        return services;
    }
}
