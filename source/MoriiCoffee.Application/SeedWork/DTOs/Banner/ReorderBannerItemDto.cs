using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Banner;

/// <summary>Single item in a bulk reorder request, mapping a banner to its new position.</summary>
public class ReorderBannerItemDto
{
    [SwaggerSchema("ID of the banner to reorder.")]
    public Guid Id { get; set; }

    [SwaggerSchema("New display order position.")]
    public int DisplayOrder { get; set; }
}
