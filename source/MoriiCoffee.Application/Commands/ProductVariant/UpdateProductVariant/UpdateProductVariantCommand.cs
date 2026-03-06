using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Application.Commands.ProductVariant.UpdateProductVariant;

/// <summary>Command to update an existing product variant.</summary>
public class UpdateProductVariantCommand : ICommand<ProductVariantDto>
{
    public UpdateProductVariantCommand(Guid id, UpdateProductVariantDto dto)
    {
        Id = id;
        Name = dto.Name;
        Size = dto.Size;
        AdditionalPrice = dto.AdditionalPrice;
        Sku = dto.Sku;
        StockQuantity = dto.StockQuantity;
        IsDefault = dto.IsDefault;
        IsAvailable = dto.IsAvailable;
    }

    public Guid Id { get; }
    public string Name { get; }
    public EProductSize Size { get; }
    public decimal AdditionalPrice { get; }
    public string? Sku { get; }
    public int StockQuantity { get; }
    public bool IsDefault { get; }
    public bool IsAvailable { get; }
}
