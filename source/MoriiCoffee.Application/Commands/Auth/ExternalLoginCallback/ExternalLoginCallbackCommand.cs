using MediatR;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;

/// <summary>
/// Command to process OAuth callback from external provider (Google).
/// Receives authorization code, exchanges it for user profile information,
/// creates or links user account, generates JWT tokens, and returns them in a secure cookie.
/// </summary>
public record ExternalLoginCallbackCommand : IRequest<AuthResponseDto>
{
    /// <summary>
    /// Authorization code from OAuth provider.
    /// Single-use code that can be exchanged for user profile information.
    /// Expires in ~10 seconds, provided by Google in the callback query parameters.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// CSRF protection token.
    /// Must match the state value stored in correlation cookie during OAuth initiation.
    /// Validated by ASP.NET Core Identity middleware automatically.
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// URL to redirect to after successful authentication.
    /// Originally provided in external-login request, passed through OAuth flow.
    /// Defaults to "/" (home page) if not specified.
    /// </summary>
    public string ReturnUrl { get; init; } = "/";

    /// <summary>
    /// Error code from OAuth provider if user denied permission.
    /// Example: "access_denied" when user clicks Cancel on consent screen.
    /// Null if authentication was successful.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Human-readable error description from OAuth provider.
    /// Example: "User denied permission to access their account."
    /// Null if authentication was successful.
    /// </summary>
    public string? ErrorDescription { get; init; }
}
