using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Category.UpdateCategory;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Category;

public class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator = new();

    private static UpdateCategoryCommand ValidCommand(Action<UpdateCategoryDto>? customize = null)
    {
        var dto = new UpdateCategoryDto { Name = "Cold Brew", DisplayOrder = 1, IsActive = true };
        customize?.Invoke(dto);
        return new UpdateCategoryCommand(Guid.NewGuid(), dto);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsError()
    {
        var dto = new UpdateCategoryDto { Name = "Cold Brew", DisplayOrder = 1 };
        var cmd = new UpdateCategoryCommand(Guid.Empty, dto);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var cmd = ValidCommand(dto => dto.Name = "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds100Chars_ReturnsError()
    {
        var cmd = ValidCommand(dto => dto.Name = new string('a', 101));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeDisplayOrder_ReturnsError()
    {
        var cmd = ValidCommand(dto => dto.DisplayOrder = -1);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }
}
