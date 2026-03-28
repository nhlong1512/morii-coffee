using Microsoft.AspNetCore.Authentication;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>
/// Response DTO for external OAuth login initiation.
/// Contains authentication properties and provider name for Challenge() call.
/// </summary>
public record ExternalLoginResponseDto
{
    /// <summary>Authentication properties including redirect URI and state for OAuth flow.</summary>
    [SwaggerSchema("Authentication properties for the external login")]
    public AuthenticationProperties Properties { get; set; } = new AuthenticationProperties();

    /// <summary>Name of the external authentication provider (e.g., "Google").</summary>
    [SwaggerSchema("Name of the external authentication provider (e.g., Google, Facebook)")]
    public string Provider { get; set; } = "Google";
}
