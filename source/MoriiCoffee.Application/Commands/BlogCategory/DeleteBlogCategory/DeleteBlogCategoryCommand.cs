using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.BlogCategory.DeleteBlogCategory;

/// <summary>
/// Command to delete a blog category when it is no longer in use.
/// </summary>
public record DeleteBlogCategoryCommand(Guid Id) : ICommand<bool>;
