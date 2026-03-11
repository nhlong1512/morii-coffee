using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>Request body for the POST /auth/refresh-token endpoint.</summary>
public class RefreshTokenDto
{
    [SwaggerSchema("Refresh token received after login.")]
    public string RefreshToken { get; set; } = null!;
}
