using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MoriiCoffee.Infrastructure.Configurations;

public static class ValidationConfiguration
{
    public static IServiceCollection ConfigureValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Application.AssemblyReference.Assembly);
        return services;
    }
}
