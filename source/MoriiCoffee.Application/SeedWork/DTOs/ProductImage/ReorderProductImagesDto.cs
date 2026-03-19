namespace MoriiCoffee.Application.SeedWork.DTOs.ProductImage;

/// <summary>
/// Payload for reordering a product's gallery images.
/// The provided list defines the new display order — images are assigned
/// <see cref="Domain.Aggregates.ProductAggregate.Entities.ProductImage.DisplayOrder"/> values
/// based on their position in the list (index 0 → order 1, index 1 → order 2, …).
/// </summary>
public class ReorderProductImagesDto
{
    /// <summary>
    /// IDs of all images for the product in the desired display order.
    /// Every image belonging to the product must be included.
    /// </summary>
    public List<Guid> ImageIds { get; set; } = new();
}
