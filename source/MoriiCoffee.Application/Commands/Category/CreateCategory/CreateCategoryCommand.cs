using Microsoft.AspNetCore.Http;
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
        Icon = dto.Icon;
        DisplayOrder = dto.DisplayOrder;
    }

    public string Name { get; }
    public string? Description { get; }

    /// <summary>Optional icon file to upload to MinIO on creation.</summary>
    public IFormFile? Icon { get; }

    public int DisplayOrder { get; }
}
