using Microsoft.Extensions.DependencyInjection;
using MoriiCoffee.Application.SeedWork.Mappings;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>Registers all AutoMapper profiles (Category, Product, User, Banner) with the DI container.</summary>
public static class MapperConfiguration
{
    public static IServiceCollection ConfigureMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(config =>
        {
            config.AddProfile<CategoryMapper>();
            config.AddProfile<ProductMapper>();
            config.AddProfile<UserMapper>();
            config.AddProfile<BannerMapper>();
        });

        return services;
    }
}
