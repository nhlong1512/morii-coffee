using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Category.CreateCategory;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Category;

public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator = new();

    private static CreateCategoryCommand ValidCommand() => new(new CreateCategoryDto
    {
        Name = "Espresso Drinks",
        Description = null,
        DisplayOrder = 0
    });

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var cmd = new CreateCategoryCommand(new CreateCategoryDto { Name = "", DisplayOrder = 0 });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds100Chars_ReturnsError()
    {
        var cmd = new CreateCategoryCommand(new CreateCategoryDto { Name = new string('a', 101), DisplayOrder = 0 });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeDisplayOrder_ReturnsError()
    {
        var cmd = new CreateCategoryCommand(new CreateCategoryDto { Name = "Coffee", DisplayOrder = -1 });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }
}
