using FluentValidation;

namespace MoriiCoffee.Application.Commands.Product.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase alphanumeric with hyphens (e.g., 'iced-latte').")
            .When(x => !string.IsNullOrEmpty(x.Slug));

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Base price must be a non-negative value.");

        RuleFor(x => x.CategoryIds)
            .NotEmpty().WithMessage("At least one category is required.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be a non-negative number.");
    }
}
