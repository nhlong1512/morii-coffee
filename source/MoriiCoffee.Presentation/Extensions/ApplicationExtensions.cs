using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Infrastructure;
using MoriiCoffee.Infrastructure.Configurations;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Presentation.Middlewares;
using System.Net;

namespace MoriiCoffee.Presentation.Extensions;

/// <summary>Configures the HTTP request pipeline: Swagger, error handling, CORS, authentication, authorization, controllers, and database migration/seeding.</summary>
internal static class ApplicationExtensions
{
    public static void UseInfrastructure(this WebApplication app, string appCors)
    {
        // 1. Swagger UI
        app.UseSwaggerDocumentation();

        // 2. Global exception handling
        app.UseMiddleware<ErrorWrappingMiddleware>();

        // 2.5 Trust forwarded headers from reverse proxy (nginx/load balancer)
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            RequireHeaderSymmetry = false,
            KnownIPNetworks = { new System.Net.IPNetwork(System.Net.IPAddress.Parse("127.0.0.1"), 32) }
        };
        app.UseForwardedHeaders(forwardedHeadersOptions);

        // 3. HTTPS redirect (production only)
        if (app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }

        // 4. CORS
        app.UseCors(appCors);

        // 5. Authentication + Authorization (JWT)
        app.UseAuthentication();
        app.UseAuthorization();

        // 6. Root redirect to Swagger
        app.MapGet("/", context =>
        {
            context.Response.Redirect("/swagger/index.html");
            return Task.CompletedTask;
        });

        // 7. Controller endpoints
        app.MapControllers();
        app.MapHealthChecks("/health");

        // 8. Hangfire dashboard
        app.UseHangfireDashboard();

        // 9. Auto-migrate and seed
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        try
        {
            logger.LogInformation("Checking for pending database migrations...");
            if (dbContext.Database.GetPendingMigrations().Any())
            {
                dbContext.Database.Migrate();
                logger.LogInformation("Database migrations applied.");
            }

            logger.LogInformation("Seeding database...");
            var seeder = services.GetRequiredService<ApplicationDbContextSeed>();
            seeder.SeedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database migration or seeding.");
            throw;
        }
    }

}
