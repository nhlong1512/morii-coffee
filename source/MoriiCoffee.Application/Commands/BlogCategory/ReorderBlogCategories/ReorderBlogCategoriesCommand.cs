using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.BlogCategory.ReorderBlogCategories;

/// <summary>
/// Command to reorder multiple blog categories in a single request.
/// </summary>
public class ReorderBlogCategoriesCommand : ICommand<bool>
{
    public ReorderBlogCategoriesCommand(ReorderBlogCategoriesDto dto)
    {
        Items = dto.Items;
    }

    public List<ReorderBlogCategoriesItemDto> Items { get; }
}
