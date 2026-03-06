using Microsoft.Extensions.DependencyInjection;
using MoriiCoffee.Application.SeedWork.Mappings;

namespace MoriiCoffee.Infrastructure.Configurations;

public static class MapperConfiguration
{
    public static IServiceCollection ConfigureMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(config =>
        {
            config.AddProfile<CategoryMapper>();
            config.AddProfile<ProductMapper>();
        });

        return services;
    }
}
