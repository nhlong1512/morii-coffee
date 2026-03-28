using MediatR;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLogin;

/// <summary>
/// Handles external OAuth login initiation.
/// Prepares OAuth authentication properties and generates redirect URL to external provider.
/// Uses ASP.NET Core Identity's SignInManager to configure OAuth challenge.
/// </summary>
public class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, ExternalLoginResponseDto>
{
    private readonly SignInManager<UserEntity> _signInManager;

    public ExternalLoginCommandHandler(SignInManager<UserEntity> signInManager)
    {
        _signInManager = signInManager;
    }

    /// <summary>
    /// Initiates OAuth flow by preparing authentication properties.
    /// Generates state parameter for CSRF protection and sets redirect URI.
    /// </summary>
    /// <param name="request">Command containing provider name and return URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response DTO with redirect URL to external provider.</returns>
    public Task<ExternalLoginResponseDto> Handle(
        ExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        // Configure OAuth authentication properties
        // - RedirectUri: Where provider should redirect after authentication
        // - State parameter: Auto-generated CSRF token by SignInManager
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            request.Provider,
            $"/api/v1/auth/external-auth-callback?returnUrl={Uri.EscapeDataString(request.ReturnUrl)}");

        // Note: In a web application, this would trigger a ChallengeResult redirect.
        // Since we're using MediatR, the controller will handle the actual redirect.
        // The properties contain the redirect URL to the external provider.
        var redirectUrl = properties.RedirectUri ?? string.Empty;

        return Task.FromResult(new ExternalLoginResponseDto
        {
            RedirectUrl = redirectUrl
        });
    }
}
