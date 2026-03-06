using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Category.UpdateCategory;

/// <summary>Command to update an existing product category.</summary>
public class UpdateCategoryCommand : ICommand<CategoryDto>
{
    public UpdateCategoryCommand(Guid id, UpdateCategoryDto dto)
    {
        Id = id;
        Name = dto.Name;
        Description = dto.Description;
        IconUrl = dto.IconUrl;
        DisplayOrder = dto.DisplayOrder;
        IsActive = dto.IsActive;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public string? IconUrl { get; }
    public int DisplayOrder { get; }
    public bool IsActive { get; }
}
