using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Product.DeleteProduct;

/// <summary>Command to soft-delete a product by ID.</summary>
public record DeleteProductCommand(Guid Id) : ICommand<bool>;
