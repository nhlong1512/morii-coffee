using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Product.CreateProduct;

/// <summary>Command to create a new product in the coffee shop catalog.</summary>
public class CreateProductCommand : ICommand<ProductDto>
{
    public CreateProductCommand(CreateProductDto dto)
    {
        Name = dto.Name;
        Slug = dto.Slug;
        Description = dto.Description;
        BasePrice = dto.BasePrice;
        CategoryIds = dto.CategoryIds;
        ThumbnailUrl = dto.ThumbnailUrl;
        IsFeatured = dto.IsFeatured;
        DisplayOrder = dto.DisplayOrder;
    }

    public string Name { get; }
    public string? Slug { get; }
    public string? Description { get; }
    public decimal BasePrice { get; }
    public List<Guid> CategoryIds { get; }
    public string? ThumbnailUrl { get; }
    public bool IsFeatured { get; }
    public int DisplayOrder { get; }
}
