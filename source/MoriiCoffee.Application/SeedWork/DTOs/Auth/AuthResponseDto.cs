using MoriiCoffee.Application.SeedWork.DTOs.User;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>Response returned by sign-up, sign-in, and refresh-token endpoints. Contains both token pair and the user profile.</summary>
public class AuthResponseDto
{
    [SwaggerSchema("JWT access token. Include in Authorization: Bearer header for authenticated requests.")]
    public string AccessToken { get; set; } = null!;

    [SwaggerSchema("Opaque refresh token. Use to obtain a new access token when the current one expires.")]
    public string RefreshToken { get; set; } = null!;

    [SwaggerSchema("Authenticated user's profile.")]
    public UserDto User { get; set; } = null!;
}
