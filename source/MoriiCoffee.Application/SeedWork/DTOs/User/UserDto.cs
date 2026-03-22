using System.Text.Json.Serialization;
using MoriiCoffee.Domain.Shared.Enums.User;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.User;

/// <summary>Full user profile returned by profile queries and auth responses. Includes roles resolved via UserManager.</summary>
public class UserDto
{
    [SwaggerSchema("Unique identifier of the user.")]
    public Guid Id { get; set; }

    [SwaggerSchema("Email address of the user.")]
    public string? Email { get; set; }

    [SwaggerSchema("Phone number of the user.")]
    public string? PhoneNumber { get; set; }

    [SwaggerSchema("Display username of the user.")]
    public string? UserName { get; set; }

    [SwaggerSchema("Full name of the user.")]
    public string? FullName { get; set; }

    [SwaggerSchema("Date of birth of the user.")]
    public DateTime? Dob { get; set; }

    [SwaggerSchema("Gender of the user.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EGender? Gender { get; set; }

    [SwaggerSchema("Short biography of the user.")]
    public string? Bio { get; set; }

    [SwaggerSchema("Public URL of the user's avatar image.")]
    public string? AvatarUrl { get; set; }

    [SwaggerSchema("Current account status.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EUserStatus Status { get; set; }

    [SwaggerSchema("UTC timestamp when the account was created.")]
    public DateTime CreatedAt { get; set; }

    [SwaggerSchema("UTC timestamp of the last account update.")]
    public DateTime? UpdatedAt { get; set; }

    [SwaggerSchema("Roles assigned to the user.")]
    public IEnumerable<string> Roles { get; set; } = null!;
}
