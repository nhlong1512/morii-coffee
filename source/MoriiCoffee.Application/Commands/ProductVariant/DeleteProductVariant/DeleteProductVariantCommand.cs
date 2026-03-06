using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.ProductVariant.DeleteProductVariant;

/// <summary>Command to soft-delete a product variant by ID.</summary>
public record DeleteProductVariantCommand(Guid Id) : ICommand<bool>;
