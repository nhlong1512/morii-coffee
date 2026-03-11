using System.Text.Json.Serialization;
using MoriiCoffee.Domain.Shared.Enums.User;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.User;

/// <summary>Lightweight user representation used in paginated admin list responses.</summary>
public class UserSummaryDto
{
    [SwaggerSchema("Unique identifier of the user.")]
    public Guid Id { get; set; }

    [SwaggerSchema("Email address of the user.")]
    public string? Email { get; set; }

    [SwaggerSchema("Display username of the user.")]
    public string? UserName { get; set; }

    [SwaggerSchema("Full name of the user.")]
    public string? FullName { get; set; }

    [SwaggerSchema("Public URL of the user's avatar image.")]
    public string? AvatarUrl { get; set; }

    [SwaggerSchema("Current account status.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EUserStatus Status { get; set; }
}
