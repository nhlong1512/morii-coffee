using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>
/// Request body for the POST /auth/reset-password endpoint.
/// The <c>email</c> field is a temporary backward-compatibility field and is ignored when a valid ticket is present.
/// </summary>
public class ResetPasswordDto
{
    [SwaggerSchema("Optional — retained for temporary backward compatibility. Ignored when a valid ticket is provided.")]
    public string? Email { get; set; }

    [SwaggerSchema("Opaque one-time reset ticket received in the password reset email link.")]
    public string Ticket { get; set; } = null!;

    [SwaggerSchema("New password to set.")]
    public string NewPassword { get; set; } = null!;
}
