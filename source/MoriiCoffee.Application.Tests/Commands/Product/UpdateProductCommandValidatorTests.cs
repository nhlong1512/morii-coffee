using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Product.UpdateProduct;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.Shared.Enums.Product;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Product;

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    private static UpdateProductCommand ValidCommand(Action<UpdateProductDto>? customize = null)
    {
        var dto = new UpdateProductDto
        {
            Name = "Iced Latte",
            BasePrice = 55_000m,
            CategoryIds = [Guid.NewGuid()],
            Status = EProductStatus.Active,
            DisplayOrder = 0
        };
        customize?.Invoke(dto);
        return new UpdateProductCommand(Guid.NewGuid(), dto);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsError()
    {
        var dto = new UpdateProductDto { Name = "Iced Latte", BasePrice = 55_000m, CategoryIds = [Guid.NewGuid()] };
        var cmd = new UpdateProductCommand(Guid.Empty, dto);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var cmd = ValidCommand(dto => dto.Name = "");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds200Chars_ReturnsError()
    {
        var cmd = ValidCommand(dto => dto.Name = new string('a', 201));
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeBasePrice_ReturnsError()
    {
        var cmd = ValidCommand(dto => dto.BasePrice = -1m);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.BasePrice);
    }

    [Fact]
    public void Validate_EmptyCategoryIds_ReturnsError()
    {
        var cmd = ValidCommand(dto => dto.CategoryIds = []);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.CategoryIds);
    }

    [Fact]
    public void Validate_SlugWithUppercase_ReturnsError()
    {
        var cmd = ValidCommand(dto => dto.Slug = "Iced-Latte");
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }
}
