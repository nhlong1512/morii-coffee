using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;

/// <summary>Command to add one or more variants (size/options) to an existing product.</summary>
public class CreateProductVariantCommand : ICommand<List<ProductVariantDto>>
{
    public CreateProductVariantCommand(Guid productId, List<CreateProductVariantDto> variants)
    {
        ProductId = productId;
        Variants = variants;
    }

    /// <summary>The product to which the variants will be added.</summary>
    public Guid ProductId { get; }

    /// <summary>List of variant definitions to create.</summary>
    public List<CreateProductVariantDto> Variants { get; }
}
