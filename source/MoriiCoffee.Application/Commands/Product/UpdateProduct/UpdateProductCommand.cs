using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.Commands.Product.UpdateProduct;

/// <summary>Command to update an existing product's details.</summary>
public class UpdateProductCommand : ICommand<ProductDto>
{
    public UpdateProductCommand(Guid id, UpdateProductDto dto)
    {
        Id = id;
        Name = dto.Name;
        Slug = dto.Slug;
        Description = dto.Description;
        BasePrice = dto.BasePrice;
        CategoryIds = dto.CategoryIds;
        ThumbnailUrl = dto.ThumbnailUrl;
        Status = dto.Status;
        IsFeatured = dto.IsFeatured;
        DisplayOrder = dto.DisplayOrder;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string? Slug { get; }
    public string? Description { get; }
    public decimal BasePrice { get; }
    public List<Guid> CategoryIds { get; }
    public string? ThumbnailUrl { get; }
    public EProductStatus Status { get; }
    public bool IsFeatured { get; }
    public int DisplayOrder { get; }
}
