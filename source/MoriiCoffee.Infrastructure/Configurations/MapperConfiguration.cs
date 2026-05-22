using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoriiCoffee.Application.SeedWork.Mappings;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>Registers all AutoMapper profiles (Category, Product, User, Banner, Store) with the DI container.</summary>
public static class MapperConfiguration
{
    public static IServiceCollection ConfigureMapper(this IServiceCollection services, IConfiguration configuration)
    {
        var s3Settings = configuration.GetSection(nameof(AwsS3Settings)).Get<AwsS3Settings>() ?? new AwsS3Settings();

        services.AddAutoMapper(config =>
        {
            config.AddProfile(new CategoryMapper(s3Settings));
            config.AddProfile(new ProductMapper(s3Settings));
            config.AddProfile<UserMapper>();
            config.AddProfile(new BannerMapper(s3Settings));
            config.AddProfile(new BlogMapper(s3Settings));
            config.AddProfile<StoreMapper>();
        });

        return services;
    }
}
