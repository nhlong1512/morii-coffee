using FluentValidation;

namespace MoriiCoffee.Application.Commands.BlogCategory.ReorderBlogCategories;

/// <summary>
/// Validates the batch reorder payload for blog categories.
/// </summary>
public class ReorderBlogCategoriesCommandValidator : AbstractValidator<ReorderBlogCategoriesCommand>
{
    public ReorderBlogCategoriesCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one blog category reorder item is required.");

        RuleFor(x => x.Items)
            .Must(items => items.Select(item => item.Id).Distinct().Count() == items.Count)
            .WithMessage("Blog category reorder items must not contain duplicate IDs.")
            .When(x => x.Items.Count > 0);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Blog category ID is required.");

            item.RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order must be a non-negative number.");
        });
    }
}
