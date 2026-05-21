using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.BlogCategory.CreateBlogCategory;

/// <summary>
/// Command to create a new blog category.
/// </summary>
public class CreateBlogCategoryCommand : ICommand<BlogCategoryDto>
{
    public CreateBlogCategoryCommand(CreateBlogCategoryDto dto)
    {
        Name = dto.Name;
        Slug = dto.Slug;
        Description = dto.Description;
        DisplayOrder = dto.DisplayOrder;
        IsActive = dto.IsActive;
    }

    public string Name { get; }
    public string? Slug { get; }
    public string? Description { get; }
    public int DisplayOrder { get; }
    public bool IsActive { get; }
}
