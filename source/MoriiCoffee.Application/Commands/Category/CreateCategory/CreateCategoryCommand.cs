using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Category.CreateCategory;

/// <summary>Command to create a new product category.</summary>
public class CreateCategoryCommand : ICommand<CategoryDto>
{
    public CreateCategoryCommand(CreateCategoryDto dto)
    {
        Name = dto.Name;
        Description = dto.Description;
        IconUrl = dto.IconUrl;
        DisplayOrder = dto.DisplayOrder;
    }

    public string Name { get; }
    public string? Description { get; }
    public string? IconUrl { get; }
    public int DisplayOrder { get; }
}
