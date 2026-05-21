using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.BlogCategory.UpdateBlogCategory;

/// <summary>
/// Command to update an existing blog category.
/// </summary>
public class UpdateBlogCategoryCommand : ICommand<BlogCategoryDto>
{
    public UpdateBlogCategoryCommand(Guid id, UpdateBlogCategoryDto dto)
    {
        Id = id;
        Name = dto.Name;
        Slug = dto.Slug;
        Description = dto.Description;
        DisplayOrder = dto.DisplayOrder;
        IsActive = dto.IsActive;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string? Slug { get; }
    public string? Description { get; }
    public int DisplayOrder { get; }
    public bool IsActive { get; }
}
