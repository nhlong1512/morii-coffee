using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>Request body for the POST /auth/reset-password endpoint.</summary>
public class ResetPasswordDto
{
    [SwaggerSchema("Email address of the account.")]
    public string Email { get; set; } = null!;

    [SwaggerSchema("Reset token received via email.")]
    public string Token { get; set; } = null!;

    [SwaggerSchema("New password to set.")]
    public string NewPassword { get; set; } = null!;
}
