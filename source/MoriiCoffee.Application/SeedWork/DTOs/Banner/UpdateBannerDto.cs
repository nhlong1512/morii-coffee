using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Banner;

/// <summary>Request body for updating an existing banner (multipart/form-data).</summary>
public class UpdateBannerDto
{
    [SwaggerSchema("Updated headline text.")]
    public string Title { get; set; } = null!;

    [SwaggerSchema("Updated description. Send null to clear.")]
    public string? Description { get; set; }

    [SwaggerSchema("New banner image. If omitted, the existing image is kept.")]
    public IFormFile? Image { get; set; }

    [SwaggerSchema("Updated display order.")]
    public int DisplayOrder { get; set; }

    [SwaggerSchema("Updated active status.")]
    public bool IsActive { get; set; }
}
