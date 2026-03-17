using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Banner;

/// <summary>Response DTO returned by all banner endpoints.</summary>
public class BannerDto
{
    [SwaggerSchema("Unique identifier of the banner.")]
    public Guid Id { get; set; }

    [SwaggerSchema("Headline text of the banner.")]
    public string Title { get; set; } = null!;

    [SwaggerSchema("Optional supporting description.")]
    public string? Description { get; set; }

    [SwaggerSchema("Public URL of the banner image.")]
    public string? ImageUrl { get; set; }

    [SwaggerSchema("Whether the banner is currently active/visible.")]
    public bool IsActive { get; set; }

    [SwaggerSchema("Sort position in the banner carousel.")]
    public int DisplayOrder { get; set; }

    [SwaggerSchema("UTC timestamp when the banner was created.")]
    public DateTime CreatedAt { get; set; }

    [SwaggerSchema("UTC timestamp of the last update, if any.")]
    public DateTime? UpdatedAt { get; set; }
}
