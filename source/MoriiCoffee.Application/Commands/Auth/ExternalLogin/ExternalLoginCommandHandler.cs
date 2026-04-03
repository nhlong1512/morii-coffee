using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLogin;

/// <summary>
/// Handles external OAuth login initiation.
/// Prepares OAuth authentication properties with dynamic redirect URL.
/// Returns properties and provider for Challenge() call in controller.
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
    /// Builds redirect URL dynamically using current HttpContext.
    /// </summary>
    /// <param name="request">Command containing provider name and return URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response DTO with authentication properties and provider name.</returns>
    public Task<ExternalLoginResponseDto> Handle(
        ExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            // Build redirect URL dynamically using current request context
            HttpContext httpContext = _signInManager.Context;
            string domainName = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            string redirectUrl = $"{domainName}/api/v1/auth/external-auth-callback?returnUrl={request.ReturnUrl}";

            // Configure OAuth authentication properties with redirect URL
            AuthenticationProperties properties = _signInManager.ConfigureExternalAuthenticationProperties(
                request.Provider,
                redirectUrl);

            properties.AllowRefresh = true;

            return new ExternalLoginResponseDto
            {
                Properties = properties,
                Provider = request.Provider
            };
        }, cancellationToken);
    }
}
