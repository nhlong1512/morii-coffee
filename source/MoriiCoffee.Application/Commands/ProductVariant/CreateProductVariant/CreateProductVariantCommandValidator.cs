using FluentValidation;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;

namespace MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;

/// <summary>Validates the bulk create variants command.</summary>
public class CreateProductVariantCommandValidator : AbstractValidator<CreateProductVariantCommand>
{
    public CreateProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Variants)
            .NotEmpty().WithMessage("At least one variant is required.");

        RuleForEach(x => x.Variants).SetValidator(new CreateProductVariantDtoValidator());
    }
}

/// <summary>Validates a single variant DTO within a bulk create request.</summary>
public class CreateProductVariantDtoValidator : AbstractValidator<CreateProductVariantDto>
{
    public CreateProductVariantDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Variant name is required.")
            .MaximumLength(100).WithMessage("Variant name must not exceed 100 characters.");

        RuleFor(x => x.AdditionalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Additional price must be a non-negative value.");

        RuleFor(x => x.Sku)
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters.")
            .When(x => x.Sku != null);
    }
}
