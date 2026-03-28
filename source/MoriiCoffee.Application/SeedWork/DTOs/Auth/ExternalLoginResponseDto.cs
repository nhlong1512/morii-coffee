namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>
/// Response DTO for external OAuth login initiation.
/// Contains the redirect URL to the external provider's authentication page.
/// </summary>
public record ExternalLoginResponseDto
{
    /// <summary>
    /// URL to redirect user to for OAuth authentication.
    /// Typically points to Google's OAuth consent screen with appropriate parameters
    /// (client_id, redirect_uri, scope, state for CSRF protection).
    /// </summary>
    public string RedirectUrl { get; init; } = string.Empty;
}
