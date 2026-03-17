using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Banner;

/// <summary>Request body for creating a new banner (multipart/form-data).</summary>
public class CreateBannerDto
{
    [SwaggerSchema("Headline text of the banner (required).")]
    public string Title { get; set; } = null!;

    [SwaggerSchema("Optional supporting description.")]
    public string? Description { get; set; }

    [SwaggerSchema("Banner image file to upload to MinIO. Optional — can be set later via update.")]
    public IFormFile? Image { get; set; }

    [SwaggerSchema("Sort position in the carousel. Must be unique.")]
    public int DisplayOrder { get; set; }

    [SwaggerSchema("Whether the banner is visible immediately after creation.")]
    public bool IsActive { get; set; } = true;
}
