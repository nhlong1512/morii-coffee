using FluentAssertions;
using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.Shared.Enums.Product;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.ProductVariant;

public class CreateProductVariantCommandValidatorTests
{
    private readonly CreateProductVariantCommandValidator _validator = new();

    private static CreateProductVariantCommand ValidCommand() => new(
        productId: Guid.NewGuid(),
        variants:
        [
            new CreateProductVariantDto
            {
                Name = "Medium",
                Size = EProductSize.Medium,
                AdditionalPrice = 5_000m,
                IsDefault = true,
                IsAvailable = true
            }
        ]
    );

    [Fact]
    public void Validate_EmptyProductId_ReturnsError()
    {
        var cmd = new CreateProductVariantCommand(Guid.Empty,
        [
            new CreateProductVariantDto { Name = "M", Size = EProductSize.Medium }
        ]);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void Validate_EmptyVariantsList_ReturnsError()
    {
        var cmd = new CreateProductVariantCommand(Guid.NewGuid(), []);
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Variants);
    }

    [Fact]
    public void Validate_VariantWithEmptyName_ReturnsErrors()
    {
        var cmd = new CreateProductVariantCommand(Guid.NewGuid(),
        [
            new CreateProductVariantDto { Name = "", Size = EProductSize.Medium, AdditionalPrice = 0 }
        ]);
        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_VariantNameExceeds100Chars_ReturnsErrors()
    {
        var cmd = new CreateProductVariantCommand(Guid.NewGuid(),
        [
            new CreateProductVariantDto { Name = new string('a', 101), Size = EProductSize.Small, AdditionalPrice = 0 }
        ]);
        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeAdditionalPrice_ReturnsErrors()
    {
        var cmd = new CreateProductVariantCommand(Guid.NewGuid(),
        [
            new CreateProductVariantDto { Name = "Small", Size = EProductSize.Small, AdditionalPrice = -1m }
        ]);
        var result = _validator.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }
}
