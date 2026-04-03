using MediatR;
using MoriiCoffee.Application.SeedWork.DTOs.Auth;

namespace MoriiCoffee.Application.Commands.Auth.ExternalLoginCallback;

/// <summary>
/// Command to process OAuth callback from external provider (Google).
/// The authentication middleware handles OAuth token exchange automatically.
/// This handler retrieves external login info from the External cookie,
/// creates or links user account, and generates JWT tokens.
/// </summary>
public record ExternalLoginCallbackCommand : IRequest<AuthResponseDto>
{
    /// <summary>
    /// Initializes a new instance of ExternalLoginCallbackCommand.
    /// </summary>
    /// <param name="returnUrl">URL to redirect to after authentication completes.</param>
    public ExternalLoginCallbackCommand(string returnUrl)
    {
        ReturnUrl = returnUrl;
    }

    /// <summary>
    /// URL to redirect to after successful authentication.
    /// Originally provided in external-login request.
    /// </summary>
    public string ReturnUrl { get; init; } = "/";
}
