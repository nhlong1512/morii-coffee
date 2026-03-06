using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace MoriiCoffee.Infrastructure.Configurations;

public static class SwaggerConfiguration
{
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MoriiCoffee API",
                Version = "v1",
                Description = """
                    A modern, RESTful API for the MoriiCoffee shop platform.
                    Provides endpoints for managing the product catalog, categories, and product variants.
                    Built with Clean Architecture, DDD, and CQRS patterns.
                    """,
                Contact = new OpenApiContact
                {
                    Name = "MoriiCoffee Development Team",
                    Email = "dev@moriicoffee.com"
                }
            });

            options.EnableAnnotations();
            options.UseInlineDefinitionsForEnums();

            // JWT Bearer support (for future auth phases)
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter: Bearer {your-jwt-token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.DocumentTitle = "MoriiCoffee API Documentation";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MoriiCoffee API v1");
            c.RoutePrefix = "swagger";
        });

        return app;
    }
}
