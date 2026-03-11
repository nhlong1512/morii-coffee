using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>Request body for the POST /auth/forgot-password endpoint.</summary>
public class ForgotPasswordDto
{
    [SwaggerSchema("Email address of the account to send the reset link to.")]
    public string Email { get; set; } = null!;
}
