using FluentValidation.TestHelper;
using MoriiCoffee.Application.Commands.Product.CreateProduct;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Product;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand ValidCommand() => new(new CreateProductDto
    {
        Name = "Iced Latte",
        Slug = null,
        Description = null,
        BasePrice = 55_000m,
        CategoryIds = new List<Guid> { Guid.NewGuid() },
        IsFeatured = false,
        DisplayOrder = 0
    });

    // ── Name ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "",
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds200Chars_ReturnsError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = new string('a', 201),
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    // ── Slug ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_SlugWithUppercase_ReturnsError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            Slug = "Iced-Latte",
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_SlugWithSpaces_ReturnsError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            Slug = "iced latte",
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_SlugExceeds200Chars_ReturnsError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            Slug = new string('a', 201),
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validate_NullSlug_NoError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            Slug = null,
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        });
        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    // ── BasePrice ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_NegativeBasePrice_ReturnsError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            BasePrice = -1m,
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.BasePrice);
    }

    [Fact]
    public void Validate_ZeroBasePrice_NoError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            BasePrice = 0m,
            CategoryIds = new List<Guid> { Guid.NewGuid() }
        });
        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(x => x.BasePrice);
    }

    // ── CategoryIds ───────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyCategoryIds_ReturnsError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            BasePrice = 55_000m,
            CategoryIds = new List<Guid>()
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.CategoryIds);
    }

    // ── DisplayOrder ──────────────────────────────────────────────────

    [Fact]
    public void Validate_NegativeDisplayOrder_ReturnsError()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { Guid.NewGuid() },
            DisplayOrder = -1
        });
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }

    // ── Valid ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidCommand_NoErrors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidCommandWithSlug_NoErrors()
    {
        var cmd = new CreateProductCommand(new CreateProductDto
        {
            Name = "Iced Latte",
            Slug = "iced-latte",
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { Guid.NewGuid() },
            DisplayOrder = 1
        });
        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}
