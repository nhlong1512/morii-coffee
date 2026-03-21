using Microsoft.Extensions.DependencyInjection;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.Repositories;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;
using UnitOfWorkImpl = MoriiCoffee.Infrastructure.Persistence.SeedWork.UnitOfWork.UnitOfWork;

namespace MoriiCoffee.Infrastructure.Persistence;

/// <summary>Registers persistence-layer services: UnitOfWork, all repositories, and the database seeder.</summary>
public static class DependencyInjection
{

    public static IServiceCollection ConfigurePersistenceServices(this IServiceCollection services)
    {
        services.ConfigureDependencyInjection();
        services.ConfigureRepositories();

        return services;
    }

    public static IServiceCollection ConfigureDependencyInjection(this IServiceCollection services)
    {
        services
            .AddTransient<ApplicationDbContextSeed>();

        return services;
    }
    public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
    {
        services.AddTransient<ApplicationDbContextSeed>();

        services
            .AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>))
            .AddScoped<IUnitOfWork, UnitOfWorkImpl>();

        services
            .AddScoped<ICategoriesRepository, CategoriesRepository>()
            .AddScoped<IProductsRepository, ProductsRepository>()
            .AddScoped<IProductVariantsRepository, ProductVariantsRepository>()
            .AddScoped<IBannersRepository, BannersRepository>();

        return services;
    }
}
