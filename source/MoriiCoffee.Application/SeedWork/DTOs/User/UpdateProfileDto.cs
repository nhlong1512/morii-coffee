using System.Text.Json.Serialization;
using MoriiCoffee.Domain.Shared.Enums.User;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.User;

/// <summary>Request body for PUT /users/me/profile. All fields are optional; null values are applied as-is.</summary>
public class UpdateProfileDto
{
    [SwaggerSchema("Full name of the user.")]
    public string? FullName { get; set; }

    [SwaggerSchema("Date of birth of the user.")]
    public DateTime? Dob { get; set; }

    [SwaggerSchema("Gender of the user.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EGender? Gender { get; set; }

    [SwaggerSchema("Short biography of the user.")]
    public string? Bio { get; set; }
}
