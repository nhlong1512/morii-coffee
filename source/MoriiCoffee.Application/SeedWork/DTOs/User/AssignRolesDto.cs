using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.User;

/// <summary>Request body for PUT /users/{id}/roles. Replaces the user's full role set atomically.</summary>
public class AssignRolesDto
{
    [SwaggerSchema("Full list of roles to assign. Replaces all existing roles (e.g. [\"ADMIN\", \"CUSTOMER\"]).")]
    public List<string> Roles { get; set; } = new();
}
