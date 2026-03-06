using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;

/// <summary>Command to add a new variant (size/option) to an existing product.</summary>
public class CreateProductVariantCommand : ICommand<ProductVariantDto>
{
    public CreateProductVariantCommand(Guid productId, CreateProductVariantDto dto)
    {
        ProductId = productId;
        Name = dto.Name;
        Size = dto.Size;
        AdditionalPrice = dto.AdditionalPrice;
        Sku = dto.Sku;
        StockQuantity = dto.StockQuantity;
        IsDefault = dto.IsDefault;
    }

    public Guid ProductId { get; }
    public string Name { get; }
    public EProductSize Size { get; }
    public decimal AdditionalPrice { get; }
    public string? Sku { get; }
    public int StockQuantity { get; }
    public bool IsDefault { get; }
}
