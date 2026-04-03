using MediatR;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLogin;

/// <summary>
/// Command to initiate external OAuth authentication flow.
/// Prepares OAuth challenge and redirects user to external provider (Google) for authentication.
/// After successful authentication, provider will redirect back to external-auth-callback endpoint.
/// </summary>
public record ExternalLoginCommand : IRequest<ExternalLoginResponseDto>
{
    /// <summary>
    /// OAuth provider name. Currently only "Google" is supported.
    /// Provider name is case-insensitive (Google, google, GOOGLE all accepted).
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// URL to redirect to after successful authentication.
    /// Defaults to "/" (home page) if not specified.
    /// Must be a valid relative or absolute URL to prevent open redirect attacks.
    /// </summary>
    public string ReturnUrl { get; init; } = "/";
}
