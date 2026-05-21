using FluentValidation;

namespace MoriiCoffee.Application.Commands.BlogCategory.CreateBlogCategory;

/// <summary>
/// Validates the create-blog-category payload.
/// </summary>
public class CreateBlogCategoryCommandValidator : AbstractValidator<CreateBlogCategoryCommand>
{
    public CreateBlogCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Blog category name is required.")
            .MaximumLength(100).WithMessage("Blog category name must not exceed 100 characters.");

        RuleFor(x => x.Slug)
            .MaximumLength(150).WithMessage("Slug must not exceed 150 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be a non-negative number.");
    }
}
