using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.User;

/// <summary>Request body for PUT /users/me/change-password. Requires the current password for verification.</summary>
public class ChangePasswordDto
{
    [SwaggerSchema("Current password for verification.")]
    public string CurrentPassword { get; set; } = null!;

    [SwaggerSchema("New password to set.")]
    public string NewPassword { get; set; } = null!;
}
