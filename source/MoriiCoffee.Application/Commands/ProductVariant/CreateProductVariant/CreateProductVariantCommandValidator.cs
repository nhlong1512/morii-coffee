using FluentValidation;

namespace MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;

public class CreateProductVariantCommandValidator : AbstractValidator<CreateProductVariantCommand>
{
    public CreateProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

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
