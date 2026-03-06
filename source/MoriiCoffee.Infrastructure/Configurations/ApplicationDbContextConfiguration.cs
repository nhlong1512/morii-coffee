using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.Interceptors;

namespace MoriiCoffee.Infrastructure.Configurations;

public static class ApplicationDbContextConfiguration
{
    public static IServiceCollection ConfigureApplicationDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<DateTrackingInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var interceptor = serviceProvider.GetRequiredService<DateTrackingInterceptor>();

            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnectionString"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(
                        typeof(Persistence.AssemblyReference).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                })
                .AddInterceptors(interceptor);
        });

        return services;
    }
}
