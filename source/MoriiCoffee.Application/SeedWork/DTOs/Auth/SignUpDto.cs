using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>Request body for the POST /auth/signup endpoint.</summary>
public class SignUpDto
{
    [SwaggerSchema("Email address for the new account.")]
    public string Email { get; set; } = null!;

    [SwaggerSchema("Phone number for the new account.")]
    public string PhoneNumber { get; set; } = null!;

    [SwaggerSchema("Password for the new account.")]
    public string Password { get; set; } = null!;

    [SwaggerSchema("Optional display name.")]
    public string? UserName { get; set; }
}
