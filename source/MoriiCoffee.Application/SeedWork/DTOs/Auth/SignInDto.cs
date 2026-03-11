using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>Request body for the POST /auth/signin endpoint. Identity can be email or username.</summary>
public class SignInDto
{
    [SwaggerSchema("Phone number or email registered for the account")]
    public string Identity { get; set; } = null!;
    
    [SwaggerSchema("Password for the account")]
    public string Password { get; set; } = null!;
}
