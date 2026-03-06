using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Category.DeleteCategory;

/// <summary>Command to soft-delete a product category by ID.</summary>
public record DeleteCategoryCommand(Guid Id) : ICommand<bool>;
