using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Configurations;

/// <summary>
/// Registers authentication schemes for MoriiCoffee application.
/// Configures JWT Bearer authentication for API access tokens and Google OAuth 2.0 for external authentication.
/// </summary>
public static class AuthenticationConfiguration
{
    /// <summary>
    /// Configures JWT Bearer authentication and Google OAuth 2.0 provider.
    /// JWT is used for API authentication, Google OAuth is used for external sign-in.
    /// </summary>
    /// <param name="services">The service collection to add authentication to.</param>
    /// <param name="configuration">The application configuration containing JWT and Google OAuth settings.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection ConfigureAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        JwtOptions jwtOptions = services.GetOptions<JwtOptions>(nameof(JwtOptions));
        Authentication authentication = services.GetOptions<Authentication>("Authentication");
        ProviderOptions googleAuthentication = authentication.Google;

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.SaveToken = true;
            })
            .AddGoogle(googleOptions =>
            {
                string googleClientId = googleAuthentication.ClientId;
                string googleClientSecret = googleAuthentication.ClientSecret;

                if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret))
                {
                    throw new InvalidOperationException(
                        "Google OAuth credentials are not configured. Please set Authentication:Google:ClientId and Authentication:Google:ClientSecret in appsettings.json or User Secrets.");
                }

                googleOptions.ClientId = googleClientId;
                googleOptions.ClientSecret = googleClientSecret;

                // Configure sign-in scheme to use External cookie
                googleOptions.SignInScheme = IdentityConstants.ExternalScheme;

                // Request user profile scopes
                googleOptions.Scope.Add("profile");
                googleOptions.Scope.Add("email");

                // Save tokens for retrieval
                googleOptions.SaveTokens = true;
            });

        return services;
    }
}
