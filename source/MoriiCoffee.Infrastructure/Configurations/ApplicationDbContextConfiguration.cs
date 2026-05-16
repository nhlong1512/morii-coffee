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

            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnectionString"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(
                        typeof(Persistence.AssemblyReference).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        3,
                        TimeSpan.FromSeconds(10),
                        null);
                })
                .AddInterceptors(interceptor);
        });

        return services;
    }
}
